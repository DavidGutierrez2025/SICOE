using SICOE.Domain.Entities;
using SICOE.Domain.ValueObjects;

namespace SICOE.Application.Interfaces.Repositories;

/// <summary>
/// Interface específica para repositorio de CFDI
/// </summary>
public interface ICFDIRepository
{
    Task<CFDI?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<CFDI?> GetByUuidAsync(UUID uuid, CancellationToken cancellationToken = default);
    Task<IEnumerable<CFDI>> GetBySolicitudDescargaIdAsync(int solicitudDescargaId, CancellationToken cancellationToken = default);
    Task<int> CountBySolicitudDescargaIdAsync(int solicitudDescargaId, CancellationToken cancellationToken = default);
    Task<IEnumerable<CFDI>> GetWithFiltersAsync(
        int? solicitudDescargaId = null,
        string? rfcEmisor = null,
        string? rfcReceptor = null,
        DateTime? fechaInicial = null,
        DateTime? fechaFinal = null,
        string? tipoComprobante = null,
        string? uuid = null,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default);
    Task<int> CountWithFiltersAsync(
        int? solicitudDescargaId = null,
        string? rfcEmisor = null,
        string? rfcReceptor = null,
        DateTime? fechaInicial = null,
        DateTime? fechaFinal = null,
        string? tipoComprobante = null,
        string? uuid = null,
        CancellationToken cancellationToken = default);
    Task AddAsync(CFDI cfdi, CancellationToken cancellationToken = default);
    Task UpdateAsync(CFDI cfdi, CancellationToken cancellationToken = default);
}

