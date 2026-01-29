using MediatR;
using SICOE.Application.Common;
using SICOE.Application.UseCases.CFDI.DescargarCFDI;

namespace SICOE.Application.UseCases.CFDI.DescargarCFDI;

/// <summary>
/// Query para descargar el XML de un CFDI
/// </summary>
public class DescargarCFDIQuery : IRequest<Result<DescargarCFDIResponse>>
{
    public string? Uuid { get; set; }
    public int? Id { get; set; }
}

