using Hangfire;
using Microsoft.Extensions.Logging;
using SICOE.Application.Interfaces.Services;
using SICOE.Infrastructure.BackgroundJobs.Descarga;

namespace SICOE.Infrastructure.MessageQueue.Hangfire;

/// <summary>
/// Implementación de IMessageQueueService usando Hangfire
/// </summary>
public class HangfireQueueService : IMessageQueueService
{
    private readonly ILogger<HangfireQueueService> _logger;

    public HangfireQueueService(ILogger<HangfireQueueService> logger)
    {
        _logger = logger;
    }

    public void EncolarVerificacionDescarga(int solicitudId, TimeSpan? delay = null)
    {
        try
        {
            if (delay.HasValue)
            {
                BackgroundJob.Schedule<VerificarDescargaJob>(
                    x => x.VerificarSolicitudAsync(solicitudId, CancellationToken.None),
                    delay.Value);
                
                _logger.LogInformation("Job de verificación programado. SolicitudId={SolicitudId}, Delay={Delay}", 
                    solicitudId, delay.Value);
            }
            else
            {
                BackgroundJob.Enqueue<VerificarDescargaJob>(
                    x => x.VerificarSolicitudAsync(solicitudId, CancellationToken.None));
                
                _logger.LogInformation("Job de verificación encolado. SolicitudId={SolicitudId}", solicitudId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al encolar job de verificación. SolicitudId={SolicitudId}", solicitudId);
            throw;
        }
    }

    public void EncolarProcesamientoCFDI(int solicitudId, byte[]? certificadoFiel = null, string? passwordFiel = null)
    {
        try
        {
            BackgroundJob.Enqueue<ProcesarCFDIJob>(
                x => x.ProcesarSolicitudAsync(solicitudId, CancellationToken.None));
            
            _logger.LogInformation("Job de procesamiento CFDI encolado. SolicitudId={SolicitudId}", solicitudId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al encolar job de procesamiento. SolicitudId={SolicitudId}", solicitudId);
            throw;
        }
    }

    public Task EnqueueAsync(string queueName, object message, CancellationToken cancellationToken = default)
    {
        // Implementación genérica para futuras necesidades
        _logger.LogWarning("Método genérico EnqueueAsync no implementado. QueueName={QueueName}", queueName);
        return Task.CompletedTask;
    }

    public Task ScheduleAsync(string queueName, object message, DateTime scheduleAt, CancellationToken cancellationToken = default)
    {
        // Implementación genérica para futuras necesidades
        _logger.LogWarning("Método genérico ScheduleAsync no implementado. QueueName={QueueName}", queueName);
        return Task.CompletedTask;
    }
}

