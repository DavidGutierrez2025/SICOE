using MediatR;
using SICOE.Application.Common;

namespace SICOE.Application.UseCases.Descarga.SolicitarDescarga;

/// <summary>
/// Command para solicitar una descarga masiva de CFDI al SAT
/// Requiere certificado FIEL para autenticación
/// NOTA: El SAT de México solicita .cer y .key por separado (NO .pfx)
/// </summary>
public class SolicitarDescargaCommand : IRequest<Result<SolicitarDescargaResponse>>
{
    /// <summary>
    /// ID del cliente que realiza la solicitud
    /// </summary>
    public int ClienteId { get; set; }
    
    /// <summary>
    /// Certificado .cer en formato base64 (formato solicitado por el SAT)
    /// OPCIONAL: Si no se proporciona, se intentará usar el token SAT almacenado.
    /// NOTA: El frontend debe enviar automáticamente desde sessionStorage.
    /// </summary>
    public string? CertificadoCer { get; set; }
    
    /// <summary>
    /// Clave privada .key en formato base64 (formato solicitado por el SAT)
    /// OPCIONAL: Si no se proporciona, se intentará usar el token SAT almacenado.
    /// NOTA: El frontend debe enviar automáticamente desde sessionStorage.
    /// </summary>
    public string? ClavePrivadaKey { get; set; }
    
    /// <summary>
    /// Contraseña de la clave privada FIEL
    /// OPCIONAL: Si no se proporciona, se intentará usar el token SAT almacenado.
    /// NOTA: El frontend debe enviar automáticamente desde sessionStorage.
    /// </summary>
    public string? PasswordFiel { get; set; }
    
    /// <summary>
    /// Fecha inicial del rango de búsqueda (formato: YYYY-MM-DD o ISO 8601)
    /// </summary>
    public DateTime FechaInicial { get; set; }
    
    /// <summary>
    /// Fecha final del rango de búsqueda (formato: YYYY-MM-DD o ISO 8601)
    /// </summary>
    public DateTime FechaFinal { get; set; }
    
    /// <summary>
    /// Tipo de solicitud: "CFDI" o "Metadata". Obligatorio según documentación del SAT.
    /// </summary>
    public string TipoSolicitud { get; set; } = "CFDI";
    
    /// <summary>
    /// Tipo de comprobante (I=Ingreso, E=Egreso, T=Traslado, N=Nómina, P=Pago). Opcional.
    /// </summary>
    public string? TipoComprobante { get; set; }
    
    /// <summary>
    /// Estado del comprobante: "Vigente", "Cancelado", "Todos". Opcional. Por defecto "Vigente".
    /// </summary>
    public string? EstadoComprobante { get; set; }
    
    /// <summary>
    /// RFC del emisor. Si no se proporciona, se usa el RFC del solicitante.
    /// </summary>
    public string? RfcEmisor { get; set; }
    
    /// <summary>
    /// RFC del receptor. 
    /// Para Recibidos: no se usa (se usa SolicitaDescargaRecibidos).
    /// Para Emitidos: opcional, filtra por receptor específico (se usa SolicitaDescargaEmitidos).
    /// </summary>
    public string? RfcReceptor { get; set; }
    
    /// <summary>
    /// Tipo de descarga: "Recibidos" o "Emitidos". Si no se proporciona, se infiere por RfcReceptor.
    /// </summary>
    public string? TipoDescarga { get; set; }
    
    /// <summary>
    /// Complemento de CFDI a descargar. Opcional.
    /// </summary>
    public string? Complemento { get; set; }
    
    /// <summary>
    /// RFC a cuenta de terceros. Opcional.
    /// </summary>
    public string? RfcACuentaTerceros { get; set; }
    
    /// <summary>
    /// Lista de UUIDs específicos a descargar. Opcional (para SolicitaDescargaFolio).
    /// </summary>
    public List<string>? Uuids { get; set; }
}

/// <summary>
/// Respuesta del caso de uso SolicitarDescarga
/// </summary>
public class SolicitarDescargaResponse
{
    public int SolicitudId { get; set; }
    public string IdSolicitudSat { get; set; } = string.Empty;
    public int CodigoEstado { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public DateTime? FechaEstimadaTermino { get; set; }
}

