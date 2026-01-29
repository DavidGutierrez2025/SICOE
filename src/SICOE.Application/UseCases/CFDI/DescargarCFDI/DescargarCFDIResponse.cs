namespace SICOE.Application.UseCases.CFDI.DescargarCFDI;

/// <summary>
/// Respuesta para descarga de XML de CFDI
/// </summary>
public class DescargarCFDIResponse
{
    public byte[] ContenidoXml { get; set; } = Array.Empty<byte>();
    public string NombreArchivo { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/xml";
    public string Uuid { get; set; } = string.Empty;
}

