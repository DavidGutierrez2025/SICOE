using SICOE.Domain.Interfaces;

namespace SICOE.Domain.Entities;

/// <summary>
/// Entidad TokenSat - Almacena tokens del SAT temporalmente (encriptados)
/// Los tokens tienen expiración y se eliminan automáticamente
/// </summary>
public class TokenSat : IEntity
{
    public int Id { get; private set; }
    public int ClienteId { get; private set; }
    public string Token { get; private set; } = string.Empty; // Token encriptado
    public DateTime FechaCreacion { get; private set; }
    public DateTime FechaExpiracion { get; private set; }
    public bool Activo { get; private set; }

    // Navegación
    public Cliente Cliente { get; private set; } = null!;

    // Constructor privado para EF Core
    private TokenSat() { }

    // Constructor público
    public TokenSat(
        int clienteId,
        string token,
        DateTime fechaExpiracion)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("El token no puede estar vacío", nameof(token));

        if (fechaExpiracion <= DateTime.UtcNow)
            throw new ArgumentException("La fecha de expiración debe ser futura", nameof(fechaExpiracion));

        ClienteId = clienteId;
        Token = token; // En producción, esto debería estar encriptado
        FechaCreacion = DateTime.UtcNow;
        FechaExpiracion = fechaExpiracion;
        Activo = true;
    }

    // Lógica de negocio
    public bool EstaExpirado()
    {
        return DateTime.UtcNow >= FechaExpiracion;
    }

    public bool EsValido()
    {
        return Activo && !EstaExpirado();
    }

    public void Desactivar()
    {
        Activo = false;
    }
}

