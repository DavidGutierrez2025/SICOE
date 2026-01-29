using SICOE.Domain.Enums;

namespace SICOE.Application.DTOs.Descarga;

/// <summary>
/// DTO para SolicitudDescarga
/// </summary>
public class SolicitudDescargaDto
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public string ClienteRfc { get; set; } = string.Empty;
    public string ClienteRazonSocial { get; set; } = string.Empty;
    public string? IdSolicitudSat { get; set; }
    public DateTime FechaInicial { get; set; }
    public DateTime FechaFinal { get; set; }
    public EstadoSolicitud Estado { get; set; }
    public int TotalSolicitado { get; set; }
    public int TotalRecibido { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaActualizacion { get; set; }
    public string? MensajeError { get; set; }
}

