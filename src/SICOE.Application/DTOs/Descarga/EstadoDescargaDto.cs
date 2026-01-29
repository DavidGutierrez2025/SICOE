using SICOE.Domain.Enums;

namespace SICOE.Application.DTOs.Descarga;

/// <summary>
/// DTO para estado de descarga
/// </summary>
public class EstadoDescargaDto
{
    public int Id { get; set; }
    public EstadoSolicitud Estado { get; set; }
    public int TotalSolicitado { get; set; }
    public int TotalRecibido { get; set; }
    public int Faltantes { get; set; }
    public bool EstaCompleta { get; set; }
    public string? MensajeError { get; set; }
    public DateTime? FechaActualizacion { get; set; }
}

