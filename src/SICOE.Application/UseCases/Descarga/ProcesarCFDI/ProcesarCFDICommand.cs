using MediatR;
using SICOE.Application.Common;

namespace SICOE.Application.UseCases.Descarga.ProcesarCFDI;

/// <summary>
/// Command para procesar y guardar CFDI's descargados del SAT
/// Integra SatService y ArchivoService para descargar y almacenar CFDI's XML
/// </summary>
public class ProcesarCFDICommand : IRequest<Result<ProcesarCFDIResponse>>
{
    public int SolicitudId { get; set; }
    public byte[]? CertificadoFiel { get; set; } // Opcional: solo si el token almacenado expiró
    public string? PasswordFiel { get; set; } // Opcional: solo si el token almacenado expiró
}

/// <summary>
/// Respuesta del caso de uso ProcesarCFDI
/// </summary>
public class ProcesarCFDIResponse
{
    public int SolicitudId { get; set; }
    public int TotalCFDIsProcesados { get; set; }
    public int TotalArchivosGuardados { get; set; }
    public List<string> Errores { get; set; } = new();
    public bool Completado { get; set; }
    
    // Información de conciliación
    public bool ConciliacionRealizada { get; set; }
    public int TotalFaltantes { get; set; }
    public int? NuevaSolicitudId { get; set; } // ID de la nueva solicitud creada para faltantes (si aplica)
}

