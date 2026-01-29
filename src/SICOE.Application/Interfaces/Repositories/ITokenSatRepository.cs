using SICOE.Domain.Entities;

namespace SICOE.Application.Interfaces.Repositories;

/// <summary>
/// Interface específica para repositorio de TokenSat
/// </summary>
public interface ITokenSatRepository
{
    Task<TokenSat?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<TokenSat?> GetTokenValidoPorClienteAsync(int clienteId, CancellationToken cancellationToken = default);
    Task<IEnumerable<TokenSat>> GetTokensExpiradosAsync(CancellationToken cancellationToken = default);
    Task AddAsync(TokenSat token, CancellationToken cancellationToken = default);
    Task UpdateAsync(TokenSat token, CancellationToken cancellationToken = default);
    Task DeleteAsync(TokenSat token, CancellationToken cancellationToken = default);
    Task DesactivarTokensPorClienteAsync(int clienteId, CancellationToken cancellationToken = default);
}

