using System.Security.Cryptography.X509Certificates;
using SICOE.Application.Common;

namespace SICOE.Application.Interfaces.Services;

/// <summary>
/// Interface específica para servicio SAT (ISP: Interface específica para SAT)
/// Maneja comunicación con Web Services del SAT para descarga de CFDI's
/// 
/// NOTA: Los métodos que requieren firma digital aceptan un certificado opcional.
/// El certificado se usa SOLO para firmar el mensaje SOAP y se descarta inmediatamente.
/// Si no se proporciona, se intentará sin firma (puede fallar si el SAT la requiere).
/// </summary>
public interface ISatService
{
    /// <summary>
    /// Solicita descarga masiva de CFDI's al SAT usando token de autenticación
    /// </summary>
    /// <param name="certificate">Certificado FIEL para firmar el mensaje SOAP. Se usa solo para firmar y se descarta inmediatamente.</param>
    Task<Result<SolicitudDescargaResponse>> SolicitarDescargaMasivaAsync(
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
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica el estado de una solicitud de descarga en el SAT
    /// </summary>
    /// <param name="certificate">Certificado FIEL para firmar el mensaje SOAP. Se usa solo para firmar y se descarta inmediatamente.</param>
    Task<Result<VerificacionDescargaResponse>> VerificarDescargaAsync(
        string tokenSat,
        string idSolicitud,
        string rfcSolicitante,
        X509Certificate2? certificate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Descarga los paquetes ZIP con CFDI's XML del SAT
    /// </summary>
    /// <param name="idsPaquetes">IDs de paquetes obtenidos previamente. Si se proporcionan, se usan directamente sin verificar el estado.</param>
    Task<Result<IEnumerable<CFDIPaquete>>> DescargarPaquetesAsync(
        string tokenSat,
        string idSolicitud,
        string rfcSolicitante,
        X509Certificate2? certificate = null,
        List<string>? idsPaquetes = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Respuesta de solicitud de descarga masiva al SAT
/// </summary>
public class SolicitudDescargaResponse
{
    public string IdSolicitud { get; set; } = string.Empty;
    public int CodigoEstado { get; set; } // 0: Aceptada, 1: En proceso, 2: Terminada, 3: Error, 4: Rechazada, 5: Vencida
    public string Mensaje { get; set; } = string.Empty;
    public int? TotalSolicitado { get; set; }
    public DateTime? FechaEstimadaTermino { get; set; }
}

/// <summary>
/// Respuesta de verificación de descarga del SAT
/// </summary>
public class VerificacionDescargaResponse
{
    public int CodigoEstado { get; set; } // 0: Aceptada, 1: En proceso, 2: Terminada, 3: Error, 4: Rechazada, 5: Vencida
    public string Mensaje { get; set; } = string.Empty;
    public int TotalPaquetes { get; set; }
    public List<string> IdsPaquetes { get; set; } = new();
    public int? TotalCFDIs { get; set; }
}

/// <summary>
/// Paquete ZIP descargado del SAT que contiene múltiples CFDI's XML
/// </summary>
public class CFDIPaquete
{
    public string IdPaquete { get; set; } = string.Empty;
    public byte[] ContenidoZip { get; set; } = Array.Empty<byte>();
    public List<CFDIXml> CFDIs { get; set; } = new();
    public DateTime FechaDescarga { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// CFDI XML extraído de un paquete del SAT
/// Contiene toda la información relevante del comprobante fiscal
/// </summary>
public class CFDIXml
{
    // Información básica del comprobante
    public string Uuid { get; set; } = string.Empty;
    public byte[] ContenidoXml { get; set; } = Array.Empty<byte>();
    public DateTime FechaEmision { get; set; }
    public DateTime? FechaTimbrado { get; set; }
    public decimal Total { get; set; }
    public decimal SubTotal { get; set; }
    public string TipoComprobante { get; set; } = string.Empty;
    public string? Serie { get; set; }
    public string? Folio { get; set; }
    public string? FormaPago { get; set; }
    public string? MetodoPago { get; set; }
    public string Moneda { get; set; } = "MXN";
    public string? LugarExpedicion { get; set; }

    // Información del Emisor
    public string RfcEmisor { get; set; } = string.Empty;
    public string? NombreEmisor { get; set; }
    public string? RegimenFiscalEmisor { get; set; }

    // Información del Receptor
    public string RfcReceptor { get; set; } = string.Empty;
    public string? NombreReceptor { get; set; }
    public string? RegimenFiscalReceptor { get; set; }
    public string? DomicilioFiscalReceptor { get; set; }
    public string? UsoCFDI { get; set; }

    // Información de Impuestos
    public decimal TotalImpuestosTrasladados { get; set; }
    
    // TODO: En el futuro se pueden agregar:
    // - Lista de Conceptos detallados
    // - Lista de Impuestos detallados (IVA, IEPS, etc.)
    // - Complementos adicionales (Pagos, Nómina, etc.)
}

