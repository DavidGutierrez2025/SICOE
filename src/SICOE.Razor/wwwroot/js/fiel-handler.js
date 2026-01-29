/**
 * SICOE - FIEL Handler
 * Maneja la captura y autenticación con FIEL en el navegador
 * 
 * IMPORTANTE: La FIEL se mantiene en memoria del navegador y NO se almacena
 * en localStorage, sessionStorage, cookies, ni se envía al servidor excepto
 * cuando es necesario obtener el token SAT.
 */

// Almacenamiento de FIEL en memoria (NO persistente)
// NOTA: El SAT de México solicita .cer y .key por separado (NO .pfx)
let fielEnMemoria = {
    certificadoCer: null,  // ArrayBuffer del certificado .cer
    clavePrivadaKey: null, // ArrayBuffer de la clave privada .key
    password: null,         // Contraseña de la clave privada (solo en memoria durante la sesión)
    rfc: null,             // RFC del usuario
    timestamp: null         // Timestamp de cuando se cargó
};

// Configuración
const CONFIG = {
    // URL base de la API
    // Usa window.apiBaseUrl si está disponible (inyectado desde el servidor)
    // En desarrollo: API está en https://localhost:7052 (HTTPS) o http://localhost:5173 (HTTP)
    // En producción: ajustar según la URL del API
    get apiBaseUrl() {
        const baseUrl = window.apiBaseUrl || 'https://localhost:7052';
        return `${baseUrl}/api`;
    },
    maxFileSize: 5 * 1024 * 1024, // 5MB máximo
    sessionTimeout: 30 * 60 * 1000 // 30 minutos
};

/**
 * Inicialización cuando el DOM está listo
 */
document.addEventListener('DOMContentLoaded', function () {
    initializeFielHandler();
});

/**
 * Inicializa el manejador de FIEL
 */
function initializeFielHandler() {
    const form = document.getElementById('fielForm');
    if (!form) return; // Si no existe el formulario, salir

    const fileCerInput = document.getElementById('fileCertificadoCer');
    const fileKeyInput = document.getElementById('fileClavePrivadaKey');
    const passwordInput = document.getElementById('passwordFiel');
    const rfcInput = document.getElementById('rfc');
    const submitBtn = document.getElementById('btnEnviar');

    // Validar archivos cuando se seleccionan
    if (fileCerInput) {
        fileCerInput.addEventListener('change', function (e) {
            handleFileSelection(e.target.files[0], 'certificadoCer');
        });
    }

    if (fileKeyInput) {
        fileKeyInput.addEventListener('change', function (e) {
            handleFileSelection(e.target.files[0], 'clavePrivadaKey');
        });
    }

    // Validar campos cuando se escriben
    if (passwordInput) {
        passwordInput.addEventListener('input', function () {
            validateForm();
        });
    }

    if (rfcInput) {
        rfcInput.addEventListener('input', function () {
            // Convertir a mayúsculas y validar formato
            this.value = this.value.toUpperCase().replace(/[^A-ZÑ&0-9]/g, '');
            validateForm();
        });
    }

    // Enviar formulario
    form.addEventListener('submit', function (e) {
        e.preventDefault();
        handleAuthentication();
    });

    // Validación inicial
    validateForm();
}

/**
 * Maneja la selección de archivos FIEL (.cer o .key)
 */
function handleFileSelection(file, type) {
    if (!file) {
        if (type === 'certificadoCer') {
            fielEnMemoria.certificadoCer = null;
        } else if (type === 'clavePrivadaKey') {
            fielEnMemoria.clavePrivadaKey = null;
        }
        validateForm();
        return;
    }

    // Validar tipo de archivo según el tipo
    // NOTA: El SAT solicita .cer y .key por separado (NO .pfx)
    let validExtensions = [];
    if (type === 'certificadoCer') {
        validExtensions = ['.cer'];
    } else if (type === 'clavePrivadaKey') {
        validExtensions = ['.key'];
    }

    const fileExtension = file.name.toLowerCase().substring(file.name.lastIndexOf('.'));

    if (!validExtensions.includes(fileExtension)) {
        showAlert('error', `Por favor seleccione un archivo ${validExtensions.join(' o ')} válido.`);
        if (type === 'certificadoCer') {
            fielEnMemoria.certificadoCer = null;
        } else if (type === 'clavePrivadaKey') {
            fielEnMemoria.clavePrivadaKey = null;
        }
        validateForm();
        return;
    }

    // Validar tamaño
    if (file.size > CONFIG.maxFileSize) {
        showAlert('error', `El archivo es demasiado grande. Tamaño máximo: ${CONFIG.maxFileSize / 1024 / 1024}MB`);
        if (type === 'certificadoCer') {
            fielEnMemoria.certificadoCer = null;
        } else if (type === 'clavePrivadaKey') {
            fielEnMemoria.clavePrivadaKey = null;
        }
        validateForm();
        return;
    }

    // Leer archivo como ArrayBuffer
    const reader = new FileReader();
    reader.onload = async function (e) {
        const arrayBuffer = e.target.result;

        // Almacenar en memoria según el tipo
        if (type === 'certificadoCer') {
            // CRÍTICO: Limpiar RFC anterior antes de cargar nuevo certificado
            // Esto asegura que no se use un RFC de un certificado anterior
            fielEnMemoria.rfc = null;
            const rfcInput = document.getElementById('rfc');
            if (rfcInput) {
                rfcInput.value = '';
                rfcInput.classList.remove('is-valid', 'is-invalid');
            }
            
            fielEnMemoria.certificadoCer = arrayBuffer;
            // Extraer RFC automáticamente del certificado .cer cargado
            // IMPORTANTE: El RFC debe venir del certificado, no de una sesión anterior
            await extraerRfcDelCertificado(arrayBuffer);
        } else if (type === 'clavePrivadaKey') {
            fielEnMemoria.clavePrivadaKey = arrayBuffer;
        }

        fielEnMemoria.timestamp = Date.now();

        console.log(`Archivo ${type} cargado en memoria (no almacenado)`);
        validateForm();
    };

    reader.onerror = function () {
        showAlert('error', 'Error al leer el archivo. Por favor intente nuevamente.');
        if (type === 'certificadoCer') {
            fielEnMemoria.certificadoCer = null;
        } else if (type === 'clavePrivadaKey') {
            fielEnMemoria.clavePrivadaKey = null;
        }
        validateForm();
    };

    reader.readAsArrayBuffer(file);
}

/**
 * Valida el formulario y habilita/deshabilita el botón de envío
 */
function validateForm() {
    const passwordInput = document.getElementById('passwordFiel');
    const rfcInput = document.getElementById('rfc');
    const submitBtn = document.getElementById('btnEnviar');

    if (!submitBtn) return;

    const hasCer = fielEnMemoria.certificadoCer !== null;
    const hasKey = fielEnMemoria.clavePrivadaKey !== null;
    const hasPassword = passwordInput && passwordInput.value.trim().length > 0;
    const hasRfc = rfcInput && rfcInput.value.trim().length >= 12;

    // Validar que tenemos .cer y .key, contraseña y RFC
    // NOTA: El SAT solicita .cer y .key por separado
    if (hasCer && hasKey && hasPassword && hasRfc) {
        submitBtn.disabled = false;

        // Guardar contraseña y RFC en memoria (solo durante esta sesión)
        if (passwordInput) {
            fielEnMemoria.password = passwordInput.value;
        }
        if (rfcInput) {
            fielEnMemoria.rfc = rfcInput.value.trim().toUpperCase();
        }
    } else {
        submitBtn.disabled = true;
    }
}

/**
 * Maneja el proceso de autenticación
 * IMPORTANTE: Solo permite una sesión FIEL activa a la vez.
 * Si ya existe una sesión activa, se limpia antes de autenticar con la nueva FIEL.
 */
async function handleAuthentication() {
    const passwordInput = document.getElementById('passwordFiel');
    
    // CRÍTICO: Verificar si ya hay una sesión FIEL activa y advertir al usuario
    // Aunque sessionStorage solo permite una clave, esto asegura transparencia
    const sesionExistente = sessionStorage.getItem('sicoe_fiel_session');
    if (sesionExistente) {
        try {
            const sesionData = JSON.parse(sesionExistente);
            const rfcExistente = sesionData.rfc;
            console.warn('Ya existe una sesión FIEL activa con RFC:', rfcExistente, 
                        '. La nueva autenticación reemplazará la sesión anterior.');
        } catch (e) {
            console.warn('Sesión FIEL existente con formato inválido, se limpiará');
        }
    }
    const rfcInput = document.getElementById('rfc');
    const submitBtn = document.getElementById('btnEnviar');

    // Validar que tenemos todos los datos necesarios
    if (!fielEnMemoria.certificadoCer || !fielEnMemoria.clavePrivadaKey || !fielEnMemoria.password || !fielEnMemoria.rfc) {
        showAlert('error', 'Por favor complete todos los campos requeridos.');
        return;
    }

    // Deshabilitar botón y mostrar loading
    if (submitBtn) {
        submitBtn.disabled = true;
        submitBtn.innerHTML = '<span class="spinner-border spinner-border-sm mr-2"></span>Enviando...';
    }
    clearAlerts();

    try {
        // Validar que los archivos estén cargados
        if (!fielEnMemoria.certificadoCer || fielEnMemoria.certificadoCer.byteLength === 0) {
            showAlert('error', 'El archivo .cer no está cargado o está vacío. Por favor, selecciónelo nuevamente.');
            if (submitBtn) {
                submitBtn.disabled = false;
                submitBtn.innerHTML = 'Enviar';
            }
            return;
        }

        if (!fielEnMemoria.clavePrivadaKey || fielEnMemoria.clavePrivadaKey.byteLength === 0) {
            showAlert('error', 'El archivo .key no está cargado o está vacío. Por favor, selecciónelo nuevamente.');
            if (submitBtn) {
                submitBtn.disabled = false;
                submitBtn.innerHTML = 'Enviar';
            }
            return;
        }

        console.log('Tamaño del archivo .cer:', fielEnMemoria.certificadoCer.byteLength, 'bytes');
        console.log('Tamaño del archivo .key:', fielEnMemoria.clavePrivadaKey.byteLength, 'bytes');

        // Convertir ArrayBuffers a base64 para enviar al servidor
        const base64Cer = arrayBufferToBase64(fielEnMemoria.certificadoCer);
        const base64Key = arrayBufferToBase64(fielEnMemoria.clavePrivadaKey);

        // Validar que las conversiones fueron exitosas
        if (!base64Cer || base64Cer.length === 0) {
            showAlert('error', 'Error al procesar el archivo .cer. Por favor, intente nuevamente.');
            if (submitBtn) {
                submitBtn.disabled = false;
                submitBtn.innerHTML = 'Enviar';
            }
            return;
        }

        if (!base64Key || base64Key.length === 0) {
            showAlert('error', 'Error al procesar el archivo .key. Por favor, intente nuevamente.');
            if (submitBtn) {
                submitBtn.disabled = false;
                submitBtn.innerHTML = 'Enviar';
            }
            return;
        }

        console.log('Tamaño Base64 .cer:', base64Cer.length, 'caracteres');
        console.log('Tamaño Base64 .key:', base64Key.length, 'caracteres');

        // CRÍTICO: Validar que el RFC provenga del certificado cargado, no de una sesión anterior
        if (!fielEnMemoria.rfc || fielEnMemoria.rfc.trim() === '') {
            throw new Error('El RFC no está disponible. Por favor, cargue el certificado .cer nuevamente.');
        }

        // Preparar datos para enviar
        // IMPORTANTE: El RFC debe ser el extraído del certificado .cer cargado
        const requestData = {
            certificadoCer: base64Cer,
            clavePrivadaKey: base64Key,
            passwordFiel: fielEnMemoria.password,
            rfc: fielEnMemoria.rfc.trim().toUpperCase() // RFC del certificado cargado
        };
        
        console.log('Enviando autenticación con RFC del certificado:', requestData.rfc);

        // Enviar al servidor para obtener token SAT
        const response = await fetch(`${CONFIG.apiBaseUrl}/autenticacion/obtener-token`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(requestData)
        });

        if (!response.ok) {
            const errorData = await response.json().catch(() => ({ message: 'Error desconocido' }));
            throw new Error(errorData.message || `Error ${response.status}: ${response.statusText}`);
        }

        const result = await response.json();

        if (result.success) {
            // Autenticación exitosa
            showAlert('success', 'Autenticación exitosa. Redirigiendo...');

            // CRÍTICO: Limpiar cualquier sesión FIEL anterior antes de guardar la nueva
            // Esto asegura que solo haya una sesión FIEL activa en sessionStorage
            sessionStorage.removeItem('sicoe_fiel_session');
            sessionStorage.removeItem('satToken');
            sessionStorage.removeItem('tokenExpiration');
            sessionStorage.removeItem('userRfc');
            console.log('Sesión FIEL anterior limpiada para asegurar una única sesión activa');

            // Guardar FIEL en sessionStorage para uso en solicitudes posteriores
            // IMPORTANTE: Se guarda SOLO en sessionStorage del navegador, se elimina al cerrar sesión
            // IMPORTANTE: Solo puede haber UNA sesión FIEL activa a la vez
            if (fielEnMemoria.certificadoCer && fielEnMemoria.clavePrivadaKey && fielEnMemoria.password) {
                const rfcFinal = fielEnMemoria.rfc || result.rfc;
                if (!rfcFinal) {
                    console.error('RFC no disponible en la respuesta de autenticación');
                    throw new Error('No se pudo obtener el RFC del certificado FIEL');
                }

                const fielSessionData = {
                    certificadoCer: arrayBufferToBase64(fielEnMemoria.certificadoCer),
                    clavePrivadaKey: arrayBufferToBase64(fielEnMemoria.clavePrivadaKey),
                    password: fielEnMemoria.password,
                    rfc: rfcFinal, // RFC único para esta sesión
                    timestamp: Date.now()
                };
                sessionStorage.setItem('sicoe_fiel_session', JSON.stringify(fielSessionData));
                console.log('FIEL guardada en sessionStorage para uso en solicitudes que requieren firma. RFC:', rfcFinal);
            }

            // Guardar token en sessionStorage (solo uno activo)
            if (result.token) {
                sessionStorage.setItem('satToken', result.token);
                sessionStorage.setItem('tokenExpiration', result.expiration);
                const rfcFinal = fielEnMemoria.rfc || result.rfc;
                if (rfcFinal) {
                    sessionStorage.setItem('userRfc', rfcFinal); // RFC único para esta sesión
                    console.log('RFC guardado en sessionStorage:', rfcFinal);
                }
            }

            // Limpiar FIEL de memoria (ya está en sessionStorage)
            clearFielFromMemory();

            // Redirigir después de un breve delay
            setTimeout(() => {
                window.location.href = '/CFDI';
            }, 1500);
        } else {
            throw new Error(result.message || 'Error al autenticar');
        }
    } catch (error) {
        console.error('Error en autenticación:', error);
        showAlert('error', `Error al autenticar: ${error.message}`);

        // Limpiar FIEL de memoria en caso de error
        clearFielFromMemory();
        passwordInput.value = '';
    } finally {
        // Restaurar botón
        submitBtn.disabled = false;
        loadingSpinner.classList.add('d-none');
        btnText.textContent = 'Autenticar con FIEL';
    }
}

/**
 * Extrae el RFC del certificado .cer automáticamente
 * @param {ArrayBuffer} certificadoCer ArrayBuffer del certificado .cer
 */
async function extraerRfcDelCertificado(certificadoCer) {
    const rfcInput = document.getElementById('rfc');
    if (!rfcInput) return;

    try {
        // Mostrar indicador de carga
        rfcInput.disabled = true;
        rfcInput.value = 'Extrayendo RFC...';
        rfcInput.classList.add('is-loading');

        // Convertir ArrayBuffer a base64
        const base64Cer = arrayBufferToBase64(certificadoCer);

        // Llamar al API para extraer RFC
        const response = await fetch(`${CONFIG.apiBaseUrl}/autenticacion/extraer-rfc`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({
                certificadoCer: base64Cer
            }),
        });

        const result = await response.json();

        if (result.success && result.data && result.data.rfc) {
            // CRÍTICO: El RFC debe venir del certificado cargado, no de ninguna otra fuente
            const rfcExtraido = result.data.rfc.trim().toUpperCase();
            
            // Validar que el RFC tenga el formato correcto (12 o 13 caracteres)
            if (rfcExtraido.length < 12 || rfcExtraido.length > 13) {
                throw new Error('El RFC extraído no tiene un formato válido');
            }
            
            // Llenar automáticamente el campo RFC con el RFC extraído del certificado
            rfcInput.value = rfcExtraido;
            fielEnMemoria.rfc = rfcExtraido; // Guardar en memoria el RFC del certificado actual
            rfcInput.classList.remove('is-invalid', 'is-loading');
            rfcInput.classList.add('is-valid');
            rfcInput.readOnly = true; // Hacer de solo lectura después de extraerlo
            
            console.log('RFC extraído del certificado .cer:', rfcExtraido);
            showAlert('success', `RFC extraído automáticamente: ${rfcExtraido}`);
        } else {
            throw new Error(result.message || 'No se pudo extraer el RFC del certificado');
        }
    } catch (error) {
        console.error('Error al extraer RFC:', error);
        rfcInput.value = '';
        rfcInput.classList.remove('is-valid', 'is-loading');
        rfcInput.classList.add('is-invalid');
        rfcInput.readOnly = false; // Permitir edición manual si falla la extracción
        showAlert('warning', 'No se pudo extraer el RFC automáticamente. Por favor, ingréselo manualmente.');
    } finally {
        // Habilitar el campo (aunque esté lleno, por si el usuario necesita corregirlo)
        rfcInput.disabled = false;
    }
}

/**
 * Convierte ArrayBuffer a Base64
 */
function arrayBufferToBase64(buffer) {
    const bytes = new Uint8Array(buffer);
    let binary = '';
    for (let i = 0; i < bytes.byteLength; i++) {
        binary += String.fromCharCode(bytes[i]);
    }
    return btoa(binary);
}

/**
 * Extrae el RFC del certificado .cer automáticamente
 * @param {ArrayBuffer} certificadoCer ArrayBuffer del certificado .cer
 */
async function extraerRfcDelCertificado(certificadoCer) {
    const rfcInput = document.getElementById('rfc');
    if (!rfcInput) return;

    try {
        // Mostrar indicador de carga
        rfcInput.disabled = true;
        rfcInput.value = 'Extrayendo RFC...';
        rfcInput.classList.add('is-loading');

        // Convertir ArrayBuffer a base64
        const base64Cer = arrayBufferToBase64(certificadoCer);

        // Llamar al API para extraer RFC
        const response = await fetch(`${CONFIG.apiBaseUrl}/autenticacion/extraer-rfc`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({
                certificadoCer: base64Cer
            }),
        });

        const result = await response.json();

        if (result.success && result.data && result.data.rfc) {
            // Llenar automáticamente el campo RFC
            rfcInput.value = result.data.rfc;
            fielEnMemoria.rfc = result.data.rfc;
            rfcInput.classList.remove('is-invalid', 'is-loading');
            rfcInput.classList.add('is-valid');
            showAlert('success', `RFC extraído automáticamente: ${result.data.rfc}`);
        } else {
            throw new Error(result.message || 'No se pudo extraer el RFC del certificado');
        }
    } catch (error) {
        console.error('Error al extraer RFC:', error);
        rfcInput.value = '';
        rfcInput.classList.remove('is-valid', 'is-loading');
        rfcInput.classList.add('is-invalid');
        showAlert('warning', 'No se pudo extraer el RFC automáticamente. Por favor, ingréselo manualmente.');
    } finally {
        // Habilitar el campo (aunque esté lleno, por si el usuario necesita corregirlo)
        rfcInput.disabled = false;
    }
}

/**
 * Limpia la FIEL de memoria
 */
function clearFielFromMemory() {
    fielEnMemoria.certificadoCer = null;
    fielEnMemoria.clavePrivadaKey = null;
    fielEnMemoria.password = null;
    fielEnMemoria.rfc = null;
    fielEnMemoria.timestamp = null;

    // Limpiar también los inputs de archivo
    const fileCerInput = document.getElementById('fileCertificadoCer');
    const fileKeyInput = document.getElementById('fileClavePrivadaKey');
    if (fileCerInput) fileCerInput.value = '';
    if (fileKeyInput) fileKeyInput.value = '';

    // Limpiar campo RFC
    const rfcInput = document.getElementById('rfc');
    if (rfcInput) {
        rfcInput.value = '';
        rfcInput.classList.remove('is-valid', 'is-invalid', 'is-loading');
    }
}

/**
 * Limpia todos los mensajes de alerta
 */
function clearAlerts() {
    const alertContainer = document.getElementById('alertContainer');
    if (alertContainer) {
        alertContainer.innerHTML = '';
    }
}

/**
 * Muestra un mensaje de alerta
 */
function showAlert(type, message) {
    const alertContainer = document.getElementById('alertContainer');
    if (!alertContainer) return;

    let alertClass = 'alert-info';
    let icon = 'fas fa-info-circle';

    if (type === 'success') {
        alertClass = 'alert-success';
        icon = 'fas fa-check-circle';
    } else if (type === 'error') {
        alertClass = 'alert-danger';
        icon = 'fas fa-exclamation-triangle';
    } else if (type === 'warning') {
        alertClass = 'alert-warning';
        icon = 'fas fa-exclamation-circle';
    }

    alertContainer.innerHTML = `
        <div class="alert ${alertClass} alert-dismissible fade show" role="alert">
            <i class="${icon} me-2"></i>${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
        </div>
    `;

    // Auto-dismiss después de 5 segundos para errores y warnings
    if (type === 'error' || type === 'warning' || type === 'info') {
        setTimeout(() => {
            const alert = alertContainer.querySelector('.alert');
            if (alert) {
                $(alert).alert('close');
            }
        }, 5000);
    }
}

/**
 * Limpia todos los mensajes de alerta
 */
function clearAlerts() {
    const alertContainer = document.getElementById('alertContainer');
    alertContainer.innerHTML = '';
}

/**
 * Limpia la FIEL de memoria cuando se cierra la página
 */
window.addEventListener('beforeunload', function () {
    clearFielFromMemory();
});

