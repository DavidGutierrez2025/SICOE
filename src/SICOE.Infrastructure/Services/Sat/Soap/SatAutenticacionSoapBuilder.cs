using System.Text;
using System.Xml;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;

namespace SICOE.Infrastructure.Services.Sat.Soap;

/// <summary>
/// Constructor de mensajes SOAP para autenticación del SAT
/// Genera el formato WS-Security correcto según documentación del SAT
/// </summary>
public class SatAutenticacionSoapBuilder
{
    private readonly ILogger<SatAutenticacionSoapBuilder> _logger;
    private readonly XmlSignatureService _signatureService;

    public SatAutenticacionSoapBuilder(ILogger<SatAutenticacionSoapBuilder> logger, XmlSignatureService signatureService)
    {
        _logger = logger;
        _signatureService = signatureService;
    }

    /// <summary>
    /// Construye mensaje SOAP para autenticación con WS-Security según formato del SAT
    /// </summary>
    public string BuildAutenticacionSoap(X509Certificate2 certificate)
    {
        try
        {
            var doc = new XmlDocument();
            doc.PreserveWhitespace = false;

            // Crear envelope SOAP
            var envelope = doc.CreateElement("s", "Envelope", "http://schemas.xmlsoap.org/soap/envelope/");
            envelope.SetAttribute("xmlns:u", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd");
            doc.AppendChild(envelope);

            // Crear Header
            var header = doc.CreateElement("s", "Header", "http://schemas.xmlsoap.org/soap/envelope/");
            envelope.AppendChild(header);

            // Crear Security element
            var security = doc.CreateElement("o", "Security", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd");
            security.SetAttribute("s:mustUnderstand", "1");
            header.AppendChild(security);

            // Crear Timestamp
            var timestampId = "_0";
            var timestamp = doc.CreateElement("u", "Timestamp", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd");
            timestamp.SetAttribute("u:Id", timestampId);
            
            var created = DateTime.UtcNow;
            var expires = created.AddMinutes(5); // El SAT espera 5 minutos de validez
            
            var createdElement = doc.CreateElement("u", "Created", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd");
            // El SAT requiere formato yyyy-MM-ddTHH:mm:ssZ (SIN milisegundos)
            createdElement.InnerText = created.ToString("yyyy-MM-ddTHH:mm:ssZ");
            timestamp.AppendChild(createdElement);
            
            var expiresElement = doc.CreateElement("u", "Expires", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd");
            // El SAT requiere formato yyyy-MM-ddTHH:mm:ssZ (SIN milisegundos)
            expiresElement.InnerText = expires.ToString("yyyy-MM-ddTHH:mm:ssZ");
            timestamp.AppendChild(expiresElement);
            
            security.AppendChild(timestamp);

            // Crear BinarySecurityToken
            // IMPORTANTE: Debe contener el certificado completo en formato Base64
            if (certificate.RawData == null || certificate.RawData.Length == 0)
            {
                throw new CryptographicException("El certificado no contiene datos válidos (RawData está vacío)");
            }

            var binaryTokenId = $"uuid-{Guid.NewGuid()}-4";
            var binaryToken = doc.CreateElement("o", "BinarySecurityToken", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd");
            binaryToken.SetAttribute("u:Id", binaryTokenId);
            binaryToken.SetAttribute("ValueType", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-x509-token-profile-1.0#X509v3");
            binaryToken.SetAttribute("EncodingType", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-soap-message-security-1.0#Base64Binary");
            var certBase64 = Convert.ToBase64String(certificate.RawData);
            binaryToken.InnerText = certBase64;
            security.AppendChild(binaryToken);
            
            _logger.LogDebug("BinarySecurityToken creado. Tamaño del certificado en Base64: {Length} caracteres", certBase64.Length);

            // Firmar el Timestamp usando WS-Security (Exclusive C14N, referencia al Timestamp)
            SignTimestampForAuthentication(security, timestamp, timestampId, binaryTokenId, certificate);

            // Crear Body
            var body = doc.CreateElement("s", "Body", "http://schemas.xmlsoap.org/soap/envelope/");
            envelope.AppendChild(body);

            // Crear elemento Autentica
            var autentica = doc.CreateElement("Autentica", "http://DescargaMasivaTerceros.gob.mx");
            body.AppendChild(autentica);

            var xmlResult = doc.OuterXml;
            
            // Log del XML generado
            _logger.LogInformation("=== XML SOAP COMPLETO PARA Autenticacion ===");
            _logger.LogInformation("XML completo:\n{Xml}", xmlResult);
            
            // Extraer y log del elemento Signature (sello) si existe
            var signatureNodes = security.GetElementsByTagName("Signature", "http://www.w3.org/2000/09/xmldsig#");
            if (signatureNodes.Count > 0 && signatureNodes[0] is XmlElement signatureElement)
            {
                var signatureXml = signatureElement.OuterXml;
                _logger.LogInformation("=== ELEMENTO SELLO (Signature) EN AUTENTICACION ===");
                _logger.LogInformation("Sello XML:\n{SignatureXml}", signatureXml);
            }

            return xmlResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al construir mensaje SOAP para autenticación");
            throw;
        }
    }

    /// <summary>
    /// Firma el Timestamp para autenticación según formato WS-Security del SAT
    /// IMPORTANTE: Usa Exclusive C14N y referencia al Timestamp, no al documento completo
    /// </summary>
    private void SignTimestampForAuthentication(
        XmlElement securityElement,
        XmlElement timestampElement,
        string timestampId,
        string binaryTokenId,
        X509Certificate2 certificate)
    {
        try
        {
            // Validar que el certificado tenga clave privada accesible
            if (!certificate.HasPrivateKey)
            {
                throw new CryptographicException("El certificado no contiene una clave privada accesible");
            }

            var doc = securityElement.OwnerDocument;

            // IMPORTANTE: Para que SignedXml encuentre el elemento por ID u:Id,
            // usamos la clase personalizada WssSignedXml
            var rsaPrivateKey = certificate.GetRSAPrivateKey();
            if (rsaPrivateKey == null)
            {
                throw new CryptographicException("No se pudo obtener la clave privada RSA del certificado. Verifique que el certificado tenga la clave privada correctamente configurada.");
            }

            _logger.LogDebug("Clave privada RSA obtenida exitosamente del certificado. Tamaño de clave: {KeySize} bits", rsaPrivateKey.KeySize);

            var signedXml = new WssSignedXml(doc)
            {
                SigningKey = rsaPrivateKey
            };

            // IMPORTANTE: Definir cómo buscar IDs u:Id (del namespace utility)
            // Agregamos el objeto SignedXml al contexto para que resuelva la referencia u:Id
            // Sin esto, signedXml.ComputeSignature() fallará al no encontrar el "#_0"
            
            if (signedXml.SignedInfo == null)
                throw new InvalidOperationException("SignedInfo no puede ser null en SignedXml");
            
            // Configurar método de canonicalización exclusivo
            signedXml.SignedInfo.CanonicalizationMethod = SignedXml.XmlDsigExcC14NTransformUrl;

            // Configurar método de firma (RSA-SHA1)
            signedXml.SignedInfo.SignatureMethod = SignedXml.XmlDsigRSASHA1Url;

            // IMPORTANTE: La referencia apunta al Timestamp (no al documento completo)
            var reference = new Reference
            {
                Uri = $"#{timestampId}", // Referencia al u:Id del Timestamp
            };

            // IMPORTANTE: Usar Exclusive C14N Transform (no EnvelopedSignature)
            reference.AddTransform(new XmlDsigExcC14NTransform());

            // Configurar método de digest (SHA1)
            reference.DigestMethod = SignedXml.XmlDsigSHA1Url;

            // Agregar referencia
            signedXml.AddReference(reference);

            // IMPORTANTE: El KeyInfo debe referenciar al BinarySecurityToken usando SecurityTokenReference
            // Necesitamos crear el KeyInfo manualmente porque SignedXml no soporta SecurityTokenReference directamente
            
            // Calcular firma primero (sin KeyInfo)
            signedXml.ComputeSignature();
            
            // Obtener el elemento Signature
            var signatureElement = signedXml.GetXml();
            
            // Remover el KeyInfo generado automáticamente (si existe)
            var keyInfoNodes = signatureElement.GetElementsByTagName("KeyInfo", "http://www.w3.org/2000/09/xmldsig#");
            foreach (XmlNode node in keyInfoNodes)
            {
                node.ParentNode?.RemoveChild(node);
            }
            
            // Crear KeyInfo con SecurityTokenReference manualmente
            var keyInfo = doc.CreateElement("KeyInfo", "http://www.w3.org/2000/09/xmldsig#");
            var securityTokenRef = doc.CreateElement("o", "SecurityTokenReference", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd");
            var referenceElement = doc.CreateElement("o", "Reference", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd");
            referenceElement.SetAttribute("ValueType", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-x509-token-profile-1.0#X509v3");
            referenceElement.SetAttribute("URI", $"#{binaryTokenId}");
            securityTokenRef.AppendChild(referenceElement);
            keyInfo.AppendChild(securityTokenRef);
            
            // Insertar KeyInfo después de SignatureValue
            var signatureValueNodes = signatureElement.GetElementsByTagName("SignatureValue", "http://www.w3.org/2000/09/xmldsig#");
            if (signatureValueNodes.Count > 0)
            {
                signatureElement.InsertAfter(keyInfo, signatureValueNodes[0]);
            }
            else
            {
                // Si no hay SignatureValue, agregar al final del Signature
                signatureElement.AppendChild(keyInfo);
            }

            // Agregar el elemento Signature al Security element
            securityElement.AppendChild(signatureElement);
            
            _logger.LogDebug("Timestamp firmado exitosamente para autenticación. TimestampId: {TimestampId}", timestampId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al firmar Timestamp para autenticación");
            throw new InvalidOperationException("Error al generar la firma digital para autenticación", ex);
        }
    }

    /// <summary>
    /// Clase auxiliar para resolver IDs en namespaces de WS-Security
    /// </summary>
    private class WssSignedXml : SignedXml
    {
        public WssSignedXml(XmlDocument doc) : base(doc) { }

        public override XmlElement? GetIdElement(XmlDocument? doc, string id)
        {
            if (doc == null) return null;
            
            // Intentar encontrar el elemento por u:Id (namespace WS-Security Utility)
            var nsManager = new XmlNamespaceManager(doc.NameTable);
            nsManager.AddNamespace("u", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd");
            
            var element = doc.SelectSingleNode("//*[@u:Id='" + id + "']", nsManager) as XmlElement;
            if (element != null) return element;

            // Fallback al comportamiento estándar
            return base.GetIdElement(doc, id);
        }
    }
}

