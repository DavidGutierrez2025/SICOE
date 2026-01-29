using Microsoft.EntityFrameworkCore.Storage;
using SICOE.Application.Interfaces.Repositories;
using SICOE.Application.Interfaces.UnitOfWork;
using SICOE.Infrastructure.Persistence;
using SICOE.Infrastructure.Persistence.Repositories;

namespace SICOE.Infrastructure.UnitOfWork;

/// <summary>
/// Implementación de Unit of Work
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly SICOEDbContext _context;
    private IDbContextTransaction? _transaction;

    private IClienteRepository? _clientes;
    private ISolicitudDescargaRepository? _solicitudesDescarga;
    private ICFDIRepository? _cfdis;
    private IArchivoRepository? _archivos;
    private ITokenSatRepository? _tokensSat;
    private IConciliacionCFDIRepository? _conciliacionesCFDI;

    public UnitOfWork(SICOEDbContext context)
    {
        _context = context;
    }

    public IClienteRepository Clientes =>
        _clientes ??= new ClienteRepository(_context);

    public ISolicitudDescargaRepository SolicitudesDescarga =>
        _solicitudesDescarga ??= new SolicitudDescargaRepository(_context);

    public ICFDIRepository CFDIs =>
        _cfdis ??= new CFDIRepository(_context);

    public IArchivoRepository Archivos =>
        _archivos ??= new ArchivoRepository(_context);

    public ITokenSatRepository TokensSat =>
        _tokensSat ??= new TokenSatRepository(_context);

    public IConciliacionCFDIRepository ConciliacionesCFDI =>
        _conciliacionesCFDI ??= new ConciliacionCFDIRepository(_context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}

