using MediatR;
using Microsoft.Extensions.Logging;
using SICOE.Application.Common;
using SICOE.Application.Interfaces.UnitOfWork;
using SICOE.Domain.Enums;

namespace SICOE.Application.UseCases.Descarga.ListarSolicitudes;

/// <summary>
/// Handler para listar solicitudes de descarga de los últimos N días
/// </summary>
public class ListarSolicitudesHandler : IRequestHandler<ListarSolicitudesQuery, Result<ListarSolicitudesResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ListarSolicitudesHandler> _logger;

    public ListarSolicitudesHandler(
        IUnitOfWork unitOfWork,
        ILogger<ListarSolicitudesHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<ListarSolicitudesResponse>> Handle(ListarSolicitudesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var fechaLimite = DateTime.UtcNow.AddDays(-request.DiasAtras);
            
            // CRÍTICO: Validar que se proporcione RFC o ClienteId para filtrar las solicitudes del usuario
            // Si no se proporciona ninguno, NO se deben mostrar todas las solicitudes (seguridad)
            // Se debe requerir al menos uno de los dos parámetros para evitar mostrar datos de otros usuarios
            if (!request.ClienteId.HasValue && string.IsNullOrWhiteSpace(request.Rfc))
            {
                _logger.LogWarning("Intento de listar solicitudes sin RFC ni ClienteId. Esto no está permitido por seguridad.");
                return Result<ListarSolicitudesResponse>.Failure(
                    "Se requiere RFC o ClienteId para filtrar las solicitudes. No se pueden mostrar todas las solicitudes sin filtro.");
            }
            
            // Determinar ClienteId: si se proporciona RFC, buscar el ClienteId correspondiente
            int? clienteIdFinal = request.ClienteId;
            
            if (!clienteIdFinal.HasValue && !string.IsNullOrWhiteSpace(request.Rfc))
            {
                var rfcNormalizado = request.Rfc.Trim().ToUpper();
                _logger.LogInformation("Buscando cliente por RFC normalizado: '{Rfc}'", rfcNormalizado);
                
                // Buscar cliente por RFC
                var cliente = await _unitOfWork.Clientes.GetByRfcAsync(rfcNormalizado, cancellationToken);
                if (cliente != null)
                {
                    clienteIdFinal = cliente.Id;
                    _logger.LogInformation("✅ Cliente encontrado por RFC '{Rfc}': ClienteId={ClienteId}, RFC en BD='{RfcBd}', Activo={Activo}", 
                        rfcNormalizado, clienteIdFinal, cliente.Rfc?.Valor ?? "null", cliente.Activo);
                    
                    // Verificar que el cliente esté activo
                    if (!cliente.Activo)
                    {
                        _logger.LogWarning("⚠️ Cliente encontrado pero está INACTIVO. ClienteId={ClienteId}, RFC={Rfc}", clienteIdFinal, rfcNormalizado);
                    }
                }
                else
                {
                    _logger.LogWarning("❌ No se encontró cliente con RFC '{Rfc}'. Verificando si hay clientes en la BD...", rfcNormalizado);
                    
                    // DEBUG: Listar todos los clientes para diagnóstico (solo en desarrollo)
                    try
                    {
                        var todosLosClientes = await _unitOfWork.Clientes.GetActivosAsync(cancellationToken);
                        var rfcEnBd = todosLosClientes.Select(c => c.Rfc?.Valor ?? "null").ToList();
                        _logger.LogInformation("Clientes activos en BD (RFCs): {RfcList}", string.Join(", ", rfcEnBd));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "No se pudieron listar clientes para diagnóstico");
                    }
                    
                    // Si no se encuentra el cliente, retornar lista vacía (no es un error, simplemente no hay datos para este RFC)
                    return Result<ListarSolicitudesResponse>.Success(new ListarSolicitudesResponse
                    {
                        Solicitudes = new List<SolicitudDescargaItem>(),
                        Total = 0
                    });
                }
            }
            
            _logger.LogInformation("Listando solicitudes de descarga. ClienteId: {ClienteId}, RFC solicitado: {Rfc}, Días atrás: {DiasAtras}, FechaLimite: {FechaLimite}",
                clienteIdFinal?.ToString() ?? "null", request.Rfc ?? "null", request.DiasAtras, fechaLimite);

            // CRÍTICO: Validar que tenemos ClienteId antes de buscar solicitudes
            // Sin ClienteId no podemos filtrar correctamente por usuario
            if (!clienteIdFinal.HasValue)
            {
                _logger.LogError("No se pudo determinar ClienteId. RFC: {Rfc}, ClienteId original: {ClienteIdOriginal}", 
                    request.Rfc ?? "null", request.ClienteId?.ToString() ?? "null");
                return Result<ListarSolicitudesResponse>.Failure(
                    "No se pudo identificar al cliente. Verifique que el RFC esté correcto.");
            }

            // Obtener solicitudes de los últimos N días usando el nuevo método del repositorio
            // El repositorio filtra por ClienteId, asegurando que solo se muestren las solicitudes del usuario
            var solicitudesFiltradas = (await _unitOfWork.SolicitudesDescarga.GetUltimosDiasAsync(
                request.DiasAtras,
                clienteIdFinal,
                cancellationToken))
                .Where(s => !string.IsNullOrWhiteSpace(s.IdSolicitudSat)) // Solo solicitudes con IdSolicitudSat válido (creadas realmente en el SAT)
                .ToList();

            _logger.LogInformation(
                "Encontradas {Total} solicitudes válidas (con IdSolicitudSat) para ClienteId={ClienteId}, RFC={Rfc} en los últimos {DiasAtras} días", 
                solicitudesFiltradas.Count, clienteIdFinal.Value, request.Rfc ?? "null", request.DiasAtras);
            
            // Log adicional para debug: mostrar RFCs de las solicitudes encontradas
            if (solicitudesFiltradas.Any())
            {
                var rfcSolicitudes = solicitudesFiltradas
                    .Select(s => s.Cliente?.Rfc?.Valor ?? "N/A")
                    .Distinct()
                    .ToList();
                _logger.LogDebug("RFCs de las solicitudes encontradas: {RfcSolicitudes}", string.Join(", ", rfcSolicitudes));
            }

            var items = solicitudesFiltradas.Select(s => new SolicitudDescargaItem
            {
                Id = s.Id,
                IdSolicitudSat = s.IdSolicitudSat!,
                RfcContribuyente = s.Cliente?.Rfc?.Valor ?? "N/A",
                TipoDescarga = "CFDI", // Por ahora siempre CFDI, se puede mejorar después
                CantidadDocumentos = s.TotalRecibido > 0 ? s.TotalRecibido : s.TotalSolicitado,
                FechaCreacion = s.FechaCreacion,
                Estado = s.Estado.ToString()
            }).ToList();

            var response = new ListarSolicitudesResponse
            {
                Solicitudes = items,
                Total = items.Count
            };

            return Result<ListarSolicitudesResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al listar solicitudes de descarga");
            return Result<ListarSolicitudesResponse>.Failure($"Error al listar solicitudes: {ex.Message}");
        }
    }
}

