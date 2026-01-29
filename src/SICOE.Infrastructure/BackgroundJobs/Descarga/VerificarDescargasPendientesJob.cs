using Hangfire;
using Microsoft.Extensions.Logging;
using SICOE.Application.Interfaces.UnitOfWork;
using SICOE.Domain.Enums;

namespace SICOE.Infrastructure.BackgroundJobs.Descarga;

/// <summary>
/// Background Job recurrente de Hangfire para verificar todas las solicitudes pendientes o en proceso
/// Se ejecuta periódicamente (cada 10 minutos por defecto)
/// </summary>
public class VerificarDescargasPendientesJob
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<VerificarDescargasPendientesJob> _logger;

    public VerificarDescargasPendientesJob(
        IUnitOfWork unitOfWork,
        ILogger<VerificarDescargasPendientesJob> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Verifica todas las solicitudes pendientes o en proceso
    /// Encola jobs individuales de verificación para cada solicitud
    /// </summary>
    [AutomaticRetry(Attempts = 1)]
    public async Task VerificarTodasLasSolicitudesPendientesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Iniciando verificación de todas las solicitudes pendientes/en proceso");

            // Obtener todas las solicitudes pendientes o en proceso que tengan IdSolicitudSat
            var solicitudes = await _unitOfWork.SolicitudesDescarga.GetPendientesOEnProcesoAsync(cancellationToken);

            if (!solicitudes.Any())
            {
                _logger.LogInformation("No hay solicitudes pendientes o en proceso para verificar");
                return;
            }

            _logger.LogInformation("Encontradas {Total} solicitudes para verificar", solicitudes.Count());

            // Identificar solicitudes que necesitan verificación
            // NOTA: Los jobs de verificación requieren certificado FIEL como parámetro
            // Este job solo identifica las solicitudes pendientes
            // La verificación real se debe hacer desde el endpoint con FIEL del usuario
            
            var solicitudesParaVerificar = solicitudes
                .Where(s => !string.IsNullOrWhiteSpace(s.IdSolicitudSat))
                .ToList();

            _logger.LogInformation(
                "Solicitudes pendientes identificadas: {Total}. " +
                "Nota: La verificación requiere certificado FIEL y debe hacerse desde el endpoint.",
                solicitudesParaVerificar.Count);

            // TODO: En futuras versiones, implementar almacenamiento temporal de tokens SAT
            // para permitir verificación automática sin requerir FIEL en cada ejecución
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al verificar solicitudes pendientes");
            throw;
        }
    }
}

