using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SICOE.Application.Common;
using SICOE.Application.Interfaces.Services;
using SICOE.Infrastructure.Services.Sat.Soap;
using System.Security.Cryptography.X509Certificates;

namespace SICOE.Infrastructure.Services.Sat;

/// <summary>
/// Implementación del servicio SAT para comunicación con Web Services del SAT
/// Maneja solicitud, verificación y descarga de CFDI's XML
/// </summary>
public class SatService : ISatService
{
    private readonly ILogger<SatService> _logger;
    private readonly IConfiguration _configuration;
    private readonly SatSoapMessageBuilder _messageBuilder;
    private readonly SatSoapHttpClient _httpClient;
    private readonly SatSoapResponseParser _responseParser;
    private readonly string _solicitudUrl;
    private readonly string _verificacionUrl;
    private readonly string _descargaUrl;

    public SatService(
        ILogger<SatService> logger,
        IConfiguration configuration,
        SatSoapMessageBuilder messageBuilder,
        SatSoapHttpClient httpClient,
        SatSoapResponseParser responseParser)
    {
        _logger = logger;
        _configuration = configuration;
        _messageBuilder = messageBuilder;
        _httpClient = httpClient;
        _responseParser = responseParser;

        var baseUrl = _configuration["Sat:BaseUrl"] 
            ?? "https://cfdidescargamasivasolicitud.clouda.sat.gob.mx";

        _solicitudUrl = $"{baseUrl}/SolicitaDescargaService.svc";
        _verificacionUrl = $"{baseUrl}/VerificaSolicitudDescargaService.svc";
        
        var descargaBaseUrl = _configuration["Sat:DescargaBaseUrl"] 
            ?? "https://cfdidescargamasiva.clouda.sat.gob.mx";
        _descargaUrl = $"{descargaBaseUrl}/DescargaMasivaService.svc";
    }

    public async Task<Result<SolicitudDescargaResponse>> SolicitarDescargaMasivaAsync(
        string tokenSat,
        string rfcSolicitante,
        DateTime fechaInicial,
        DateTime fechaFinal,
        string tipoSolicitud = "CFDI",
        string? tipoComprobante = null,
        string? estadoComprobante = null,
        string? rfcEmisor = null,
        string? rfcReceptor = null,
        string tipoDescarga = "recibidos",
        string? complemento = null,
        string? rfcACuentaTerceros = null,
        List<string>? uuids = null,
        X509Certificate2? certificate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(tokenSat))
                return Result<SolicitudDescargaResponse>.Failure("El token SAT es requerido");

            if (string.IsNullOrWhiteSpace(rfcSolicitante))
                return Result<SolicitudDescargaResponse>.Failure("El RFC solicitante es requerido");

            if (fechaFinal < fechaInicial)
                return Result<SolicitudDescargaResponse>.Failure("La fecha final debe ser mayor o igual a la fecha inicial");

            // Si no se proporciona rfcEmisor, usar rfcSolicitante
            var rfcEmisorFinal = rfcEmisor ?? rfcSolicitante;

            _logger.LogInformation("Solicitando descarga masiva al SAT. TipoDescarga: {TipoDescarga}, RFC Solicitante: {RfcSolicitante}, Fechas: {FechaInicial} - {FechaFinal}", 
                tipoDescarga, rfcSolicitante, fechaInicial, fechaFinal);

            // NORMALIZACIÓN PARA SAT: Si es CFDI, el SAT solo permite Vigente. 
            // Si el usuario puso "Todos" o "Cancelado", lo forzamos a "Vigente" para evitar rechazo 5 (Rechazada).
            if (tipoSolicitud.Equals("CFDI", StringComparison.OrdinalIgnoreCase))
            {
                if (estadoComprobante != null && !estadoComprobante.Equals("Vigente", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("TipoSolicitud 'CFDI' detectado con EstadoComprobante '{Estado}'. Normalizando a 'Vigente' para cumplir con reglas del SAT y evitar rechazo.", estadoComprobante);
                    estadoComprobante = "Vigente";
                }
            }

            // Determinar si es solicitud de emitidos o recibidos basándose en el parámetro explícito
            var esEmitidos = tipoDescarga.Equals("emitidos", StringComparison.OrdinalIgnoreCase);
            
            // Construir mensaje SOAP
            string soapBody;
            string soapAction;
            
            if (esEmitidos)
            {
                // Para Emitidos, el emisor siempre es el solicitante
                string[]? rfcReceptores = null;
                if (!string.IsNullOrEmpty(rfcReceptor))
                {
                    rfcReceptores = new[] { rfcReceptor };
                }
                
                soapBody = _messageBuilder.BuildSolicitaDescargaEmitidosSoap(
                    rfcEmisor: rfcSolicitante, 
                    rfcSolicitante: rfcSolicitante,
                    fechaInicial: fechaInicial,
                    fechaFinal: fechaFinal,
                    tipoSolicitud: tipoSolicitud,
                    rfcReceptores: rfcReceptores, 
                    tipoComprobante: tipoComprobante,
                    estadoComprobante: estadoComprobante,
                    complemento: complemento,
                    rfcACuentaTerceros: rfcACuentaTerceros,
                    certificate: certificate);
                
                soapAction = "http://DescargaMasivaTerceros.sat.gob.mx/ISolicitaDescargaService/SolicitaDescargaEmitidos";
                _logger.LogInformation("Llamando a SolicitaDescargaEmitidos. RFC Solicitante/Emisor: {Rfc}, Fechas: {Inicio} a {Fin}, Filtro Estado: {Estado}", 
                    rfcSolicitante, fechaInicial, fechaFinal, estadoComprobante ?? "Vigente");
            }
            else
            {
                // SolicitaDescargaRecibidos
                // Para Recibidos, el receptor es el solicitante
                var rfcReceptorFinal = rfcSolicitante; 
                
                soapBody = _messageBuilder.BuildSolicitaDescargaRecibidosSoap(
                    fechaInicial: fechaInicial,
                    fechaFinal: fechaFinal,
                    rfcReceptor: rfcReceptorFinal, 
                    rfcSolicitante: rfcSolicitante,
                    tipoSolicitud: tipoSolicitud,
                    rfcEmisor: rfcEmisor, 
                    tipoComprobante: tipoComprobante,
                    estadoComprobante: estadoComprobante,
                    complemento: complemento,
                    rfcACuentaTerceros: rfcACuentaTerceros,
                    uuids: uuids,
                    certificate: certificate);
                
                soapAction = "http://DescargaMasivaTerceros.sat.gob.mx/ISolicitaDescargaService/SolicitaDescargaRecibidos";
                _logger.LogInformation("Llamando a SolicitaDescargaRecibidos. RFC Solicitante/Receptor: {Rfc}, Fechas: {Inicio} a {Fin}, Filtro Estado: {Estado}, Filtro Emisor: {Emisor}", 
                    rfcSolicitante, fechaInicial, fechaFinal, estadoComprobante ?? "Vigente", rfcEmisor ?? "Todos");
            }

            // Log del XML completo que se envía al SAT (para debugging)
            _logger.LogInformation("XML SOAP que se envía al SAT:\n{SoapBody}", soapBody);
            
            // Enviar solicitud SOAP
            var xmlResponse = await _httpClient.SendSoapRequestAsync(
                _solicitudUrl,
                soapAction,
                soapBody,
                tokenSat,
                cancellationToken);

            // Parsear respuesta según el tipo de solicitud
            string idSolicitud;
            string rfcSolicitanteResp;
            string codEstatus;
            string mensaje;
            
            if (esEmitidos)
            {
                (idSolicitud, rfcSolicitanteResp, codEstatus, mensaje) = 
                    _responseParser.ParseSolicitaDescargaEmitidosResponse(xmlResponse);
            }
            else
            {
                (idSolicitud, rfcSolicitanteResp, codEstatus, mensaje) = 
                    _responseParser.ParseSolicitaDescargaRecibidosResponse(xmlResponse);
            }

            // Mapear CodEstatus del SAT a CodigoEstado interno
            // IMPORTANTE: En SolicitaDescarga, el SAT solo devuelve CodEstatus (string), NO EstadoSolicitud (int)
            // CodEstatus "5000" = Solicitud Aceptada (corresponde a EstadoSolicitud = 1 cuando se verifique)
            // Otros CodEstatus (300, 301, 302, etc.) = Errores (corresponde a EstadoSolicitud = 4)
            // El EstadoSolicitud real (1-6) se obtendrá cuando se verifique la solicitud con VerificaSolicitudDescarga
            // Por ahora, mapeamos CodEstatus a un código interno que luego se convertirá a EstadoSolicitud
            // CodEstatus "5000" -> CodigoEstado 1 (Aceptada/Pendiente)
            // Otros CodEstatus -> CodigoEstado 4 (Error)
            var codigoEstado = codEstatus == "5000" ? 1 : 4;

            _logger.LogInformation("Solicitud de descarga procesada. IdSolicitud: {IdSolicitud}, CodEstatus: {CodEstatus}, Mensaje: {Mensaje}", 
                idSolicitud, codEstatus, mensaje);

            var response = new SolicitudDescargaResponse
            {
                IdSolicitud = idSolicitud,
                CodigoEstado = codigoEstado,
                Mensaje = mensaje,
                TotalSolicitado = null, // Se obtendrá después de la verificación
                FechaEstimadaTermino = DateTime.UtcNow.AddHours(2) // Estimación
            };

            return Result<SolicitudDescargaResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al solicitar descarga masiva al SAT");
            return Result<SolicitudDescargaResponse>.Failure($"Error al solicitar descarga masiva: {ex.Message}");
        }
    }

    public async Task<Result<VerificacionDescargaResponse>> VerificarDescargaAsync(
        string tokenSat,
        string idSolicitud,
        string rfcSolicitante,
        X509Certificate2? certificate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(tokenSat))
                return Result<VerificacionDescargaResponse>.Failure("El token SAT es requerido");

            if (string.IsNullOrWhiteSpace(idSolicitud))
                return Result<VerificacionDescargaResponse>.Failure("El ID de solicitud es requerido");

            if (string.IsNullOrWhiteSpace(rfcSolicitante))
                return Result<VerificacionDescargaResponse>.Failure("El RFC solicitante es requerido");

            _logger.LogInformation("Verificando estado de descarga. IdSolicitud: {IdSolicitud}, RFC: {RfcSolicitante}", 
                idSolicitud, rfcSolicitante);

            // Construir mensaje SOAP
            var soapBody = _messageBuilder.BuildVerificaSolicitudDescargaSoap(
                idSolicitud: idSolicitud,
                rfcSolicitante: rfcSolicitante,
                certificate: certificate);

            var soapAction = "http://DescargaMasivaTerceros.sat.gob.mx/IVerificaSolicitudDescargaService/VerificaSolicitudDescarga";

            // Enviar solicitud SOAP
            var xmlResponse = await _httpClient.SendSoapRequestAsync(
                _verificacionUrl,
                soapAction,
                soapBody,
                tokenSat,
                cancellationToken);

            // Parsear respuesta
            var (estadoSolicitud, codigoEstadoSolicitud, numeroCFDIS, codEstatus, mensaje, idsPaquetes) = 
                _responseParser.ParseVerificaSolicitudDescargaResponse(xmlResponse);

            // Mapear EstadoSolicitud a CodigoEstado
            var codigoEstado = _responseParser.MapEstadoSolicitudToCodigoEstado(estadoSolicitud);

            _logger.LogInformation("Estado de descarga verificado. EstadoSolicitud: {EstadoSolicitud}, CodigoEstado: {CodigoEstado}, TotalPaquetes: {TotalPaquetes}, NumeroCFDIS: {NumeroCFDIS}", 
                estadoSolicitud, codigoEstado, idsPaquetes.Count, numeroCFDIS);

            var response = new VerificacionDescargaResponse
            {
                CodigoEstado = codigoEstado,
                Mensaje = mensaje,
                TotalPaquetes = idsPaquetes.Count,
                IdsPaquetes = idsPaquetes,
                TotalCFDIs = numeroCFDIS > 0 ? numeroCFDIS : null
            };

            return Result<VerificacionDescargaResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al verificar descarga en el SAT");
            return Result<VerificacionDescargaResponse>.Failure($"Error al verificar descarga: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<CFDIPaquete>>> DescargarPaquetesAsync(
        string tokenSat,
        string idSolicitud,
        string rfcSolicitante,
        X509Certificate2? certificate = null,
        List<string>? idsPaquetes = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(tokenSat))
                return Result<IEnumerable<CFDIPaquete>>.Failure("El token SAT es requerido");

            if (string.IsNullOrWhiteSpace(idSolicitud))
                return Result<IEnumerable<CFDIPaquete>>.Failure("El ID de solicitud es requerido");

            if (string.IsNullOrWhiteSpace(rfcSolicitante))
                return Result<IEnumerable<CFDIPaquete>>.Failure("El RFC solicitante es requerido");

            _logger.LogInformation("Descargando paquetes del SAT. IdSolicitud: {IdSolicitud}, RFC: {RfcSolicitante}, IdsPaquetes proporcionados: {TieneIds}", 
                idSolicitud, rfcSolicitante, idsPaquetes != null && idsPaquetes.Any());

            List<string> idsPaquetesParaDescargar;

            // Si se proporcionaron IDs de paquetes, usarlos directamente sin verificar
            if (idsPaquetes != null && idsPaquetes.Any())
            {
                _logger.LogInformation("Usando IDs de paquetes proporcionados directamente. Total: {Total}", idsPaquetes.Count);
                idsPaquetesParaDescargar = idsPaquetes;
            }
            else
            {
                // Si no se proporcionaron, verificar el estado para obtener los IDs de paquetes
                // CRÍTICO: Se debe pasar el certificado para firmar la petición de verificación
                _logger.LogInformation("No se proporcionaron IDs de paquetes. Verificando estado en el SAT...");
                var verificacion = await VerificarDescargaAsync(tokenSat, idSolicitud, rfcSolicitante, certificate, cancellationToken);
                if (verificacion.IsFailure)
                {
                    return Result<IEnumerable<CFDIPaquete>>.Failure(verificacion.Error);
                }

                if (verificacion.Value.CodigoEstado != 2) // 2: Terminada (Completada)
                {
                    return Result<IEnumerable<CFDIPaquete>>.Failure(
                        $"La solicitud no está terminada. Estado actual: {verificacion.Value.CodigoEstado} - {verificacion.Value.Mensaje}");
                }

                if (verificacion.Value.TotalPaquetes == 0 || !verificacion.Value.IdsPaquetes.Any())
                {
                    return Result<IEnumerable<CFDIPaquete>>.Failure("No hay paquetes disponibles para descargar");
                }

                idsPaquetesParaDescargar = verificacion.Value.IdsPaquetes;
                _logger.LogInformation("IDs de paquetes obtenidos de verificación. Total: {Total}", idsPaquetesParaDescargar.Count);
            }

            var paquetes = new List<CFDIPaquete>();

            // Descargar cada paquete
            foreach (var idPaquete in idsPaquetesParaDescargar)
            {
                try
                {
                    _logger.LogInformation("Descargando paquete via SOAP: {IdPaquete}", idPaquete);

                    // 1. Construir mensaje SOAP de descarga (PeticionDescargaMasivaTerceros)
                    // Requiere firma digital
                    var soapBody = _messageBuilder.BuildPeticionDescargaMasivaSoap(
                        idPaquete: idPaquete,
                        rfcSolicitante: rfcSolicitante,
                        certificate: certificate);

                    // CRÍTICO: El SOAPAction correcto según documentación SAT es "Descargar" del servicio "IDescargaMasivaTercerosService"
                    // NOTA: Aunque el documento puede no ser el más reciente, este es el SOAPAction que aparece en los ejemplos oficiales
                    var soapAction = "http://DescargaMasivaTerceros.sat.gob.mx/IDescargaMasivaTercerosService/Descargar";

                    // 2. Enviar solicitud SOAP
                    var xmlResponse = await _httpClient.SendSoapRequestAsync(
                        _descargaUrl,
                        soapAction,
                        soapBody,
                        tokenSat,
                        cancellationToken);

                    // 3. Parsear respuesta SOAP para extraer el contenido binario (Base64)
                    var (contenidoZip, codEstatus, mensaje) = _responseParser.ParsePeticionDescargaMasivaResponse(xmlResponse);

                    if (codEstatus != "5000" && (contenidoZip == null || contenidoZip.Length == 0))
                    {
                        _logger.LogWarning("Error al descargar paquete {IdPaquete}. CodEstatus: {CodEstatus}, Mensaje: {Mensaje}", 
                            idPaquete, codEstatus, mensaje);
                        continue;
                    }

                    var paquete = new CFDIPaquete
                    {
                        IdPaquete = idPaquete,
                        ContenidoZip = contenidoZip!,
                        CFDIs = new List<CFDIXml>(),
                        FechaDescarga = DateTime.UtcNow
                    };

                    // Extraer XML del ZIP y parsear
                    if (contenidoZip.Length > 0)
                    {
                        using (var zip = new ZipArchive(new MemoryStream(contenidoZip), ZipArchiveMode.Read))
                        {
                            foreach (var entry in zip.Entries)
                            {
                                if (entry.Name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                                {
                                    using (var stream = entry.Open())
                                    {
                                        var xmlBytes = await ReadStreamToByteArrayAsync(stream);
                                        var cfdiXml = ParseCFDIXml(xmlBytes);
                                        paquete.CFDIs.Add(cfdiXml);
                                    }
                                }
                            }
                        }
                    }

                    paquetes.Add(paquete);
                    _logger.LogInformation("Paquete descargado y procesado. IdPaquete: {IdPaquete}, CFDIs: {Count}", 
                        idPaquete, paquete.CFDIs.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al descargar paquete {IdPaquete}", idPaquete);
                    // Continuar con el siguiente paquete
                }
            }

            _logger.LogInformation("Paquetes descargados. Total: {TotalPaquetes}, CFDIs totales: {TotalCFDIs}", 
                paquetes.Count, paquetes.Sum(p => p.CFDIs.Count));

            return Result<IEnumerable<CFDIPaquete>>.Success(paquetes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al descargar paquetes del SAT");
            return Result<IEnumerable<CFDIPaquete>>.Failure($"Error al descargar paquetes: {ex.Message}");
        }
    }

    /// <summary>
    /// Lee un stream a un array de bytes
    /// </summary>
    private async Task<byte[]> ReadStreamToByteArrayAsync(Stream stream)
    {
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        return memoryStream.ToArray();
    }

    /// <summary>
    /// Parsea un XML de CFDI para extraer metadata completa
    /// Extrae todos los campos relevantes del CFDI 4.0 incluyendo Emisor, Receptor, Conceptos e Impuestos
    /// </summary>
    private CFDIXml ParseCFDIXml(byte[] xmlBytes)
    {
        try
        {
            var xml = XDocument.Load(new MemoryStream(xmlBytes));
            var ns = XNamespace.Get("http://www.sat.gob.mx/cfd/4");
            var tfdNs = XNamespace.Get("http://www.sat.gob.mx/TimbreFiscalDigital");
            var comprobante = xml.Element(ns + "Comprobante");

            if (comprobante == null)
                throw new InvalidOperationException("El XML no contiene un Comprobante válido");

            // Extraer UUID del TimbreFiscalDigital (está en el Complemento)
            // El TimbreFiscalDigital puede estar en cualquier namespace dentro del Complemento
            var complemento = comprobante.Element(ns + "Complemento");
            XElement? timbreFiscal = null;
            
            // Buscar TimbreFiscalDigital en el namespace correcto
            if (complemento != null)
            {
                // Buscar con namespace específico
                timbreFiscal = complemento.Element(tfdNs + "TimbreFiscalDigital");
                
                // Si no se encuentra, buscar sin namespace (algunos XMLs no declaran el namespace correctamente)
                if (timbreFiscal == null)
                {
                    timbreFiscal = complemento.Elements().FirstOrDefault(e => 
                        e.Name.LocalName == "TimbreFiscalDigital");
                }
            }
            
            var uuid = timbreFiscal?.Attribute("UUID")?.Value ?? string.Empty;

            // Extraer datos del Comprobante (atributos directos)
            var fechaEmisionStr = comprobante.Attribute("Fecha")?.Value ?? string.Empty;
            var fechaEmision = DateTime.TryParse(fechaEmisionStr, out var fecha) ? fecha : DateTime.MinValue;

            var totalStr = comprobante.Attribute("Total")?.Value ?? "0";
            var total = decimal.TryParse(totalStr, out var totalDecimal) ? totalDecimal : 0;

            var subTotalStr = comprobante.Attribute("SubTotal")?.Value ?? "0";
            var subTotal = decimal.TryParse(subTotalStr, out var subTotalDecimal) ? subTotalDecimal : 0;

            var tipoComprobante = comprobante.Attribute("TipoDeComprobante")?.Value ?? string.Empty;
            var serie = comprobante.Attribute("Serie")?.Value;
            var folio = comprobante.Attribute("Folio")?.Value;
            var formaPago = comprobante.Attribute("FormaPago")?.Value;
            var metodoPago = comprobante.Attribute("MetodoPago")?.Value;
            var moneda = comprobante.Attribute("Moneda")?.Value ?? "MXN";
            var lugarExpedicion = comprobante.Attribute("LugarExpedicion")?.Value;

            // Extraer datos del Emisor (elemento <cfdi:Emisor>)
            var emisor = comprobante.Element(ns + "Emisor");
            var rfcEmisor = emisor?.Attribute("Rfc")?.Value ?? string.Empty;
            var nombreEmisor = emisor?.Attribute("Nombre")?.Value;
            var regimenFiscalEmisor = emisor?.Attribute("RegimenFiscal")?.Value;

            // Extraer datos del Receptor (elemento <cfdi:Receptor>)
            var receptor = comprobante.Element(ns + "Receptor");
            var rfcReceptor = receptor?.Attribute("Rfc")?.Value ?? string.Empty;
            var nombreReceptor = receptor?.Attribute("Nombre")?.Value;
            var regimenFiscalReceptor = receptor?.Attribute("RegimenFiscalReceptor")?.Value;
            var domicilioFiscalReceptor = receptor?.Attribute("DomicilioFiscalReceptor")?.Value;
            var usoCFDI = receptor?.Attribute("UsoCFDI")?.Value;

            // Extraer Impuestos (Totales)
            var impuestos = comprobante.Element(ns + "Impuestos");
            var totalImpuestosTrasladadosStr = impuestos?.Attribute("TotalImpuestosTrasladados")?.Value ?? "0";
            var totalImpuestosTrasladados = decimal.TryParse(totalImpuestosTrasladadosStr, out var totalImpTrasladados) ? totalImpTrasladados : 0;

            // Extraer fecha de timbrado del TimbreFiscalDigital
            var fechaTimbradoStr = timbreFiscal?.Attribute("FechaTimbrado")?.Value ?? string.Empty;
            var fechaTimbrado = DateTime.TryParse(fechaTimbradoStr, out var fechaTimb) ? fechaTimb : (DateTime?)null;

            _logger.LogDebug("CFDI parseado: UUID={Uuid}, RFC Emisor={RfcEmisor}, RFC Receptor={RfcReceptor}, Total={Total}", 
                uuid, rfcEmisor, rfcReceptor, total);

            return new CFDIXml
            {
                Uuid = uuid,
                ContenidoXml = xmlBytes,
                RfcEmisor = rfcEmisor,
                RfcReceptor = rfcReceptor,
                FechaEmision = fechaEmision,
                Total = total,
                TipoComprobante = tipoComprobante,
                Serie = serie,
                Folio = folio,
                // Campos adicionales
                NombreEmisor = nombreEmisor,
                NombreReceptor = nombreReceptor,
                RegimenFiscalEmisor = regimenFiscalEmisor,
                RegimenFiscalReceptor = regimenFiscalReceptor,
                DomicilioFiscalReceptor = domicilioFiscalReceptor,
                UsoCFDI = usoCFDI,
                SubTotal = subTotal,
                TotalImpuestosTrasladados = totalImpuestosTrasladados,
                FormaPago = formaPago,
                MetodoPago = metodoPago,
                Moneda = moneda,
                LugarExpedicion = lugarExpedicion,
                FechaTimbrado = fechaTimbrado
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al parsear XML de CFDI");
            throw;
        }
    }
}
