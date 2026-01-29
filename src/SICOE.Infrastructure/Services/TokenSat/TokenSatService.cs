using Microsoft.Extensions.Logging;
using SICOE.Application.Common;
using SICOE.Application.Interfaces.Repositories;
using SICOE.Application.Interfaces.Services;
using SICOE.Application.Interfaces.UnitOfWork;
using TokenSatEntity = SICOE.Domain.Entities.TokenSat;

namespace SICOE.Infrastructure.Services.TokenSat;

/// <summary>
/// Implementación del servicio para gestión de tokens SAT
/// </summary>
public class TokenSatService : ITokenSatService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<TokenSatService> _logger;

    public TokenSatService(
        IUnitOfWork unitOfWork,
        IEncryptionService encryptionService,
        ILogger<TokenSatService> logger)
    {
        _unitOfWork = unitOfWork;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public async Task<Result<string?>> ObtenerTokenValidoAsync(int clienteId, CancellationToken cancellationToken = default)
    {
        try
        {
            var token = await _unitOfWork.TokensSat.GetTokenValidoPorClienteAsync(clienteId, cancellationToken);

            if (token == null)
            {
                _logger.LogInformation("No se encontró token válido para ClienteId={ClienteId}", clienteId);
                return Result<string?>.Success(null);
            }

            _logger.LogInformation("Token válido encontrado para ClienteId={ClienteId}, Expira={FechaExpiracion}", 
                clienteId, token.FechaExpiracion);

            // Desencriptar token antes de retornarlo
            try
            {
                var decryptedToken = _encryptionService.Decrypt(token.Token);
                // CRÍTICO: Limpiar el token - eliminar saltos de línea y tabulaciones, pero MANTENER &wrap_subject
                // IMPORTANTE: El token del SAT DEBE incluir &wrap_subject=... como parte del token completo
                // El formato correcto según documentación SAT es: JWT&wrap_subject=valor
                // NO eliminar el &wrap_subject, solo limpiar caracteres de control
                var tokenLimpio = decryptedToken?.Trim()
                    .Replace("\r", string.Empty)
                    .Replace("\n", string.Empty)
                    .Replace("\t", string.Empty) ?? string.Empty;
                // NO eliminar espacios - el token puede tener espacios válidos antes de &wrap_subject
                
                if (string.IsNullOrWhiteSpace(tokenLimpio))
                {
                    _logger.LogError("El token desencriptado quedó vacío después de limpiarlo. El token almacenado puede estar corrupto.");
                    token.Desactivar();
                    await _unitOfWork.TokensSat.UpdateAsync(token, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    return Result<string?>.Failure("El token almacenado está corrupto. Se requiere obtener un nuevo token.");
                }
                
                _logger.LogDebug("Token desencriptado y limpiado. Longitud original: {OriginalLength}, Longitud limpia: {CleanLength}", 
                    decryptedToken?.Length ?? 0, tokenLimpio.Length);
                return Result<string?>.Success(tokenLimpio);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al desencriptar token para ClienteId={ClienteId}. El token puede estar corrupto.", clienteId);
                // Si el token no se puede desencriptar, marcarlo como inválido
                token.Desactivar();
                await _unitOfWork.TokensSat.UpdateAsync(token, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return Result<string?>.Failure("El token almacenado no se pudo desencriptar. Se requiere obtener un nuevo token.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener token válido para ClienteId={ClienteId}", clienteId);
            return Result<string?>.Failure($"Error al obtener token: {ex.Message}");
        }
    }

    public async Task<Result> GuardarTokenAsync(int clienteId, string token, DateTime fechaExpiracion, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return Result.Failure("El token no puede estar vacío");
            }

            if (fechaExpiracion <= DateTime.UtcNow)
            {
                return Result.Failure("La fecha de expiración debe ser futura");
            }

            // CRÍTICO: Limpiar el token antes de guardarlo - eliminar saltos de línea y tabulaciones, pero MANTENER &wrap_subject
            // IMPORTANTE: El token del SAT DEBE incluir &wrap_subject=... como parte del token completo
            // El formato correcto según documentación SAT es: JWT&wrap_subject=valor
            // NO eliminar el &wrap_subject, solo limpiar caracteres de control
            var tokenLimpio = token.Trim()
                .Replace("\r", string.Empty)
                .Replace("\n", string.Empty)
                .Replace("\t", string.Empty);
            // NO eliminar espacios - el token puede tener espacios válidos antes de &wrap_subject

            if (string.IsNullOrWhiteSpace(tokenLimpio))
            {
                _logger.LogError("El token quedó vacío después de limpiarlo. No se puede guardar un token vacío.");
                return Result.Failure("El token no puede estar vacío después de limpiarlo");
            }

            // Desactivar tokens anteriores del cliente
            await _unitOfWork.TokensSat.DesactivarTokensPorClienteAsync(clienteId, cancellationToken);

            // Encriptar token limpio antes de guardarlo
            var encryptedToken = _encryptionService.Encrypt(tokenLimpio);
            _logger.LogInformation("Token encriptado antes de guardar para ClienteId={ClienteId}", clienteId);

            // Crear nuevo token con token encriptado (ya limpio)
            var tokenSat = new TokenSatEntity(clienteId, encryptedToken, fechaExpiracion);

            await _unitOfWork.TokensSat.AddAsync(tokenSat, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Token SAT guardado para ClienteId={ClienteId}, Expira={FechaExpiracion}",
                clienteId, fechaExpiracion);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al guardar token para ClienteId={ClienteId}", clienteId);
            return Result.Failure($"Error al guardar token: {ex.Message}");
        }
    }

    public async Task<Result> DesactivarTokensClienteAsync(int clienteId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _unitOfWork.TokensSat.DesactivarTokensPorClienteAsync(clienteId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Tokens desactivados para ClienteId={ClienteId}", clienteId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al desactivar tokens para ClienteId={ClienteId}", clienteId);
            return Result.Failure($"Error al desactivar tokens: {ex.Message}");
        }
    }

    public async Task<Result<int>> LimpiarTokensExpiradosAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var tokensExpirados = (await _unitOfWork.TokensSat.GetTokensExpiradosAsync(cancellationToken)).ToList();

            if (!tokensExpirados.Any())
            {
                _logger.LogInformation("No hay tokens expirados para limpiar");
                return Result<int>.Success(0);
            }

            foreach (var token in tokensExpirados)
            {
                await _unitOfWork.TokensSat.DeleteAsync(token, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Tokens expirados eliminados: {Total}", tokensExpirados.Count);
            return Result<int>.Success(tokensExpirados.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al limpiar tokens expirados");
            return Result<int>.Failure($"Error al limpiar tokens: {ex.Message}");
        }
    }
}

