using MediatR;
using Microsoft.Extensions.Logging;
using SICOE.Application.Common;
using SICOE.Application.Interfaces.Repositories;
using SICOE.Application.Interfaces.Services;
using SICOE.Application.Interfaces.UnitOfWork;
using SICOE.Application.UseCases.Descarga.SolicitarDescarga;
using SICOE.Domain.Entities;
using SICOE.Domain.Enums;
using SICOE.Domain.ValueObjects;

namespace SICOE.Application.UseCases.Conciliacion.ConciliarCFDI;

/// <summary>
/// Handler para conciliar CFDI solicitados vs recibidos
/// Compara TotalSolicitado vs TotalRecibido y crea nueva solicitud para faltantes si es necesario
/// </summary>
public class ConciliarCFDIHandler : IRequestHandler<ConciliarCFDICommand, Result<ConciliarCFDIResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ConciliarCFDIHandler> _logger;
    private readonly IMediator _mediator;

    public ConciliarCFDIHandler(
        IUnitOfWork unitOfWork,
        ILogger<ConciliarCFDIHandler> logger,
        IMediator mediator)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _mediator = mediator;
    }

    public async Task<Result<ConciliarCFDIResponse>> Handle(ConciliarCFDICommand request, CancellationToken cancellationToken)
    {
        try
        {
            // 1. Obtener solicitud de descarga con CFDI recibidos
            var solicitud = await _unitOfWork.SolicitudesDescarga.GetByIdAsync(request.SolicitudDescargaId, cancellationToken);
            if (solicitud == null)
            {
                return Result<ConciliarCFDIResponse>.Failure($"Solicitud de descarga no encontrada: {request.SolicitudDescargaId}");
            }

            // 2. Contar CFDI recibidos (desde la relación)
            var totalRecibido = await _unitOfWork.CFDIs.CountBySolicitudDescargaIdAsync(request.SolicitudDescargaId, cancellationToken);
            
            _logger.LogInformation(
                "Iniciando conciliación. SolicitudId={SolicitudId}, TotalSolicitado={TotalSolicitado}, TotalRecibido={TotalRecibido}",
                solicitud.Id, solicitud.TotalSolicitado, totalRecibido);

            // 3. Buscar o crear conciliación
            var conciliacion = await _unitOfWork.ConciliacionesCFDI.GetBySolicitudDescargaIdAsync(request.SolicitudDescargaId, cancellationToken);
            
            if (conciliacion == null)
            {
                // Crear nueva conciliación
                conciliacion = new ConciliacionCFDI(
                    solicitud.Id,
                    solicitud.TotalSolicitado,
                    totalRecibido);

                await _unitOfWork.ConciliacionesCFDI.AddAsync(conciliacion, cancellationToken);
                _logger.LogInformation("Nueva conciliación creada. ConciliacionId={ConciliacionId}", conciliacion.Id);
            }
            else
            {
                // Actualizar conciliación existente
                conciliacion.ActualizarTotales(solicitud.TotalSolicitado, totalRecibido);
                _logger.LogInformation("Conciliación actualizada. ConciliacionId={ConciliacionId}", conciliacion.Id);
            }

            // 4. Actualizar TotalRecibido en la solicitud si es diferente
            if (solicitud.TotalRecibido != totalRecibido)
            {
                solicitud.Completar(totalRecibido);
                _logger.LogInformation("TotalRecibido actualizado en solicitud. SolicitudId={SolicitudId}, NuevoTotal={TotalRecibido}", 
                    solicitud.Id, totalRecibido);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 5. Si hay faltantes y se solicita, crear nueva solicitud para faltantes
            int? nuevaSolicitudId = null;
            if (conciliacion.TieneFaltantes() && request.SolicitarFaltantes && conciliacion.PuedeReconciliar(maxIntentos: 3))
            {
                _logger.LogInformation(
                    "Detectados {Faltantes} CFDI faltantes. Creando nueva solicitud para faltantes. ConciliacionId={ConciliacionId}",
                    conciliacion.TotalFaltantes, conciliacion.Id);

                try
                {
                    // Crear nueva solicitud con las mismas fechas y parámetros
                    // NOTA: Por ahora solo creamos la solicitud, no la enviamos al SAT
                    // El usuario deberá enviarla manualmente o mediante un job
                    var nuevaSolicitud = new SolicitudDescarga(
                        solicitud.ClienteId,
                        solicitud.FechaInicial,
                        solicitud.FechaFinal,
                        totalSolicitado: 0); // Se actualizará cuando se procese

                    await _unitOfWork.SolicitudesDescarga.AddAsync(nuevaSolicitud, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    nuevaSolicitudId = nuevaSolicitud.Id;
                    conciliacion.AsignarNuevaSolicitud(nuevaSolicitud.Id);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    _logger.LogInformation(
                        "Nueva solicitud creada para faltantes. SolicitudId={NuevaSolicitudId}, ConciliacionId={ConciliacionId}",
                        nuevaSolicitudId, conciliacion.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al crear nueva solicitud para faltantes. ConciliacionId={ConciliacionId}", conciliacion.Id);
                    conciliacion.MarcarError($"Error al crear nueva solicitud para faltantes: {ex.Message}");
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    // Continuar con la respuesta aunque falle crear nueva solicitud
                }
            }
            else if (conciliacion.TieneFaltantes() && !conciliacion.PuedeReconciliar(maxIntentos: 3))
            {
                _logger.LogWarning(
                    "Hay faltantes pero se alcanzó el máximo de intentos de reconciliación. ConciliacionId={ConciliacionId}, Intentos={Intentos}",
                    conciliacion.Id, conciliacion.IntentosReconciliacion);
            }

            var response = new ConciliarCFDIResponse
            {
                ConciliacionId = conciliacion.Id,
                SolicitudDescargaId = solicitud.Id,
                TotalSolicitado = conciliacion.TotalSolicitado,
                TotalRecibido = conciliacion.TotalRecibido,
                TotalFaltantes = conciliacion.TotalFaltantes,
                TieneFaltantes = conciliacion.TieneFaltantes(),
                NuevaSolicitudId = nuevaSolicitudId,
                Estado = conciliacion.Estado.ToString(),
                Mensaje = conciliacion.TieneFaltantes() 
                    ? $"Faltan {conciliacion.TotalFaltantes} CFDI. {(nuevaSolicitudId.HasValue ? $"Nueva solicitud creada: {nuevaSolicitudId.Value}" : "No se pudo crear nueva solicitud.")}"
                    : "Conciliación completada. Todos los CFDI fueron recibidos."
            };

            return Result<ConciliarCFDIResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al conciliar CFDI para SolicitudId={SolicitudId}", request.SolicitudDescargaId);
            return Result<ConciliarCFDIResponse>.Failure($"Error inesperado al conciliar: {ex.Message}");
        }
    }
}

