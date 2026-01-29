using SICOE.Domain.Interfaces;
using SICOE.Domain.ValueObjects;

namespace SICOE.Domain.Entities;

/// <summary>
/// Entidad Cliente - Representa un cliente del sistema
/// </summary>
public class Cliente : IEntity
{
    public int Id { get; private set; }
    public RFC Rfc { get; private set; } = null!;
    public string RazonSocial { get; private set; } = string.Empty;
    public Email? Email { get; private set; }
    public bool Activo { get; private set; }
    public DateTime FechaCreacion { get; private set; }
    public DateTime? FechaActualizacion { get; private set; }

    // Navegación
    public ICollection<SolicitudDescarga> SolicitudesDescarga { get; private set; } = new List<SolicitudDescarga>();

    // Constructor privado para EF Core
    private Cliente() { }

    // Constructor público con validación
    public Cliente(RFC rfc, string razonSocial, Email? email = null)
    {
        Rfc = rfc ?? throw new ArgumentNullException(nameof(rfc));
        RazonSocial = razonSocial ?? throw new ArgumentNullException(nameof(razonSocial));
        Email = email;
        Activo = true;
        FechaCreacion = DateTime.UtcNow;
    }

    // Métodos de negocio (lógica de dominio)
    public void Desactivar()
    {
        Activo = false;
        FechaActualizacion = DateTime.UtcNow;
    }

    public void Activar()
    {
        Activo = true;
        FechaActualizacion = DateTime.UtcNow;
    }

    public void ActualizarEmail(Email nuevoEmail)
    {
        Email = nuevoEmail ?? throw new ArgumentNullException(nameof(nuevoEmail));
        FechaActualizacion = DateTime.UtcNow;
    }
}

