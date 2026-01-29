using Microsoft.EntityFrameworkCore;
using SICOE.Application.Interfaces.Repositories;
using SICOE.Domain.Entities;
using SICOE.Infrastructure.Persistence;
using System.Linq;

namespace SICOE.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementación del repositorio de Cliente (DIP: Implementa IClienteRepository)
/// </summary>
public class ClienteRepository : IClienteRepository
{
    private readonly SICOEDbContext _context;

    public ClienteRepository(SICOEDbContext context)
    {
        _context = context;
    }

    public async Task<Cliente?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Clientes
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Cliente?> GetByRfcAsync(string rfc, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rfc))
            return null;
        
        // Normalizar RFC a mayúsculas para comparación consistente
        var rfcNormalizado = rfc.Trim().ToUpper();
        
        // EF Core moderno debería poder traducir c.Rfc.Valor cuando Rfc está configurado con OwnsOne
        // Si no funciona, cargamos en memoria y filtramos como fallback
        try
        {
            // Intentar búsqueda case-insensitive en BD
            var clientes = await _context.Clientes.ToListAsync(cancellationToken);
            return clientes.FirstOrDefault(c => 
                c.Rfc.Valor != null && 
                c.Rfc.Valor.Trim().ToUpper() == rfcNormalizado);
        }
        catch (Exception)
        {
            // Fallback: cargar en memoria y filtrar (menos eficiente pero garantiza funcionamiento)
            var clientes = await _context.Clientes.ToListAsync(cancellationToken);
            return clientes.FirstOrDefault(c => 
                c.Rfc.Valor != null && 
                c.Rfc.Valor.Trim().ToUpper() == rfcNormalizado);
        }
    }

    public async Task<IEnumerable<Cliente>> GetActivosAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Clientes
            .Where(c => c.Activo)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Cliente cliente, CancellationToken cancellationToken = default)
    {
        await _context.Clientes.AddAsync(cliente, cancellationToken);
    }

    public Task UpdateAsync(Cliente cliente, CancellationToken cancellationToken = default)
    {
        _context.Clientes.Update(cliente);
        return Task.CompletedTask;
    }
}

