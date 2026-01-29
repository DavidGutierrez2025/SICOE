using MediatR;
using Microsoft.Extensions.Logging;
using SICOE.Application.Common;
using SICOE.Application.Interfaces.Services;
using SICOE.Application.Common.Helpers.Fiel; // Para FielCerKeyHelper

namespace SICOE.Application.UseCases.Autenticacion.AutenticarConFiel;

public class AutenticarConFielHandler : IRequestHandler<AutenticarConFielCommand, Result<AutenticacionResponseDto>>
{
    private readonly IFielService _fielService;
    private readonly ILogger<AutenticarConFielHandler> _logger;

    public AutenticarConFielHandler(
        IFielService fielService,
        ILogger<AutenticarConFielHandler> logger)
    {
        _fielService = fielService;
        _logger = logger;
    }

    public async Task<Result<AutenticacionResponseDto>> Handle(AutenticarConFielCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // 1. Convertir Base64 a Bytes
            byte[] certificadoCerBytes;
            byte[] clavePrivadaKeyBytes;

            try
            {
                certificadoCerBytes = Convert.FromBase64String(request.CertificadoCer);
                clavePrivadaKeyBytes = Convert.FromBase64String(request.ClavePrivadaKey);

                if (certificadoCerBytes.Length == 0) return Result<AutenticacionResponseDto>.Failure("El archivo .cer está vacío.");
                if (clavePrivadaKeyBytes.Length == 0) return Result<AutenticacionResponseDto>.Failure("El archivo .key está vacío.");
            }
            catch (FormatException)
            {
                return Result<AutenticacionResponseDto>.Failure("El formato de los archivos .cer o .key no es válido (Base64 incorrecto).");
            }

            // 2. Combinar .cer y .key en un .pfx temporal
            byte[] certificadoBytes;
            try
            {
                certificadoBytes = FielCerKeyHelper.CombinarCerYKeyEnPfx(
                    certificadoCerBytes,
                    clavePrivadaKeyBytes,
                    request.PasswordFiel);
            }
            catch (System.Security.Cryptography.CryptographicException ex)
            {
                _logger.LogWarning(ex, "Error criptográfico al combinar .cer y .key");
                return Result<AutenticacionResponseDto>.Failure($"Error al procesar archivos FIEL: {ex.Message}. Verifique contraseña.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado en CombinarCerYKeyEnPfx");
                return Result<AutenticacionResponseDto>.Failure($"Error inesperado al procesar archivos: {ex.Message}");
            }

            // 3. Validar FIEL
            // ValidarFielAsync realiza validaciones de vigencia, revocación (si implementado) y cadena de confianza
            var validacionResult = await _fielService.ValidarFielAsync(
                certificadoBytes,
                request.PasswordFiel,
                cancellationToken);

            if (validacionResult.IsFailure)
            {
                return Result<AutenticacionResponseDto>.Failure(validacionResult.Error);
            }

            // 4. Obtener Token del SAT
            var tokenResult = await _fielService.ObtenerTokenSatAsync(
                certificadoBytes,
                request.PasswordFiel,
                cancellationToken);

            if (tokenResult.IsFailure)
            {
                return Result<AutenticacionResponseDto>.Failure($"Error al obtener token SAT: {tokenResult.Error}");
            }

            // 5. Extraer RFC para retornarlo
            var rfcResult = await _fielService.ExtraerRfcDelCertificadoAsync(
                certificadoBytes,
                request.PasswordFiel,
                cancellationToken);
            
            string rfc = rfcResult.IsSuccess ? rfcResult.Value : "Desconocido";

            // 6. Construir respuesta
            var response = new AutenticacionResponseDto
            {
                Token = tokenResult.Value,
                // CRÍTICO: Token expira en 8 minutos (el SAT expira tokens en ~10 minutos)
                Expiration = DateTime.UtcNow.AddMinutes(8),
                Rfc = rfc
            };

            return Result<AutenticacionResponseDto>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error no controlado en AutenticarConFielHandler");
            return Result<AutenticacionResponseDto>.Failure($"Error interno: {ex.Message}");
        }
    }
}
