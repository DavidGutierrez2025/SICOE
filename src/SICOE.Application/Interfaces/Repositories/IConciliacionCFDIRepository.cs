using SICOE.Domain.Entities;
using SICOE.Domain.Enums;

namespace SICOE.Application.Interfaces.Repositories;

/// <summary>
/// Interface específica para repositorio de ConciliacionCFDI
/// </summary>
public interface IConciliacionCFDIRepository
{
    Task<ConciliacionCFDI?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ConciliacionCFDI?> GetBySolicitudDescargaIdAsync(int solicitudDescargaId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ConciliacionCFDI>> GetByEstadoAsync(EstadoConciliacion estado, CancellationToken cancellationToken = default);
    Task<IEnumerable<ConciliacionCFDI>> GetConFaltantesAsync(int maxIntentos = 3, CancellationToken cancellationToken = default);
    Task AddAsync(ConciliacionCFDI conciliacion, CancellationToken cancellationToken = default);
    Task UpdateAsync(ConciliacionCFDI conciliacion, CancellationToken cancellationToken = default);
}

