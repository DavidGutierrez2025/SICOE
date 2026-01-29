namespace SICOE.Domain.Enums;

/// <summary>
/// Estados de conciliación de CFDI
/// </summary>
public enum EstadoConciliacion
{
    Pendiente = 0,      // Pendiente de conciliación
    EnProceso = 1,      // Conciliación en curso
    Completada = 2,     // Conciliación completada (todo coincide)
    ConFaltantes = 3,   // Hay faltantes detectados
    Error = 4,          // Error en la conciliación
    Cancelada = 5       // Conciliación cancelada
}

