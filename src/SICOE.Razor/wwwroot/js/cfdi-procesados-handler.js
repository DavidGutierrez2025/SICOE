// Handler para listar y descargar CFDI procesados

const API_BASE_URL = window.apiBaseUrl || 'https://localhost:7052';
const CFDI_API_URL = `${API_BASE_URL}/api/cfdi`;

let currentPage = 1;
let pageSize = 50;
let totalPages = 1;
let totalRegistros = 0;
let selectedCFDI = new Set(); // Set de UUIDs seleccionados

// Inicializar cuando el DOM esté listo
document.addEventListener('DOMContentLoaded', function() {
    inicializarEventos();
    cargarCFDI();
});

/**
 * Inicializa los event listeners
 */
function inicializarEventos() {
    // Botón buscar
    const btnBuscar = document.getElementById('btnBuscarCFDI');
    if (btnBuscar) {
        btnBuscar.addEventListener('click', function() {
            currentPage = 1;
            cargarCFDI();
        });
    }

    // Botón limpiar filtros
    const btnLimpiar = document.getElementById('btnLimpiarFiltros');
    if (btnLimpiar) {
        btnLimpiar.addEventListener('click', function() {
            limpiarFiltros();
            currentPage = 1;
            cargarCFDI();
        });
    }

    // Checkbox "Seleccionar todos"
    const checkTodos = document.getElementById('checkTodos');
    if (checkTodos) {
        checkTodos.addEventListener('change', function() {
            const checked = this.checked;
            const checkboxes = document.querySelectorAll('#tablaCFDI input[type="checkbox"][data-uuid]');
            checkboxes.forEach(cb => {
                cb.checked = checked;
                const uuid = cb.getAttribute('data-uuid');
                if (checked) {
                    selectedCFDI.add(uuid);
                } else {
                    selectedCFDI.delete(uuid);
                }
            });
            actualizarBotonDescargar();
        });
    }

    // Botón descargar seleccionados
    const btnDescargarSeleccionados = document.getElementById('btnDescargarSeleccionados');
    if (btnDescargarSeleccionados) {
        btnDescargarSeleccionados.addEventListener('click', descargarSeleccionados);
    }

    // Permitir búsqueda con Enter
    const formFiltros = document.getElementById('formFiltrosCFDI');
    if (formFiltros) {
        formFiltros.addEventListener('submit', function(e) {
            e.preventDefault();
            currentPage = 1;
            cargarCFDI();
        });
    }
}

/**
 * Carga los CFDI desde el API con los filtros actuales
 */
async function cargarCFDI() {
    try {
        const tablaBody = document.getElementById('tablaCFDI');
        if (!tablaBody) return;

        // Mostrar indicador de carga
        tablaBody.innerHTML = '<tr><td colspan="11" class="text-center"><i class="fas fa-spinner fa-spin"></i> Cargando CFDI...</td></tr>';

        // Construir query string con filtros
        const params = new URLSearchParams();
        
        const rfcEmisor = document.getElementById('filtroRfcEmisor')?.value;
        const rfcReceptor = document.getElementById('filtroRfcReceptor')?.value;
        const fechaInicial = document.getElementById('filtroFechaInicial')?.value;
        const fechaFinal = document.getElementById('filtroFechaFinal')?.value;
        const uuid = document.getElementById('filtroUuid')?.value;
        const tipoComprobante = document.getElementById('filtroTipoComprobante')?.value;
        const solicitudId = document.getElementById('filtroSolicitudId')?.value;

        // Obtener clienteId de la sesión si está disponible
        const rfc = sessionStorage.getItem('userRfc');
        if (rfc) {
            // Por ahora no filtramos por clienteId, se puede agregar después
            // params.append('clienteId', clienteId);
        }

        if (rfcEmisor) params.append('rfcEmisor', rfcEmisor);
        if (rfcReceptor) params.append('rfcReceptor', rfcReceptor);
        if (fechaInicial) params.append('fechaInicial', fechaInicial);
        if (fechaFinal) params.append('fechaFinal', fechaFinal);
        if (uuid) params.append('uuid', uuid);
        if (tipoComprobante) params.append('tipoComprobante', tipoComprobante);
        if (solicitudId) params.append('solicitudDescargaId', solicitudId);
        
        params.append('pageNumber', currentPage);
        params.append('pageSize', pageSize);

        // Llamar al API
        const response = await fetch(`${CFDI_API_URL}/listar?${params.toString()}`, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json'
            }
        });

        if (!response.ok) {
            throw new Error(`Error ${response.status}: ${response.statusText}`);
        }

        const result = await response.json();
        
        if (!result.success || !result.data) {
            throw new Error(result.message || 'Error al obtener CFDI');
        }

        const data = result.data;
        totalRegistros = data.totalRegistros;
        totalPages = data.totalPages;
        const cfdis = data.cfdis || [];

        // Limpiar selección
        selectedCFDI.clear();
        document.getElementById('checkTodos').checked = false;

        // Renderizar tabla
        renderizarTabla(cfdis);
        actualizarPaginacion();
        actualizarBotonDescargar();

        // Mostrar/ocultar estado vacío
        const emptyState = document.getElementById('emptyState');
        const tablaContainer = tablaBody.closest('.card');
        
        if (cfdis.length === 0) {
            if (emptyState) emptyState.style.display = 'block';
            if (tablaContainer) tablaContainer.style.display = 'none';
        } else {
            if (emptyState) emptyState.style.display = 'none';
            if (tablaContainer) tablaContainer.style.display = 'block';
        }

        // Actualizar badge
        const badgeTotal = document.getElementById('badgeTotalRegistros');
        if (badgeTotal) {
            badgeTotal.textContent = `${totalRegistros} registros`;
        }

    } catch (error) {
        console.error('Error al cargar CFDI:', error);
        mostrarError('Error al cargar CFDI: ' + error.message);
        
        const tablaBody = document.getElementById('tablaCFDI');
        if (tablaBody) {
            tablaBody.innerHTML = '<tr><td colspan="11" class="text-center text-danger">Error al cargar CFDI. Intenta nuevamente.</td></tr>';
        }
    }
}

/**
 * Renderiza los CFDI en la tabla
 */
function renderizarTabla(cfdis) {
    const tablaBody = document.getElementById('tablaCFDI');
    if (!tablaBody) return;

    tablaBody.innerHTML = '';

    if (cfdis.length === 0) {
        tablaBody.innerHTML = '<tr><td colspan="11" class="text-center text-muted">No se encontraron CFDI con los filtros aplicados</td></tr>';
        return;
    }

    cfdis.forEach(cfdi => {
        const row = document.createElement('tr');
        const isSelected = selectedCFDI.has(cfdi.uuid);
        
        // Formatear fecha
        const fechaEmision = new Date(cfdi.fechaEmision).toLocaleDateString('es-MX', {
            year: 'numeric',
            month: '2-digit',
            day: '2-digit'
        });

        // Formatear total
        const totalFormateado = new Intl.NumberFormat('es-MX', {
            style: 'currency',
            currency: cfdi.moneda || 'MXN'
        }).format(cfdi.total);

        // Badge de estatus
        const estatusBadge = obtenerBadgeEstatus(cfdi.estatus);
        
        // Tipo comprobante
        const tipoComprobante = obtenerTipoComprobante(cfdi.tipoComprobante);

        row.innerHTML = `
            <td class="text-center">
                <input type="checkbox" 
                       class="form-check-input" 
                       data-uuid="${cfdi.uuid}"
                       ${isSelected ? 'checked' : ''}
                       onchange="toggleSeleccion('${cfdi.uuid}', this.checked)">
            </td>
            <td class="text-center">
                ${cfdi.tieneArchivo 
                    ? `<button class="btn btn-sm btn-primary" 
                              onclick="descargarCFDI('${cfdi.uuid}')" 
                              title="Descargar XML">
                         <i class="fas fa-download"></i>
                       </button>`
                    : '<span class="text-muted">-</span>'}
            </td>
            <td><span class="font-monospace small">${cfdi.uuid}</span></td>
            <td>${cfdi.rfcEmisor || 'N/A'}</td>
            <td>${cfdi.nombreEmisor || 'N/A'}</td>
            <td>${cfdi.rfcReceptor || 'N/A'}</td>
            <td>${fechaEmision}</td>
            <td class="text-end">${totalFormateado}</td>
            <td><span class="badge bg-secondary">${tipoComprobante}</span></td>
            <td>${cfdi.serie || ''}${cfdi.folio ? ' / ' + cfdi.folio : ''}</td>
            <td>${estatusBadge}</td>
        `;

        tablaBody.appendChild(row);
    });
}

/**
 * Toggle de selección de un CFDI
 */
function toggleSeleccion(uuid, checked) {
    if (checked) {
        selectedCFDI.add(uuid);
    } else {
        selectedCFDI.delete(uuid);
        document.getElementById('checkTodos').checked = false;
    }
    actualizarBotonDescargar();
}

/**
 * Actualiza el botón de descargar seleccionados
 */
function actualizarBotonDescargar() {
    const btnDescargar = document.getElementById('btnDescargarSeleccionados');
    if (btnDescargar) {
        if (selectedCFDI.size > 0) {
            btnDescargar.style.display = 'block';
            btnDescargar.innerHTML = `<i class="fas fa-download me-1"></i> Descargar (${selectedCFDI.size})`;
        } else {
            btnDescargar.style.display = 'none';
        }
    }
}

/**
 * Descarga un CFDI individual
 */
async function descargarCFDI(uuid) {
    try {
        const btn = event.target.closest('button');
        const originalHtml = btn.innerHTML;
        btn.disabled = true;
        btn.innerHTML = '<i class="fas fa-spinner fa-spin"></i>';

        const response = await fetch(`${CFDI_API_URL}/${uuid}/descargar`, {
            method: 'GET'
        });

        if (!response.ok) {
            const errorData = await response.json().catch(() => ({}));
            throw new Error(errorData.message || `Error ${response.status}: ${response.statusText}`);
        }

        // Obtener nombre del archivo del header Content-Disposition
        const contentDisposition = response.headers.get('Content-Disposition');
        let filename = `${uuid}.xml`;
        
        if (contentDisposition) {
            const filenameMatch = contentDisposition.match(/filename="?(.+)"?/i);
            if (filenameMatch) {
                filename = filenameMatch[1];
            }
        }

        // Descargar archivo
        const blob = await response.blob();
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = filename;
        document.body.appendChild(a);
        a.click();
        window.URL.revokeObjectURL(url);
        document.body.removeChild(a);

        // Restaurar botón
        btn.disabled = false;
        btn.innerHTML = originalHtml;

        mostrarExito('CFDI descargado exitosamente');
    } catch (error) {
        console.error('Error al descargar CFDI:', error);
        mostrarError('Error al descargar CFDI: ' + error.message);
        
        // Restaurar botón
        const btn = event.target.closest('button');
        btn.disabled = false;
        btn.innerHTML = '<i class="fas fa-download"></i>';
    }
}

/**
 * Descarga los CFDI seleccionados en un ZIP
 */
async function descargarSeleccionados() {
    if (selectedCFDI.size === 0) {
        mostrarError('No hay CFDI seleccionados');
        return;
    }

    try {
        const btn = document.getElementById('btnDescargarSeleccionados');
        const originalHtml = btn.innerHTML;
        btn.disabled = true;
        btn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Descargando...';

        const response = await fetch(`${CFDI_API_URL}/descargar-lote`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                uuids: Array.from(selectedCFDI)
            })
        });

        if (!response.ok) {
            const errorData = await response.json().catch(() => ({}));
            throw new Error(errorData.message || `Error ${response.status}: ${response.statusText}`);
        }

        // Obtener nombre del archivo
        const contentDisposition = response.headers.get('Content-Disposition');
        let filename = `CFDI_Lote_${new Date().toISOString().slice(0,10)}.zip`;
        
        if (contentDisposition) {
            const filenameMatch = contentDisposition.match(/filename="?(.+)"?/i);
            if (filenameMatch) {
                filename = filenameMatch[1];
            }
        }

        // Descargar archivo
        const blob = await response.blob();
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = filename;
        document.body.appendChild(a);
        a.click();
        window.URL.revokeObjectURL(url);
        document.body.removeChild(a);

        // Limpiar selección
        selectedCFDI.clear();
        document.getElementById('checkTodos').checked = false;
        actualizarBotonDescargar();

        // Restaurar botón
        btn.disabled = false;
        btn.innerHTML = originalHtml;

        mostrarExito(`${selectedCFDI.size} CFDI descargados exitosamente`);
    } catch (error) {
        console.error('Error al descargar lote:', error);
        mostrarError('Error al descargar lote: ' + error.message);
        
        // Restaurar botón
        const btn = document.getElementById('btnDescargarSeleccionados');
        btn.disabled = false;
        btn.innerHTML = '<i class="fas fa-download me-1"></i> Descargar Seleccionados';
    }
}

/**
 * Limpia los filtros del formulario
 */
function limpiarFiltros() {
    document.getElementById('filtroRfcEmisor').value = '';
    document.getElementById('filtroRfcReceptor').value = '';
    document.getElementById('filtroFechaInicial').value = '';
    document.getElementById('filtroFechaFinal').value = '';
    document.getElementById('filtroUuid').value = '';
    document.getElementById('filtroTipoComprobante').value = '';
    document.getElementById('filtroSolicitudId').value = '';
}

/**
 * Actualiza la paginación
 */
function actualizarPaginacion() {
    const paginacionInfo = document.getElementById('paginacionInfo');
    const paginacion = document.getElementById('paginacion');

    if (paginacionInfo) {
        const inicio = totalRegistros === 0 ? 0 : (currentPage - 1) * pageSize + 1;
        const fin = Math.min(currentPage * pageSize, totalRegistros);
        paginacionInfo.textContent = `Mostrando ${inicio} - ${fin} de ${totalRegistros} registros`;
    }

    if (paginacion) {
        paginacion.innerHTML = '';

        // Botón Anterior
        const liAnterior = document.createElement('li');
        liAnterior.className = `page-item ${currentPage === 1 ? 'disabled' : ''}`;
        liAnterior.innerHTML = `<a class="page-link" href="#" onclick="cambiarPagina(${currentPage - 1}); return false;">Anterior</a>`;
        paginacion.appendChild(liAnterior);

        // Números de página
        const maxButtons = 5;
        let startPage = Math.max(1, currentPage - Math.floor(maxButtons / 2));
        let endPage = Math.min(totalPages, startPage + maxButtons - 1);
        
        if (endPage - startPage < maxButtons - 1) {
            startPage = Math.max(1, endPage - maxButtons + 1);
        }

        for (let i = startPage; i <= endPage; i++) {
            const li = document.createElement('li');
            li.className = `page-item ${i === currentPage ? 'active' : ''}`;
            li.innerHTML = `<a class="page-link" href="#" onclick="cambiarPagina(${i}); return false;">${i}</a>`;
            paginacion.appendChild(li);
        }

        // Botón Siguiente
        const liSiguiente = document.createElement('li');
        liSiguiente.className = `page-item ${currentPage === totalPages ? 'disabled' : ''}`;
        liSiguiente.innerHTML = `<a class="page-link" href="#" onclick="cambiarPagina(${currentPage + 1}); return false;">Siguiente</a>`;
        paginacion.appendChild(liSiguiente);
    }
}

/**
 * Cambia de página
 */
function cambiarPagina(pagina) {
    if (pagina < 1 || pagina > totalPages) return;
    currentPage = pagina;
    cargarCFDI();
    // Scroll hacia arriba
    window.scrollTo({ top: 0, behavior: 'smooth' });
}

/**
 * Obtiene el badge HTML para el estatus
 */
function obtenerBadgeEstatus(estatus) {
    const badges = {
        'Vigente': 'bg-success',
        'Cancelado': 'bg-danger',
        'NoEncontrado': 'bg-warning'
    };
    const badgeClass = badges[estatus] || 'bg-secondary';
    return `<span class="badge ${badgeClass}">${estatus}</span>`;
}

/**
 * Obtiene el texto del tipo de comprobante
 */
function obtenerTipoComprobante(tipo) {
    const tipos = {
        'Ingreso': 'I',
        'Egreso': 'E',
        'Traslado': 'T',
        'Nomina': 'N',
        'Pago': 'P'
    };
    return tipos[tipo] || tipo;
}

/**
 * Muestra mensaje de error
 */
function mostrarError(mensaje) {
    // TODO: Implementar toast/alert más elegante
    alert('Error: ' + mensaje);
}

/**
 * Muestra mensaje de éxito
 */
function mostrarExito(mensaje) {
    // TODO: Implementar toast/alert más elegante
    alert('Éxito: ' + mensaje);
}

// Exportar funciones globales para uso en HTML
window.descargarCFDI = descargarCFDI;
window.toggleSeleccion = toggleSeleccion;
window.cambiarPagina = cambiarPagina;

