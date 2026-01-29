using MediatR;
using Microsoft.Extensions.Logging;
using SICOE.Application.Common;
using SICOE.Application.Interfaces.Services;
using SICOE.Application.Interfaces.UnitOfWork;
using SICOE.Domain.Enums;
using SICOE.Application.UseCases.Descarga.VerificarDescarga;

namespace SICOE.Application.UseCases.Descarga.DescargarPaquete;

/// <summary>
/// Handler para descargar el paquete ZIP de una solicitud completada
/// </summary>
public class DescargarPaqueteHandler : IRequestHandler<DescargarPaqueteQuery, Result<DescargarPaqueteResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISatService _satService;
    private readonly IFielService _fielService;
    private readonly ILogger<DescargarPaqueteHandler> _logger;
    private readonly IMediator _mediator;

    public DescargarPaqueteHandler(
        IUnitOfWork unitOfWork,
        ISatService satService,
        IFielService fielService,
        ILogger<DescargarPaqueteHandler> logger,
        IMediator mediator)
    {
        _unitOfWork = unitOfWork;
        _satService = satService;
        _fielService = fielService;
        _logger = logger;
        _mediator = mediator;
    }

    public async Task<Result<DescargarPaqueteResponse>> Handle(
        DescargarPaqueteQuery request, 
        CancellationToken cancellationToken)
    {
        try
        {
            // 1. Obtener solicitud de descarga
            var solicitud = await _unitOfWork.SolicitudesDescarga.GetByIdAsync(request.SolicitudId, cancellationToken);
            if (solicitud == null)
            {
                return Result<DescargarPaqueteResponse>.Failure($"Solicitud no encontrada: {request.SolicitudId}");
            }

            if (string.IsNullOrWhiteSpace(solicitud.IdSolicitudSat))
            {
                return Result<DescargarPaqueteResponse>.Failure("La solicitud no tiene ID de solicitud SAT");
            }

            // CRÍTICO: Siempre verificar el estado para obtener los IdsPaquetes del SAT
            // Los IdsPaquetes no se guardan en la BD, solo se obtienen del SAT cuando se verifica
            // Incluso si el estado es "Completada", necesitamos verificar para obtener los IDs de los paquetes
            bool estadoVerificado = false;
            int totalPaquetesDisponibles = 0;
            List<string>? idsPaquetesObtenidos = null;
            
            _logger.LogInformation(
                "Verificando estado en el SAT para obtener IdsPaquetes. Estado actual: {Estado}",
                solicitud.Estado);

            // Intentar verificar el estado actual en el SAT para obtener los IdsPaquetes
            if (request.CertificadoFiel != null && request.CertificadoFiel.Length > 0 && !string.IsNullOrWhiteSpace(request.PasswordFiel))
            {
                var verificarCommand = new VerificarDescargaCommand
                {
                    SolicitudId = solicitud.Id,
                    CertificadoFiel = request.CertificadoFiel,
                    PasswordFiel = request.PasswordFiel
                };

                var verificacionResult = await _mediator.Send(verificarCommand, cancellationToken);
                
                if (verificacionResult.IsSuccess)
                {
                    estadoVerificado = true;
                    totalPaquetesDisponibles = verificacionResult.Value.TotalPaquetes;
                    idsPaquetesObtenidos = verificacionResult.Value.IdsPaquetes?.ToList();
                    
                    // Recargar solicitud para obtener estado actualizado
                    solicitud = await _unitOfWork.SolicitudesDescarga.GetByIdAsync(solicitud.Id, cancellationToken);
                    if (solicitud == null)
                    {
                        return Result<DescargarPaqueteResponse>.Failure("Error al recargar la solicitud después de verificar estado");
                    }

                    _logger.LogInformation(
                        "Estado verificado en SAT. Estado: {Estado}, TotalPaquetes: {TotalPaquetes}, IdsPaquetes obtenidos: {IdsPaquetes}",
                        solicitud.Estado, totalPaquetesDisponibles, idsPaquetesObtenidos?.Count ?? 0);
                    
                    if (idsPaquetesObtenidos == null || !idsPaquetesObtenidos.Any())
                    {
                        _logger.LogWarning(
                            "Estado verificado pero no se obtuvieron IdsPaquetes. TotalPaquetes reportado: {TotalPaquetes}, Estado: {Estado}",
                            totalPaquetesDisponibles, solicitud.Estado);
                    }
                }
                else
                {
                    _logger.LogWarning("No se pudo verificar el estado en el SAT: {Error}", verificacionResult.Error);
                }
            }
            else
            {
                _logger.LogWarning("No se proporcionó certificado FIEL. No se puede verificar el estado para obtener IdsPaquetes.");
            }

            // Rechazar descarga si el estado es Error o Cancelada (estados que definitivamente no permiten descarga)
            if (solicitud.Estado == EstadoSolicitud.Error || solicitud.Estado == EstadoSolicitud.Cancelada)
            {
                var mensajeEstado = solicitud.Estado switch
                {
                    EstadoSolicitud.Error => "La solicitud tiene un error y no se puede descargar. Por favor, verifique el estado de la solicitud.",
                    EstadoSolicitud.Cancelada => "La solicitud fue cancelada o rechazada y no se puede descargar.",
                    _ => $"La solicitud no está disponible para descarga. Estado: {solicitud.Estado}"
                };

                return Result<DescargarPaqueteResponse>.Failure(mensajeEstado);
            }

            // CRÍTICO: Validar que tengamos los IdsPaquetes antes de intentar descargar
            if (!estadoVerificado)
            {
                // No se pudo verificar el estado - requiere FIEL para verificar
                return Result<DescargarPaqueteResponse>.Failure(
                    "Se requiere certificado FIEL para verificar el estado y obtener los IDs de los paquetes. " +
                    "Por favor, asegúrese de que la sesión esté activa y que el certificado FIEL esté disponible.");
            }

            if (idsPaquetesObtenidos == null || !idsPaquetesObtenidos.Any())
            {
                // Se verificó el estado pero no se obtuvieron IDs de paquetes
                var mensajeEstado = solicitud.Estado switch
                {
                    EstadoSolicitud.Pendiente => "La solicitud aún está pendiente de procesamiento en el SAT. Aún no hay paquetes disponibles para descargar.",
                    EstadoSolicitud.EnProceso => "La solicitud está en proceso. Aún no hay paquetes disponibles para descargar. Por favor, intente más tarde.",
                    EstadoSolicitud.Completada => "La solicitud está completada pero el SAT no reportó IDs de paquetes disponibles. Esto puede indicar un problema con la solicitud o que los paquetes ya fueron descargados.",
                    _ => $"No se obtuvieron IDs de paquetes del SAT. Estado: {solicitud.Estado}, TotalPaquetes reportado: {totalPaquetesDisponibles}"
                };

                _logger.LogWarning(
                    "No se pueden descargar paquetes: {Mensaje}. Estado: {Estado}, TotalPaquetes: {TotalPaquetes}",
                    mensajeEstado, solicitud.Estado, totalPaquetesDisponibles);

                return Result<DescargarPaqueteResponse>.Failure(mensajeEstado);
            }

            _logger.LogInformation(
                "IdsPaquetes obtenidos exitosamente. Total: {Total}, IDs: {Ids}",
                idsPaquetesObtenidos.Count, string.Join(", ", idsPaquetesObtenidos));

            _logger.LogInformation(
                "Iniciando descarga de paquete. SolicitudId={SolicitudId}, IdSolicitudSat={IdSolicitudSat}, Estado={Estado}",
                solicitud.Id, solicitud.IdSolicitudSat, solicitud.Estado);

            // 2. Obtener token SAT dinámicamente de la FIEL (no se almacena)
            // CRÍTICO: SIEMPRE requerir FIEL - el token se obtiene dinámicamente de la sesión
            if (request.CertificadoFiel == null || request.CertificadoFiel.Length == 0 || string.IsNullOrWhiteSpace(request.PasswordFiel))
            {
                return Result<DescargarPaqueteResponse>.Failure(
                    "Se requiere certificado FIEL para obtener token SAT. " +
                    "El frontend debe enviar automáticamente la FIEL desde sessionStorage.");
            }

            var tokenResult = await _fielService.ObtenerTokenSatAsync(
                request.CertificadoFiel,
                request.PasswordFiel,
                cancellationToken);

            if (tokenResult.IsFailure)
            {
                return Result<DescargarPaqueteResponse>.Failure($"Error al obtener token SAT: {tokenResult.Error}");
            }

            var tokenSat = tokenResult.Value;
            _logger.LogInformation("Token SAT obtenido dinámicamente para descarga. ClienteId={ClienteId}", solicitud.ClienteId);

            // 3. Obtener RFC del cliente (necesario para descarga)
            // Asegurar que Cliente esté cargado
            if (solicitud.Cliente == null)
            {
                _logger.LogWarning("Cliente no está cargado en la solicitud. Recargando con Include...");
                solicitud = await _unitOfWork.SolicitudesDescarga.GetByIdAsync(request.SolicitudId, cancellationToken);
                if (solicitud?.Cliente == null)
                {
                    return Result<DescargarPaqueteResponse>.Failure("No se pudo obtener la información del cliente para esta solicitud");
                }
            }
            
            var rfcSolicitante = solicitud.Cliente.Rfc?.Valor;
            if (string.IsNullOrWhiteSpace(rfcSolicitante))
            {
                return Result<DescargarPaqueteResponse>.Failure("RFC del solicitante no disponible en la solicitud");
            }
            
            // 3. Obtener certificado para firmar (se requiere para descarga SOAP)
            System.Security.Cryptography.X509Certificates.X509Certificate2? certificate = null;
            if (request.CertificadoFiel != null && request.CertificadoFiel.Length > 0 && !string.IsNullOrWhiteSpace(request.PasswordFiel))
            {
                try
                {
                    certificate = new System.Security.Cryptography.X509Certificates.X509Certificate2(
                        request.CertificadoFiel,
                        request.PasswordFiel,
                        System.Security.Cryptography.X509Certificates.X509KeyStorageFlags.MachineKeySet |
                        System.Security.Cryptography.X509Certificates.X509KeyStorageFlags.PersistKeySet);
                    _logger.LogInformation("Certificado FIEL cargado desde comando para firmar descarga");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "No se pudo cargar certificado. La descarga podría fallar.");
                }
            }

            // 4. Descargar paquetes del SAT
            // IMPORTANTE: Si ya obtuvimos los IDs de paquetes en la verificación previa, pasarlos directamente
            // para evitar verificar el estado nuevamente (puede fallar si el estado ya es "Completada")
            var paquetesResult = await _satService.DescargarPaquetesAsync(
                tokenSat,
                solicitud.IdSolicitudSat,
                rfcSolicitante,
                certificate,
                idsPaquetesObtenidos, // Pasar los IDs obtenidos en la verificación previa
                cancellationToken);
            
            // Descargar el certificado después de usarlo
            certificate?.Dispose();

            if (paquetesResult.IsFailure)
            {
                _logger.LogError("Error al descargar paquetes del SAT: {Error}", paquetesResult.Error);
                return Result<DescargarPaqueteResponse>.Failure(paquetesResult.Error);
            }

            var paquetes = paquetesResult.Value.ToList();
            if (!paquetes.Any())
            {
                return Result<DescargarPaqueteResponse>.Failure("No hay paquetes disponibles para descargar");
            }

            // 4. Por ahora, retornar el primer paquete
            // TODO: Si hay múltiples paquetes, crear un ZIP que los contenga todos
            var primerPaquete = paquetes.First();

            if (primerPaquete.ContenidoZip == null || primerPaquete.ContenidoZip.Length == 0)
            {
                _logger.LogWarning("El paquete descargado está vacío. IdPaquete={IdPaquete}", primerPaquete.IdPaquete);
                return Result<DescargarPaqueteResponse>.Failure("El paquete descargado está vacío");
            }

            var response = new DescargarPaqueteResponse
            {
                ContenidoZip = primerPaquete.ContenidoZip,
                NombreArchivo = $"CFDI_{solicitud.IdSolicitudSat}_{DateTime.UtcNow:yyyyMMddHHmmss}.zip",
                ContentType = "application/zip"
            };

            _logger.LogInformation(
                "Paquete descargado exitosamente. SolicitudId={SolicitudId}, Tamaño={Tamaño} bytes",
                solicitud.Id, response.ContenidoZip.Length);

            return Result<DescargarPaqueteResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al descargar paquete. SolicitudId={SolicitudId}", request.SolicitudId);
            return Result<DescargarPaqueteResponse>.Failure($"Error inesperado al descargar paquete: {ex.Message}");
        }
    }
}

