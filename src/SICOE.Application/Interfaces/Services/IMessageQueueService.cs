namespace SICOE.Application.Interfaces.Services;

/// <summary>
/// Interface para servicio de cola de mensajes (abstracción para Hangfire u otros MQ)
/// </summary>
public interface IMessageQueueService
{
    /// <summary>
    /// Encola un job para verificar el estado de una descarga
    /// Usa token SAT almacenado temporalmente
    /// </summary>
    void EncolarVerificacionDescarga(int solicitudId, TimeSpan? delay = null);

    /// <summary>
    /// Encola un job para procesar y guardar CFDI's descargados
    /// </summary>
    void EncolarProcesamientoCFDI(int solicitudId, byte[] certificadoFiel, string passwordFiel);

    // Métodos genéricos para futuras necesidades
    Task EnqueueAsync(string queueName, object message, CancellationToken cancellationToken = default);
    Task ScheduleAsync(string queueName, object message, DateTime scheduleAt, CancellationToken cancellationToken = default);
}

