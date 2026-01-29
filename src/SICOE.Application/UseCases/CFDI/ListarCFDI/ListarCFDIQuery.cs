using MediatR;
using SICOE.Application.Common;
using SICOE.Application.UseCases.CFDI.ListarCFDI;

namespace SICOE.Application.UseCases.CFDI.ListarCFDI;

/// <summary>
/// Query para listar CFDI procesados con filtros opcionales
/// </summary>
public class ListarCFDIQuery : IRequest<Result<ListarCFDIResponse>>
{
    public int? SolicitudDescargaId { get; set; }
    public string? RfcEmisor { get; set; }
    public string? RfcReceptor { get; set; }
    public DateTime? FechaInicial { get; set; }
    public DateTime? FechaFinal { get; set; }
    public string? TipoComprobante { get; set; }
    public string? Uuid { get; set; }
    public int? ClienteId { get; set; } // Filtrar por cliente (RFC receptor debe coincidir)
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

