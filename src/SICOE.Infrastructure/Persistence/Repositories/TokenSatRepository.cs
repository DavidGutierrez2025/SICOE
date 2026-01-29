using Microsoft.EntityFrameworkCore;
using SICOE.Application.Interfaces.Repositories;
using SICOE.Domain.Entities;
using SICOE.Infrastructure.Persistence;

namespace SICOE.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementación del repositorio de TokenSat
/// </summary>
public class TokenSatRepository : ITokenSatRepository
{
    private readonly SICOEDbContext _context;

    public TokenSatRepository(SICOEDbContext context)
    {
        _context = context;
    }

    public async Task<TokenSat?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.TokensSat
            .Include(t => t.Cliente)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<TokenSat?> GetTokenValidoPorClienteAsync(int clienteId, CancellationToken cancellationToken = default)
    {
        var ahora = DateTime.UtcNow;
        return await _context.TokensSat
            .Where(t => t.ClienteId == clienteId)
            .Where(t => t.Activo)
            .Where(t => t.FechaExpiracion > ahora)
            .OrderByDescending(t => t.FechaCreacion)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<TokenSat>> GetTokensExpiradosAsync(CancellationToken cancellationToken = default)
    {
        var ahora = DateTime.UtcNow;
        return await _context.TokensSat
            .Where(t => t.FechaExpiracion <= ahora || !t.Activo)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(TokenSat token, CancellationToken cancellationToken = default)
    {
        await _context.TokensSat.AddAsync(token, cancellationToken);
    }

    public Task UpdateAsync(TokenSat token, CancellationToken cancellationToken = default)
    {
        _context.TokensSat.Update(token);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(TokenSat token, CancellationToken cancellationToken = default)
    {
        _context.TokensSat.Remove(token);
        return Task.CompletedTask;
    }

    public async Task DesactivarTokensPorClienteAsync(int clienteId, CancellationToken cancellationToken = default)
    {
        var tokens = await _context.TokensSat
            .Where(t => t.ClienteId == clienteId && t.Activo)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.Desactivar();
        }
    }
}

