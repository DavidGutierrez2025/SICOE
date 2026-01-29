using Microsoft.EntityFrameworkCore;
using SICOE.Application.Interfaces.Repositories;
using SICOE.Domain.Entities;
using SICOE.Domain.Enums;
using SICOE.Infrastructure.Persistence;

namespace SICOE.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementación del repositorio de SolicitudDescarga
/// </summary>
public class SolicitudDescargaRepository : ISolicitudDescargaRepository
{
    private readonly SICOEDbContext _context;

    public SolicitudDescargaRepository(SICOEDbContext context)
    {
        _context = context;
    }

    public async Task<SolicitudDescarga?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.SolicitudesDescarga
            .Include(s => s.Cliente)
            .Include(s => s.CFDIs)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<SolicitudDescarga?> GetByIdSolicitudSatAsync(string idSolicitudSat, CancellationToken cancellationToken = default)
    {
        return await _context.SolicitudesDescarga
            .FirstOrDefaultAsync(s => s.IdSolicitudSat == idSolicitudSat, cancellationToken);
    }

    public async Task<IEnumerable<SolicitudDescarga>> GetByClienteIdAsync(int clienteId, CancellationToken cancellationToken = default)
    {
        return await _context.SolicitudesDescarga
            .Where(s => s.ClienteId == clienteId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<SolicitudDescarga>> GetByEstadoAsync(EstadoSolicitud estado, CancellationToken cancellationToken = default)
    {
        return await _context.SolicitudesDescarga
            .Where(s => s.Estado == estado)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<SolicitudDescarga>> GetPendientesOEnProcesoAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SolicitudesDescarga
            .Include(s => s.Cliente)
            .Where(s => s.Estado == EstadoSolicitud.Pendiente || s.Estado == EstadoSolicitud.EnProceso)
            .Where(s => !string.IsNullOrWhiteSpace(s.IdSolicitudSat))
            .OrderBy(s => s.FechaCreacion)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<SolicitudDescarga>> GetUltimosDiasAsync(int dias, int? clienteId = null, CancellationToken cancellationToken = default)
    {
        var fechaLimite = DateTime.UtcNow.AddDays(-dias);
        var query = _context.SolicitudesDescarga
            .Include(s => s.Cliente)
            .Where(s => s.FechaCreacion >= fechaLimite)
            .Where(s => !string.IsNullOrWhiteSpace(s.IdSolicitudSat));

        if (clienteId.HasValue && clienteId.Value > 0)
        {
            query = query.Where(s => s.ClienteId == clienteId.Value);
        }

        return await query
            .OrderByDescending(s => s.FechaCreacion)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(SolicitudDescarga solicitud, CancellationToken cancellationToken = default)
    {
        await _context.SolicitudesDescarga.AddAsync(solicitud, cancellationToken);
    }

    public Task UpdateAsync(SolicitudDescarga solicitud, CancellationToken cancellationToken = default)
    {
        _context.SolicitudesDescarga.Update(solicitud);
        return Task.CompletedTask;
    }
}

