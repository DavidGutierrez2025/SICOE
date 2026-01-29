using Hangfire;
using MediatR;
using Microsoft.Extensions.Logging;
using SICOE.Application.UseCases.Descarga.ProcesarCFDI;

namespace SICOE.Infrastructure.BackgroundJobs.Descarga;

/// <summary>
/// Background Job de Hangfire para procesar y guardar CFDI's descargados del SAT
/// Se ejecuta cuando una descarga está terminada y lista para procesar
/// </summary>
public class ProcesarCFDIJob
{
    private readonly IMediator _mediator;
    private readonly ILogger<ProcesarCFDIJob> _logger;

    public ProcesarCFDIJob(
        IMediator mediator,
        ILogger<ProcesarCFDIJob> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Procesa una solicitud de descarga específica
    /// Descarga paquetes, extrae XML y guarda archivos
    /// Usa token SAT almacenado temporalmente, no requiere FIEL
    /// </summary>
    /// <param name="solicitudId">ID de la solicitud de descarga</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 900 })]
    public async Task ProcesarSolicitudAsync(
        int solicitudId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Iniciando procesamiento de CFDI's. SolicitudId={SolicitudId}", solicitudId);

            var command = new ProcesarCFDICommand
            {
                SolicitudId = solicitudId
                // CertificadoFiel y PasswordFiel son opcionales, se usarán solo si el token expiró
            };

            var result = await _mediator.Send(command, cancellationToken);

            if (result.IsFailure)
            {
                _logger.LogError("Error al procesar CFDI's. SolicitudId={SolicitudId}, Error={Error}", 
                    solicitudId, result.Error);
                throw new Exception($"Error al procesar CFDI's: {result.Error}");
            }

            _logger.LogInformation(
                "Procesamiento de CFDI's completado. SolicitudId={SolicitudId}, TotalProcesados={Total}, ArchivosGuardados={Archivos}, Errores={Errores}",
                solicitudId, result.Value.TotalCFDIsProcesados, result.Value.TotalArchivosGuardados, result.Value.Errores.Count);

            // Si hay errores pero se procesaron algunos, loguear advertencia
            if (result.Value.Errores.Any() && result.Value.TotalCFDIsProcesados > 0)
            {
                _logger.LogWarning(
                    "Procesamiento completado con errores. SolicitudId={SolicitudId}, Errores={Errores}",
                    solicitudId, string.Join("; ", result.Value.Errores));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al procesar CFDI's. SolicitudId={SolicitudId}", solicitudId);
            throw; // Hangfire reintentará según configuración
        }
    }

}

