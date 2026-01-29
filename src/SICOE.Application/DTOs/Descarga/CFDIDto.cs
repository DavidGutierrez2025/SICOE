using SICOE.Domain.Enums;

namespace SICOE.Application.DTOs.Descarga;

/// <summary>
/// DTO para CFDI
/// </summary>
public class CFDIDto
{
    public int Id { get; set; }
    public string Uuid { get; set; } = string.Empty;
    public string RfcEmisor { get; set; } = string.Empty;
    public string RfcReceptor { get; set; } = string.Empty;
    public DateTime FechaEmision { get; set; }
    public decimal Total { get; set; }
    public string Moneda { get; set; } = "MXN";
    public TipoComprobante TipoComprobante { get; set; }
    public EstatusCFDI Estatus { get; set; }
    public int? ArchivoId { get; set; }
    public DateTime FechaCreacion { get; set; }
}

