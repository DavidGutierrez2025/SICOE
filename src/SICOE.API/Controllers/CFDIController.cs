using MediatR;
using Microsoft.AspNetCore.Mvc;
using SICOE.API.Controllers.Base;
using SICOE.Application.UseCases.CFDI.DescargarCFDI;
using SICOE.Application.UseCases.CFDI.DescargarCFDILote;
using SICOE.Application.UseCases.CFDI.ListarCFDI;
using SICOE.Application.UseCases.CFDI.ObtenerCFDI;

namespace SICOE.API.Controllers;

/// <summary>
/// Controller para operaciones con CFDI procesados
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CFDIController : SICOEBaseController
{
    private readonly IMediator _mediator;

    public CFDIController(
        IConfiguration configuration,
        ILogger<CFDIController> logger,
        IMediator mediator)
        : base(configuration, logger)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Lista CFDI procesados con filtros y paginación
    /// </summary>
    [HttpGet("listar")]
    [ProducesResponseType(typeof(ListarCFDIResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ListarCFDI(
        [FromQuery] int? solicitudDescargaId = null,
        [FromQuery] string? rfcEmisor = null,
        [FromQuery] string? rfcReceptor = null,
        [FromQuery] DateTime? fechaInicial = null,
        [FromQuery] DateTime? fechaFinal = null,
        [FromQuery] string? tipoComprobante = null,
        [FromQuery] string? uuid = null,
        [FromQuery] int? clienteId = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = new ListarCFDIQuery
        {
            SolicitudDescargaId = solicitudDescargaId,
            RfcEmisor = rfcEmisor,
            RfcReceptor = rfcReceptor,
            FechaInicial = fechaInicial,
            FechaFinal = fechaFinal,
            TipoComprobante = tipoComprobante,
            Uuid = uuid,
            ClienteId = clienteId,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var result = await _mediator.Send(query);
        return HandleResult(result);
    }

    /// <summary>
    /// Obtiene detalles de un CFDI por UUID o ID
    /// </summary>
    [HttpGet("{uuidOrId}")]
    [ProducesResponseType(typeof(ObtenerCFDIResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerCFDI(string uuidOrId)
    {
        var query = new ObtenerCFDIQuery();

        // Intentar parsear como UUID (36 caracteres) o ID (número)
        if (Guid.TryParse(uuidOrId, out _))
        {
            query.Uuid = uuidOrId;
        }
        else if (int.TryParse(uuidOrId, out var id))
        {
            query.Id = id;
        }
        else
        {
            return BadRequest(new { Success = false, Message = "UUID o ID inválido" });
        }

        var result = await _mediator.Send(query);
        
        if (result.IsFailure)
        {
            return NotFound(new { Success = false, Message = result.Error });
        }

        return Ok(new { Success = true, Message = _GenericSuccess, Data = result.Value });
    }

    /// <summary>
    /// Descarga el XML de un CFDI por UUID o ID
    /// </summary>
    [HttpGet("{uuidOrId}/descargar")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DescargarCFDI(string uuidOrId)
    {
        var query = new DescargarCFDIQuery();

        // Intentar parsear como UUID (36 caracteres) o ID (número)
        if (Guid.TryParse(uuidOrId, out _))
        {
            query.Uuid = uuidOrId;
        }
        else if (int.TryParse(uuidOrId, out var id))
        {
            query.Id = id;
        }
        else
        {
            return BadRequest(new { Success = false, Message = "UUID o ID inválido" });
        }

        var result = await _mediator.Send(query);

        if (result.IsFailure)
        {
            return NotFound(new { Success = false, Message = result.Error });
        }

        var response = result.Value;
        return File(
            response.ContenidoXml,
            response.ContentType,
            response.NombreArchivo);
    }

    /// <summary>
    /// Descarga múltiples CFDI en un archivo ZIP
    /// </summary>
    [HttpPost("descargar-lote")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DescargarCFDILote([FromBody] DescargarCFDILoteCommand command)
    {
        if (command == null)
        {
            return BadRequest(new { Success = false, Message = "El comando no puede ser nulo" });
        }

        var result = await _mediator.Send(command);

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
}

