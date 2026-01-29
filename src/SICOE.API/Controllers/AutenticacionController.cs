using MediatR;
using Microsoft.AspNetCore.Mvc;
using SICOE.API.Controllers.Base;
using SICOE.Application.Interfaces.Services;
using SICOE.Infrastructure.Services.Fiel;

namespace SICOE.API.Controllers;

/// <summary>
/// Controller para autenticación con FIEL
/// IMPORTANTE: La FIEL se recibe, se usa para obtener token SAT, y se descarta inmediatamente.
/// NUNCA se almacena.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AutenticacionController : SICOEBaseController
{
    private readonly IFielService _fielService;
    private readonly ITokenSatService _tokenSatService;
    private readonly ISender _sender;

    public AutenticacionController(
        IConfiguration configuration,
        ILogger<AutenticacionController> logger,
        IFielService fielService,
        ITokenSatService tokenSatService,
        ISender sender)
        : base(configuration, logger)
    {
        _fielService = fielService;
        _tokenSatService = tokenSatService;
        _sender = sender;
    }

    /// <summary>
    /// Extrae el RFC del certificado .cer sin necesidad de la clave privada
    /// </summary>
    [HttpPost("extraer-rfc")]
    public async Task<IActionResult> ExtraerRfc([FromBody] ExtraerRfcRequest request)
    {
        try
        {
            // Validar request
            if (request == null || string.IsNullOrWhiteSpace(request.CertificadoCer))
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = "El certificado .cer es requerido"
                });
            }

            // Convertir base64 a bytes
            byte[] certificadoCerBytes;
            try
            {
                certificadoCerBytes = Convert.FromBase64String(request.CertificadoCer);
            }
            catch (FormatException)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = "El formato del certificado .cer no es válido (Base64)"
                });
            }

            // Extraer RFC del certificado (solo .cer, no necesita .key ni password)
            var rfcResult = await _fielService.ExtraerRfcDelCertificadoCerAsync(
                certificadoCerBytes,
                CancellationToken.None);

            if (rfcResult.IsFailure)
            {
                _logger.LogWarning("Error al extraer RFC del certificado .cer: {Error}", rfcResult.Error);
                return BadRequest(new
                {
                    Success = false,
                    Message = $"Error al extraer RFC del certificado: {rfcResult.Error}"
                });
            }

            return Ok(new
            {
                Success = true,
                Data = new
                {
                    Rfc = rfcResult.Value
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al extraer RFC del certificado");
            return StatusCode(500, new { Success = false, Message = $"Error interno del servidor: {ex.Message}" });
        }
    }

    /// <summary>
    /// Obtiene un token SAT usando el certificado FIEL proporcionado
    /// La FIEL se usa temporalmente y se descarta inmediatamente después de obtener el token
    /// Acepta archivos .cer y .key por separado, o un .pfx combinado
    /// </summary>
    [HttpPost("obtener-token")]
    public async Task<IActionResult> ObtenerToken([FromBody] ObtenerTokenRequest request)
    {
        try
        {
            // Validar request básico
            if (request == null)
            {
                return BadRequest(new { Success = false, Message = "El request no puede estar vacío" });
            }

            var command = new SICOE.Application.UseCases.Autenticacion.AutenticarConFiel.AutenticarConFielCommand
            {
                CertificadoCer = request.CertificadoCer,
                ClavePrivadaKey = request.ClavePrivadaKey,
                PasswordFiel = request.PasswordFiel,
                ClienteId = request.ClienteId
            };

            var result = await _sender.Send(command);

            if (result.IsSuccess)
            {
                var response = result.Value;
                _logger.LogInformation("Token SAT obtenido exitosamente para RFC: {Rfc}", response.Rfc);

                return Ok(new
                {
                    Success = true,
                    Message = "Token obtenido exitosamente",
                    Token = response.Token,
                    Expiration = response.Expiration,
                    Rfc = response.Rfc
                });
            }
            else
            {
                _logger.LogWarning("Error en autenticación CQRS: {Error}", result.Error);
                return BadRequest(new { Success = false, Message = result.Error });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al obtener token SAT");
            return StatusCode(500, new { 
                Success = false, 
                Message = "Error interno del servidor al procesar la autenticación" 
            });
        }
    }
}

/// <summary>
/// Request para extraer RFC del certificado .cer
/// </summary>
public class ExtraerRfcRequest
{
    /// <summary>
    /// Certificado .cer en formato base64
    /// </summary>
    public string CertificadoCer { get; set; } = string.Empty;
}

/// <summary>
/// Request para obtener token SAT
/// NOTA: El SAT de México solicita .cer y .key por separado (NO .pfx)
/// </summary>
public class ObtenerTokenRequest
{
    /// <summary>
    /// Certificado .cer en formato base64 (formato solicitado por el SAT)
    /// </summary>
    public string CertificadoCer { get; set; } = string.Empty;

    /// <summary>
    /// Clave privada .key en formato base64 (formato solicitado por el SAT)
    /// </summary>
    public string ClavePrivadaKey { get; set; } = string.Empty;

    /// <summary>
    /// Contraseña de la clave privada FIEL
    /// </summary>
    public string PasswordFiel { get; set; } = string.Empty;

    /// <summary>
    /// RFC del contribuyente (se extrae automáticamente del certificado, pero se incluye para validación)
    /// </summary>
    public string? Rfc { get; set; }

    /// <summary>
    /// ID del cliente (opcional, para guardar token en BD)
    /// </summary>
    public int? ClienteId { get; set; }
}

