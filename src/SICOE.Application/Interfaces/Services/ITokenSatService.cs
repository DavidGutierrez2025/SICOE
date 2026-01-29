using SICOE.Application.Common;

namespace SICOE.Application.Interfaces.Services;

/// <summary>
/// Interface para servicio de gestión de tokens SAT
/// </summary>
public interface ITokenSatService
{
    /// <summary>
    /// Obtiene un token válido para un cliente, o null si no existe uno válido
    /// </summary>
    Task<Result<string?>> ObtenerTokenValidoAsync(int clienteId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Guarda un token SAT para un cliente con fecha de expiración
    /// </summary>
    Task<Result> GuardarTokenAsync(int clienteId, string token, DateTime fechaExpiracion, CancellationToken cancellationToken = default);

    /// <summary>
    /// Desactiva todos los tokens de un cliente (útil cuando se obtiene un nuevo token)
    /// </summary>
    Task<Result> DesactivarTokensClienteAsync(int clienteId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Elimina tokens expirados de la base de datos
    /// </summary>
    Task<Result<int>> LimpiarTokensExpiradosAsync(CancellationToken cancellationToken = default);
}

