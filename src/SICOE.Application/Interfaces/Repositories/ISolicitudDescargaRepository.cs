using SICOE.Domain.Entities;
using SICOE.Domain.Enums;

namespace SICOE.Application.Interfaces.Repositories;

/// <summary>
/// Interface específica para repositorio de SolicitudDescarga
/// </summary>
public interface ISolicitudDescargaRepository
{
    Task<SolicitudDescarga?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<SolicitudDescarga?> GetByIdSolicitudSatAsync(string idSolicitudSat, CancellationToken cancellationToken = default);
    Task<IEnumerable<SolicitudDescarga>> GetByClienteIdAsync(int clienteId, CancellationToken cancellationToken = default);
    Task<IEnumerable<SolicitudDescarga>> GetByEstadoAsync(EstadoSolicitud estado, CancellationToken cancellationToken = default);
    Task<IEnumerable<SolicitudDescarga>> GetPendientesOEnProcesoAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<SolicitudDescarga>> GetUltimosDiasAsync(int dias, int? clienteId = null, CancellationToken cancellationToken = default);
    Task AddAsync(SolicitudDescarga solicitud, CancellationToken cancellationToken = default);
    Task UpdateAsync(SolicitudDescarga solicitud, CancellationToken cancellationToken = default);
}

