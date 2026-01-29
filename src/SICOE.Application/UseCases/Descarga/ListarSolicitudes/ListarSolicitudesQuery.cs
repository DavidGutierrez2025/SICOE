using MediatR;
using SICOE.Application.Common;

namespace SICOE.Application.UseCases.Descarga.ListarSolicitudes;

/// <summary>
/// Query para listar solicitudes de descarga de los últimos N días
/// Puede filtrarse por ClienteId o por RFC (si se proporciona RFC, se busca el ClienteId automáticamente)
/// </summary>
public class ListarSolicitudesQuery : IRequest<Result<ListarSolicitudesResponse>>
{
    public int? ClienteId { get; set; }
    public string? Rfc { get; set; } // Si se proporciona, se busca el ClienteId automáticamente
    public int DiasAtras { get; set; } = 30; // Por defecto últimos 30 días (1 mes)
}

/// <summary>
/// Respuesta del caso de uso ListarSolicitudes
/// </summary>
public class ListarSolicitudesResponse
{
    public List<SolicitudDescargaItem> Solicitudes { get; set; } = new();
    public int Total { get; set; }
}

/// <summary>
/// Item de solicitud de descarga para la lista
/// </summary>
public class SolicitudDescargaItem
{
    public int Id { get; set; }
    public string? IdSolicitudSat { get; set; }
    public string RfcContribuyente { get; set; } = string.Empty;
    public string TipoDescarga { get; set; } = string.Empty; // "CFDI" o "Metadata"
    public int CantidadDocumentos { get; set; }
    public DateTime FechaCreacion { get; set; }
    public string Estado { get; set; } = string.Empty;
}

