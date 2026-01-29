using MediatR;
using Microsoft.Extensions.Logging;
using SICOE.Application.Common;
using SICOE.Application.Interfaces.Repositories;
using SICOE.Application.Interfaces.Services;
using SICOE.Application.Interfaces.UnitOfWork;
using SICOE.Application.UseCases.Conciliacion.ConciliarCFDI;
using SICOE.Domain.Entities;
using SICOE.Domain.Enums;
using SICOE.Domain.ValueObjects;

namespace SICOE.Application.UseCases.Descarga.ProcesarCFDI;

/// <summary>
/// Handler para procesar y guardar CFDI's descargados del SAT
/// Integra SatService, ArchivoService y FielService
/// </summary>
public class ProcesarCFDIHandler : IRequestHandler<ProcesarCFDICommand, Result<ProcesarCFDIResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFielService _fielService;
    private readonly ISatService _satService;
    private readonly IArchivoService _archivoService;
    private readonly ITokenSatService _tokenSatService;
    private readonly ILogger<ProcesarCFDIHandler> _logger;
    private readonly IMediator _mediator;

    public ProcesarCFDIHandler(
        IUnitOfWork unitOfWork,
        IFielService fielService,
        ISatService satService,
        IArchivoService archivoService,
        ITokenSatService tokenSatService,
        ILogger<ProcesarCFDIHandler> logger,
        IMediator mediator)
    {
        _unitOfWork = unitOfWork;
        _fielService = fielService;
        _satService = satService;
        _archivoService = archivoService;
        _tokenSatService = tokenSatService;
        _logger = logger;
        _mediator = mediator;
    }

    public async Task<Result<ProcesarCFDIResponse>> Handle(ProcesarCFDICommand request, CancellationToken cancellationToken)
    {
        try
        {
            // 1. Obtener solicitud de descarga
            var solicitud = await _unitOfWork.SolicitudesDescarga.GetByIdAsync(request.SolicitudId, cancellationToken);
            if (solicitud == null)
            {
                return Result<ProcesarCFDIResponse>.Failure($"Solicitud de descarga no encontrada: {request.SolicitudId}");
            }

            if (string.IsNullOrWhiteSpace(solicitud.IdSolicitudSat))
            {
                return Result<ProcesarCFDIResponse>.Failure("La solicitud no tiene un ID de solicitud SAT asignado");
            }

            _logger.LogInformation(
                "Iniciando procesamiento de CFDI's. SolicitudId={SolicitudId}, IdSolicitudSat={IdSolicitudSat}",
                solicitud.Id, solicitud.IdSolicitudSat);

            // 2. Obtener token SAT dinámicamente de la FIEL (no se almacena)
            // CRÍTICO: SIEMPRE requerir FIEL - el token se obtiene dinámicamente de la sesión
            if (request.CertificadoFiel == null || request.CertificadoFiel.Length == 0 || string.IsNullOrWhiteSpace(request.PasswordFiel))
            {
                return Result<ProcesarCFDIResponse>.Failure(
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
                return Result<ProcesarCFDIResponse>.Failure(tokenResult.Error);
            }

            var tokenSat = tokenResult.Value;
            _logger.LogInformation("Token SAT obtenido dinámicamente para ClienteId={ClienteId}", solicitud.ClienteId);

            // 3. Obtener RFC del cliente (necesario para verificación)
            var rfcSolicitante = solicitud.Cliente?.Rfc?.Valor;
            if (string.IsNullOrWhiteSpace(rfcSolicitante))
            {
                return Result<ProcesarCFDIResponse>.Failure("RFC del solicitante no disponible. Asegúrese de cargar el Cliente con la solicitud.");
            }
            
            // 4. Cargar certificado FIEL si está disponible en el comando (para firmar)
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
                    _logger.LogInformation("Certificado FIEL cargado desde comando para firmar mensaje SOAP");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "No se pudo cargar certificado del comando. Se intentará sin firma.");
                }
            }
            
            // 5. Verificar estado de la descarga en el SAT
            var verificacionResult = await _satService.VerificarDescargaAsync(
                tokenSat,
                solicitud.IdSolicitudSat,
                rfcSolicitante,
                certificate, // Certificado para firmar (se descarta después)
                cancellationToken);

            if (verificacionResult.IsFailure)
            {
                // Ahora sí podemos hacer dispose si falló la verificación, 
                // ya que no llegaremos a la descarga.
                certificate?.Dispose();
                solicitud.MarcarComoError($"Error al verificar descarga: {verificacionResult.Error}");
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return Result<ProcesarCFDIResponse>.Failure(verificacionResult.Error);
            }

            var verificacion = verificacionResult.Value;

            // Actualizar estado según respuesta del SAT
            var estadoSolicitud = verificacion.CodigoEstado switch
            {
                0 => EstadoSolicitud.Pendiente,
                1 => EstadoSolicitud.EnProceso,
                2 => EstadoSolicitud.Completada,
                3 => EstadoSolicitud.Error,
                4 => EstadoSolicitud.Cancelada,
                5 => EstadoSolicitud.Cancelada,
                _ => solicitud.Estado
            };
            solicitud.ActualizarEstado(estadoSolicitud);

            if (verificacion.CodigoEstado != 2) // 2: Terminada
            {
                _logger.LogInformation(
                    "La descarga aún no está terminada. Estado: {CodigoEstado} - {Mensaje}",
                    verificacion.CodigoEstado, verificacion.Mensaje);
                
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<ProcesarCFDIResponse>.Success(new ProcesarCFDIResponse
                {
                    SolicitudId = solicitud.Id,
                    TotalCFDIsProcesados = 0,
                    TotalArchivosGuardados = 0,
                    Completado = false
                });
            }

            // 6. Descargar paquetes del SAT (REQUERIDO: firma digital SOAP)
            // No pasamos idsPaquetes aquí porque ProcesarCFDI verifica el estado internamente
            var paquetesResult = await _satService.DescargarPaquetesAsync(
                tokenSat,
                solicitud.IdSolicitudSat,
                rfcSolicitante,
                certificate, // Pasar certificado para firmar SOAP
                idsPaquetes: null, // Dejar que DescargarPaquetesAsync verifique el estado
                cancellationToken);
            
            // Descargar el certificado después de todos los usos en el SAT
            certificate?.Dispose();

            if (paquetesResult.IsFailure)
            {
                solicitud.MarcarComoError($"Error al descargar paquetes: {paquetesResult.Error}");
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return Result<ProcesarCFDIResponse>.Failure(paquetesResult.Error);
            }

            var paquetes = paquetesResult.Value.ToList();
            _logger.LogInformation("Paquetes descargados: {TotalPaquetes}", paquetes.Count);

            // 5. Procesar cada paquete y guardar CFDI's
            var response = new ProcesarCFDIResponse
            {
                SolicitudId = solicitud.Id,
                Errores = new List<string>()
            };

            foreach (var paquete in paquetes)
            {
                _logger.LogInformation("Procesando paquete: {IdPaquete}", paquete.IdPaquete);

                foreach (var cfdiXml in paquete.CFDIs)
                {
                    try
                    {
                        // Guardar archivo XML usando ArchivoService
                        using var xmlStream = new MemoryStream(cfdiXml.ContenidoXml);
                        var archivoResult = await _archivoService.UploadFileAsync(
                            xmlStream,
                            $"{cfdiXml.Uuid}.xml",
                            "application/xml",
                            TipoArchivo.CFDI,
                            cancellationToken);

                        if (archivoResult.IsFailure)
                        {
                            response.Errores.Add($"Error al guardar archivo XML {cfdiXml.Uuid}: {archivoResult.Error}");
                            continue;
                        }

                        var archivo = archivoResult.Value;

                        _logger.LogInformation("Archivo XML guardado. ArchivoId={ArchivoId}, UUID={Uuid}", 
                            archivo.Id, cfdiXml.Uuid);

                        // Crear Value Objects con validación
                        UUID uuid;
                        RFC rfcEmisor;
                        RFC rfcReceptor;
                        Monto monto;

                        try
                        {
                            uuid = new UUID(cfdiXml.Uuid);
                            rfcEmisor = new RFC(cfdiXml.RfcEmisor);
                            rfcReceptor = new RFC(cfdiXml.RfcReceptor);
                            // Usar la moneda del XML si está disponible, si no MXN por defecto
                            monto = new Monto(cfdiXml.Total, cfdiXml.Moneda ?? "MXN");
                        }
                        catch (ArgumentException ex)
                        {
                            response.Errores.Add($"Error al crear Value Objects para CFDI {cfdiXml.Uuid}: {ex.Message}");
                            continue;
                        }

                        // Mapear tipo de comprobante
                        var tipoComprobante = cfdiXml.TipoComprobante switch
                        {
                            "I" => TipoComprobante.Ingreso,
                            "E" => TipoComprobante.Egreso,
                            "T" => TipoComprobante.Traslado,
                            "N" => TipoComprobante.Nomina,
                            "P" => TipoComprobante.Pago,
                            _ => TipoComprobante.Ingreso // Por defecto
                        };

                        // Crear entidad CFDI en dominio con todos los campos extraídos
                        var cfdi = new Domain.Entities.CFDI(
                            solicitudDescargaId: solicitud.Id,
                            uuid: uuid,
                            rfcEmisor: rfcEmisor,
                            rfcReceptor: rfcReceptor,
                            fechaEmision: cfdiXml.FechaEmision,
                            total: monto,
                            tipoComprobante: tipoComprobante,
                            archivoId: archivo.Id,
                            subTotal: cfdiXml.SubTotal,
                            totalImpuestosTrasladados: cfdiXml.TotalImpuestosTrasladados,
                            fechaTimbrado: cfdiXml.FechaTimbrado,
                            serie: cfdiXml.Serie,
                            folio: cfdiXml.Folio,
                            nombreEmisor: cfdiXml.NombreEmisor,
                            nombreReceptor: cfdiXml.NombreReceptor,
                            regimenFiscalEmisor: cfdiXml.RegimenFiscalEmisor,
                            regimenFiscalReceptor: cfdiXml.RegimenFiscalReceptor,
                            domicilioFiscalReceptor: cfdiXml.DomicilioFiscalReceptor,
                            usoCFDI: cfdiXml.UsoCFDI,
                            formaPago: cfdiXml.FormaPago,
                            metodoPago: cfdiXml.MetodoPago,
                            lugarExpedicion: cfdiXml.LugarExpedicion
                        );

                        await _unitOfWork.CFDIs.AddAsync(cfdi, cancellationToken);
                        response.TotalCFDIsProcesados++;
                        response.TotalArchivosGuardados++;
                    }
                    catch (Exception ex)
                    {
                        var error = $"Error al procesar CFDI {cfdiXml.Uuid}: {ex.Message}";
                        _logger.LogError(ex, error);
                        response.Errores.Add(error);
                    }
                }
            }

            // 6. Actualizar solicitud con total recibido
            solicitud.Completar(response.TotalCFDIsProcesados);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 7. Ejecutar conciliación automática (comparar solicitados vs recibidos)
            try
            {
                var conciliacionCommand = new ConciliarCFDICommand
                {
                    SolicitudDescargaId = solicitud.Id,
                    SolicitarFaltantes = true // Crear nueva solicitud automáticamente si hay faltantes
                };

                var conciliacionResult = await _mediator.Send(conciliacionCommand, cancellationToken);
                
                if (conciliacionResult.IsSuccess && conciliacionResult.Value.TieneFaltantes)
                {
                    _logger.LogInformation(
                        "Conciliación ejecutada. SolicitudId={SolicitudId}, Faltantes={Faltantes}, NuevaSolicitudId={NuevaSolicitudId}",
                        solicitud.Id, conciliacionResult.Value.TotalFaltantes, conciliacionResult.Value.NuevaSolicitudId);
                    
                    // Agregar información de conciliación a la respuesta
                    response.ConciliacionRealizada = true;
                    response.TotalFaltantes = conciliacionResult.Value.TotalFaltantes;
                    response.NuevaSolicitudId = conciliacionResult.Value.NuevaSolicitudId;
                }
                else if (conciliacionResult.IsSuccess)
                {
                    _logger.LogInformation("Conciliación completada. Todos los CFDI fueron recibidos. SolicitudId={SolicitudId}", solicitud.Id);
                    response.ConciliacionRealizada = true;
                }
                else
                {
                    _logger.LogWarning("Error al ejecutar conciliación: {Error}", conciliacionResult.Error);
                    // No fallar el procesamiento si la conciliación falla
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al ejecutar conciliación automática. No se afecta el procesamiento. SolicitudId={SolicitudId}", solicitud.Id);
                // No fallar el procesamiento si la conciliación falla
            }

            response.Completado = true;

            _logger.LogInformation(
                "Procesamiento de CFDI's completado. SolicitudId={SolicitudId}, TotalProcesados={Total}, Errores={Errores}",
                solicitud.Id, response.TotalCFDIsProcesados, response.Errores.Count);

            return Result<ProcesarCFDIResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al procesar CFDI's para SolicitudId={SolicitudId}", request.SolicitudId);
            return Result<ProcesarCFDIResponse>.Failure($"Error inesperado al procesar CFDI's: {ex.Message}");
        }
    }
}

