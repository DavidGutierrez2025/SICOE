/**
 * SICOE - FIEL Helper
 * Funciones auxiliares para obtener FIEL de sessionStorage y prepararla para envío
 */

/**
 * Limpia completamente la sesión FIEL de sessionStorage
 * Asegura que solo haya una sesión FIEL activa a la vez
 */
function limpiarSesionFiel() {
    sessionStorage.removeItem('sicoe_fiel_session');
    sessionStorage.removeItem('satToken');
    sessionStorage.removeItem('tokenExpiration');
    sessionStorage.removeItem('userRfc');
    console.log('Sesión FIEL limpiada completamente de sessionStorage');
}

/**
 * Redirige al usuario a la página de login cuando la sesión expira
 * @param {string} mensaje - Mensaje opcional a mostrar antes de redirigir
 */
function redirigirALoginPorSesionExpirada(mensaje) {
    const mensajeFinal = mensaje || 'Su sesión ha expirado. Por favor, autentíquese nuevamente con su certificado FIEL.';
    
    // Limpiar completamente la sesión FIEL de sessionStorage
    limpiarSesionFiel();
    
    console.warn('Sesión expirada. Redirigiendo al login...', mensajeFinal);
    
    // Mostrar mensaje y redirigir
    alert(mensajeFinal);
    window.location.href = '/';
}

/**
 * Obtiene la FIEL de sessionStorage y la prepara para enviar al servidor
 * Si la sesión expiró o no hay FIEL disponible, redirige automáticamente al login
 * @param {boolean} redirigirSiExpirada - Si es true, redirige al login cuando la sesión expira (default: true)
 * @returns {Object|null} Objeto con certificadoCer, clavePrivadaKey, password (todos en base64) o null
 */
function obtenerFielParaEnvio(redirigirSiExpirada = true) {
    try {
        const fielSession = sessionStorage.getItem('sicoe_fiel_session');
        if (!fielSession) {
            console.warn('No hay FIEL en sessionStorage. El usuario debe autenticarse nuevamente.');
            if (redirigirSiExpirada) {
                redirigirALoginPorSesionExpirada('No hay sesión activa. Por favor, autentíquese nuevamente con su certificado FIEL.');
            }
            return null;
        }
        
        const fielData = JSON.parse(fielSession);
        
        // Verificar que no esté expirada (30 minutos)
        const sessionTimeout = 30 * 60 * 1000; // 30 minutos
        if (Date.now() - fielData.timestamp > sessionTimeout) {
            console.warn('La sesión de FIEL ha expirado. El usuario debe autenticarse nuevamente.');
            if (redirigirSiExpirada) {
                redirigirALoginPorSesionExpirada('Su sesión ha expirado por inactividad. Por favor, autentíquese nuevamente con su certificado FIEL.');
            } else {
                sessionStorage.removeItem('sicoe_fiel_session');
            }
            return null;
        }
        
        return {
            certificadoCer: fielData.certificadoCer, // Ya está en base64
            clavePrivadaKey: fielData.clavePrivadaKey, // Ya está en base64
            password: fielData.password,
            rfc: fielData.rfc
        };
    } catch (error) {
        console.error('Error al obtener FIEL de sessionStorage:', error);
        if (redirigirSiExpirada) {
            redirigirALoginPorSesionExpirada('Error al verificar la sesión. Por favor, autentíquese nuevamente con su certificado FIEL.');
        }
        return null;
    }
}

/**
 * Obtiene el RFC de la sesión actual
 * Prioridad: 1) Sesión FIEL, 2) sessionStorage directo (userRfc)
 * @returns {string|null} RFC normalizado a mayúsculas o null si no se encuentra
 */
function obtenerRfcDeSesion() {
    // Prioridad 1: Intentar obtener de la sesión FIEL
    const fiel = obtenerFielParaEnvio(false); // No redirigir, solo verificar
    if (fiel && fiel.rfc) {
        return fiel.rfc.trim().toUpperCase();
    }
    
    // Prioridad 2: Intentar obtener de sessionStorage directo
    const userRfc = sessionStorage.getItem('userRfc');
    if (userRfc) {
        return userRfc.trim().toUpperCase();
    }
    
    return null;
}

/**
 * Valida que solo haya una sesión FIEL activa y que el RFC sea consistente
 * @returns {boolean} true si hay una sesión válida y única, false en caso contrario
 */
function validarSesionFielUnica() {
    try {
        // Verificar que solo haya una entrada de sesión FIEL
        const fielSession = sessionStorage.getItem('sicoe_fiel_session');
        const userRfc = sessionStorage.getItem('userRfc');
        
        if (!fielSession) {
            // No hay sesión FIEL activa
            return false;
        }
        
        const fielData = JSON.parse(fielSession);
        const rfcDeFiel = fielData.rfc;
        
        // Validar que el RFC en la sesión FIEL coincida con userRfc (si existe)
        if (userRfc && userRfc !== rfcDeFiel) {
            console.error('Inconsistencia detectada: RFC en sesión FIEL (' + rfcDeFiel + 
                         ') no coincide con userRfc (' + userRfc + ')');
            // Limpiar sesión inconsistente
            limpiarSesionFiel();
            return false;
        }
        
        // Validar que el RFC no esté vacío
        if (!rfcDeFiel || rfcDeFiel.trim() === '') {
            console.error('RFC vacío en sesión FIEL');
            limpiarSesionFiel();
            return false;
        }
        
        console.log('Sesión FIEL válida y única. RFC:', rfcDeFiel);
        return true;
    } catch (error) {
        console.error('Error al validar sesión FIEL única:', error);
        limpiarSesionFiel();
        return false;
    }
}

/**
 * Verifica si hay FIEL disponible en la sesión
 * @param {boolean} redirigirSiExpirada - Si es true, redirige al login cuando la sesión expira (default: false)
 * @returns {boolean}
 */
function hayFielDisponible(redirigirSiExpirada = false) {
    return obtenerFielParaEnvio(redirigirSiExpirada) !== null;
}

/**
 * Agrega automáticamente la FIEL a un objeto de datos antes de enviarlo al servidor
 * @param {Object} data - Objeto de datos a enviar
 * @returns {Object} Objeto con la FIEL agregada
 */
function agregarFielADatos(data) {
    const fiel = obtenerFielParaEnvio();
    if (fiel) {
        return {
            ...data,
            certificadoCer: fiel.certificadoCer,
            clavePrivadaKey: fiel.clavePrivadaKey,
            passwordFiel: fiel.password
        };
    }
    return data;
}

