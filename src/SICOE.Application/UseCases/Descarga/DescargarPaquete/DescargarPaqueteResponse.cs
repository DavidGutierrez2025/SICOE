namespace SICOE.Application.UseCases.Descarga.DescargarPaquete;

/// <summary>
/// Respuesta con el contenido del paquete ZIP para descarga
/// </summary>
public class DescargarPaqueteResponse
{
    public byte[] ContenidoZip { get; set; } = Array.Empty<byte>();
    public string NombreArchivo { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/zip";
}

