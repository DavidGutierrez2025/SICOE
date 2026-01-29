using MediatR;
using SICOE.Application.Common;
using SICOE.Application.UseCases.CFDI.ObtenerCFDI;

namespace SICOE.Application.UseCases.CFDI.ObtenerCFDI;

/// <summary>
/// Query para obtener detalles de un CFDI por UUID o ID
/// </summary>
public class ObtenerCFDIQuery : IRequest<Result<ObtenerCFDIResponse>>
{
    public string? Uuid { get; set; }
    public int? Id { get; set; }
}

