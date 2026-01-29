using Microsoft.EntityFrameworkCore;
using SICOE.Application.Interfaces.Repositories;
using SICOE.Domain.Entities;
using SICOE.Domain.ValueObjects;
using SICOE.Infrastructure.Persistence;

namespace SICOE.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementación del repositorio de CFDI
/// </summary>
public class CFDIRepository : ICFDIRepository
{
    private readonly SICOEDbContext _context;

    public CFDIRepository(SICOEDbContext context)
    {
        _context = context;
    }

    public async Task<CFDI?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.CFDIs
            .Include(c => c.SolicitudDescarga)
            .Include(c => c.Archivo)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<CFDI?> GetByUuidAsync(UUID uuid, CancellationToken cancellationToken = default)
    {
        // Entity Framework con OwnsOne: cuando usamos ColumnName("UUID"), 
        // EF crea una propiedad shadow con el nombre de la columna
        // Usamos EF.Property para acceder directamente a la columna de la base de datos
        return await _context.CFDIs
            .FirstOrDefaultAsync(c => EF.Property<string>(c, "UUID") == uuid.Valor, cancellationToken);
    }

    public async Task<IEnumerable<CFDI>> GetBySolicitudDescargaIdAsync(int solicitudDescargaId, CancellationToken cancellationToken = default)
    {
        return await _context.CFDIs
            .Where(c => c.SolicitudDescargaId == solicitudDescargaId)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountBySolicitudDescargaIdAsync(int solicitudDescargaId, CancellationToken cancellationToken = default)
    {
        return await _context.CFDIs
            .CountAsync(c => c.SolicitudDescargaId == solicitudDescargaId, cancellationToken);
    }

    public async Task AddAsync(CFDI cfdi, CancellationToken cancellationToken = default)
    {
        await _context.CFDIs.AddAsync(cfdi, cancellationToken);
    }

    public Task UpdateAsync(CFDI cfdi, CancellationToken cancellationToken = default)
    {
        _context.CFDIs.Update(cfdi);
        return Task.CompletedTask;
    }

    public async Task<IEnumerable<CFDI>> GetWithFiltersAsync(
        int? solicitudDescargaId = null,
        string? rfcEmisor = null,
        string? rfcReceptor = null,
        DateTime? fechaInicial = null,
        DateTime? fechaFinal = null,
        string? tipoComprobante = null,
        string? uuid = null,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        var query = _context.CFDIs
            .Include(c => c.SolicitudDescarga)
            .ThenInclude(s => s!.Cliente)
            .AsQueryable();

        // Aplicar filtros
        if (solicitudDescargaId.HasValue)
        {
            query = query.Where(c => c.SolicitudDescargaId == solicitudDescargaId.Value);
        }

        if (!string.IsNullOrWhiteSpace(rfcEmisor))
        {
            query = query.Where(c => c.RfcEmisor.Valor == rfcEmisor);
        }

        if (!string.IsNullOrWhiteSpace(rfcReceptor))
        {
            query = query.Where(c => c.RfcReceptor.Valor == rfcReceptor);
        }

        if (fechaInicial.HasValue)
        {
            query = query.Where(c => c.FechaEmision >= fechaInicial.Value);
        }

        if (fechaFinal.HasValue)
        {
            query = query.Where(c => c.FechaEmision <= fechaFinal.Value);
        }

        if (!string.IsNullOrWhiteSpace(tipoComprobante))
        {
            var tipoComprobanteEnum = tipoComprobante switch
            {
                "I" => Domain.Enums.TipoComprobante.Ingreso,
                "E" => Domain.Enums.TipoComprobante.Egreso,
                "T" => Domain.Enums.TipoComprobante.Traslado,
                "N" => Domain.Enums.TipoComprobante.Nomina,
                "P" => Domain.Enums.TipoComprobante.Pago,
                _ => (Domain.Enums.TipoComprobante?)null
            };

            if (tipoComprobanteEnum.HasValue)
            {
                query = query.Where(c => c.TipoComprobante == tipoComprobanteEnum.Value);
            }
        }

        if (!string.IsNullOrWhiteSpace(uuid))
        {
            query = query.Where(c => c.Uuid.Valor == uuid);
        }

        return await query
            .OrderByDescending(c => c.FechaEmision)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountWithFiltersAsync(
        int? solicitudDescargaId = null,
        string? rfcEmisor = null,
        string? rfcReceptor = null,
        DateTime? fechaInicial = null,
        DateTime? fechaFinal = null,
        string? tipoComprobante = null,
        string? uuid = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.CFDIs.AsQueryable();

        // Aplicar los mismos filtros
        if (solicitudDescargaId.HasValue)
        {
            query = query.Where(c => c.SolicitudDescargaId == solicitudDescargaId.Value);
        }

        if (!string.IsNullOrWhiteSpace(rfcEmisor))
        {
            query = query.Where(c => c.RfcEmisor.Valor == rfcEmisor);
        }

        if (!string.IsNullOrWhiteSpace(rfcReceptor))
        {
            query = query.Where(c => c.RfcReceptor.Valor == rfcReceptor);
        }

        if (fechaInicial.HasValue)
        {
            query = query.Where(c => c.FechaEmision >= fechaInicial.Value);
        }

        if (fechaFinal.HasValue)
        {
            query = query.Where(c => c.FechaEmision <= fechaFinal.Value);
        }

        if (!string.IsNullOrWhiteSpace(tipoComprobante))
        {
            var tipoComprobanteEnum = tipoComprobante switch
            {
                "I" => Domain.Enums.TipoComprobante.Ingreso,
                "E" => Domain.Enums.TipoComprobante.Egreso,
                "T" => Domain.Enums.TipoComprobante.Traslado,
                "N" => Domain.Enums.TipoComprobante.Nomina,
                "P" => Domain.Enums.TipoComprobante.Pago,
                _ => (Domain.Enums.TipoComprobante?)null
            };

            if (tipoComprobanteEnum.HasValue)
            {
                query = query.Where(c => c.TipoComprobante == tipoComprobanteEnum.Value);
            }
        }

        if (!string.IsNullOrWhiteSpace(uuid))
        {
            query = query.Where(c => c.Uuid.Valor == uuid);
        }

        return await query.CountAsync(cancellationToken);
    }
}

