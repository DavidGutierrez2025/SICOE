using MediatR;
using SICOE.Application.Common;
using SICOE.Application.UseCases.Descarga.DescargarPaquete;

namespace SICOE.Application.UseCases.Descarga.DescargarPaquete;

/// <summary>
/// Query para descargar el paquete ZIP de una solicitud completada
/// Requiere certificado FIEL para obtener token SAT dinámicamente
/// </summary>
public class DescargarPaqueteQuery : IRequest<Result<DescargarPaqueteResponse>>
{
    public int SolicitudId { get; set; }
    
    /// <summary>
    /// Certificado FIEL en formato PFX (byte array)
    /// REQUERIDO: El frontend debe enviar automáticamente desde sessionStorage
    /// </summary>
    public byte[]? CertificadoFiel { get; set; }
    
    /// <summary>
    /// Contraseña de la clave privada FIEL
    /// REQUERIDO: El frontend debe enviar automáticamente desde sessionStorage
    /// </summary>
    public string? PasswordFiel { get; set; }
}

