using Microsoft.EntityFrameworkCore;
using SICOE.Application.Interfaces.Repositories;
using SICOE.Domain.Entities;
using SICOE.Domain.Enums;
using SICOE.Infrastructure.Persistence;

namespace SICOE.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementación del repositorio de ConciliacionCFDI
/// </summary>
public class ConciliacionCFDIRepository : IConciliacionCFDIRepository
{
    private readonly SICOEDbContext _context;

    public ConciliacionCFDIRepository(SICOEDbContext context)
    {
        _context = context;
    }

    public async Task<ConciliacionCFDI?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.ConciliacionesCFDI
            .Include(c => c.SolicitudDescarga)
            .ThenInclude(s => s!.Cliente)
            .Include(c => c.NuevaSolicitud)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<ConciliacionCFDI?> GetBySolicitudDescargaIdAsync(int solicitudDescargaId, CancellationToken cancellationToken = default)
    {
        return await _context.ConciliacionesCFDI
            .Include(c => c.SolicitudDescarga)
            .ThenInclude(s => s!.Cliente)
            .Include(c => c.NuevaSolicitud)
            .FirstOrDefaultAsync(c => c.SolicitudDescargaId == solicitudDescargaId, cancellationToken);
    }

    public async Task<IEnumerable<ConciliacionCFDI>> GetByEstadoAsync(EstadoConciliacion estado, CancellationToken cancellationToken = default)
    {
        return await _context.ConciliacionesCFDI
            .Include(c => c.SolicitudDescarga)
            .ThenInclude(s => s!.Cliente)
            .Where(c => c.Estado == estado)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ConciliacionCFDI>> GetConFaltantesAsync(int maxIntentos = 3, CancellationToken cancellationToken = default)
    {
        return await _context.ConciliacionesCFDI
            .Include(c => c.SolicitudDescarga)
            .ThenInclude(s => s!.Cliente)
            .Where(c => c.Estado == EstadoConciliacion.ConFaltantes && c.IntentosReconciliacion < maxIntentos)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ConciliacionCFDI conciliacion, CancellationToken cancellationToken = default)
    {
        await _context.ConciliacionesCFDI.AddAsync(conciliacion, cancellationToken);
    }

    public async Task UpdateAsync(ConciliacionCFDI conciliacion, CancellationToken cancellationToken = default)
    {
        _context.ConciliacionesCFDI.Update(conciliacion);
        await Task.CompletedTask;
    }
}

