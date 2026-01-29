using MediatR;
using SICOE.Application.Common;

namespace SICOE.Application.UseCases.Descarga.VerificarDescarga;

/// <summary>
/// Command para verificar el estado de una solicitud de descarga en el SAT
/// </summary>
public class VerificarDescargaCommand : IRequest<Result<VerificarDescargaResponse>>
{
    public int SolicitudId { get; set; }
    public byte[]? CertificadoFiel { get; set; } // Opcional: solo si el token almacenado expiró
    public string? PasswordFiel { get; set; } // Opcional: solo si el token almacenado expiró
}

/// <summary>
/// Respuesta del caso de uso VerificarDescarga
/// </summary>
public class VerificarDescargaResponse
{
    public int SolicitudId { get; set; }
    public int CodigoEstado { get; set; } // 0: Aceptada, 1: En proceso, 2: Terminada, 3: Error, 4: Rechazada, 5: Vencida
    public string Mensaje { get; set; } = string.Empty;
    public int TotalPaquetes { get; set; }
    public List<string> IdsPaquetes { get; set; } = new();
    public int? TotalCFDIs { get; set; }
    public bool RequiereProcesamiento { get; set; }
}

