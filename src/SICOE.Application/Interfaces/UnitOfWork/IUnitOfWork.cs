using SICOE.Application.Interfaces.Repositories;

namespace SICOE.Application.Interfaces.UnitOfWork;

/// <summary>
/// Unit of Work Pattern (DIP: Abstracción para gestión de transacciones)
/// </summary>
public interface IUnitOfWork : IDisposable
{
    IClienteRepository Clientes { get; }
    ISolicitudDescargaRepository SolicitudesDescarga { get; }
    ICFDIRepository CFDIs { get; }
    IArchivoRepository Archivos { get; }
    ITokenSatRepository TokensSat { get; }
    IConciliacionCFDIRepository ConciliacionesCFDI { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}

