using System;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Xml;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography.X509Certificates;

namespace SICOE.Infrastructure.Services.Sat.Soap;

/// <summary>
/// Servicio para generar firmas digitales XML según especificaciones del SAT
/// - Firma el elemento solicitud con sello dentro del mismo elemento
/// - Usa RSA-SHA1 y SHA1 con canonicalización C14N normal (NO Exclusive C14N)
/// - Usa enveloped signature (URI vacío) según especificación SAT
/// - KeyInfo incluye X509IssuerSerial y X509Certificate
/// NOTA: Para autenticación se usa Exclusive C14N, pero para solicitudes de descarga se usa C14N normal
/// </summary>
public class XmlSignatureService
{
    private readonly ILogger<XmlSignatureService> _logger;

    public XmlSignatureService(ILogger<XmlSignatureService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Firma el elemento solicitud para solicitudes de descarga del SAT.
    /// El sello se inserta DENTRO del elemento solicitud según especificaciones del SAT.
    /// Usa canonicalización C14N normal (http://www.w3.org/TR/2001/REC-xml-c14n-20010315)
    /// y SOLO EnvelopedSignature como transform (NO Exclusive C14N).
    /// NOTA: Esto es diferente de la autenticación que usa Exclusive C14N.
    /// </summary>
    public void SignXmlElement(XmlElement elementToSign, X509Certificate2 certificate)
    {
        try
        {
            if (elementToSign is null) throw new ArgumentNullException(nameof(elementToSign));
            if (certificate is null) throw new ArgumentNullException(nameof(certificate));
            if (!certificate.HasPrivateKey) throw new CryptographicException("El certificado no contiene clave privada accesible.");

            var doc = elementToSign.OwnerDocument ?? throw new InvalidOperationException("El elemento debe pertenecer a un XmlDocument.");

            // IMPORTANTE: Para enveloped signature con URI vacío, el SignedXml debe configurarse
            // para firmar el elemento padre del elemento donde se inserta la firma.
            // Como vamos a insertar la firma dentro de elementToSign, necesitamos que el elemento
            // padre de elementToSign sea el que se firme. Sin embargo, según el SAT, el sello
            // debe estar dentro del elemento solicitud y debe firmar el elemento solicitud mismo.
            // Para lograr esto, usamos un enfoque especial: calculamos la firma ANTES de insertarla,
            // y el transform EnvelopedSignature excluirá automáticamente el elemento Signature.

            // Preparar SignedXml para firmar el elemento (enveloped signature)
            var signedXml = new SignedXml(doc)
            {
                SigningKey = certificate.GetRSAPrivateKey()
            };

            // CRÍTICO: Canonicalización según especificación SAT para solicitudes de descarga:
            // El SAT requiere C14N normal (http://www.w3.org/TR/2001/REC-xml-c14n-20010315)
            // NO usar Exclusive C14N para solicitudes de descarga.
            if (signedXml.SignedInfo == null)
                throw new InvalidOperationException("SignedInfo no puede ser null en SignedXml");
            
            signedXml.SignedInfo.CanonicalizationMethod = SignedXml.XmlDsigC14NTransformUrl; // C14N Normal
            signedXml.SignedInfo.SignatureMethod = SignedXml.XmlDsigRSASHA1Url;

            // Referencia con URI vacío para enveloped signature
            // IMPORTANTE: Con URI vacío, el SignedXml firma el elemento padre del elemento donde
            // se inserta la firma. Como vamos a insertar la firma dentro de elementToSign,
            // el elemento que se firma es elementToSign mismo (sin incluir el elemento Signature).
            var reference = new Reference
            {
                Uri = "", // URI vacío = enveloped signature
                DigestMethod = SignedXml.XmlDsigSHA1Url
            };
            
            // CRÍTICO: Según documentación oficial del SAT, para solicitudes de descarga masiva
            // se usa SOLO EnvelopedSignature como transform, NO Exclusive C14N
            // Esto es diferente de la autenticación que sí usa Exclusive C14N
            reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
            // NO agregar XmlDsigExcC14NTransform aquí para solicitudes de descarga
            signedXml.AddReference(reference);

            // KeyInfo con X509Data según especificación SAT
            // El SAT requiere X509IssuerSerial y X509Certificate
            var keyInfoData = new KeyInfo();
            var x509Data = new KeyInfoX509Data(certificate);
            // Agregar IssuerSerial (requerido por SAT)
            x509Data.AddIssuerSerial(certificate.IssuerName.Name, certificate.SerialNumber);
            // Agregar certificado
            x509Data.AddCertificate(certificate);
            keyInfoData.AddClause(x509Data);
            signedXml.KeyInfo = keyInfoData;

            // Calcular la firma (se calcula ANTES de insertarla en el documento)
            signedXml.ComputeSignature();

            // Obtener el XML de la firma
            var xmlDigitalSignature = signedXml.GetXml();

            // Importar e insertar la firma DENTRO del elemento solicitud
            var importedSig = doc.ImportNode(xmlDigitalSignature, true);
            elementToSign.AppendChild(importedSig);

            // Log del sello generado
            _logger.LogInformation("=== ELEMENTO SELLO (Signature) GENERADO ===");
            _logger.LogInformation("Sello XML:\n{SignatureXml}", xmlDigitalSignature.OuterXml);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error firmando elemento XML para solicitud de descarga SAT");
            throw;
        }
    }

    /// <summary>
    /// Ordena atributos de un elemento alfabéticamente por nombre local.
    /// Úsalo antes de firmar el elemento de solicitud (p.ej. &lt;solicitud ... /&gt;).
    /// CRÍTICO: El SAT requiere que los atributos estén en orden alfabético estricto antes de firmar.
    /// </summary>
    public static void OrderAttributesAlphabetically(XmlElement element)
    {
        var doc = element.OwnerDocument!;
        var attrs = element.Attributes.Cast<XmlAttribute>().ToList();
        if (attrs.Count <= 1) return;

        // Guardar valores y eliminarlos
        var tuples = attrs.Select(a => (Name: a.Name, LocalName: a.LocalName, Value: a.Value, NamespaceUri: a.NamespaceURI)).ToList();
        
        // Log del orden antes de ordenar
        var ordenAntes = string.Join(", ", tuples.Select(t => t.LocalName));
        
        foreach (var a in attrs) element.RemoveAttributeNode(a);

        // Definir el orden específico requerido por el SAT (según manual V1.5 VF Pág 23)
        // NOTA: El SAT lo llama "alfabético", pero usa este orden predefinido 1-10.
        // Seguir este orden EXACTO para evitar el error 302/303 (Sello Mal Formado).
        var satOrder = new List<string> {
            "Folio",              // Para SolicitaDescargaFolio (Pág 30)
            "Complemento",        // 1
            "EstadoComprobante",  // 2
            "FechaInicial",       // 3
            "FechaFinal",         // 4
            "RfcEmisor",          // 5
            "RfcSolicitante",     // 6
            "TipoComprobante",    // 7
            "TipoSolicitud",      // 8
            "RfcReceptor",        // 9
            "RfcACuentaTerceros", // 10
            "IdSolicitud",        // Para Verificación
            "idPaquete",          // Para Descarga
            "IdPaquete"           // Para Descarga (variante)
        };

        // Ordenar primero por el índice en satOrder, si no existe en la lista, usar el nombre
        var ordenados = tuples.OrderBy(t => {
            int index = satOrder.IndexOf(t.LocalName);
            return index >= 0 ? index : 999;
        }).ThenBy(t => t.LocalName, StringComparer.Ordinal).ToList();
        
        // Log del orden después de ordenar
        var ordenDespues = string.Join(", ", ordenados.Select(t => t.LocalName));
        
        foreach (var t in ordenados)
        {
            var newAttr = doc.CreateAttribute(t.Name, t.NamespaceUri);
            newAttr.Value = t.Value;
            element.Attributes.Append(newAttr);
        }
        
        // Log para verificación (solo en Debug para no saturar logs)
        // Nota: Este log se puede activar temporalmente para diagnóstico
        // System.Diagnostics.Debug.WriteLine($"Atributos ordenados. Antes: {ordenAntes}, Después: {ordenDespues}");
    }
}

