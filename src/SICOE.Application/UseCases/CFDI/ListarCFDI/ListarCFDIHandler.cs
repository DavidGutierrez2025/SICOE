using MediatR;
using Microsoft.Extensions.Logging;
using SICOE.Application.Common;
using SICOE.Application.Interfaces.UnitOfWork;
using SICOE.Domain.Enums;

namespace SICOE.Application.UseCases.CFDI.ListarCFDI;

/// <summary>
/// Handler para listar CFDI procesados con filtros y paginación
/// </summary>
public class ListarCFDIHandler : IRequestHandler<ListarCFDIQuery, Result<ListarCFDIResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ListarCFDIHandler> _logger;

    public ListarCFDIHandler(
        IUnitOfWork unitOfWork,
        ILogger<ListarCFDIHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<ListarCFDIResponse>> Handle(ListarCFDIQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Filtrar por cliente si se proporciona (RFC receptor debe coincidir con el RFC del cliente)
            string? rfcReceptorFiltro = null;
            if (request.ClienteId.HasValue)
            {
                var cliente = await _unitOfWork.Clientes.GetByIdAsync(request.ClienteId.Value, cancellationToken);
                if (cliente != null)
                {
                    rfcReceptorFiltro = cliente.Rfc.Valor;
                }
                else
                {
                    // Si el cliente no existe, no hay resultados
                    return Result<ListarCFDIResponse>.Success(new ListarCFDIResponse
                    {
                        CFDIs = new List<CFDIItemDto>(),
                        TotalRegistros = 0,
                        PageNumber = request.PageNumber,
                        PageSize = request.PageSize
                    });
                }
            }

            // Si se especifica RFC receptor en la query, tiene prioridad sobre el filtro de cliente
            if (!string.IsNullOrWhiteSpace(request.RfcReceptor))
            {
                rfcReceptorFiltro = request.RfcReceptor;
            }

            // Calcular skip para paginación
            var skip = (request.PageNumber - 1) * request.PageSize;

            // Obtener CFDI usando el método del repositorio que maneja Includes en Infrastructure
            var totalRegistros = await _unitOfWork.CFDIs.CountWithFiltersAsync(
                solicitudDescargaId: request.SolicitudDescargaId,
                rfcEmisor: request.RfcEmisor,
                rfcReceptor: rfcReceptorFiltro ?? request.RfcReceptor,
                fechaInicial: request.FechaInicial,
                fechaFinal: request.FechaFinal,
                tipoComprobante: request.TipoComprobante,
                uuid: request.Uuid,
                cancellationToken: cancellationToken);

            var cfdis = await _unitOfWork.CFDIs.GetWithFiltersAsync(
                solicitudDescargaId: request.SolicitudDescargaId,
                rfcEmisor: request.RfcEmisor,
                rfcReceptor: rfcReceptorFiltro ?? request.RfcReceptor,
                fechaInicial: request.FechaInicial,
                fechaFinal: request.FechaFinal,
                tipoComprobante: request.TipoComprobante,
                uuid: request.Uuid,
                skip: skip,
                take: request.PageSize,
                cancellationToken: cancellationToken);

            // Mapear a DTOs
            var dtos = cfdis.Select(c => new CFDIItemDto
            {
                Id = c.Id,
                Uuid = c.Uuid.Valor,
                RfcEmisor = c.RfcEmisor.Valor,
                NombreEmisor = c.NombreEmisor,
                RfcReceptor = c.RfcReceptor.Valor,
                NombreReceptor = c.NombreReceptor,
                FechaEmision = c.FechaEmision,
                FechaTimbrado = c.FechaTimbrado,
                Total = c.Total.Valor,
                SubTotal = c.SubTotal,
                TotalImpuestosTrasladados = c.TotalImpuestosTrasladados,
                TipoComprobante = c.TipoComprobante.ToString(),
                Serie = c.Serie,
                Folio = c.Folio,
                Estatus = c.Estatus.ToString(),
                Moneda = c.Total.Moneda,
                SolicitudDescargaId = c.SolicitudDescargaId,
                TieneArchivo = c.ArchivoId.HasValue
            }).ToList();

            var response = new ListarCFDIResponse
            {
                CFDIs = dtos,
                TotalRegistros = totalRegistros,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };

            _logger.LogInformation(
                "CFDI listados. Total={Total}, Pagina={PageNumber}, Tamaño={PageSize}",
                totalRegistros, request.PageNumber, request.PageSize);

            return Result<ListarCFDIResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al listar CFDI");
            return Result<ListarCFDIResponse>.Failure($"Error al listar CFDI: {ex.Message}");
        }
    }
}

