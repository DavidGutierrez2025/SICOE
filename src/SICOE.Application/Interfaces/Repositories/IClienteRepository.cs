using SICOE.Domain.Entities;

namespace SICOE.Application.Interfaces.Repositories;

/// <summary>
/// Interface específica para repositorio de Cliente (ISP: Interface específica, no general)
/// </summary>
public interface IClienteRepository
{
    Task<Cliente?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Cliente?> GetByRfcAsync(string rfc, CancellationToken cancellationToken = default);
    Task<IEnumerable<Cliente>> GetActivosAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Cliente cliente, CancellationToken cancellationToken = default);
    Task UpdateAsync(Cliente cliente, CancellationToken cancellationToken = default);
}

