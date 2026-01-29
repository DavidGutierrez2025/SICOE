using MediatR;
using Microsoft.Extensions.Logging;
using SICOE.Application.Common;
using SICOE.Application.Interfaces.Services;
using SICOE.Application.Interfaces.UnitOfWork;
using SICOE.Domain.Enums;

namespace SICOE.Application.UseCases.Descarga.VerificarDescarga;

/// <summary>
/// Handler para verificar el estado de una solicitud de descarga en el SAT
/// </summary>
public class VerificarDescargaHandler : IRequestHandler<VerificarDescargaCommand, Result<VerificarDescargaResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFielService _fielService;
    private readonly ISatService _satService;
    private readonly ITokenSatService _tokenSatService;
    private readonly ILogger<VerificarDescargaHandler> _logger;
    private readonly IFielCertificateProvider _certificateProvider;

    public VerificarDescargaHandler(
        IUnitOfWork unitOfWork,
        IFielService fielService,
        ISatService satService,
        ITokenSatService tokenSatService,
        ILogger<VerificarDescargaHandler> logger,
        IFielCertificateProvider certificateProvider)
    {
        _unitOfWork = unitOfWork;
        _fielService = fielService;
        _satService = satService;
        _tokenSatService = tokenSatService;
        _logger = logger;
        _certificateProvider = certificateProvider;
    }

    public async Task<Result<VerificarDescargaResponse>> Handle(VerificarDescargaCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // 1. Obtener solicitud de descarga
            var solicitud = await _unitOfWork.SolicitudesDescarga.GetByIdAsync(request.SolicitudId, cancellationToken);
            if (solicitud == null)
            {
                return Result<VerificarDescargaResponse>.Failure($"Solicitud de descarga no encontrada: {request.SolicitudId}");
            }

            if (string.IsNullOrWhiteSpace(solicitud.IdSolicitudSat))
            {
                return Result<VerificarDescargaResponse>.Failure("La solicitud no tiene un ID de solicitud SAT asignado");
            }

            _logger.LogInformation(
                "Verificando estado de descarga. SolicitudId={SolicitudId}, IdSolicitudSat={IdSolicitudSat}",
                solicitud.Id, solicitud.IdSolicitudSat);

            // 2. Obtener token SAT dinámicamente de la FIEL (no se almacena)
            // CRÍTICO: SIEMPRE requerir FIEL - el token se obtiene dinámicamente de la sesión
            if (request.CertificadoFiel == null || request.CertificadoFiel.Length == 0 || string.IsNullOrWhiteSpace(request.PasswordFiel))
            {
                return Result<VerificarDescargaResponse>.Failure(
                    "Se requiere certificado FIEL para obtener token SAT y firmar la verificación. " +
                    "El frontend debe enviar automáticamente la FIEL desde sessionStorage.");
            }

            var tokenResult = await _fielService.ObtenerTokenSatAsync(
                request.CertificadoFiel,
                request.PasswordFiel,
                cancellationToken);

            if (tokenResult.IsFailure)
            {
                solicitud.MarcarComoError($"Error al obtener token SAT: {tokenResult.Error}");
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return Result<VerificarDescargaResponse>.Failure(tokenResult.Error);
            }

            var tokenSat = tokenResult.Value;
            _logger.LogInformation("Token SAT obtenido dinámicamente para ClienteId={ClienteId}", solicitud.ClienteId);

            // 3. Obtener RFC del cliente (necesario para verificación)
            var rfcSolicitante = solicitud.Cliente?.Rfc?.Valor;
            if (string.IsNullOrWhiteSpace(rfcSolicitante))
            {
                // Intentar obtener RFC del token almacenado o de la solicitud
                _logger.LogWarning("RFC no disponible en Cliente. Intentando obtener de otra fuente.");
                // Por ahora, requerimos que el Cliente esté cargado con Include
                return Result<VerificarDescargaResponse>.Failure("RFC del solicitante no disponible. Asegúrese de cargar el Cliente con la solicitud.");
            }
            
            // 4. Obtener certificado para firmar (se requiere SIEMPRE para verificar descarga)
            // Prioridad: 1. PFX proporcionado en comando, 2. Certificado en Almacén Local
            System.Security.Cryptography.X509Certificates.X509Certificate2? certificate = null;

            if (request.CertificadoFiel != null && request.CertificadoFiel.Length > 0 && !string.IsNullOrWhiteSpace(request.PasswordFiel))
            {
                var certResult = _certificateProvider.GetFromPfx(request.CertificadoFiel, request.PasswordFiel);
                if (certResult.IsSuccess)
                {
                    certificate = certResult.Value;
                    _logger.LogInformation("Certificado cargado desde comando para firmar mensaje SOAP");
                }
            }
            
            if (certificate == null)
            {
                // Intentar cargar desde Almacén (Thumbprint en config)
                var certFromStore = _certificateProvider.GetDefaultCertificate();
                if (certFromStore.IsSuccess)
                {
                    certificate = certFromStore.Value;
                    _logger.LogInformation("Certificado cargado desde Almacén de Windows para firmar mensaje SOAP. Subject: {Subject}", certificate.Subject);
                }
            }

            if (certificate == null)
            {
                _logger.LogWarning("No se tiene certificado disponible para firmar. La verificación al SAT probablemente fallará por falta de firma.");
            }
            
            // 5. Verificar estado en el SAT
            var verificacionResult = await _satService.VerificarDescargaAsync(
                tokenSat,
                solicitud.IdSolicitudSat,
                rfcSolicitante,
                certificate, // Certificado para firmar (se descarta después)
                cancellationToken);
            
            // Descarta el certificado inmediatamente después de usarlo
            certificate?.Dispose();

            if (verificacionResult.IsFailure)
            {
                solicitud.MarcarComoError($"Error al verificar descarga: {verificacionResult.Error}");
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return Result<VerificarDescargaResponse>.Failure(verificacionResult.Error);
            }

            var verificacion = verificacionResult.Value;

            // 4. Actualizar estado de la solicitud según respuesta del SAT
            // IMPORTANTE: verificacion.CodigoEstado ya viene mapeado por SatSoapResponseParser:
            // 0=Pendiente (Aceptada), 1=EnProceso, 2=Completada (Terminada), 3=Error, 4=Cancelada (Rechazada/Vencida)
            var estadoSolicitud = verificacion.CodigoEstado switch
            {
                0 => EstadoSolicitud.Pendiente, // Aceptada (del SAT)
                1 => EstadoSolicitud.EnProceso, // En Proceso (del SAT)
                2 => EstadoSolicitud.Completada, // Terminada (del SAT)
                3 => EstadoSolicitud.Error, // Error (del SAT)
                4 => EstadoSolicitud.Cancelada, // Rechazada o Vencida (del SAT)
                _ => solicitud.Estado
            };

            solicitud.ActualizarEstado(estadoSolicitud);

            // Actualizar total solicitado si viene en la respuesta (disponible cuando está en proceso o completada)
            // El SAT devuelve NumeroCFDIS (TotalCFDIs) cuando la solicitud está en proceso o completada
            // El SAT devuelve NumeroCFDIS (TotalCFDIs) cuando la solicitud está en proceso o completada.
            // BUG FIX: El SAT puede devolver 0 en NumeroCFDIS pero incluir IdsPaquetes (visto en documentación Pág 11 Verificación).
            // Siempre debemos actualizar el total si viene un valor (incluso 0) para reflejar la respuesta del SAT.
            if (verificacion.TotalCFDIs.HasValue)
            {
                solicitud.ActualizarTotalSolicitado(verificacion.TotalCFDIs.Value);
                _logger.LogInformation("TotalSolicitado actualizado a {TotalSolicitado} para SolicitudId={SolicitudId}", 
                    verificacion.TotalCFDIs.Value, solicitud.Id);
            }
        
            // Si TotalSolicitado es 0 pero hay paquetes, nos aseguramos de que el estado permita procesar
            if (verificacion.TotalPaquetes > 0)
            {
                // Si el SAT no devolvió número de CFDIs pero sí paquetes, estimar al menos 1 por paquete
                // o usar el número de CFDIs si está disponible.
                int totalEstimado = verificacion.TotalCFDIs ?? (verificacion.TotalPaquetes * 1);
                
                if (solicitud.TotalSolicitado == 0 || solicitud.TotalSolicitado < totalEstimado)
                {
                    solicitud.ActualizarTotalSolicitado(totalEstimado);
                }

                _logger.LogInformation("SolicitudId={SolicitudId} tiene {TotalPaquetes} paquetes. TotalSolicitado ajustado a {Total}.",
                    solicitud.Id, verificacion.TotalPaquetes, solicitud.TotalSolicitado);
            }

            // IMPORTANTE: Si la solicitud está terminada (Completada), también actualizar TotalRecibido
            // con el valor de TotalCFDIs para que se muestre correctamente en la tabla
            // Esto permite que la columna "Cantidad de Documentos" se actualice sin necesidad de procesar los CFDI
            if (estadoSolicitud == EstadoSolicitud.Completada && verificacion.TotalCFDIs.HasValue && verificacion.TotalCFDIs.Value > 0)
            {
                // Si TotalRecibido aún no se ha actualizado (está en 0 o es menor), actualizarlo
                if (solicitud.TotalRecibido == 0 || solicitud.TotalRecibido < verificacion.TotalCFDIs.Value)
                {
                    solicitud.ActualizarTotalRecibido(verificacion.TotalCFDIs.Value);
                    _logger.LogInformation(
                        "TotalRecibido actualizado a {TotalRecibido} para SolicitudId={SolicitudId} (solicitud completada en el SAT)", 
                        verificacion.TotalCFDIs.Value, solicitud.Id);
                }
                else
                {
                    // Si ya tiene un valor mayor, mantenerlo (puede haber sido actualizado por ProcesarCFDI)
                    _logger.LogInformation(
                        "TotalRecibido ya tiene un valor ({TotalRecibido}) mayor o igual al reportado por el SAT ({TotalCFDIs}) para SolicitudId={SolicitudId}. Manteniendo valor actual.", 
                        solicitud.TotalRecibido, verificacion.TotalCFDIs.Value, solicitud.Id);
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Estado de descarga verificado. SolicitudId={SolicitudId}, CodigoEstado={CodigoEstado}, TotalPaquetes={TotalPaquetes}",
                solicitud.Id, verificacion.CodigoEstado, verificacion.TotalPaquetes);

            var response = new VerificarDescargaResponse
            {
                SolicitudId = solicitud.Id,
                CodigoEstado = verificacion.CodigoEstado,
                Mensaje = verificacion.Mensaje,
                TotalPaquetes = verificacion.TotalPaquetes,
                IdsPaquetes = verificacion.IdsPaquetes,
                TotalCFDIs = verificacion.TotalCFDIs,
                RequiereProcesamiento = verificacion.CodigoEstado == 2 && verificacion.TotalPaquetes > 0 // 2 = Completada (Terminada)
            };

            return Result<VerificarDescargaResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al verificar descarga. SolicitudId={SolicitudId}", request.SolicitudId);
            return Result<VerificarDescargaResponse>.Failure($"Error inesperado al verificar descarga: {ex.Message}");
        }
    }
}

