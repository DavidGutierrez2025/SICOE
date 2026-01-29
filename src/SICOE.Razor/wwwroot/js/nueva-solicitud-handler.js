/**
 * Handler para crear nuevas solicitudes de descarga de CFDI
 * Integra con el API para enviar solicitudes al SAT
 */

/**
 * Abre el modal para crear una nueva solicitud
 */
function abrirModalNuevaSolicitud() {
    // Inicializar selector de año: año actual y año anterior
    const ahora = new Date();
    const anioActual = ahora.getFullYear();
    const anioAnterior = anioActual - 1;

    // Llenar selector de años (año actual y año anterior)
    const selectAnio = $('#modalNuevaSolicitudAnio');
    selectAnio.empty();
    selectAnio.append(`<option value="${anioActual}" selected>${anioActual}</option>`);
    selectAnio.append(`<option value="${anioAnterior}">${anioAnterior}</option>`);

    // Inicializar mes: mes anterior del año actual
    const mes = ahora.getMonth(); // 0-11 (0 = Enero, 11 = Diciembre)
    let mesAnterior = mes - 1;
    
    // Si estamos en enero (mes 0), el mes anterior es diciembre del año anterior
    if (mesAnterior < 0) {
        mesAnterior = 11; // Diciembre
    }

    // Seleccionar mes anterior (mesAnterior + 1 porque el select usa 1-12)
    $('#modalNuevaSolicitudMes').val(mesAnterior + 1);

    // Calcular fechas automáticamente basadas en año y mes seleccionados
    calcularFechasMesCalendario();

    // Limpiar campos opcionales
    $('#modalNuevaSolicitudTipoComprobante').val('');
    $('#modalNuevaSolicitudRfcEmisor').val('');
    $('#modalNuevaSolicitudRfcReceptor').val('');
    // IMPORTANTE: El SAT no permite descargar comprobantes cancelados
    // Por defecto usar "Vigente"
    $('#modalNuevaSolicitudEstadoComprobante').val('Vigente');
    // Por defecto: Recibidos
    $('#modalNuevaSolicitudTipoDescarga').val('recibidos');
    // Mostrar campos correspondientes
    actualizarCamposTipoDescarga();

    // Mostrar modal
    $('#modalNuevaSolicitud').modal('show');
}

/**
 * Calcula automáticamente las fechas de inicio y fin del mes calendario seleccionado
 * Basado en el año y mes seleccionados
 */
function calcularFechasMesCalendario() {
    const anio = parseInt($('#modalNuevaSolicitudAnio').val());
    const mes = parseInt($('#modalNuevaSolicitudMes').val());

    if (!anio || !mes) {
        // Si no hay año o mes seleccionado, limpiar fechas
        $('#modalNuevaSolicitudFechaInicial').val('');
        $('#modalNuevaSolicitudFechaFinal').val('');
        return;
    }

    // Fecha inicial: Primer día del mes (día 1)
    const fechaInicial = new Date(anio, mes - 1, 1); // mes - 1 porque Date usa 0-11

    // Fecha final: Último día del mes
    // Usar día 0 del mes siguiente para obtener el último día del mes actual
    const fechaFinal = new Date(anio, mes, 0);

    // Guardar en campos de fecha (formato ISO para el backend)
    $('#modalNuevaSolicitudFechaInicial').val(fechaInicial.toISOString().split('T')[0]);
    $('#modalNuevaSolicitudFechaFinal').val(fechaFinal.toISOString().split('T')[0]);

    console.log('Fechas calculadas para mes calendario:', {
        anio,
        mes,
        fechaInicial: fechaInicial.toISOString().split('T')[0],
        fechaFinal: fechaFinal.toISOString().split('T')[0]
    });
}

/**
 * Crea una nueva solicitud de descarga
 */
async function crearNuevaSolicitud() {
    try {
        // Validar campos requeridos
        const fechaInicial = $('#modalNuevaSolicitudFechaInicial').val();
        const fechaFinal = $('#modalNuevaSolicitudFechaFinal').val();

        if (!fechaInicial || !fechaFinal) {
            mostrarError('Las fechas inicial y final son requeridas');
            return;
        }

        // Crear fechas según el formato del SAT
        // El SAT requiere: yyyy-MM-ddTHH:mm:ss (con segundos, sin zona horaria)
        // Ejemplo del SAT: "2025-05-12T18:57:43"
        // IMPORTANTE: El SAT NO acepta fechas futuras, la fecha final debe ser <= fecha actual
        // Fecha inicial: inicio del día (00:00:00)
        // Fecha final: usar la hora actual del momento (no 23:59:59 automáticamente)
        // El backend convertirá a hora de México y validará que no sea futura
        const fechaInicialDate = new Date(fechaInicial + 'T00:00:00Z'); // UTC - inicio del día

        // Para fecha final, usar la hora actual del momento (no forzar 23:59:59)
        // Esto asegura que la fecha final no sea mayor que la fecha actual
        const ahora = new Date();
        let fechaFinalDate = new Date(fechaFinal + `T${ahora.getHours().toString().padStart(2, '0')}:${ahora.getMinutes().toString().padStart(2, '0')}:${ahora.getSeconds().toString().padStart(2, '0')}Z`);

        // Validar que la fecha final no sea mayor que la fecha actual
        if (fechaFinalDate > ahora) {
            // Si la fecha final es mayor que ahora, usar ahora como fecha final
            fechaFinalDate = ahora;
        }

        if (fechaInicialDate > fechaFinalDate) {
            mostrarError('La fecha inicial no puede ser mayor que la fecha final');
            return;
        }

        // Obtener FIEL desde sessionStorage (REQUERIDO)
        // Si la sesión expiró, obtenerFielParaEnvio() redirigirá automáticamente al login
        const fielData = obtenerFielParaEnvio();

        if (!fielData) {
            // Si llegamos aquí sin redirección, es porque redirigirSiExpirada=false fue usado
            // En este caso, mostrar error y cerrar modal
            mostrarError('La sesión ha expirado. Será redirigido a la página de login...');
            setTimeout(() => {
                window.location.href = '/';
            }, 2000);
            return;
        }

        // Determinar tipo de descarga
        const tipoDescarga = $('#modalNuevaSolicitudTipoDescarga').val();

        // Construir request según tipo de descarga
        // Para Emitidos: RfcReceptor es opcional (filtra por receptor), RfcEmisor no se usa
        // Para Recibidos: RfcEmisor es opcional (filtra por emisor), RfcReceptor no se usa
        const rfcReceptor = tipoDescarga === 'emitidos' ? ($('#modalNuevaSolicitudRfcReceptor').val() || null) : null;
        const rfcEmisor = tipoDescarga === 'recibidos' ? ($('#modalNuevaSolicitudRfcEmisor').val() || null) : null;

        // Construir request
        // Enviar fechas en formato ISO 8601 (el backend las convertirá a hora de México)
        const request = {
            clienteId: 1, // TODO: Obtener del contexto de usuario autenticado
            fechaInicial: fechaInicialDate.toISOString(), // Formato completo ISO 8601
            fechaFinal: fechaFinalDate.toISOString(), // Formato completo ISO 8601
            tipoSolicitud: "CFDI", // Por defecto "CFDI", podría ser "Metadata" en el futuro
            tipoComprobante: $('#modalNuevaSolicitudTipoComprobante').val() || null,
            // IMPORTANTE: El SAT no permite "Cancelado", solo "Vigente" o "Todos"
            // "Todos" será convertido a "Vigente" por el backend (el SAT no permite cancelados)
            estadoComprobante: $('#modalNuevaSolicitudEstadoComprobante').val() || "Vigente", // Por defecto "Vigente"
            rfcEmisor: rfcEmisor,
            rfcReceptor: rfcReceptor,
            tipoDescarga: tipoDescarga, // "recibidos" o "emitidos"
            uuids: null // Por ahora no soportamos UUIDs específicos
        };

        // Agregar FIEL (siempre requerida)
        request.certificadoCer = fielData.certificadoCer;
        request.clavePrivadaKey = fielData.clavePrivadaKey;
        request.passwordFiel = fielData.password;

        // Mostrar loading
        const btnEnviar = $('#btnEnviarSolicitud');
        const textoOriginal = btnEnviar.html();
        btnEnviar.prop('disabled', true).html('<i class="fas fa-spinner fa-spin me-1"></i> Enviando...');

        // Enviar solicitud al API
        const apiUrl = window.apiBaseUrl || 'https://localhost:7052';
        const response = await fetch(`${apiUrl}/api/descarga/solicitar`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(request)
        });

        // Verificar si la respuesta tiene contenido antes de parsear JSON
        let result;
        const contentType = response.headers.get('content-type');
        const hasJsonContent = contentType && contentType.includes('application/json');

        if (hasJsonContent) {
            try {
                const text = await response.text();
                if (text && text.trim().length > 0) {
                    result = JSON.parse(text);
                } else {
                    // Respuesta vacía
                    throw new Error('La respuesta del servidor está vacía');
                }
            } catch (parseError) {
                console.error('Error al parsear respuesta JSON:', parseError);
                mostrarError(`Error al procesar la respuesta del servidor: ${parseError.message}`);
                return;
            }
        } else {
            // Respuesta no es JSON (probablemente HTML de error)
            const text = await response.text();
            console.error('Respuesta no JSON del servidor:', text);
            mostrarError(`Error del servidor (${response.status}): ${response.statusText}`);
            return;
        }

        if (response.ok && result.success) {
            // Éxito
            const idSolicitudSat = result.data?.idSolicitudSat || result.data?.IdSolicitudSat;
            const mensaje = idSolicitudSat
                ? `Solicitud creada exitosamente. ID SAT: ${idSolicitudSat}`
                : 'Solicitud creada exitosamente';
            mostrarExito(mensaje);

            // Cerrar el modal
            $('#modalNuevaSolicitud').modal('hide');

            // Función para recargar la tabla de solicitudes
            const recargarTablaSolicitudes = () => {
                // Intentar múltiples formas de acceder a la función
                if (typeof window.cargarSolicitudes === 'function') {
                    console.log('Recargando tabla de solicitudes mediante window.cargarSolicitudes...');
                    window.cargarSolicitudes();
                    return true;
                }
                if (typeof cargarSolicitudes === 'function') {
                    console.log('Recargando tabla de solicitudes mediante cargarSolicitudes...');
                    cargarSolicitudes();
                    return true;
                }
                return false;
            };

            // Recargar la tabla después de un delay para dar tiempo a que el modal se cierre
            // y la base de datos se actualice
            setTimeout(() => {
                if (!recargarTablaSolicitudes()) {
                    console.warn('La función cargarSolicitudes no está disponible, reintentando...');
                    // Reintentar después de otro pequeño delay
                    setTimeout(() => {
                        if (!recargarTablaSolicitudes()) {
                            console.error('No se pudo recargar la tabla automáticamente. Por favor, recargue la página manualmente.');
                        }
                    }, 300);
                }
            }, 800); // Delay de 800ms para dar tiempo al cierre del modal y actualización de BD
        } else {
            // Error
            mostrarError(result.message || 'Error al crear la solicitud');
        }
    } catch (error) {
        console.error('Error al crear solicitud:', error);
        mostrarError('Error inesperado al crear la solicitud: ' + error.message);
    } finally {
        // Restaurar botón
        const btnEnviar = $('#btnEnviarSolicitud');
        btnEnviar.prop('disabled', false).html('<i class="fas fa-paper-plane me-1"></i> Enviar Solicitud');
    }
}

/**
 * Muestra un mensaje de error
 */
function mostrarError(mensaje) {
    // Crear o actualizar alert de error
    let alertDiv = $('#alertErrorNuevaSolicitud');
    if (alertDiv.length === 0) {
        alertDiv = $('<div class="alert alert-danger alert-dismissible fade show" role="alert" id="alertErrorNuevaSolicitud"></div>');
        $('#modalNuevaSolicitud .modal-body').prepend(alertDiv);
    }

    alertDiv.html(`
        <i class="fas fa-exclamation-circle me-2"></i>${mensaje}
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    `);

    // Auto-ocultar después de 5 segundos
    setTimeout(() => {
        alertDiv.alert('close');
    }, 5000);
}

/**
 * Actualiza los campos visibles según el tipo de descarga seleccionado
 */
function actualizarCamposTipoDescarga() {
    const tipoDescarga = $('#modalNuevaSolicitudTipoDescarga').val();
    const esEmitidos = tipoDescarga === 'emitidos';

    if (esEmitidos) {
        // Emitidos: mostrar campo RFC Receptor, ocultar RFC Emisor
        $('#rowRfcEmitidos').removeClass('d-none');
        $('#rowRfcRecibidos').addClass('d-none');
        // Limpiar RFC Emisor (no se usa para emitidos)
        $('#modalNuevaSolicitudRfcEmisor').val('');
    } else {
        // Recibidos: mostrar campo RFC Emisor, ocultar RFC Receptor
        $('#rowRfcRecibidos').removeClass('d-none');
        $('#rowRfcEmitidos').addClass('d-none');
        // Limpiar RFC Receptor (no se usa para recibidos)
        $('#modalNuevaSolicitudRfcReceptor').val('');
    }
}

// Event listeners
$(document).ready(function () {
    $('#modalNuevaSolicitudTipoDescarga').on('change', actualizarCamposTipoDescarga);
    
    // Recalcular fechas cuando cambian año o mes
    $('#modalNuevaSolicitudAnio, #modalNuevaSolicitudMes').on('change', function() {
        calcularFechasMesCalendario();
    });
});

/**
 * Muestra un mensaje de éxito
 */
function mostrarExito(mensaje) {
    // Crear toast o alert de éxito
    const toast = $(`
        <div class="toast align-items-center text-white bg-success border-0" role="alert" aria-live="assertive" aria-atomic="true">
            <div class="d-flex">
                <div class="toast-body">
                    <i class="fas fa-check-circle me-2"></i>${mensaje}
                </div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
            </div>
        </div>
    `);

    // Agregar al contenedor de toasts (crear si no existe)
    let toastContainer = $('#toastContainer');
    if (toastContainer.length === 0) {
        toastContainer = $('<div class="toast-container position-fixed top-0 end-0 p-3" id="toastContainer"></div>');
        $('body').append(toastContainer);
    }

    toastContainer.append(toast);
    const bsToast = new bootstrap.Toast(toast[0]);
    bsToast.show();

    // Remover después de que se oculte
    toast.on('hidden.bs.toast', function () {
        $(this).remove();
    });
}

