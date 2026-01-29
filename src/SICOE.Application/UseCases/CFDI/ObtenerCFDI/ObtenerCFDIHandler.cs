using MediatR;
using Microsoft.Extensions.Logging;
using SICOE.Application.Common;
using SICOE.Application.Interfaces.UnitOfWork;
using SICOE.Domain.ValueObjects;

namespace SICOE.Application.UseCases.CFDI.ObtenerCFDI;

/// <summary>
/// Handler para obtener detalles de un CFDI por UUID o ID
/// </summary>
public class ObtenerCFDIHandler : IRequestHandler<ObtenerCFDIQuery, Result<ObtenerCFDIResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ObtenerCFDIHandler> _logger;

    public ObtenerCFDIHandler(
        IUnitOfWork unitOfWork,
        ILogger<ObtenerCFDIHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<ObtenerCFDIResponse>> Handle(ObtenerCFDIQuery request, CancellationToken cancellationToken)
    {
        try
        {
            Domain.Entities.CFDI? cfdi = null;

            if (!string.IsNullOrWhiteSpace(request.Uuid))
            {
                var uuid = new UUID(request.Uuid);
                cfdi = await _unitOfWork.CFDIs.GetByUuidAsync(uuid, cancellationToken);
            }
            else if (request.Id.HasValue)
            {
                cfdi = await _unitOfWork.CFDIs.GetByIdAsync(request.Id.Value, cancellationToken);
            }
            else
            {
                return Result<ObtenerCFDIResponse>.Failure("Se debe proporcionar UUID o ID del CFDI");
            }

            if (cfdi == null)
            {
                return Result<ObtenerCFDIResponse>.Failure("CFDI no encontrado");
            }

            var response = new ObtenerCFDIResponse
            {
                Id = cfdi.Id,
                Uuid = cfdi.Uuid.Valor,
                RfcEmisor = cfdi.RfcEmisor.Valor,
                NombreEmisor = cfdi.NombreEmisor,
                RegimenFiscalEmisor = cfdi.RegimenFiscalEmisor,
                RfcReceptor = cfdi.RfcReceptor.Valor,
                NombreReceptor = cfdi.NombreReceptor,
                RegimenFiscalReceptor = cfdi.RegimenFiscalReceptor,
                DomicilioFiscalReceptor = cfdi.DomicilioFiscalReceptor,
                UsoCFDI = cfdi.UsoCFDI,
                FechaEmision = cfdi.FechaEmision,
                FechaTimbrado = cfdi.FechaTimbrado,
                Total = cfdi.Total.Valor,
                SubTotal = cfdi.SubTotal,
                TotalImpuestosTrasladados = cfdi.TotalImpuestosTrasladados,
                TipoComprobante = cfdi.TipoComprobante.ToString(),
                Serie = cfdi.Serie,
                Folio = cfdi.Folio,
                FormaPago = cfdi.FormaPago,
                MetodoPago = cfdi.MetodoPago,
                Moneda = cfdi.Total.Moneda,
                LugarExpedicion = cfdi.LugarExpedicion,
                Estatus = cfdi.Estatus.ToString(),
                SolicitudDescargaId = cfdi.SolicitudDescargaId,
                ArchivoId = cfdi.ArchivoId,
                TieneArchivo = cfdi.ArchivoId.HasValue,
                FechaCreacion = cfdi.FechaCreacion
            };

            return Result<ObtenerCFDIResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener CFDI. Uuid={Uuid}, Id={Id}", request.Uuid, request.Id);
            return Result<ObtenerCFDIResponse>.Failure($"Error al obtener CFDI: {ex.Message}");
        }
    }
}

