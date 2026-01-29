namespace SICOE.Application.UseCases.Conciliacion.ConciliarCFDI;

/// <summary>
/// Respuesta de conciliación de CFDI
/// </summary>
public class ConciliarCFDIResponse
{
    public int ConciliacionId { get; set; }
    public int SolicitudDescargaId { get; set; }
    public int TotalSolicitado { get; set; }
    public int TotalRecibido { get; set; }
    public int TotalFaltantes { get; set; }
    public bool TieneFaltantes { get; set; }
    public int? NuevaSolicitudId { get; set; } // ID de la nueva solicitud creada para faltantes (si aplica)
    public string Estado { get; set; } = string.Empty;
    public string? Mensaje { get; set; }
}

