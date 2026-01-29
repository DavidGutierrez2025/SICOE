namespace SICOE.Application.UseCases.CFDI.DescargarCFDILote;

/// <summary>
/// Respuesta para descarga de lote de CFDI en ZIP
/// </summary>
public class DescargarCFDILoteResponse
{
    public byte[] ContenidoZip { get; set; } = Array.Empty<byte>();
    public string NombreArchivo { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/zip";
    public int TotalCFDIs { get; set; }
    public List<string> UuidsIncluidos { get; set; } = new();
    public List<string> UuidsNoEncontrados { get; set; } = new();
    public List<string> UuidsSinArchivo { get; set; } = new();
}

