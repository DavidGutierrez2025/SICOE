using Microsoft.AspNetCore.Mvc;

namespace SICOE.API.Controllers.Base;

/// <summary>
/// Controller base para SICOE (similar a BudaTIBaseController de GEDINET)
/// </summary>
public abstract class SICOEBaseController : ControllerBase
{
    protected readonly IConfiguration _configuration;
    protected readonly ILogger _logger;

    protected readonly string _GenericError;
    protected readonly string _GenericSuccess;
    protected readonly string _RecordNotFound;
    protected readonly string _Unauthorized;

    protected SICOEBaseController(
        IConfiguration configuration,
        ILogger logger)
    {
        _configuration = configuration;
        _logger = logger;

        _GenericError = _configuration.GetValue<string>("UserMessages:Generic:GenericError") ?? "Error";
        _GenericSuccess = _configuration.GetValue<string>("UserMessages:Generic:Success") ?? "Éxito";
        _RecordNotFound = _configuration.GetValue<string>("UserMessages:Generic:RecordNotFound") ?? "No encontrado";
        _Unauthorized = _configuration.GetValue<string>("UserMessages:Generic:Unauthorized") ?? "No autorizado";
    }

    protected IActionResult HandleResult<T>(SICOE.Application.Common.Result<T> result)
    {
        if (result.IsSuccess)
            return Ok(new { Success = true, Message = _GenericSuccess, Data = result.Value });

        return BadRequest(new { Success = false, Message = result.Error });
    }
}

