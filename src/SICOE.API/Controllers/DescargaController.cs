using MediatR;
using Microsoft.AspNetCore.Mvc;
using SICOE.API.Controllers.Base;
using SICOE.Application.UseCases.Descarga.DescargarPaquete;
using SICOE.Application.UseCases.Descarga.ListarSolicitudes;
using SICOE.Application.UseCases.Descarga.ProcesarCFDI;
using SICOE.Application.UseCases.Descarga.SolicitarDescarga;
using SICOE.Application.UseCases.Descarga.VerificarDescarga;

namespace SICOE.API.Controllers;

/// <summary>
/// Controller para operaciones de descarga de CFDI's del SAT
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DescargaController : SICOEBaseController
{
    private readonly IMediator _mediator;

    public DescargaController(
        IConfiguration configuration,
        ILogger<DescargaController> logger,
        IMediator mediator)
        : base(configuration, logger)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Solicita una descarga masiva de CFDI's al SAT
    /// 
    /// NOTA IMPORTANTE: Los certificados FIEL (.cer y .key) son OPCIONALES.
    /// El frontend debe enviarlos automáticamente desde sessionStorage.
    /// Si no se proporcionan, se intentará usar el token SAT almacenado.
    /// 
    /// Si se proporcionan los certificados, se usarán para:
    /// 1. Obtener un nuevo token SAT (si no hay uno válido almacenado)
    /// 2. Firmar el mensaje SOAP al SAT
    /// 
    /// Los certificados se descartan inmediatamente después de usarlos.
    /// </summary>
    /// <param name="command">Comando con parámetros de la solicitud de descarga</param>
    /// <returns>Respuesta con IdSolicitudSat y estado de la solicitud</returns>
    [HttpPost("solicitar")]
    [ProducesResponseType(typeof(SolicitarDescargaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SolicitarDescarga([FromBody] SolicitarDescargaCommand command)
    {
        try
        {
            if (command == null)
            {
                return BadRequest(new { Success = false, Message = "El comando de solicitud no puede ser nulo" });
            }

            // Log del comando recibido para debugging
            _logger.LogInformation("Recibida solicitud de descarga. ClienteId={ClienteId}, FechaInicial={FechaInicial}, FechaFinal={FechaFinal}, EstadoComprobante={EstadoComprobante}",
                command.ClienteId, command.FechaInicial, command.FechaFinal, command.EstadoComprobante);

            var result = await _mediator.Send(command);
            
            if (result.IsSuccess)
            {
                return Ok(new { 
                    Success = true, 
                    Message = _GenericSuccess, 
                    Data = result.Value 
                });
            }

            _logger.LogWarning("Error al procesar solicitud de descarga: {Error}", result.Error);
            return BadRequest(new { Success = false, Message = result.Error });
        }
        catch (FluentValidation.ValidationException ex)
        {
            // Capturar errores de FluentValidation
            var errors = ex.Errors.Select(e => e.ErrorMessage).ToList();
            var errorMessage = string.Join("; ", errors);
            _logger.LogWarning("Error de validación: {Errors}", errorMessage);
            return BadRequest(new { 
                Success = false, 
                Message = errorMessage,
                Errors = errors
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al solicitar descarga. ClienteId={ClienteId}", command?.ClienteId);
            return StatusCode(500, new { 
                Success = false, 
                Message = $"Error interno del servidor: {ex.Message}" 
            });
        }
    }

    /// <summary>
    /// Procesa y guarda los CFDI's descargados del SAT
    /// Descarga paquetes, extrae XML y guarda archivos usando ArchivoService
    /// </summary>
    [HttpPost("procesar")]
    public async Task<IActionResult> ProcesarCFDI([FromBody] ProcesarCFDICommand command)
    {
        var result = await _mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Lista las solicitudes de descarga de los últimos N días
    /// Solo muestra solicitudes que tienen IdSolicitudSat válido (creadas realmente en el SAT)
    /// Puede filtrarse por ClienteId o por RFC (si se proporciona RFC, se busca el ClienteId automáticamente)
    /// </summary>
    [HttpGet("listar")]
    public async Task<IActionResult> ListarSolicitudes(
        [FromQuery] int? clienteId = null, 
        [FromQuery] string? rfc = null,
        [FromQuery] int diasAtras = 30)
    {
        // Log para debug: verificar qué parámetros se están recibiendo
        _logger.LogInformation(
            "API: ListarSolicitudes recibida. ClienteId: {ClienteId}, RFC: {Rfc}, DíasAtras: {DiasAtras}",
            clienteId?.ToString() ?? "null", rfc ?? "null", diasAtras);
        
        // CRÍTICO: Validar que se proporcione RFC o ClienteId para filtrar por usuario
        // Sin uno de estos parámetros, NO se deben mostrar todas las solicitudes (seguridad)
        if (!clienteId.HasValue && string.IsNullOrWhiteSpace(rfc))
        {
            _logger.LogWarning("API: Intento de listar solicitudes sin RFC ni ClienteId. Rechazado por seguridad.");
            return BadRequest(new { 
                Success = false, 
                Message = "Se requiere RFC o ClienteId para filtrar las solicitudes. No se pueden mostrar todas las solicitudes sin filtro por seguridad." 
            });
        }
        
        var query = new ListarSolicitudesQuery
        {
            ClienteId = clienteId,
            Rfc = rfc?.Trim().ToUpper(), // Normalizar RFC a mayúsculas
            DiasAtras = diasAtras
        };
        
        _logger.LogInformation(
            "API: Enviando query a Handler. ClienteId: {ClienteId}, RFC normalizado: {Rfc}, DíasAtras: {DiasAtras}",
            query.ClienteId?.ToString() ?? "null", query.Rfc ?? "null", query.DiasAtras);
        
        var result = await _mediator.Send(query);
        
        if (result.IsSuccess)
        {
            return Ok(new { 
                Success = true, 
                Message = _GenericSuccess, 
                Data = new { 
                    solicitudes = result.Value.Solicitudes,
                    total = result.Value.Total
                }
            });
        }
        
        return BadRequest(new { Success = false, Message = result.Error });
    }

    /// <summary>
    /// Descarga el paquete ZIP de una solicitud completada
    /// Requiere certificado FIEL para obtener token SAT dinámicamente
    /// </summary>
    [HttpPost("descargar-paquete/{solicitudId}")]
    public async Task<IActionResult> DescargarPaquete(int solicitudId, [FromBody] DescargarPaqueteRequest request)
    {
        if (request == null)
        {
            return BadRequest(new { Success = false, Message = "Se requiere certificado FIEL para descargar el paquete" });
        }

        // Convertir certificadoCer y clavePrivadaKey de base64 a byte arrays
        byte[]? certificadoCerBytes = null;
        byte[]? clavePrivadaKeyBytes = null;

        if (!string.IsNullOrWhiteSpace(request.CertificadoCer))
        {
            try
            {
                certificadoCerBytes = Convert.FromBase64String(request.CertificadoCer);
            }
            catch (FormatException)
            {
                return BadRequest(new { Success = false, Message = "El certificado .cer no está en formato base64 válido" });
            }
        }

        if (!string.IsNullOrWhiteSpace(request.ClavePrivadaKey))
        {
            try
            {
                clavePrivadaKeyBytes = Convert.FromBase64String(request.ClavePrivadaKey);
            }
            catch (FormatException)
            {
                return BadRequest(new { Success = false, Message = "La clave privada .key no está en formato base64 válido" });
            }
        }

        // Combinar .cer y .key en .pfx temporalmente (en memoria)
        byte[]? pfxFile = null;
        if (certificadoCerBytes != null && clavePrivadaKeyBytes != null && !string.IsNullOrWhiteSpace(request.PasswordFiel))
        {
            try
            {
                pfxFile = SICOE.Application.Common.Helpers.Fiel.FielCerKeyHelper.CombinarCerYKeyEnPfx(
                    certificadoCerBytes,
                    clavePrivadaKeyBytes,
                    request.PasswordFiel);
                
                _logger.LogInformation("Archivos .cer y .key combinados exitosamente en .pfx temporal para descarga");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al combinar .cer y .key en .pfx para descarga");
                return BadRequest(new { Success = false, Message = $"Error al procesar los archivos FIEL: {ex.Message}. Verifique que los archivos .cer y .key sean válidos y que la contraseña sea correcta." });
            }
        }

        var query = new DescargarPaqueteQuery
        {
            SolicitudId = solicitudId,
            CertificadoFiel = pfxFile,
            PasswordFiel = request.PasswordFiel
        };
        
        var result = await _mediator.Send(query);
        
        if (result.IsFailure)
        {
            return BadRequest(new { Success = false, Message = result.Error });
        }

        var response = result.Value;
        return File(
            response.ContenidoZip,
            response.ContentType,
            response.NombreArchivo);
    }

    /// <summary>
    /// Verifica el estado de una solicitud de descarga en el SAT
    /// Requiere certificado FIEL para obtener token SAT dinámicamente
    /// </summary>
    [HttpPost("verificar-estado/{solicitudId}")]
    public async Task<IActionResult> VerificarEstado(int solicitudId, [FromBody] VerificarEstadoRequest request)
    {
        if (request == null)
        {
            return BadRequest(new { Success = false, Message = "Se requiere certificado FIEL para verificar el estado" });
        }

        // Convertir certificadoCer y clavePrivadaKey de base64 a byte arrays
        byte[]? certificadoCerBytes = null;
        byte[]? clavePrivadaKeyBytes = null;

        if (!string.IsNullOrWhiteSpace(request.CertificadoCer))
        {
            try
            {
                certificadoCerBytes = Convert.FromBase64String(request.CertificadoCer);
            }
            catch (FormatException)
            {
                return BadRequest(new { Success = false, Message = "El certificado .cer no está en formato base64 válido" });
            }
        }

        if (!string.IsNullOrWhiteSpace(request.ClavePrivadaKey))
        {
            try
            {
                clavePrivadaKeyBytes = Convert.FromBase64String(request.ClavePrivadaKey);
            }
            catch (FormatException)
            {
                return BadRequest(new { Success = false, Message = "La clave privada .key no está en formato base64 válido" });
            }
        }

        // Combinar .cer y .key en .pfx temporalmente (en memoria)
        byte[]? pfxFile = null;
        if (certificadoCerBytes != null && clavePrivadaKeyBytes != null && !string.IsNullOrWhiteSpace(request.PasswordFiel))
        {
            try
            {
                pfxFile = SICOE.Application.Common.Helpers.Fiel.FielCerKeyHelper.CombinarCerYKeyEnPfx(
                    certificadoCerBytes,
                    clavePrivadaKeyBytes,
                    request.PasswordFiel);
                
                _logger.LogInformation("Archivos .cer y .key combinados exitosamente en .pfx temporal para verificar estado");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al combinar .cer y .key en .pfx para verificar estado");
                return BadRequest(new { Success = false, Message = $"Error al procesar los archivos FIEL: {ex.Message}. Verifique que los archivos .cer y .key sean válidos y que la contraseña sea correcta." });
            }
        }

        var command = new VerificarDescargaCommand
        {
            SolicitudId = solicitudId,
            CertificadoFiel = pfxFile,
            PasswordFiel = request.PasswordFiel
        };
        
        var result = await _mediator.Send(command);
        
        if (result.IsFailure)
        {
            return BadRequest(new { Success = false, Message = result.Error });
        }

        var response = result.Value;
        return Ok(new { 
            Success = true, 
            Message = "Estado verificado exitosamente",
            Data = new {
                solicitudId = response.SolicitudId,
                codigoEstado = response.CodigoEstado,
                mensaje = response.Mensaje,
                totalPaquetes = response.TotalPaquetes,
                totalCFDIs = response.TotalCFDIs,
                requiereProcesamiento = response.RequiereProcesamiento
            }
        });
    }

    /// <summary>
    /// Request DTO para descargar paquete (requiere FIEL)
    /// </summary>
    public class DescargarPaqueteRequest
    {
        public string? CertificadoCer { get; set; } // Base64
        public string? ClavePrivadaKey { get; set; } // Base64
        public string? PasswordFiel { get; set; }
    }

    /// <summary>
    /// Request DTO para verificar estado (requiere FIEL)
    /// </summary>
    public class VerificarEstadoRequest
    {
        public string? CertificadoCer { get; set; } // Base64
        public string? ClavePrivadaKey { get; set; } // Base64
        public string? PasswordFiel { get; set; }
    }
}

