using MediatR;
using Microsoft.Extensions.Logging;
using SICOE.Application.Common;
using SICOE.Application.Interfaces.Services;
using SICOE.Application.Interfaces.UnitOfWork;
using SICOE.Domain.Entities;
using SICOE.Domain.Enums;
using SICOE.Domain.ValueObjects;
using System;

namespace SICOE.Application.UseCases.Descarga.SolicitarDescarga;

/// <summary>
/// Handler para el caso de uso SolicitarDescarga
/// Integra FielService y SatService para solicitar descarga masiva de CFDI's al SAT
/// Implementa CQRS Pattern con MediatR
/// </summary>
public class SolicitarDescargaHandler : IRequestHandler<SolicitarDescargaCommand, Result<SolicitarDescargaResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFielService _fielService;
    private readonly ISatService _satService;
    private readonly ITokenSatService _tokenSatService;
    private readonly IMessageQueueService _messageQueueService;
    private readonly ILogger<SolicitarDescargaHandler> _logger;
    private readonly IFielCertificateProvider _certificateProvider;

    public SolicitarDescargaHandler(
        IUnitOfWork unitOfWork,
        IFielService fielService,
        ISatService satService,
        ITokenSatService tokenSatService,
        IMessageQueueService messageQueueService,
        ILogger<SolicitarDescargaHandler> logger,
        IFielCertificateProvider certificateProvider)
    {
        _unitOfWork = unitOfWork;
        _fielService = fielService;
        _satService = satService;
        _tokenSatService = tokenSatService;
        _messageQueueService = messageQueueService;
        _logger = logger;
        _certificateProvider = certificateProvider;
    }

    public async Task<Result<SolicitarDescargaResponse>> Handle(SolicitarDescargaCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validar fechas
            if (request.FechaInicial > request.FechaFinal)
            {
                return Result<SolicitarDescargaResponse>.Failure("La fecha inicial no puede ser mayor que la fecha final");
            }

            // Validar y normalizar EstadoComprobante según TipoSolicitud
            // REGLAS DEL SAT según especificación:
            // - Para Metadata: permite "Vigente", "Cancelado", "Todos" (todos incluye vigentes y cancelados)
            // - Para CFDI: solo permite "Vigente", NO permite "Cancelado" ni "Todos" con cancelados
            //   - Si TipoSolicitud="CFDI" y EstadoComprobante="Cancelado" -> ERROR (no permitido)
            //   - Si TipoSolicitud="CFDI" y EstadoComprobante="Todos" -> convertir a "Vigente" (solo vigentes)
            string tipoSolicitudFinal = request.TipoSolicitud ?? "CFDI";
            string? estadoComprobanteNormalizado = null;
            
            if (!string.IsNullOrWhiteSpace(request.EstadoComprobante))
            {
                var estadoComprobanteOriginal = request.EstadoComprobante.Trim();
                
                // Validación específica para CFDI
                if (tipoSolicitudFinal.Equals("CFDI", StringComparison.OrdinalIgnoreCase))
                {
                    if (estadoComprobanteOriginal.Equals("Cancelado", StringComparison.OrdinalIgnoreCase))
                    {
                        return Result<SolicitarDescargaResponse>.Failure(
                            "Para solicitudes de tipo CFDI, el SAT NO permite la descarga de comprobantes cancelados. " +
                            "Solo se pueden descargar comprobantes vigentes. Por favor, seleccione 'Vigente' o 'Todos' (solo vigentes).");
                    }
                    
                    // Para CFDI, "Todos" solo incluye vigentes (el SAT no permite cancelados)
                    if (estadoComprobanteOriginal.Equals("Todos", StringComparison.OrdinalIgnoreCase))
                    {
                        estadoComprobanteNormalizado = "Vigente";
                        _logger.LogInformation(
                            "TipoSolicitud=CFDI: EstadoComprobante 'Todos' convertido a 'Vigente' (el SAT no permite cancelados para CFDI)");
                    }
                    else if (estadoComprobanteOriginal.Equals("Vigente", StringComparison.OrdinalIgnoreCase))
                    {
                        estadoComprobanteNormalizado = "Vigente";
                    }
                }
                else if (tipoSolicitudFinal.Equals("Metadata", StringComparison.OrdinalIgnoreCase))
                {
                    // Para Metadata, se permiten todos los valores: "Vigente", "Cancelado", "Todos"
                    estadoComprobanteNormalizado = estadoComprobanteOriginal;
                    _logger.LogInformation(
                        "TipoSolicitud=Metadata: EstadoComprobante '{EstadoComprobante}' aceptado (Metadata permite vigentes y cancelados)",
                        estadoComprobanteOriginal);
                }
                else
                {
                    // Tipo de solicitud desconocido, mantener el valor original
                    estadoComprobanteNormalizado = estadoComprobanteOriginal;
                }
            }
            // Si es null o vacío, se deja como null (el SAT usa "Vigente" por defecto)

            // IMPORTANTE: La FIEL se recibe del frontend desde sessionStorage, se usa SOLO para obtener token SAT y firmar,
            // y se descarta inmediatamente. NUNCA se almacena.
            // El token se obtiene dinámicamente de la FIEL en cada solicitud (no se almacena en BD).
            
            // CRÍTICO: SIEMPRE requerir FIEL - el token se obtiene dinámicamente de la sesión
            if (string.IsNullOrWhiteSpace(request.CertificadoCer) || 
                string.IsNullOrWhiteSpace(request.ClavePrivadaKey) || 
                string.IsNullOrWhiteSpace(request.PasswordFiel))
            {
                return Result<SolicitarDescargaResponse>.Failure(
                    "Se requiere certificado FIEL para obtener token SAT y firmar la solicitud. " +
                    "El frontend debe enviar automáticamente la FIEL desde sessionStorage.");
            }

            string rfcSolicitante;
            string tokenSat;
            byte[]? pfxFile = null;
            System.Security.Cryptography.X509Certificates.X509Certificate2? certificate = null;

            // Convertir base64 a byte arrays
            byte[] certificadoCerBytes;
            byte[] clavePrivadaKeyBytes;
            try
            {
                certificadoCerBytes = Convert.FromBase64String(request.CertificadoCer);
                clavePrivadaKeyBytes = Convert.FromBase64String(request.ClavePrivadaKey);
            }
            catch (FormatException ex)
            {
                _logger.LogError(ex, "Error al decodificar archivos FIEL desde base64");
                return Result<SolicitarDescargaResponse>.Failure("Error al procesar los archivos FIEL. Verifique que los archivos .cer y .key estén en formato base64 válido.");
            }

            // Combinar .cer y .key en .pfx temporalmente (en memoria)
            try
            {
                pfxFile = SICOE.Application.Common.Helpers.Fiel.FielCerKeyHelper.CombinarCerYKeyEnPfx(
                    certificadoCerBytes,
                    clavePrivadaKeyBytes,
                    request.PasswordFiel);
                
                _logger.LogInformation("Archivos .cer y .key combinados exitosamente en .pfx temporal");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al combinar .cer y .key en .pfx");
                return Result<SolicitarDescargaResponse>.Failure($"Error al procesar los archivos FIEL: {ex.Message}. Verifique que los archivos .cer y .key sean válidos y que la contraseña sea correcta.");
            }

            // 1. Validar FIEL (la FIEL se descarta después de obtener el token)
            var validacionFiel = await _fielService.ValidarFielAsync(
                pfxFile,
                request.PasswordFiel,
                cancellationToken);

            if (validacionFiel.IsFailure)
            {
                _logger.LogWarning("Validación de FIEL falló: {Error}", validacionFiel.Error);
                return Result<SolicitarDescargaResponse>.Failure($"Error al validar FIEL: {validacionFiel.Error}");
            }

            // 2. Extraer RFC del certificado
            var rfcResult = await _fielService.ExtraerRfcDelCertificadoAsync(
                pfxFile,
                request.PasswordFiel,
                cancellationToken);

            if (rfcResult.IsFailure)
            {
                return Result<SolicitarDescargaResponse>.Failure($"Error al extraer RFC del certificado: {rfcResult.Error}");
            }

            rfcSolicitante = rfcResult.Value;
            _logger.LogInformation("RFC extraído del certificado FIEL: {Rfc}", rfcSolicitante);

            // 3. Buscar o crear cliente por RFC
            var cliente = await _unitOfWork.Clientes.GetByRfcAsync(rfcSolicitante, cancellationToken);
            
            if (cliente == null)
            {
                // Cliente no existe, crearlo automáticamente
                try
                {
                    var rfcValueObject = new RFC(rfcSolicitante);
                    // Usar RFC como razón social por defecto (puede actualizarse después)
                    cliente = new Cliente(rfcValueObject, $"Contribuyente {rfcSolicitante}");
                    await _unitOfWork.Clientes.AddAsync(cliente, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation("Cliente creado automáticamente para RFC: {Rfc}, ClienteId: {ClienteId}", rfcSolicitante, cliente.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al crear cliente para RFC: {Rfc}", rfcSolicitante);
                    return Result<SolicitarDescargaResponse>.Failure($"Error al crear cliente: {ex.Message}");
                }
            }
            else if (!cliente.Activo)
            {
                // Cliente existe pero está inactivo, activarlo
                cliente.Activar();
                await _unitOfWork.Clientes.UpdateAsync(cliente, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Cliente inactivo activado para RFC: {Rfc}, ClienteId: {ClienteId}", rfcSolicitante, cliente.Id);
            }

            _logger.LogInformation(
                "Iniciando solicitud de descarga. ClienteId={ClienteId}, RFC={Rfc}, Fechas={FechaInicial} a {FechaFinal}",
                cliente.Id, rfcSolicitante, request.FechaInicial, request.FechaFinal);

            // 3. Obtener token del SAT dinámicamente (token fresco de la sesión)
            var tokenResult = await _fielService.ObtenerTokenSatAsync(
                pfxFile,
                request.PasswordFiel,
                cancellationToken);

            if (tokenResult.IsFailure)
            {
                _logger.LogError("Error al obtener token SAT: {Error}", tokenResult.Error);
                return Result<SolicitarDescargaResponse>.Failure($"Error al obtener token del SAT: {tokenResult.Error}");
            }

            tokenSat = tokenResult.Value.Trim(); // Asegurar que no tenga espacios
            _logger.LogInformation("Token SAT obtenido dinámicamente para RFC: {Rfc}, Longitud: {Length}", 
                rfcSolicitante, tokenSat.Length);
            _logger.LogDebug("Token SAT preview: {Preview}", 
                tokenSat.Length > 20 ? $"{tokenSat.Substring(0, 10)}...{tokenSat.Substring(tokenSat.Length - 10)}" : "***");

            // 4. Obtener certificado para firmar (se requiere SIEMPRE para solicitar descarga)
            // Prioridad: 1. PFX generado en este request, 2. Certificado en Almacén Local
            if (pfxFile != null && !string.IsNullOrWhiteSpace(request.PasswordFiel))
            {
                var certResult = _certificateProvider.GetFromPfx(pfxFile, request.PasswordFiel);
                if (certResult.IsSuccess)
                {
                    certificate = certResult.Value;
                    _logger.LogInformation("Certificado cargado desde PFX para firmar mensaje SOAP");
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
                else
                {
                    _logger.LogWarning("No se pudo cargar certificado por defecto del Almacén: {Error}", certFromStore.Error);
                }
            }

            if (certificate == null)
            {
                _logger.LogWarning("No se tiene certificado disponible para firmar. La solicitud al SAT probablemente fallará por falta de firma.");
            }

            // 6. Crear solicitud en dominio usando el cliente obtenido/creado
            var solicitud = new SolicitudDescarga(
                cliente.Id,
                request.FechaInicial,
                request.FechaFinal,
                0 // Se actualizará cuando se reciba respuesta del SAT
            );

            // Guardar en BD primero para tener el ID
            await _unitOfWork.SolicitudesDescarga.AddAsync(solicitud, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Solicitud de descarga creada en BD: Id={SolicitudId}, ClienteId={ClienteId}",
                solicitud.Id, cliente.Id);

            // 7. Solicitar descarga masiva al SAT
            // Nota: estadoComprobanteNormalizado ya fue validado y normalizado según TipoSolicitud
            // - Para CFDI: solo "Vigente" permitido (no cancelados)
            // - Para Metadata: "Vigente", "Cancelado", "Todos" permitidos
            // Lógica de Recibidos vs Emitidos:
            // - Si request.TipoDescarga = "emitidos": usar SolicitaDescargaEmitidos
            //   - RfcReceptor: opcional, filtra por receptor específico
            //   - RfcEmisor: debe ser el RfcSolicitante (no se usa RfcEmisor del request)
            // - Si request.TipoDescarga = "recibidos" o null: usar SolicitaDescargaRecibidos
            //   - RfcReceptor: no se usa (siempre es el RfcSolicitante)
            //   - RfcEmisor: opcional del request, filtra por emisor específico
            
            string? rfcReceptorFinal = null;
            string? rfcEmisorFinal = null;
            
            // Determinar tipo de descarga: si TipoDescarga no se proporciona, inferir de RfcReceptor (compatibilidad hacia atrás)
            var tipoDescarga = request.TipoDescarga?.Equals("emitidos", StringComparison.OrdinalIgnoreCase) == true 
                ? "emitidos" 
                : (request.TipoDescarga?.Equals("recibidos", StringComparison.OrdinalIgnoreCase) == true 
                    ? "recibidos" 
                    : (!string.IsNullOrWhiteSpace(request.RfcReceptor) ? "emitidos" : "recibidos")); // Inferir de RfcReceptor si no se proporciona TipoDescarga
            
            if (tipoDescarga == "emitidos")
            {
                // Emitidos: RfcReceptor es opcional (para filtrar por receptor específico)
                // Si se proporciona, solo descarga los CFDI emitidos a ese receptor
                // Si no se proporciona, descarga todos los CFDI emitidos
                rfcReceptorFinal = request.RfcReceptor; // Puede ser null (descarga todos los emitidos)
                rfcEmisorFinal = null; // Para Emitidos, el RfcEmisor siempre es el RfcSolicitante (se maneja en SatService)
                _logger.LogInformation("Tipo de descarga: Emitidos. RfcReceptor para filtrar: {RfcReceptor}", rfcReceptorFinal ?? "null (todos los emitidos)");
            }
            else
            {
                // Recibidos: RfcReceptor no se usa, RfcEmisor es opcional para filtrar por emisor específico
                // Si se proporciona RfcEmisor, solo descarga los CFDI recibidos de ese emisor
                // Si no se proporciona, descarga todos los CFDI recibidos
                rfcReceptorFinal = null; // Para Recibidos, no se usa RfcReceptor (siempre es el RfcSolicitante)
                rfcEmisorFinal = request.RfcEmisor; // Opcional: filtra por emisor específico
                _logger.LogInformation("Tipo de descarga: Recibidos. RfcEmisor para filtrar: {RfcEmisor}", rfcEmisorFinal ?? "null (todos los recibidos)");
            }
            
            var solicitudSatResult = await _satService.SolicitarDescargaMasivaAsync(
                tokenSat,
                rfcSolicitante,
                request.FechaInicial,
                request.FechaFinal,
                request.TipoSolicitud ?? "CFDI", // Por defecto "CFDI" si no se proporciona
                request.TipoComprobante,
                estadoComprobanteNormalizado, // "Vigente" o null (ambos significan vigentes para el SAT)
                rfcEmisorFinal,
                rfcReceptorFinal,
                tipoDescarga, // Pasar tipo de descarga (emitidos/recibidos)
                request.Complemento,
                request.RfcACuentaTerceros,
                request.Uuids,
                certificate, // Certificado para firmar (se descarta después)
                cancellationToken);
            
            // Descarta el certificado y pfxFile inmediatamente después de usarlo
            certificate?.Dispose();
            // Limpiar pfxFile de memoria (ya no es necesario)
            if (pfxFile != null)
            {
                Array.Clear(pfxFile, 0, pfxFile.Length);
            }

            if (solicitudSatResult.IsFailure)
            {
                _logger.LogError("Error al solicitar descarga al SAT: {Error}", solicitudSatResult.Error);
                
                // Actualizar estado de solicitud a Error
                solicitud.MarcarComoError(solicitudSatResult.Error);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<SolicitarDescargaResponse>.Failure($"Error al solicitar descarga al SAT: {solicitudSatResult.Error}");
            }

            var respuestaSat = solicitudSatResult.Value;

            // 7. Actualizar solicitud con respuesta del SAT
            // IMPORTANTE: El IdSolicitud solo se asigna si no está vacío
            // Cuando el SAT devuelve un error (CodEstatus != "5000"), el IdSolicitud puede estar vacío
            // Según la documentación del SAT, el IdSolicitud solo se devuelve cuando la solicitud es aceptada (CodEstatus = "5000")
            
            // Mapear código de estado del SAT al enum del dominio
            // IMPORTANTE: En SolicitaDescarga, el SAT devuelve CodEstatus (string), NO EstadoSolicitud (int)
            // CodEstatus "5000" = "Solicitud Aceptada" -> EstadoSolicitud = 1 (Aceptada) cuando se verifique
            // Otros CodEstatus (300, 301, 302, etc.) = Errores -> EstadoSolicitud = 4 (Error)
            // El EstadoSolicitud real (1-6) se obtendrá cuando se verifique la solicitud con VerificaSolicitudDescarga
            // Por ahora, CodigoEstado viene de SatService:
            // - CodigoEstado = 1 cuando CodEstatus = "5000" (Solicitud Aceptada)
            // - CodigoEstado = 4 cuando CodEstatus != "5000" (Error)
            var estadoSolicitud = respuestaSat.CodigoEstado switch
            {
                1 => EstadoSolicitud.Pendiente, // Solicitud Aceptada (se verificará después para obtener EstadoSolicitud real)
                4 => EstadoSolicitud.Error, // Error en la solicitud
                _ => EstadoSolicitud.Error // Por defecto, cualquier otro código es error
            };
            
            // Asignar IdSolicitud solo si no está vacío
            // Según la documentación del SAT, el IdSolicitud solo se devuelve cuando la solicitud es aceptada (CodEstatus = "5000")
            // Si el estado es éxito (Pendiente) pero no hay IdSolicitud, es un caso anómalo que debe tratarse como error
            if (!string.IsNullOrWhiteSpace(respuestaSat.IdSolicitud))
            {
                // Caso normal: éxito con IdSolicitud
                solicitud.ActualizarIdSolicitudSat(respuestaSat.IdSolicitud);
                _logger.LogInformation("IdSolicitud SAT asignado: {IdSolicitud}", respuestaSat.IdSolicitud);
            }
            else if (estadoSolicitud == EstadoSolicitud.Pendiente)
            {
                // Caso anómalo: el SAT reportó éxito pero no devolvió IdSolicitud
                _logger.LogError("El SAT reportó éxito (CodigoEstado=1) pero no devolvió IdSolicitud. Marcando como error.");
                estadoSolicitud = EstadoSolicitud.Error;
                solicitud.MarcarComoError("El SAT no devolvió el ID de solicitud. Mensaje: " + (respuestaSat.Mensaje ?? "Sin mensaje"));
            }
            else
            {
                // Error normal: el SAT devolvió un error y no hay IdSolicitud (comportamiento esperado)
                _logger.LogWarning("El SAT devolvió un error y no proporcionó IdSolicitud. Mensaje: {Mensaje}", respuestaSat.Mensaje);
                solicitud.MarcarComoError(respuestaSat.Mensaje ?? "Error al solicitar descarga al SAT");
            }
            
            solicitud.ActualizarTotalSolicitado(respuestaSat.TotalSolicitado ?? 0);
            
            // Actualizar estado solo si no se marcó como error (MarcarComoError ya actualiza el estado)
            if (estadoSolicitud != EstadoSolicitud.Error)
            {
                solicitud.ActualizarEstado(estadoSolicitud);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Solicitud de descarga completada. SolicitudId={SolicitudId}, IdSolicitudSat={IdSolicitudSat}, Estado={Estado}",
                solicitud.Id, respuestaSat.IdSolicitud, estadoSolicitud);

            // Encolar job para verificar la descarga después de un tiempo
            // NOTA: El job deberá obtener el token dinámicamente de la FIEL cuando se ejecute
            if (estadoSolicitud == EstadoSolicitud.Pendiente || estadoSolicitud == EstadoSolicitud.EnProceso)
            {
                // Programar verificación después de 5 minutos
                // NOTA: El job requerirá FIEL para obtener token fresco
                _messageQueueService.EncolarVerificacionDescarga(
                    solicitud.Id,
                    TimeSpan.FromMinutes(5));

                _logger.LogInformation("Job de verificación programado. SolicitudId={SolicitudId}", solicitud.Id);
            }

            var response = new SolicitarDescargaResponse
            {
                SolicitudId = solicitud.Id,
                IdSolicitudSat = respuestaSat.IdSolicitud ?? string.Empty,
                CodigoEstado = respuestaSat.CodigoEstado,
                Mensaje = respuestaSat.Mensaje ?? string.Empty,
                FechaEstimadaTermino = respuestaSat.FechaEstimadaTermino
            };

            return Result<SolicitarDescargaResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al solicitar descarga");
            return Result<SolicitarDescargaResponse>.Failure($"Error inesperado al solicitar descarga: {ex.Message}");
        }
    }
}
