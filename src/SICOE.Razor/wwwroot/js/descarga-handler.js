// Handler para descarga de paquetes CFDI y carga de solicitudes

/**
 * Carga las solicitudes de descarga de los últimos 30 días (1 mes) desde el API
 */
async function cargarSolicitudes() {
    try {
        console.log('Iniciando carga de solicitudes...');
        const tablaBody = document.getElementById('tablaSolicitudes');
        const emptyState = document.getElementById('emptyState');
        const tablaContainer = tablaBody?.closest('.table-responsive');

        if (!tablaBody) {
            console.error('No se encontró el elemento tablaSolicitudes');
            return;
        }

        // Mostrar indicador de carga
            tablaBody.innerHTML = '<tr><td colspan="6" class="text-center"><i class="fas fa-spinner fa-spin"></i> Cargando solicitudes...</td></tr>';

        // Obtener RFC de la sesión (obligatorio para filtrar las solicitudes del usuario)
        // CRÍTICO: Debe filtrarse por el RFC del usuario autenticado para mostrar solo sus solicitudes
        console.log('=== OBTENIENDO RFC DE SESIÓN ===');
        let rfc = null;

        // Prioridad 1: Intentar obtener usando la función helper (si está disponible)
        if (typeof obtenerRfcDeSesion === 'function') {
            rfc = obtenerRfcDeSesion();
            console.log('1. RFC obtenido mediante obtenerRfcDeSesion():', rfc);
        } else {
            console.log('1. obtenerRfcDeSesion() no está disponible');
        }

        // Prioridad 2: Intentar obtener de sessionStorage directo
        if (!rfc) {
            rfc = sessionStorage.getItem('userRfc');
            console.log('2. RFC obtenido de sessionStorage (userRfc):', rfc);
        }

        // Prioridad 3: Intentar obtener desde la sesión FIEL
        if (!rfc) {
            try {
                const fielSession = sessionStorage.getItem('sicoe_fiel_session');
                if (fielSession) {
                    const fielData = JSON.parse(fielSession);
                    rfc = fielData.rfc || null;
                    console.log('3. RFC obtenido de sesión FIEL:', rfc);
                    console.log('3. Datos completos de FIEL session:', fielData);
                } else {
                    console.log('3. No hay sesión FIEL almacenada');
                }
            } catch (e) {
                console.warn('3. Error al parsear sesión FIEL:', e);
            }
        }

        // Validar que tengamos RFC
        if (!rfc || rfc.trim() === '') {
            console.error('=== ERROR: No se encontró RFC en la sesión ===');
            console.error('sessionStorage keys:', Object.keys(sessionStorage));
            console.error('userRfc:', sessionStorage.getItem('userRfc'));
            console.error('sicoe_fiel_session:', sessionStorage.getItem('sicoe_fiel_session'));

            tablaBody.innerHTML = '<tr><td colspan="6" class="text-center text-danger">' +
                '<i class="fas fa-exclamation-triangle me-2"></i>' +
                'No se encontró RFC en la sesión. Por favor, autentíquese nuevamente.' +
                '</td></tr>';
            return;
        }

        // Limpiar y normalizar RFC a mayúsculas para consistencia
        rfc = rfc.trim().toUpperCase();
        console.log('=== RFC NORMALIZADO ===');
        console.log('RFC final normalizado:', rfc);
        console.log('Longitud del RFC:', rfc.length);

        // Llamar al API con el RFC para filtrar las solicitudes del usuario actual
        // CRÍTICO: El RFC es obligatorio para filtrar solo las solicitudes del usuario autenticado
        // Sin RFC, NO se deben mostrar solicitudes (seguridad)
        const apiUrl = window.apiBaseUrl || 'https://localhost:7052';
        const queryParams = new URLSearchParams({
            diasAtras: '30',
            rfc: rfc // RFC normalizado a mayúsculas - OBLIGATORIO para filtrar por usuario
        });

        const urlCompleta = `${apiUrl}/api/descarga/listar?${queryParams.toString()}`;
        console.log('URL de solicitud completa:', urlCompleta);

        const response = await fetch(`${apiUrl}/api/descarga/listar?${queryParams.toString()}`, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json'
            }
        });

        if (!response.ok) {
            throw new Error(`Error ${response.status}: ${response.statusText}`);
        }

        const result = await response.json();

        console.log('=== RESPUESTA DEL API ===');
        console.log('Respuesta completa:', JSON.stringify(result, null, 2));

        if (!result.success || !result.data) {
            console.error('=== ERROR EN RESPUESTA DEL API ===');
            console.error('Success:', result.success);
            console.error('Message:', result.message);
            console.error('Data:', result.data);
            throw new Error(result.message || 'Error al obtener solicitudes');
        }

        const solicitudes = result.data.solicitudes || [];
        const total = result.data.total || solicitudes.length;

        console.log(`=== RESULTADO DE BÚSQUEDA ===`);
        console.log(`RFC filtro aplicado: ${rfc}`);
        console.log(`Total de solicitudes encontradas: ${solicitudes.length}`);
        console.log(`Total reportado por API: ${total}`);

        // Actualizar contador de registros INMEDIATAMENTE después de obtener los datos
        const contadorRegistros = document.getElementById('contadorRegistros');
        if (contadorRegistros) {
            if (solicitudes.length === 0) {
                contadorRegistros.textContent = 'No hay registros';
            } else {
                contadorRegistros.textContent = `Mostrando ${solicitudes.length} de ${total} registros`;
            }
            console.log('✅ Contador actualizado:', contadorRegistros.textContent);
        } else {
            console.warn('⚠️ No se encontró el elemento contadorRegistros en el DOM');
        }

        if (solicitudes.length > 0) {
            console.log('Primeras solicitudes:', solicitudes.slice(0, 3).map(s => ({
                id: s.id,
                idSolicitudSat: s.idSolicitudSat || s.IdSolicitudSat,
                rfc: s.rfcContribuyente || s.RfcContribuyente
            })));
        }

        // Log adicional: verificar que las solicitudes pertenezcan al RFC correcto
        if (solicitudes.length > 0) {
            const rfcSolicitudes = solicitudes.map(s => (s.rfcContribuyente || s.RfcContribuyente || 'N/A').toUpperCase());
            const rfcUnicos = [...new Set(rfcSolicitudes)];
            console.log('RFCs en las solicitudes encontradas:', rfcUnicos);

            // Validar que todas las solicitudes pertenezcan al RFC del usuario en sesión
            const rfcIncorrectos = rfcUnicos.filter(r => r !== rfc);
            if (rfcIncorrectos.length > 0) {
                console.error('⚠️ ADVERTENCIA DE SEGURIDAD: Se encontraron solicitudes de otros RFCs:', rfcIncorrectos);
                console.error('RFC esperado:', rfc);
                console.error('Esto NO debería suceder. El filtro por RFC puede no estar funcionando correctamente.');
            } else {
                console.log('✅ Todas las solicitudes pertenecen al RFC del usuario en sesión:', rfc);
            }
        }

        // Limpiar tabla
        tablaBody.innerHTML = '';

        if (solicitudes.length === 0) {
            // Mostrar estado vacío
            console.log('No hay solicitudes para mostrar');
            console.log('RFC usado para búsqueda:', rfc);
            console.log('Total de solicitudes retornadas por API:', total);

            // Limpiar tabla y mostrar mensaje informativo
            tablaBody.innerHTML = '';

            if (tablaContainer) {
                tablaContainer.style.display = 'none';
            }
            if (emptyState) {
                emptyState.style.display = 'block';
                // Actualizar mensaje del empty state si existe
                const emptyStateText = emptyState.querySelector('p, .text-muted');
                if (emptyStateText) {
                    emptyStateText.textContent = `No hay solicitudes de descarga en los últimos 30 días para el RFC: ${rfc}`;
                }
            }
            // El contador ya se actualizó arriba, no necesita actualizarse aquí
            return;
        }

        // Ocultar estado vacío
        if (emptyState) {
            emptyState.style.display = 'none';
        }
        if (tablaContainer) {
            tablaContainer.style.display = 'block';
        }

        // Llenar tabla con datos reales
        solicitudes.forEach(solicitud => {
            const row = document.createElement('tr');

            // Determinar badge según tipo de descarga
            const badgeClass = solicitud.tipoDescarga === 'CFDI' ? 'bg-info' : 'bg-secondary';
            const badgeText = solicitud.tipoDescarga || 'CFDI';

            // Determinar si la solicitud está completada y se puede descargar
            const estadoRaw = (solicitud.estado || solicitud.Estado || '').toLowerCase();
            const estaCompletada = estadoRaw === 'completada' || estadoRaw === 'completado';
            const estaRechazada = estadoRaw === 'rechazada' || estadoRaw === 'cancelada' || estadoRaw === 'vencida';
            const puedeDescargar = estaCompletada && solicitud.idSolicitudSat && !estaRechazada;

            // Determinar clase, icono y tooltip del botón según el estado
            let btnClass = 'btn btn-sm';
            let btnTitle = '';
            let iconClass = 'fas fa-download';
            let disabledAttr = '';

            if (puedeDescargar) {
                btnClass += ' btn-primary';
                btnTitle = 'Descargar paquete';
            } else {
                btnClass += ' btn-secondary';
                disabledAttr = 'disabled';

                // Mapeo detallado de iconos y títulos por estado
                if (estadoRaw === 'pendiente') {
                    iconClass = 'fas fa-clock';
                    btnTitle = 'Estado: Pendiente (Esperando procesamiento del SAT)';
                } else if (estadoRaw === 'enproceso' || estadoRaw === 'en_proceso') {
                    iconClass = 'fas fa-spinner fa-spin';
                    btnTitle = 'Estado: En Proceso (El SAT está procesando la solicitud)';
                } else if (estadoRaw === 'error') {
                    iconClass = 'fas fa-times-circle text-danger';
                    btnTitle = 'Estado: Error (Hubo un problema con la solicitud en el SAT)';
                } else if (estadoRaw === 'cancelada' || estadoRaw === 'cancelado') {
                    iconClass = 'fas fa-ban text-warning';
                    btnTitle = 'Estado: Cancelada/Rechazada (La solicitud fue rechazada o venció)';
                } else {
                    iconClass = 'fas fa-question-circle';
                    btnTitle = estadoRaw ? `Estado: ${solicitud.estado || solicitud.Estado}. Solo se pueden descargar solicitudes completadas.` :
                        'No disponible para descarga';
                }
            }

            // Crear celda de acciones con ambos botones
            const accionesCell = document.createElement('td');
            accionesCell.className = 'text-center';

            // Contenedor de botones
            const btnGroup = document.createElement('div');
            btnGroup.className = 'btn-group btn-group-sm';
            btnGroup.setAttribute('role', 'group');

            // Botón de verificar estado (SIEMPRE visible)
            const btnVerificar = document.createElement('button');
            btnVerificar.type = 'button';
            btnVerificar.className = 'btn btn-sm btn-info';
            btnVerificar.title = 'Verificar estado en el SAT';
            btnVerificar.setAttribute('data-solicitud-id-verificar', solicitud.id);
            btnVerificar.setAttribute('aria-label', 'Verificar estado');
            btnVerificar.innerHTML = '<i class="fas fa-sync-alt"></i>';
            btnVerificar.style.minWidth = '40px'; // Asegurar que tenga un tamaño mínimo
            btnVerificar.onclick = function (e) {
                e.preventDefault();
                e.stopPropagation();
                verificarEstado(solicitud.id);
            };

            // Botón de descargar
            const btnDescargar = document.createElement('button');
            btnDescargar.type = 'button';
            btnDescargar.className = btnClass;
            btnDescargar.title = btnTitle;
            btnDescargar.setAttribute('data-solicitud-id', solicitud.id);
            if (disabledAttr) {
                btnDescargar.disabled = true;
            }
            btnDescargar.innerHTML = `<i class="${iconClass}"></i>`;
            if (puedeDescargar) {
                btnDescargar.onclick = function (e) {
                    e.preventDefault();
                    e.stopPropagation();
                    descargarPaquete(solicitud.id, e);
                };
            }

            // Agregar botones al grupo (IMPORTANTE: orden correcto)
            btnGroup.appendChild(btnVerificar);
            btnGroup.appendChild(btnDescargar);
            accionesCell.appendChild(btnGroup);

            // Debug: verificar que los botones se crearon correctamente
            console.log(`Botones creados para solicitud ${solicitud.id}:`, {
                verificar: btnVerificar ? 'OK' : 'FALLO',
                descargar: btnDescargar ? 'OK' : 'FALLO',
                grupo: btnGroup.children.length,
                accionesCell: accionesCell.children.length
            });

            // Crear el resto de las celdas
            const folioCell = document.createElement('td');
            folioCell.innerHTML = `<span class="font-monospace">${solicitud.idSolicitudSat || solicitud.IdSolicitudSat || 'N/A'}</span>`;

            const rfcCell = document.createElement('td');
            rfcCell.textContent = solicitud.rfcContribuyente || solicitud.RfcContribuyente || 'N/A';

            const tipoCell = document.createElement('td');
            tipoCell.innerHTML = `<span class="badge ${badgeClass}">${badgeText}</span>`;

            // Crear celda de estado con badge según el estado
            // IMPORTANTE: estadoRaw ya está definido arriba (línea 203)
            const estadoCell = document.createElement('td');
            estadoCell.className = 'text-center';
            const estadoTexto = solicitud.estado || solicitud.Estado || 'Pendiente';
            const estadoRawParaCelda = estadoTexto.toLowerCase(); // Usar el estado del objeto, no el de arriba
            let estadoBadgeClass = 'bg-secondary';
            let estadoTextoMostrar = estadoTexto;
            
            // Mapear estados a badges y textos legibles
            // El enum devuelve: Pendiente, EnProceso, Completada, Error, Cancelada
            switch (estadoRawParaCelda) {
                case 'completada':
                case 'completado':
                    estadoBadgeClass = 'bg-success';
                    estadoTextoMostrar = 'Completada';
                    break;
                case 'enproceso':  // Sin guión bajo (como viene del enum)
                case 'en_proceso': // Con guión bajo (por compatibilidad)
                    estadoBadgeClass = 'bg-info';
                    estadoTextoMostrar = 'En Proceso';
                    break;
                case 'pendiente':
                    estadoBadgeClass = 'bg-warning';
                    estadoTextoMostrar = 'Pendiente';
                    break;
                case 'rechazada':
                case 'cancelada':
                case 'vencida':
                    estadoBadgeClass = 'bg-danger';
                    estadoTextoMostrar = estadoRawParaCelda === 'rechazada' ? 'Rechazada' : 
                                        estadoRawParaCelda === 'cancelada' ? 'Cancelada' : 'Vencida';
                    break;
                case 'error':
                    estadoBadgeClass = 'bg-danger';
                    estadoTextoMostrar = 'Error';
                    break;
                default:
                    estadoBadgeClass = 'bg-secondary';
                    estadoTextoMostrar = estadoTexto;
            }
            estadoCell.innerHTML = `<span class="badge ${estadoBadgeClass}">${estadoTextoMostrar}</span>`;

            const cantidadCell = document.createElement('td');
            cantidadCell.className = 'text-center';
            cantidadCell.textContent = solicitud.cantidadDocumentos || solicitud.CantidadDocumentos || 0;

            // Agregar todas las celdas a la fila
            row.appendChild(accionesCell);
            row.appendChild(folioCell);
            row.appendChild(rfcCell);
            row.appendChild(tipoCell);
            row.appendChild(estadoCell);
            row.appendChild(cantidadCell);

            // Agregar la fila a la tabla
            tablaBody.appendChild(row);
        });

        console.log(`Cargadas ${solicitudes.length} solicitudes de descarga exitosamente`);
    } catch (error) {
        console.error('Error al cargar solicitudes:', error);

        // Actualizar contador en caso de error
        const contadorRegistros = document.getElementById('contadorRegistros');
        if (contadorRegistros) {
            contadorRegistros.textContent = 'Error al cargar registros';
        }

        const tablaBody = document.getElementById('tablaSolicitudes');
        if (tablaBody) {
            tablaBody.innerHTML = `<tr><td colspan="6" class="text-center text-danger">
                <i class="fas fa-exclamation-triangle me-2"></i>
                Error al cargar solicitudes: ${error.message}
                <br><small>Por favor, recarga la página o intenta nuevamente.</small>
            </td></tr>`;
        }

        // Mostrar estado vacío en caso de error
        const emptyState = document.getElementById('emptyState');
        const tablaContainer = tablaBody?.closest('.table-responsive');
        if (tablaContainer) {
            tablaContainer.style.display = 'none';
        }
        if (emptyState) {
            emptyState.style.display = 'block';
        }
    }
}

/**
 * Descarga el paquete ZIP de una solicitud completada
 * @param {number} solicitudId - ID de la solicitud de descarga
 */
async function descargarPaquete(solicitudId, eventParam) {
    try {
        console.log('descargarPaquete llamado con solicitudId:', solicitudId);

        // Obtener el botón que disparó la acción
        // eventParam se pasa explícitamente o se usa window.event como fallback
        const eventObj = eventParam || window.event;
        const btn = eventObj?.target?.closest('button') || document.querySelector(`[data-solicitud-id="${solicitudId}"]`);

        if (!btn) {
            console.error('No se encontró el botón de descarga');
            return;
        }

        // Prevenir cualquier comportamiento por defecto (navegación, etc.)
        if (eventObj) {
            eventObj.preventDefault();
            eventObj.stopPropagation();
        }

        // Obtener FIEL desde sessionStorage (REQUERIDO)
        // Si la sesión expiró, obtenerFielParaEnvio() redirigirá automáticamente al login
        const fielData = obtenerFielParaEnvio();

        if (!fielData) {
            // Si llegamos aquí sin redirección, es porque redirigirSiExpirada=false fue usado
            // En este caso, mostrar error
            alert('La sesión ha expirado. Por favor, autentíquese nuevamente con su certificado FIEL.');
            setTimeout(() => {
                window.location.href = '/';
            }, 2000);
            return;
        }

        // Mostrar indicador de carga
        const originalHtml = btn.innerHTML;
        btn.disabled = true;
        btn.innerHTML = '<i class="fas fa-spinner fa-spin"></i>';

        const apiUrl = window.apiBaseUrl || 'https://localhost:7052';

        // Preparar request body con FIEL
        const requestBody = {
            certificadoCer: fielData.certificadoCer, // Ya está en base64
            clavePrivadaKey: fielData.clavePrivadaKey, // Ya está en base64
            passwordFiel: fielData.password
        };

        console.log('=== INICIANDO DESCARGA ===');
        console.log('SolicitudId:', solicitudId);
        console.log('API URL:', `${apiUrl}/api/descarga/descargar-paquete/${solicitudId}`);
        console.log('Método HTTP: POST');
        console.log('Request body keys:', Object.keys(requestBody));
        console.log('Request body tiene certificadoCer:', !!requestBody.certificadoCer);
        console.log('Request body tiene clavePrivadaKey:', !!requestBody.clavePrivadaKey);
        console.log('Request body tiene passwordFiel:', !!requestBody.passwordFiel);

        const url = `${apiUrl}/api/descarga/descargar-paquete/${solicitudId}`;
        const fetchOptions = {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Accept': 'application/zip, application/octet-stream'
            },
            body: JSON.stringify(requestBody)
        };

        console.log('Fetch options:', JSON.stringify(fetchOptions, null, 2));

        const response = await fetch(url, fetchOptions);

        console.log('Response status:', response.status, response.statusText);
        console.log('Response headers:', Object.fromEntries(response.headers.entries()));

        if (!response.ok) {
            // Intentar parsear error como JSON
            let errorMessage = `Error ${response.status}: ${response.statusText}`;
            try {
                const errorData = await response.json();
                errorMessage = errorData.message || errorMessage;
            } catch (e) {
                // Si no es JSON, usar el mensaje de estado
            }
            throw new Error(errorMessage);
        }

        // Obtener nombre del archivo del header Content-Disposition o usar uno por defecto
        const contentDisposition = response.headers.get('Content-Disposition');
        let filename = `CFDI_${solicitudId}_${new Date().toISOString().slice(0, 10)}.zip`;

        if (contentDisposition) {
            const filenameMatch = contentDisposition.match(/filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/);
            if (filenameMatch && filenameMatch[1]) {
                filename = filenameMatch[1].replace(/['"]/g, '');
            }
        }

        // Descargar archivo
        const blob = await response.blob();
        const blobUrl = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = blobUrl;
        a.download = filename;
        document.body.appendChild(a);
        a.click();
        window.URL.revokeObjectURL(url);
        document.body.removeChild(a);

        // Restaurar botón
        btn.disabled = false;
        btn.innerHTML = originalHtml;

        // Mostrar mensaje de éxito (opcional, puede usar toast si está disponible)
        console.log('Paquete descargado exitosamente:', filename);
    } catch (error) {
        console.error('Error al descargar paquete:', error);
        alert('Error al descargar paquete: ' + error.message);

        // Restaurar botón en caso de error
        const btn = event?.target?.closest('button') || document.querySelector(`[data-solicitud-id="${solicitudId}"]`);
        if (btn) {
            btn.disabled = false;
            btn.innerHTML = '<i class="fas fa-download"></i>';
        }
    }
}

/**
 * Verifica el estado de una solicitud en el SAT y actualiza la información
 * @param {number} solicitudId - ID de la solicitud de descarga
 */
async function verificarEstado(solicitudId) {
    // Prevenir múltiples verificaciones simultáneas para la misma solicitud
    if (verificacionesEnCurso.has(solicitudId)) {
        console.warn(`Verificación ya en curso para solicitud ${solicitudId}. Ignorando llamada duplicada.`);
        return;
    }

    // CRÍTICO: Obtener el botón ANTES del try para que esté disponible en finally
    let btn = null;
    let originalHtml = '<i class="fas fa-sync-alt"></i>';
    
    try {
        // Marcar como en curso
        verificacionesEnCurso.add(solicitudId);
        
        console.log('Iniciando verificación de estado para solicitud:', solicitudId);

        // Buscar el botón usando el selector directo (no depender de event)
        btn = document.querySelector(`button[data-solicitud-id-verificar="${solicitudId}"]`);

        if (!btn) {
            console.error('No se encontró el botón de verificar estado para solicitud:', solicitudId);
            return;
        }

        // Guardar HTML original y mostrar spinner
        originalHtml = btn.innerHTML;
        btn.disabled = true;
        btn.innerHTML = '<i class="fas fa-spinner fa-spin"></i>';

        // Obtener FIEL desde sessionStorage (REQUERIDO)
        const fielData = obtenerFielParaEnvio();

        if (!fielData) {
            alert('La sesión ha expirado. Por favor, autentíquese nuevamente con su certificado FIEL.');
            setTimeout(() => {
                window.location.href = '/';
            }, 2000);
            return;
        }

        const apiUrl = window.apiBaseUrl || 'https://localhost:7052';

        // Preparar request body con FIEL
        const requestBody = {
            certificadoCer: fielData.certificadoCer,
            clavePrivadaKey: fielData.clavePrivadaKey,
            passwordFiel: fielData.password
        };

        const url = `${apiUrl}/api/descarga/verificar-estado/${solicitudId}`;
        console.log('=== INICIANDO VERIFICACIÓN ===');
        console.log('URL:', url);
        console.log('SolicitudId:', solicitudId);
        console.log('Request body tiene certificadoCer:', !!requestBody.certificadoCer);
        console.log('Request body tiene clavePrivadaKey:', !!requestBody.clavePrivadaKey);
        console.log('Request body tiene passwordFiel:', !!requestBody.passwordFiel);
        
        let response;
        try {
            console.log('Enviando fetch a:', url);
            response = await fetch(url, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(requestBody)
            });
            console.log('Fetch completado. Status:', response.status, response.statusText);
            console.log('Response headers:', Object.fromEntries(response.headers.entries()));
        } catch (fetchError) {
            console.error('ERROR EN FETCH:', fetchError);
            console.error('Error name:', fetchError.name);
            console.error('Error message:', fetchError.message);
            console.error('Error stack:', fetchError.stack);
            
            if (fetchError.message && (fetchError.message.includes('Failed to fetch') || fetchError.message.includes('NetworkError'))) {
                throw new Error('No se pudo conectar con el servidor. Verifique que el API esté corriendo en ' + apiUrl + ' y que el certificado SSL sea válido. Error: ' + fetchError.message);
            }
            throw new Error('Error de red: ' + (fetchError.message || fetchError.toString()));
        }

        if (!response.ok) {
            let errorMessage = `Error ${response.status}: ${response.statusText}`;
            try {
                const errorData = await response.json();
                errorMessage = errorData.message || errorMessage;
            } catch (e) {
                // Si no es JSON, usar el mensaje de estado
            }
            throw new Error(errorMessage);
        }

        const result = await response.json();

        if (!result.success) {
            throw new Error(result.message || 'Error al verificar el estado');
        }

        // Mostrar mensaje de éxito
        // IMPORTANTE: codigoEstado viene mapeado: 0=Pendiente, 1=EnProceso, 2=Completada, 3=Error, 4=Cancelada
        const estadoText = result.data?.codigoEstado === 2 ? 'Terminada' :
            result.data?.codigoEstado === 1 ? 'En Proceso' :
                result.data?.codigoEstado === 0 ? 'Aceptada' :
                    result.data?.codigoEstado === 3 ? 'Error' :
                        result.data?.codigoEstado === 4 ? 'Rechazada/Vencida' :
                            'Desconocido';

        const mensaje = `Estado verificado exitosamente.\n` +
            `Estado: ${estadoText}\n` +
            `${result.data?.totalCFDIs ? `Documentos disponibles: ${result.data.totalCFDIs}` : ''}` +
            `${result.data?.totalPaquetes ? `\nPaquetes: ${result.data.totalPaquetes}` : ''}`;

        // Mostrar mensaje de éxito
        if (typeof mostrarExito === 'function') {
            mostrarExito(mensaje);
        } else {
            console.log('Verificación exitosa:', mensaje);
        }

        console.log('Verificación de estado completada exitosamente');
        
        // IMPORTANTE: Restaurar el botón ANTES de recargar la tabla
        // para evitar que cargarSolicitudes() lo recree con spinner
        if (btn) {
            btn.disabled = false;
            btn.innerHTML = originalHtml;
            console.log('Botón restaurado ANTES de recargar tabla');
        }

        // Recargar la tabla para mostrar la información actualizada
        // Usar un delay más largo para asegurar que el botón se restauró primero
        console.log('Recargando tabla de solicitudes después de verificar estado...');
        setTimeout(() => {
            if (typeof window.cargarSolicitudes === 'function') {
                window.cargarSolicitudes();
            } else if (typeof cargarSolicitudes === 'function') {
                cargarSolicitudes();
            }
        }, 1000); // Delay aumentado para dar tiempo a que se restaure el botón

    } catch (error) {
        console.error('Error al verificar estado:', error);

        // Mostrar mensaje de error
        if (typeof mostrarError === 'function') {
            mostrarError('Error al verificar estado: ' + error.message);
        } else {
            alert('Error al verificar estado: ' + error.message);
        }
    } finally {
        // SIEMPRE restaurar el botón y quitar de verificaciones en curso
        verificacionesEnCurso.delete(solicitudId);
        
        if (btn) {
            btn.disabled = false;
            btn.innerHTML = originalHtml;
            console.log('Botón restaurado en finally después de verificar estado');
        } else {
            // Si no encontramos el botón, intentar buscarlo de nuevo
            const btnFallback = document.querySelector(`button[data-solicitud-id-verificar="${solicitudId}"]`);
            if (btnFallback) {
                btnFallback.disabled = false;
                btnFallback.innerHTML = '<i class="fas fa-sync-alt"></i>';
                console.log('Botón restaurado usando fallback en finally');
            }
        }
    }
}

// Track de verificaciones en curso para evitar múltiples llamadas simultáneas
const verificacionesEnCurso = new Set();

// Hacer las funciones disponibles globalmente
window.descargarPaquete = descargarPaquete;
window.cargarSolicitudes = cargarSolicitudes;
window.verificarEstado = verificarEstado;

