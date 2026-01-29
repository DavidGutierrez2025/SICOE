using Hangfire;
using MediatR;
using Microsoft.Extensions.Logging;
using SICOE.Application.UseCases.Descarga.VerificarDescarga;

namespace SICOE.Infrastructure.BackgroundJobs.Descarga;

/// <summary>
/// Background Job de Hangfire para verificar el estado de descargas pendientes en el SAT
/// Se ejecuta periódicamente para verificar solicitudes en proceso
/// </summary>
public class VerificarDescargaJob
{
    private readonly IMediator _mediator;
    private readonly ILogger<VerificarDescargaJob> _logger;

    public VerificarDescargaJob(
        IMediator mediator,
        ILogger<VerificarDescargaJob> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Verifica el estado de una solicitud específica en el SAT
    /// Usa token SAT almacenado temporalmente, no requiere FIEL
    /// </summary>
    /// <param name="solicitudId">ID de la solicitud de descarga</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 900 })]
    public async Task VerificarSolicitudAsync(
        int solicitudId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Iniciando verificación de descarga. SolicitudId={SolicitudId}", solicitudId);

            var command = new VerificarDescargaCommand
            {
                SolicitudId = solicitudId
            };

            var result = await _mediator.Send(command, cancellationToken);

            if (result.IsFailure)
            {
                _logger.LogError("Error al verificar descarga. SolicitudId={SolicitudId}, Error={Error}", 
                    solicitudId, result.Error);
                throw new Exception($"Error al verificar descarga: {result.Error}");
            }

            _logger.LogInformation(
                "Verificación de descarga completada. SolicitudId={SolicitudId}, Estado={Estado}, TotalPaquetes={TotalPaquetes}",
                solicitudId, result.Value.CodigoEstado, result.Value.TotalPaquetes);

            // Si la descarga está terminada, encolar job de procesamiento
            if (result.Value.CodigoEstado == 2 && result.Value.RequiereProcesamiento) // 2: Terminada
            {
                _logger.LogInformation("Descarga terminada, encolando job de procesamiento. SolicitudId={SolicitudId}", solicitudId);
                BackgroundJob.Enqueue<ProcesarCFDIJob>(x => 
                    x.ProcesarSolicitudAsync(solicitudId, cancellationToken));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al verificar descarga. SolicitudId={SolicitudId}", solicitudId);
            throw; // Hangfire reintentará según configuración
        }
    }

    /// <summary>
    /// Verifica todas las solicitudes pendientes o en proceso
    /// Job recurrente que se ejecuta cada X minutos
    /// </summary>
    [AutomaticRetry(Attempts = 1)] // No reintentar el job completo, solo las verificaciones individuales
    public Task VerificarSolicitudesPendientesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Iniciando verificación de todas las solicitudes pendientes");

            // Este job necesita acceso al repositorio para obtener solicitudes pendientes
            // Por ahora, este método será llamado por un job recurrente que obtiene las solicitudes
            // y llama a VerificarSolicitudAsync para cada una

            _logger.LogInformation("Verificación de solicitudes pendientes completada");
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al verificar solicitudes pendientes");
            throw;
        }
    }
}

