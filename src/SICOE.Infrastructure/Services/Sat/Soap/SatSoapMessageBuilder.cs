using System.Text;
using System.Xml;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography.X509Certificates;

namespace SICOE.Infrastructure.Services.Sat.Soap;

/// <summary>
/// Constructor de mensajes SOAP para los servicios del SAT
/// Incluye firma digital XML según especificaciones del SAT
/// </summary>
public class SatSoapMessageBuilder
{
    private readonly ILogger<SatSoapMessageBuilder> _logger;
    private readonly XmlSignatureService _signatureService;

    public SatSoapMessageBuilder(ILogger<SatSoapMessageBuilder> logger, XmlSignatureService signatureService)
    {
        _logger = logger;
        _signatureService = signatureService;
    }

    /// <summary>
    /// Construye mensaje SOAP para SolicitaDescargaEmitidos con firma digital
    /// </summary>
    public string BuildSolicitaDescargaEmitidosSoap(
        string rfcEmisor,
        string rfcSolicitante,
        DateTime fechaInicial,
        DateTime fechaFinal,
        string tipoSolicitud,
        string[]? rfcReceptores = null,
        string? tipoComprobante = null,
        string? estadoComprobante = null,
        string? complemento = null,
        string? rfcACuentaTerceros = null,
        X509Certificate2? certificate = null)
    {
        try
        {
            var doc = new XmlDocument();
            doc.PreserveWhitespace = false;

            // Crear envelope SOAP
            var envelope = doc.CreateElement("soapenv", "Envelope", "http://schemas.xmlsoap.org/soap/envelope/");
            envelope.SetAttribute("xmlns:des", "http://DescargaMasivaTerceros.sat.gob.mx");
            envelope.SetAttribute("xmlns:xd", "http://www.w3.org/2000/09/xmldsig#");
            // IMPORTANTE: Agregar namespace WS-Security Utility para wsu:Id
            envelope.SetAttribute("xmlns:wsu", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd");
            doc.AppendChild(envelope);

            var header = doc.CreateElement("soapenv", "Header", "http://schemas.xmlsoap.org/soap/envelope/");
            envelope.AppendChild(header);

            var body = doc.CreateElement("soapenv", "Body", "http://schemas.xmlsoap.org/soap/envelope/");
            envelope.AppendChild(body);

            // Crear elemento SolicitaDescargaEmitidos
            var solicitaDescarga = doc.CreateElement("des", "SolicitaDescargaEmitidos", "http://DescargaMasivaTerceros.sat.gob.mx");
            body.AppendChild(solicitaDescarga);

            // Crear elemento solicitud con atributos ordenados alfabéticamente
            // Orden según documentación SAT: Complemento, EstadoComprobante, FechaInicial, FechaFinal, 
            // RfcEmisor, RfcSolicitante, TipoComprobante, TipoSolicitud, RfcACuentaTerceros
            var solicitud = doc.CreateElement("des", "solicitud", "http://DescargaMasivaTerceros.sat.gob.mx");
            
            // 1. Complemento
            if (!string.IsNullOrEmpty(complemento))
                solicitud.SetAttribute("Complemento", complemento);
            
            // 2. EstadoComprobante
            if (!string.IsNullOrEmpty(estadoComprobante))
                solicitud.SetAttribute("EstadoComprobante", estadoComprobante);
            
            // 3. FechaInicial
            // El SAT requiere fechas en hora local de México
            // Asegurar que la fecha inicial sea al inicio del día (00:00:00)
            var fechaInicialFormateada = FormatearFechaParaSat(fechaInicial, esInicioDia: true);
            _logger.LogInformation("FechaInicial original: {FechaOriginal}, formateada para SAT: {FechaFormateada}", 
                fechaInicial, fechaInicialFormateada);
            solicitud.SetAttribute("FechaInicial", fechaInicialFormateada);
            
            // 4. FechaFinal
            // IMPORTANTE: El SAT valida que la fecha final no sea mayor que la fecha actual
            // NO ajustar automáticamente a 23:59:59, usar la hora exacta proporcionada
            var fechaFinalFormateada = FormatearFechaParaSat(fechaFinal, esInicioDia: false);
            _logger.LogInformation("FechaFinal original: {FechaOriginal}, formateada para SAT: {FechaFormateada}", 
                fechaFinal, fechaFinalFormateada);
            solicitud.SetAttribute("FechaFinal", fechaFinalFormateada);
            
            // 5. RfcEmisor
            solicitud.SetAttribute("RfcEmisor", rfcEmisor);
            
            // 6. RfcSolicitante
            if (!string.IsNullOrEmpty(rfcSolicitante))
                solicitud.SetAttribute("RfcSolicitante", rfcSolicitante);
            
            // 7. TipoComprobante
            if (!string.IsNullOrEmpty(tipoComprobante))
                solicitud.SetAttribute("TipoComprobante", tipoComprobante);
            
            // 8. TipoSolicitud
            solicitud.SetAttribute("TipoSolicitud", tipoSolicitud);
            
            // 9. RfcACuentaTerceros (último según orden alfabético)
            if (!string.IsNullOrEmpty(rfcACuentaTerceros))
                solicitud.SetAttribute("RfcACuentaTerceros", rfcACuentaTerceros);

            // Agregar RfcReceptores si existen
            if (rfcReceptores != null && rfcReceptores.Length > 0)
            {
                var rfcReceptoresElement = doc.CreateElement("des", "RfcReceptores", "http://DescargaMasivaTerceros.sat.gob.mx");
                foreach (var rfc in rfcReceptores)
                {
                    var rfcElement = doc.CreateElement("des", "RfcReceptor", "http://DescargaMasivaTerceros.sat.gob.mx");
                    rfcElement.InnerText = rfc;
                    rfcReceptoresElement.AppendChild(rfcElement);
                }
                solicitud.AppendChild(rfcReceptoresElement);
            }

            // CRÍTICO: Ordenar atributos alfabéticamente ANTES de firmar
            // El SAT requiere que los atributos estén en orden alfabético estricto antes de firmar
            // Si no se hace esto, el SAT rechazará la solicitud con error "Sello Mal Formado" (302)
            XmlSignatureService.OrderAttributesAlphabetically(solicitud);

            // Firmar el elemento solicitud si se proporciona certificado
            if (certificate != null)
            {
                _signatureService.SignXmlElement(solicitud, certificate);
            }

            solicitaDescarga.AppendChild(solicitud);

            var xmlResult = doc.OuterXml;
            
            // Log del XML SOAP completo generado (incluye el sello)
            _logger.LogInformation("=== XML SOAP COMPLETO PARA SolicitaDescargaEmitidos ===");
            _logger.LogInformation("XML completo:\n{Xml}", xmlResult);
            
            // Extraer y log del elemento Signature (sello) si existe
            if (solicitud != null)
            {
                var signatureNodes = solicitud.GetElementsByTagName("Signature", "http://www.w3.org/2000/09/xmldsig#");
                if (signatureNodes.Count > 0 && signatureNodes[0] is XmlElement signatureElement)
                {
                    var signatureXml = signatureElement.OuterXml;
                    _logger.LogInformation("=== ELEMENTO SELLO (Signature) EN EL SOAP ===");
                    _logger.LogInformation("Sello XML:\n{SignatureXml}", signatureXml);
                }
            }
            
            return xmlResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al construir mensaje SOAP para SolicitaDescargaEmitidos");
            throw;
        }
    }

    /// <summary>
    /// Construye mensaje SOAP para SolicitaDescargaRecibidos con firma digital
    /// Orden alfabético de atributos: Complemento, EstadoComprobante, FechaInicial, FechaFinal, 
    /// RfcACuentaTerceros, RfcEmisor, RfcReceptor, RfcSolicitante, TipoComprobante, TipoSolicitud
    /// </summary>
    public string BuildSolicitaDescargaRecibidosSoap(
        string rfcReceptor,
        string rfcSolicitante,
        DateTime fechaInicial,
        DateTime fechaFinal,
        string tipoSolicitud,
        string? rfcEmisor = null,
        string? tipoComprobante = null,
        string? estadoComprobante = null,
        string? complemento = null,
        string? rfcACuentaTerceros = null,
        List<string>? uuids = null,
        X509Certificate2? certificate = null)
    {
        try
        {
            // Validaciones según especificación SAT
            // RfcReceptor es OBLIGATORIO
            if (string.IsNullOrWhiteSpace(rfcReceptor))
            {
                throw new ArgumentException("RfcReceptor es obligatorio para SolicitaDescargaRecibidos y no puede estar vacío", nameof(rfcReceptor));
            }
            
            // RfcSolicitante es opcional, pero si se proporciona DEBE coincidir con RfcReceptor
            if (!string.IsNullOrWhiteSpace(rfcSolicitante) && 
                !rfcSolicitante.Trim().Equals(rfcReceptor.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    $"RfcSolicitante ('{rfcSolicitante}') debe coincidir con RfcReceptor ('{rfcReceptor}') según especificación SAT", 
                    nameof(rfcSolicitante));
            }
            
            // TipoSolicitud es obligatorio
            if (string.IsNullOrWhiteSpace(tipoSolicitud))
            {
                throw new ArgumentException("TipoSolicitud es obligatorio y no puede estar vacío", nameof(tipoSolicitud));
            }
            var doc = new XmlDocument();
            doc.PreserveWhitespace = false;

            // Crear envelope SOAP
            var envelope = doc.CreateElement("soapenv", "Envelope", "http://schemas.xmlsoap.org/soap/envelope/");
            envelope.SetAttribute("xmlns:des", "http://DescargaMasivaTerceros.sat.gob.mx");
            envelope.SetAttribute("xmlns:xd", "http://www.w3.org/2000/09/xmldsig#");
            // IMPORTANTE: Agregar namespace WS-Security Utility para wsu:Id
            envelope.SetAttribute("xmlns:wsu", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd");
            doc.AppendChild(envelope);

            var header = doc.CreateElement("soapenv", "Header", "http://schemas.xmlsoap.org/soap/envelope/");
            envelope.AppendChild(header);

            var body = doc.CreateElement("soapenv", "Body", "http://schemas.xmlsoap.org/soap/envelope/");
            envelope.AppendChild(body);

            // Crear elemento SolicitaDescargaRecibidos
            var solicitaDescarga = doc.CreateElement("des", "SolicitaDescargaRecibidos", "http://DescargaMasivaTerceros.sat.gob.mx");
            body.AppendChild(solicitaDescarga);

            // Crear elemento solicitud
            // NOTA: Los atributos se agregan en cualquier orden aquí, pero luego se ordenan alfabéticamente
            // antes de firmar con OrderAttributesAlphabetically() según especificación SAT.
            // Orden alfabético final esperado: Complemento, EstadoComprobante, FechaFinal, FechaInicial,
            // RfcACuentaTerceros, RfcEmisor, RfcReceptor, RfcSolicitante, TipoComprobante, TipoSolicitud
            var solicitud = doc.CreateElement("des", "solicitud", "http://DescargaMasivaTerceros.sat.gob.mx");
            
            // 1. Complemento (opcional)
            if (!string.IsNullOrEmpty(complemento))
                solicitud.SetAttribute("Complemento", complemento);
            
            // 2. EstadoComprobante (opcional, por defecto "Vigente")
            // REGLA: Para Metadata permite "Vigente", "Cancelado", "Todos"
            // REGLA: Para CFDI solo permite "Vigente" (no permite cancelados)
            if (!string.IsNullOrEmpty(estadoComprobante))
                solicitud.SetAttribute("EstadoComprobante", estadoComprobante);
            
            // 3. FechaInicial (OBLIGATORIO)
            // El SAT requiere fechas en hora local de México
            // Asegurar que la fecha inicial sea al inicio del día (00:00:00)
            var fechaInicialFormateada = FormatearFechaParaSat(fechaInicial, esInicioDia: true);
            _logger.LogInformation("FechaInicial original: {FechaOriginal}, formateada para SAT: {FechaFormateada}", 
                fechaInicial, fechaInicialFormateada);
            solicitud.SetAttribute("FechaInicial", fechaInicialFormateada);
            
            // 4. FechaFinal (OBLIGATORIO)
            // IMPORTANTE: El SAT valida que la fecha final no sea mayor que la fecha actual
            // NO ajustar automáticamente a 23:59:59, usar la hora exacta proporcionada
            var fechaFinalFormateada = FormatearFechaParaSat(fechaFinal, esInicioDia: false);
            _logger.LogInformation("FechaFinal original: {FechaOriginal}, formateada para SAT: {FechaFormateada}", 
                fechaFinal, fechaFinalFormateada);
            solicitud.SetAttribute("FechaFinal", fechaFinalFormateada);
            
            // 5. RfcEmisor (opcional - filtra por emisor específico)
            if (!string.IsNullOrEmpty(rfcEmisor))
                solicitud.SetAttribute("RfcEmisor", rfcEmisor);
            
            // 6. RfcSolicitante (según Pág 20 del manual del SAT)
            if (!string.IsNullOrEmpty(rfcSolicitante))
                solicitud.SetAttribute("RfcSolicitante", rfcSolicitante.Trim().ToUpper());
            
            // 7. TipoComprobante (según Pág 20 del manual del SAT)
            if (!string.IsNullOrEmpty(tipoComprobante))
                solicitud.SetAttribute("TipoComprobante", tipoComprobante);
            
            // 8. TipoSolicitud (según Pág 20 del manual del SAT)
            solicitud.SetAttribute("TipoSolicitud", tipoSolicitud);

            // 9. RfcReceptor (OBLIGATORIO según especificación SAT Pág 20)
            // Debe contener el RFC del contribuyente que recibió los comprobantes
            solicitud.SetAttribute("RfcReceptor", rfcReceptor.Trim().ToUpper());
            
            // 10. RfcACuentaTerceros (opcional)
            if (!string.IsNullOrEmpty(rfcACuentaTerceros))
                solicitud.SetAttribute("RfcACuentaTerceros", rfcACuentaTerceros.Trim().ToUpper());

            // Agregar UUIDs si existen (Solo para Recibidos)
            if (uuids != null && uuids.Count > 0)
            {
                var uuidsElement = doc.CreateElement("des", "UUIDs", "http://DescargaMasivaTerceros.sat.gob.mx");
                foreach (var uuid in uuids)
                {
                    var uuidElement = doc.CreateElement("des", "Uuid", "http://DescargaMasivaTerceros.sat.gob.mx");
                    uuidElement.InnerText = uuid;
                    uuidsElement.AppendChild(uuidElement);
                }
                solicitud.AppendChild(uuidsElement);
            }

            // CRÍTICO: Ordenar atributos alfabéticamente ANTES de firmar
            // El SAT requiere que los atributos estén en orden alfabético estricto antes de firmar
            // Si no se hace esto, el SAT rechazará la solicitud con error "Sello Mal Formado" (302)
            XmlSignatureService.OrderAttributesAlphabetically(solicitud);

            // Firmar el elemento solicitud si se proporciona certificado
            if (certificate != null)
            {
                _signatureService.SignXmlElement(solicitud, certificate);
            }

            solicitaDescarga.AppendChild(solicitud);

            var xmlResult = doc.OuterXml;
            
            // Log del XML SOAP completo generado (incluye el sello)
            _logger.LogInformation("=== XML SOAP COMPLETO PARA SolicitaDescargaRecibidos ===");
            _logger.LogInformation("XML completo:\n{Xml}", xmlResult);
            
            // Extraer y log del elemento Signature (sello) si existe
            if (solicitud != null)
            {
                var signatureNodes = solicitud.GetElementsByTagName("Signature", "http://www.w3.org/2000/09/xmldsig#");
                if (signatureNodes.Count > 0 && signatureNodes[0] is XmlElement signatureElement)
                {
                    var signatureXml = signatureElement.OuterXml;
                    _logger.LogInformation("=== ELEMENTO SELLO (Signature) EN EL SOAP ===");
                    _logger.LogInformation("Sello XML:\n{SignatureXml}", signatureXml);
                }
            }
            
            return xmlResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al construir mensaje SOAP para SolicitaDescargaRecibidos");
            throw;
        }
    }

    /// <summary>
    /// Construye mensaje SOAP para VerificaSolicitudDescarga con firma digital
    /// </summary>
    public string BuildVerificaSolicitudDescargaSoap(
        string idSolicitud,
        string rfcSolicitante,
        X509Certificate2? certificate = null)
    {
        try
        {
            var doc = new XmlDocument();
            doc.PreserveWhitespace = false;

            // Crear envelope SOAP
            var envelope = doc.CreateElement("soapenv", "Envelope", "http://schemas.xmlsoap.org/soap/envelope/");
            envelope.SetAttribute("xmlns:des", "http://DescargaMasivaTerceros.sat.gob.mx");
            envelope.SetAttribute("xmlns:xd", "http://www.w3.org/2000/09/xmldsig#");
            // IMPORTANTE: Agregar namespace WS-Security Utility para wsu:Id
            envelope.SetAttribute("xmlns:wsu", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd");
            doc.AppendChild(envelope);

            var header = doc.CreateElement("soapenv", "Header", "http://schemas.xmlsoap.org/soap/envelope/");
            envelope.AppendChild(header);

            var body = doc.CreateElement("soapenv", "Body", "http://schemas.xmlsoap.org/soap/envelope/");
            envelope.AppendChild(body);

            // Crear elemento VerificaSolicitudDescarga
            var verificaDescarga = doc.CreateElement("des", "VerificaSolicitudDescarga", "http://DescargaMasivaTerceros.sat.gob.mx");
            body.AppendChild(verificaDescarga);

            // Crear elemento solicitud con atributos ordenados alfabéticamente
            var solicitud = doc.CreateElement("des", "solicitud", "http://DescargaMasivaTerceros.sat.gob.mx");
            
            // Orden alfabético: IdSolicitud, RfcSolicitante
            solicitud.SetAttribute("IdSolicitud", idSolicitud);
            solicitud.SetAttribute("RfcSolicitante", rfcSolicitante);

            // CRÍTICO: Ordenar atributos alfabéticamente ANTES de firmar
            // El SAT requiere que los atributos estén en orden alfabético estricto antes de firmar
            XmlSignatureService.OrderAttributesAlphabetically(solicitud);

            // Firmar el elemento solicitud si se proporciona certificado
            if (certificate != null)
            {
                _signatureService.SignXmlElement(solicitud, certificate);
            }

            verificaDescarga.AppendChild(solicitud);

            var xmlResult = doc.OuterXml;
            
            // Log del XML SOAP completo generado (incluye el sello)
            _logger.LogInformation("=== XML SOAP COMPLETO PARA VerificaSolicitudDescarga ===");
            _logger.LogInformation("XML completo:\n{Xml}", xmlResult);
            
            // Extraer y log del elemento Signature (sello) si existe
            if (solicitud != null)
            {
                var signatureNodes = solicitud.GetElementsByTagName("Signature", "http://www.w3.org/2000/09/xmldsig#");
                if (signatureNodes.Count > 0 && signatureNodes[0] is XmlElement signatureElement)
                {
                    var signatureXml = signatureElement.OuterXml;
                    _logger.LogInformation("=== ELEMENTO SELLO (Signature) EN EL SOAP ===");
                    _logger.LogInformation("Sello XML:\n{SignatureXml}", signatureXml);
                }
            }

            return xmlResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al construir mensaje SOAP para VerificaSolicitudDescarga");
            throw;
        }
    }

    /// <summary>
    /// Construye mensaje SOAP para PeticionDescargaMasivaTerceros (Descarga de paquetes) con firma digital
    /// </summary>
    public string BuildPeticionDescargaMasivaSoap(
        string idPaquete,
        string rfcSolicitante,
        X509Certificate2? certificate = null)
    {
        try
        {
            var doc = new XmlDocument();
            doc.PreserveWhitespace = false;

            // Crear envelope SOAP
            var envelope = doc.CreateElement("soapenv", "Envelope", "http://schemas.xmlsoap.org/soap/envelope/");
            envelope.SetAttribute("xmlns:des", "http://DescargaMasivaTerceros.sat.gob.mx");
            envelope.SetAttribute("xmlns:xd", "http://www.w3.org/2000/09/xmldsig#");
            doc.AppendChild(envelope);

            var header = doc.CreateElement("soapenv", "Header", "http://schemas.xmlsoap.org/soap/envelope/");
            envelope.AppendChild(header);

            var body = doc.CreateElement("soapenv", "Body", "http://schemas.xmlsoap.org/soap/envelope/");
            envelope.AppendChild(body);

            // CRÍTICO: Según documentación SAT, el elemento raíz debe ser "PeticionDescargaMasivaTercerosEntrada"
            // NOTA: Aunque el documento puede no ser el más reciente, este es el nombre que aparece en los ejemplos oficiales
            var operacion = doc.CreateElement("des", "PeticionDescargaMasivaTercerosEntrada", "http://DescargaMasivaTerceros.sat.gob.mx");
            body.AppendChild(operacion);

            // Crear elemento peticionDescarga
            var peticion = doc.CreateElement("des", "peticionDescarga", "http://DescargaMasivaTerceros.sat.gob.mx");
            
            // Los atributos para descarga son 'IdPaquete' y 'RfcSolicitante' 
            // NOTA: Se usa PascalCase para consistencia con otros servicios del SAT
            peticion.SetAttribute("IdPaquete", idPaquete);
            peticion.SetAttribute("RfcSolicitante", rfcSolicitante);

            // CRÍTICO: Ordenar atributos alfabéticamente ANTES de firmar
            // Orden alfabético: idPaquete, rfcSolicitante
            XmlSignatureService.OrderAttributesAlphabetically(peticion);

            // Firmar el elemento peticion si se proporciona certificado
            if (certificate != null)
            {
                _signatureService.SignXmlElement(peticion, certificate);
            }

            operacion.AppendChild(peticion);

            var xmlResult = doc.OuterXml;
            
            _logger.LogInformation("=== XML SOAP COMPLETO PARA PeticionDescargaMasivaTerceros ===");
            _logger.LogDebug("XML completo:\n{Xml}", xmlResult);

            return xmlResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al construir mensaje SOAP para PeticionDescargaMasivaTerceros");
            throw;
        }
    }

    /// <summary>
    /// Formatea una fecha para el SAT según especificación oficial
    /// El SAT requiere formato EXACTO: yyyy-MM-ddTHH:mm:ss (con segundos, sin milisegundos, sin zona horaria)
    /// Ejemplo del SAT: "2025-05-12T18:57:43"
    /// 
    /// IMPORTANTE: 
    /// - Fecha inicial: puede ser 00:00:00 del día o la hora exacta proporcionada
    /// - Fecha final: debe ser la hora exacta proporcionada (con segundos), pero NO puede ser mayor que la fecha/hora actual
    /// - El SAT valida estrictamente que la fecha final <= fecha actual en hora de México
    /// - NO poner segundos en 0 automáticamente, usar los segundos de la fecha original
    /// </summary>
    private string FormatearFechaParaSat(DateTime fecha, bool esInicioDia)
    {
        // Obtener zona horaria de México
        TimeZoneInfo mexicoTimeZone;
        try
        {
            // Intentar obtener zona horaria de México (Windows)
            mexicoTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time (Mexico)");
        }
        catch
        {
            try
            {
                // Fallback para sistemas Unix/Linux
                mexicoTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Mexico_City");
            }
            catch
            {
                // Si no se puede obtener, usar UTC-6 (hora estándar de México)
                mexicoTimeZone = TimeZoneInfo.CreateCustomTimeZone("Mexico", TimeSpan.FromHours(-6), "Mexico", "Mexico");
            }
        }
        
        // Convertir fecha recibida a UTC si no lo es
        DateTime fechaUtc = fecha.Kind == DateTimeKind.Utc 
            ? fecha 
            : fecha.Kind == DateTimeKind.Local 
                ? fecha.ToUniversalTime() 
                : DateTime.SpecifyKind(fecha, DateTimeKind.Utc);
        
        // Convertir UTC a hora local de México
        DateTime fechaLocal = TimeZoneInfo.ConvertTimeFromUtc(fechaUtc, mexicoTimeZone);
        
        // Obtener fecha/hora actual en hora de México para validación
        DateTime ahoraMexico = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, mexicoTimeZone);
        
        DateTime fechaFormateada;
        if (esInicioDia)
        {
            // Fecha inicial: inicio del día (00:00:00) según ejemplo del SAT
            fechaFormateada = new DateTime(fechaLocal.Year, fechaLocal.Month, fechaLocal.Day, 0, 0, 0, DateTimeKind.Unspecified);
        }
        else
        {
            // Fecha final: usar la hora exacta proporcionada (CON SEGUNDOS)
            // PERO: si es mayor que ahora, ajustar a ahora (el SAT no acepta fechas futuras)
            if (fechaLocal > ahoraMexico)
            {
                // Ajustar a la fecha/hora actual en México (CON SEGUNDOS, como en el ejemplo del SAT)
                fechaFormateada = new DateTime(
                    ahoraMexico.Year, 
                    ahoraMexico.Month, 
                    ahoraMexico.Day, 
                    ahoraMexico.Hour, 
                    ahoraMexico.Minute, 
                    ahoraMexico.Second, // MANTENER SEGUNDOS (como en ejemplo: 18:57:43)
                    DateTimeKind.Unspecified);
                
                _logger.LogWarning(
                    "Fecha final proporcionada ({FechaOriginal}) es mayor que la fecha actual en México ({AhoraMexico}). " +
                    "Ajustando a fecha actual para cumplir con validación del SAT.",
                    fechaLocal, ahoraMexico);
            }
            else
            {
                // Usar la fecha proporcionada TAL CUAL, incluyendo segundos (como en ejemplo del SAT: 18:57:43)
                fechaFormateada = new DateTime(
                    fechaLocal.Year, 
                    fechaLocal.Month, 
                    fechaLocal.Day, 
                    fechaLocal.Hour, 
                    fechaLocal.Minute, 
                    fechaLocal.Second, // MANTENER SEGUNDOS ORIGINALES
                    DateTimeKind.Unspecified);
            }
        }
        
        // Formato EXACTO requerido por SAT: yyyy-MM-ddTHH:mm:ss (con segundos, sin milisegundos, sin zona horaria)
        // Ejemplo: "2025-05-12T18:57:43"
        // IMPORTANTE: Usar formato invariante para evitar problemas de localización
        string fechaFormateadaStr = fechaFormateada.ToString("yyyy-MM-ddTHH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
        
        _logger.LogInformation("Fecha formateada para SAT: {FechaFormateada} (original: {FechaOriginal}, esInicioDia: {EsInicioDia}, hora local México: {FechaLocal})", 
            fechaFormateadaStr, fecha, esInicioDia, fechaLocal);
        
        return fechaFormateadaStr;
    }
}

