using Microsoft.EntityFrameworkCore;
using SICOE.Application.Interfaces.Repositories;
using SICOE.Domain.Entities;
using SICOE.Infrastructure.Persistence;

namespace SICOE.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementación del repositorio de Archivo
/// </summary>
public class ArchivoRepository : IArchivoRepository
{
    private readonly SICOEDbContext _context;

    public ArchivoRepository(SICOEDbContext context)
    {
        _context = context;
    }

    public async Task<Archivo?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Archivos
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<Archivo?> GetByRutaCompletaAsync(string rutaCompleta, CancellationToken cancellationToken = default)
    {
        return await _context.Archivos
            .FirstOrDefaultAsync(a => a.RutaCompleta == rutaCompleta, cancellationToken);
    }

    public async Task AddAsync(Archivo archivo, CancellationToken cancellationToken = default)
    {
        await _context.Archivos.AddAsync(archivo, cancellationToken);
    }

    public Task UpdateAsync(Archivo archivo, CancellationToken cancellationToken = default)
    {
        _context.Archivos.Update(archivo);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Archivo archivo, CancellationToken cancellationToken = default)
    {
        _context.Archivos.Remove(archivo);
        return Task.CompletedTask;
    }
}

