namespace SICOE.Application.UseCases.CFDI.ListarCFDI;

/// <summary>
/// Respuesta de listado de CFDI
/// </summary>
public class ListarCFDIResponse
{
    public List<CFDIItemDto> CFDIs { get; set; } = new();
    public int TotalRegistros { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalRegistros / (double)PageSize);
}

/// <summary>
/// DTO para un CFDI en el listado
/// </summary>
public class CFDIItemDto
{
    public int Id { get; set; }
    public string Uuid { get; set; } = string.Empty;
    public string RfcEmisor { get; set; } = string.Empty;
    public string? NombreEmisor { get; set; }
    public string RfcReceptor { get; set; } = string.Empty;
    public string? NombreReceptor { get; set; }
    public DateTime FechaEmision { get; set; }
    public DateTime? FechaTimbrado { get; set; }
    public decimal Total { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TotalImpuestosTrasladados { get; set; }
    public string TipoComprobante { get; set; } = string.Empty;
    public string? Serie { get; set; }
    public string? Folio { get; set; }
    public string Estatus { get; set; } = string.Empty;
    public string Moneda { get; set; } = "MXN";
    public int SolicitudDescargaId { get; set; }
    public bool TieneArchivo { get; set; } // Indica si el XML está disponible para descarga
}

