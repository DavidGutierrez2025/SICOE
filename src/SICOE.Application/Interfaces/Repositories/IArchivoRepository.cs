using SICOE.Domain.Entities;

namespace SICOE.Application.Interfaces.Repositories;

/// <summary>
/// Interface específica para repositorio de Archivo
/// </summary>
public interface IArchivoRepository
{
    Task<Archivo?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Archivo?> GetByRutaCompletaAsync(string rutaCompleta, CancellationToken cancellationToken = default);
    Task AddAsync(Archivo archivo, CancellationToken cancellationToken = default);
    Task UpdateAsync(Archivo archivo, CancellationToken cancellationToken = default);
    Task DeleteAsync(Archivo archivo, CancellationToken cancellationToken = default);
}

