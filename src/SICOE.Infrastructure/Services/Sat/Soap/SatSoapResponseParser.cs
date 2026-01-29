using System.Xml;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;

namespace SICOE.Infrastructure.Services.Sat.Soap;

/// <summary>
/// Parser para respuestas SOAP del SAT
/// </summary>
public class SatSoapResponseParser
{
    private readonly ILogger<SatSoapResponseParser> _logger;

    public SatSoapResponseParser(ILogger<SatSoapResponseParser> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Parsea la respuesta de SolicitaDescargaEmitidos
    /// </summary>
    public (string IdSolicitud, string RfcSolicitante, string CodEstatus, string Mensaje) ParseSolicitaDescargaEmitidosResponse(string xmlResponse)
    {
        try
        {
            var doc = XDocument.Parse(xmlResponse);
            var ns = XNamespace.Get("http://DescargaMasivaTerceros.sat.gob.mx");
            var soapNs = XNamespace.Get("http://schemas.xmlsoap.org/soap/envelope/");

            var result = doc.Descendants(ns + "SolicitaDescargaEmitidosResult").FirstOrDefault();
            if (result == null)
            {
                throw new InvalidOperationException("No se encontró SolicitaDescargaEmitidosResult en la respuesta");
            }

            return (
                IdSolicitud: result.Attribute("IdSolicitud")?.Value ?? string.Empty,
                RfcSolicitante: result.Attribute("RfcSolicitante")?.Value ?? string.Empty,
                CodEstatus: result.Attribute("CodEstatus")?.Value ?? string.Empty,
                Mensaje: result.Attribute("Mensaje")?.Value ?? string.Empty
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al parsear respuesta de SolicitaDescargaEmitidos");
            throw;
        }
    }

    /// <summary>
    /// Parsea la respuesta de SolicitaDescargaRecibidos
    /// </summary>
    public (string IdSolicitud, string RfcSolicitante, string CodEstatus, string Mensaje) ParseSolicitaDescargaRecibidosResponse(string xmlResponse)
    {
        try
        {
            var doc = XDocument.Parse(xmlResponse);
            var ns = XNamespace.Get("http://DescargaMasivaTerceros.sat.gob.mx");
            var soapNs = XNamespace.Get("http://schemas.xmlsoap.org/soap/envelope/");

            var result = doc.Descendants(ns + "SolicitaDescargaRecibidosResult").FirstOrDefault();
            if (result == null)
            {
                throw new InvalidOperationException("No se encontró SolicitaDescargaRecibidosResult en la respuesta");
            }

            return (
                IdSolicitud: result.Attribute("IdSolicitud")?.Value ?? string.Empty,
                RfcSolicitante: result.Attribute("RfcSolicitante")?.Value ?? string.Empty,
                CodEstatus: result.Attribute("CodEstatus")?.Value ?? string.Empty,
                Mensaje: result.Attribute("Mensaje")?.Value ?? string.Empty
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al parsear respuesta de SolicitaDescargaRecibidos");
            throw;
        }
    }

    /// <summary>
    /// Parsea la respuesta de VerificaSolicitudDescarga
    /// </summary>
    public (int EstadoSolicitud, string CodigoEstadoSolicitud, int NumeroCFDIS, string CodEstatus, string Mensaje, List<string> IdsPaquetes) ParseVerificaSolicitudDescargaResponse(string xmlResponse)
    {
        try
        {
            // Log de la respuesta XML completa del SAT para debugging
            _logger.LogInformation("=== RESPUESTA XML COMPLETA DEL SAT (VerificaSolicitudDescarga) ===");
            _logger.LogInformation("XML Response:\n{XmlResponse}", xmlResponse);
            
            var doc = XDocument.Parse(xmlResponse);
            var ns = XNamespace.Get("http://DescargaMasivaTerceros.sat.gob.mx");

            var result = doc.Descendants(ns + "VerificaSolicitudDescargaResult").FirstOrDefault();
            if (result == null)
            {
                throw new InvalidOperationException("No se encontró VerificaSolicitudDescargaResult en la respuesta");
            }

            // Log de todos los atributos del resultado
            _logger.LogInformation("=== ATRIBUTOS DE VerificaSolicitudDescargaResult ===");
            foreach (var attr in result.Attributes())
            {
                _logger.LogInformation("Atributo: {Name} = {Value}", attr.Name.LocalName, attr.Value);
            }

            var estadoSolicitud = int.TryParse(result.Attribute("EstadoSolicitud")?.Value, out var estado) ? estado : 0;
            var codigoEstadoSolicitud = result.Attribute("CodigoEstadoSolicitud")?.Value ?? string.Empty;
            
            // CRÍTICO: El SAT devuelve "NumeroCFDIs" (con 's' mayúscula), no "NumeroCFDIS"
            // Intentar ambos nombres para compatibilidad
            var numeroCFDIS = 0;
            if (result.Attribute("NumeroCFDIs") != null)
            {
                numeroCFDIS = int.TryParse(result.Attribute("NumeroCFDIs")?.Value, out var num) ? num : 0;
                _logger.LogInformation("NumeroCFDIs encontrado (con 's'): {Value}", numeroCFDIS);
            }
            else if (result.Attribute("NumeroCFDIS") != null)
            {
                numeroCFDIS = int.TryParse(result.Attribute("NumeroCFDIS")?.Value, out var num) ? num : 0;
                _logger.LogInformation("NumeroCFDIS encontrado (sin 's'): {Value}", numeroCFDIS);
            }
            
            var codEstatus = result.Attribute("CodEstatus")?.Value ?? string.Empty;
            var mensaje = result.Attribute("Mensaje")?.Value ?? string.Empty;

            _logger.LogInformation("Valores parseados: EstadoSolicitud={EstadoSolicitud}, NumeroCFDIS={NumeroCFDIS}, CodEstatus={CodEstatus}, Mensaje={Mensaje}",
                estadoSolicitud, numeroCFDIS, codEstatus, mensaje);

            var idsPaquetes = new List<string>();
            
            // CRÍTICO: El SAT puede devolver IdsPaquetes de dos formas:
            // 1. Múltiples elementos <IdsPaquetes>ID</IdsPaquetes> (según manual)
            // 2. Un solo elemento <IdsPaquetes>ID</IdsPaquetes> con el ID como texto directo
            // 3. Un elemento <IdsPaquetes> con elementos hijos <idPaquete>ID</idPaquete>
            
            // Buscar todos los elementos <IdsPaquetes> (puede haber múltiples)
            var idsPaquetesElements = result.Elements(ns + "IdsPaquetes").ToList();
            
            if (idsPaquetesElements.Any())
            {
                _logger.LogInformation("=== ELEMENTOS IdsPaquetes ENCONTRADOS: {Count} ===", idsPaquetesElements.Count);
                
                foreach (var idsPaqueteElement in idsPaquetesElements)
                {
                    _logger.LogInformation("IdsPaquetes XML:\n{IdsPaquetesXml}", idsPaqueteElement.ToString());
                    
                    // Primero intentar extraer el texto directo del elemento
                    var textoDirecto = idsPaqueteElement.Value?.Trim();
                    if (!string.IsNullOrWhiteSpace(textoDirecto))
                    {
                        _logger.LogInformation("ID de paquete encontrado como texto directo: {IdPaquete}", textoDirecto);
                        idsPaquetes.Add(textoDirecto);
                    }
                    
                    // También buscar elementos hijos <idPaquete> o <IdPaquete> (por si acaso)
                    var elementosHijos = idsPaqueteElement.Elements()
                        .Where(e => e.Name.LocalName.Equals("idPaquete", StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    
                    if (elementosHijos.Any())
                    {
                        _logger.LogInformation("Elementos hijos <idPaquete> encontrados: {Count}", elementosHijos.Count);
                        foreach (var elem in elementosHijos)
                        {
                            var idPaquete = elem.Value?.Trim();
                            if (!string.IsNullOrWhiteSpace(idPaquete))
                            {
                                _logger.LogInformation("ID de paquete encontrado en elemento hijo: {IdPaquete}", idPaquete);
                                if (!idsPaquetes.Contains(idPaquete))
                                {
                                    idsPaquetes.Add(idPaquete);
                                }
                            }
                        }
                    }
                }
                    
                _logger.LogInformation("Total de IDs de paquetes extraídos: {Count}", idsPaquetes.Count);
            }
            else
            {
                _logger.LogWarning("=== ELEMENTOS IdsPaquetes NO ENCONTRADOS EN LA RESPUESTA ===");
                _logger.LogWarning("Elementos hijos de VerificaSolicitudDescargaResult:");
                foreach (var child in result.Elements())
                {
                    _logger.LogWarning("Hijo: {Name}, Namespace: {Namespace}, Value: {Value}", 
                        child.Name.LocalName, child.Name.NamespaceName, child.Value);
                }
            }

            return (estadoSolicitud, codigoEstadoSolicitud, numeroCFDIS, codEstatus, mensaje, idsPaquetes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al parsear respuesta de VerificaSolicitudDescarga. XML: {XmlResponse}", xmlResponse);
            throw;
        }
    }

    /// <summary>
    /// Parsea la respuesta de PeticionDescargaMasivaTerceros
    /// Según documentación SAT, la respuesta exitosa contiene:
    /// - RespuestaDescargaMasivaTercerosSalida (elemento raíz)
    /// - Paquete (elemento hijo con contenido base64, NO atributo)
    /// - CodEstatus y Mensaje pueden estar en el Header o en el Body
    /// </summary>
    public (byte[] Paquete, string CodEstatus, string Mensaje) ParsePeticionDescargaMasivaResponse(string xmlResponse)
    {
        try
        {
            var doc = XDocument.Parse(xmlResponse);
            var ns = XNamespace.Get("http://DescargaMasivaTerceros.sat.gob.mx");

            // Buscar RespuestaDescargaMasivaTercerosSalida (según documentación SAT)
            var respuesta = doc.Descendants(ns + "RespuestaDescargaMasivaTercerosSalida").FirstOrDefault();
            if (respuesta == null)
            {
                // Fallback: buscar RespuestaDescargaMasivaTercerosResult (versión anterior)
                var result = doc.Descendants(ns + "RespuestaDescargaMasivaTercerosResult").FirstOrDefault();
                if (result == null)
                {
                    _logger.LogWarning("No se encontró RespuestaDescargaMasivaTercerosSalida ni RespuestaDescargaMasivaTercerosResult en la respuesta");
                    _logger.LogDebug("Respuesta XML completa: {Xml}", xmlResponse);
                    throw new InvalidOperationException("No se encontró respuesta válida en la respuesta del SAT");
                }
                respuesta = result;
            }

            // El Paquete es un ELEMENTO hijo, no un atributo
            var paqueteElement = respuesta.Element(ns + "Paquete");
            var paqueteBase64 = paqueteElement?.Value ?? string.Empty;

            // CodEstatus y Mensaje pueden estar en el Header o como atributos del elemento respuesta
            // Primero intentar en el Header
            var header = doc.Descendants(XName.Get("Header", "http://schemas.xmlsoap.org/soap/envelope/")).FirstOrDefault();
            var respuestaHeader = header?.Descendants().FirstOrDefault(e => e.Name.LocalName.Equals("respuesta", StringComparison.OrdinalIgnoreCase));
            
            var codEstatus = respuestaHeader?.Attribute("CodEstatus")?.Value 
                          ?? respuesta.Attribute("CodEstatus")?.Value 
                          ?? string.Empty;
            
            var mensaje = respuestaHeader?.Attribute("Mensaje")?.Value 
                       ?? respuesta.Attribute("Mensaje")?.Value 
                       ?? string.Empty;

            _logger.LogInformation("Respuesta de descarga parseada. CodEstatus: {CodEstatus}, Mensaje: {Mensaje}, PaqueteLength: {Length}", 
                codEstatus, mensaje, paqueteBase64.Length);

            byte[] paqueteContent = !string.IsNullOrEmpty(paqueteBase64) 
                ? Convert.FromBase64String(paqueteBase64) 
                : Array.Empty<byte>();

            return (paqueteContent, codEstatus, mensaje);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al parsear respuesta de PeticionDescargaMasivaTerceros. XML: {Xml}", xmlResponse);
            throw;
        }
    }

    /// <summary>
    /// Mapea el EstadoSolicitud del SAT (Int) al CodigoEstado de nuestro sistema
    /// </summary>
    public int MapEstadoSolicitudToCodigoEstado(int estadoSolicitud)
    {
        // Mapeo según ESPECIFICACIONES_COMPLETAS_SAT_WS.md
        // EstadoSolicitud (SAT): 1=Aceptada, 2=En Proceso, 3=Terminada, 4=Error, 5=Rechazada, 6=Vencida
        // CodigoEstado (SICOE): 0=Pendiente, 1=EnProceso, 2=Completada, 3=Error, 4=Cancelada
        return estadoSolicitud switch
        {
            1 => 0, // Aceptada -> Pendiente
            2 => 1, // En Proceso -> EnProceso
            3 => 2, // Terminada -> Completada
            4 => 3, // Error -> Error
            5 => 4, // Rechazada -> Cancelada
            6 => 4, // Vencida -> Cancelada
            _ => 3  // Desconocido -> Error
        };
    }
}

