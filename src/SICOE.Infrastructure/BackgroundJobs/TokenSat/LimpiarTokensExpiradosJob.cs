using Hangfire;
using Microsoft.Extensions.Logging;
using SICOE.Application.Interfaces.Services;

namespace SICOE.Infrastructure.BackgroundJobs.TokenSat;

/// <summary>
/// Background Job recurrente de Hangfire para limpiar tokens SAT expirados
/// Se ejecuta diariamente para mantener la base de datos limpia
/// </summary>
public class LimpiarTokensExpiradosJob
{
    private readonly ITokenSatService _tokenSatService;
    private readonly ILogger<LimpiarTokensExpiradosJob> _logger;

    public LimpiarTokensExpiradosJob(
        ITokenSatService tokenSatService,
        ILogger<LimpiarTokensExpiradosJob> logger)
    {
        _tokenSatService = tokenSatService;
        _logger = logger;
    }

    /// <summary>
    /// Limpia tokens SAT expirados de la base de datos
    /// </summary>
    [AutomaticRetry(Attempts = 2, DelaysInSeconds = new[] { 300, 900 })]
    public async Task LimpiarTokensAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Iniciando limpieza de tokens SAT expirados");

            var result = await _tokenSatService.LimpiarTokensExpiradosAsync(cancellationToken);

            if (result.IsFailure)
            {
                _logger.LogError("Error al limpiar tokens expirados: {Error}", result.Error);
                throw new Exception($"Error al limpiar tokens: {result.Error}");
            }

            _logger.LogInformation("Limpieza de tokens completada. Total eliminados: {Total}", result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al limpiar tokens expirados");
            throw;
        }
    }
}

