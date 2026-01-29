using SICOE.Domain.Enums;
using SICOE.Domain.Interfaces;

namespace SICOE.Domain.Entities;

/// <summary>
/// Entidad Archivo - Representa un archivo almacenado en el sistema
/// </summary>
public class Archivo : IEntity
{
    public int Id { get; private set; }
    public string NombreOriginal { get; private set; } = string.Empty;
    public string NombreAlmacenado { get; private set; } = string.Empty;
    public string RutaCompleta { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long TamanioBytes { get; private set; }
    public TipoArchivo TipoArchivo { get; private set; }
    public string? Descripcion { get; private set; }
    public DateTime FechaCreacion { get; private set; }
    public DateTime? FechaActualizacion { get; private set; }

    // Navegación
    public ICollection<CFDI> CFDIs { get; private set; } = new List<CFDI>();

    // Constructor privado para EF Core
    private Archivo() { }

    // Constructor público
    public Archivo(
        string nombreOriginal,
        string nombreAlmacenado,
        string rutaCompleta,
        string contentType,
        long tamanioBytes,
        TipoArchivo tipoArchivo,
        string? descripcion = null)
    {
        if (string.IsNullOrWhiteSpace(nombreOriginal))
            throw new ArgumentException("El nombre original no puede estar vacío", nameof(nombreOriginal));

        if (string.IsNullOrWhiteSpace(nombreAlmacenado))
            throw new ArgumentException("El nombre almacenado no puede estar vacío", nameof(nombreAlmacenado));

        if (string.IsNullOrWhiteSpace(rutaCompleta))
            throw new ArgumentException("La ruta completa no puede estar vacía", nameof(rutaCompleta));

        if (tamanioBytes < 0)
            throw new ArgumentException("El tamaño no puede ser negativo", nameof(tamanioBytes));

        NombreOriginal = nombreOriginal;
        NombreAlmacenado = nombreAlmacenado;
        RutaCompleta = rutaCompleta;
        ContentType = contentType;
        TamanioBytes = tamanioBytes;
        TipoArchivo = tipoArchivo;
        Descripcion = descripcion;
        FechaCreacion = DateTime.UtcNow;
    }

    // Lógica de negocio
    public void ActualizarDescripcion(string descripcion)
    {
        Descripcion = descripcion;
        FechaActualizacion = DateTime.UtcNow;
    }
}

