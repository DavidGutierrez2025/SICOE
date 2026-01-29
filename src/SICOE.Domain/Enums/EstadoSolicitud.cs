namespace SICOE.Domain.Enums;

/// <summary>
/// Estados de una solicitud de descarga
/// </summary>
public enum EstadoSolicitud
{
    Pendiente = 1,
    EnProceso = 2,
    Completada = 3,
    Error = 4,
    Cancelada = 5
}

