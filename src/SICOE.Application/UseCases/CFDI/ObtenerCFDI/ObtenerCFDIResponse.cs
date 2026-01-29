namespace SICOE.Application.UseCases.CFDI.ObtenerCFDI;

/// <summary>
/// Respuesta con detalles completos de un CFDI
/// </summary>
public class ObtenerCFDIResponse
{
    public int Id { get; set; }
    public string Uuid { get; set; } = string.Empty;
    public string RfcEmisor { get; set; } = string.Empty;
    public string? NombreEmisor { get; set; }
    public string? RegimenFiscalEmisor { get; set; }
    public string RfcReceptor { get; set; } = string.Empty;
    public string? NombreReceptor { get; set; }
    public string? RegimenFiscalReceptor { get; set; }
    public string? DomicilioFiscalReceptor { get; set; }
    public string? UsoCFDI { get; set; }
    public DateTime FechaEmision { get; set; }
    public DateTime? FechaTimbrado { get; set; }
    public decimal Total { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TotalImpuestosTrasladados { get; set; }
    public string TipoComprobante { get; set; } = string.Empty;
    public string? Serie { get; set; }
    public string? Folio { get; set; }
    public string? FormaPago { get; set; }
    public string? MetodoPago { get; set; }
    public string Moneda { get; set; } = "MXN";
    public string? LugarExpedicion { get; set; }
    public string Estatus { get; set; } = string.Empty;
    public int SolicitudDescargaId { get; set; }
    public int? ArchivoId { get; set; }
    public bool TieneArchivo { get; set; } // Indica si el XML está disponible para descarga
    public DateTime FechaCreacion { get; set; }
}

