using MediatR;
using SICOE.Application.Common;
using SICOE.Application.UseCases.CFDI.DescargarCFDILote;

namespace SICOE.Application.UseCases.CFDI.DescargarCFDILote;

/// <summary>
/// Command para descargar múltiples CFDI en un archivo ZIP
/// </summary>
public class DescargarCFDILoteCommand : IRequest<Result<DescargarCFDILoteResponse>>
{
    public List<string>? Uuids { get; set; }
    public List<int>? Ids { get; set; }
    public int? SolicitudDescargaId { get; set; } // Descargar todos los CFDI de una solicitud
}

