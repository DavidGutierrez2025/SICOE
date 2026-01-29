/**
 * SICOE - FIEL Session Manager
 * Gestiona la FIEL en sessionStorage para usarla en solicitudes que requieren firma
 * 
 * IMPORTANTE: La FIEL se mantiene SOLO en sessionStorage del navegador
 * y se elimina automáticamente al cerrar la sesión.
 */

const FIEL_SESSION_KEY = 'sicoe_fiel_session';

/**
 * Guarda la FIEL en sessionStorage después del login exitoso
 * IMPORTANTE: Solo permite una sesión FIEL activa a la vez.
 * Si ya existe una sesión activa, se limpia antes de guardar la nueva.
 * @param {Object} fielData - Objeto con certificadoCer, clavePrivadaKey, password, rfc
 */
function guardarFielEnSesion(fielData) {
    try {
        // CRÍTICO: Limpiar cualquier sesión FIEL anterior antes de guardar la nueva
        // Esto asegura que solo haya una sesión FIEL activa en sessionStorage
        eliminarFielDeSesion();
        
        if (!fielData.rfc) {
            console.error('RFC no proporcionado en fielData');
            throw new Error('RFC es requerido para guardar la sesión FIEL');
        }

        const sessionData = {
            certificadoCer: arrayBufferToBase64(fielData.certificadoCer),
            clavePrivadaKey: arrayBufferToBase64(fielData.clavePrivadaKey),
            password: fielData.password, // Se mantiene en sessionStorage durante la sesión
            rfc: fielData.rfc, // RFC único para esta sesión
            timestamp: Date.now()
        };
        
        sessionStorage.setItem(FIEL_SESSION_KEY, JSON.stringify(sessionData));
        console.log('FIEL guardada en sessionStorage para uso en solicitudes. RFC:', fielData.rfc);
    } catch (error) {
        console.error('Error al guardar FIEL en sessionStorage:', error);
        throw error;
    }
}

/**
 * Obtiene la FIEL de sessionStorage
 * @returns {Object|null} Objeto con certificadoCer, clavePrivadaKey, password, rfc o null si no existe
 */
function obtenerFielDeSesion() {
    try {
        const sessionData = sessionStorage.getItem(FIEL_SESSION_KEY);
        if (!sessionData) {
            return null;
        }
        
        const data = JSON.parse(sessionData);
        
        // Convertir base64 de vuelta a ArrayBuffer
        return {
            certificadoCer: base64ToArrayBuffer(data.certificadoCer),
            clavePrivadaKey: base64ToArrayBuffer(data.clavePrivadaKey),
            password: data.password,
            rfc: data.rfc,
            timestamp: data.timestamp
        };
    } catch (error) {
        console.error('Error al obtener FIEL de sessionStorage:', error);
        return null;
    }
}

/**
 * Elimina completamente la sesión FIEL de sessionStorage (al hacer logout o antes de una nueva autenticación)
 * IMPORTANTE: Limpia TODOS los datos relacionados con la sesión FIEL para asegurar que solo haya una sesión activa
 */
function eliminarFielDeSesion() {
    try {
        sessionStorage.removeItem(FIEL_SESSION_KEY);
        sessionStorage.removeItem('satToken');
        sessionStorage.removeItem('tokenExpiration');
        sessionStorage.removeItem('userRfc');
        console.log('Sesión FIEL eliminada completamente de sessionStorage');
    } catch (error) {
        console.error('Error al eliminar FIEL de sessionStorage:', error);
    }
}

/**
 * Verifica si hay FIEL disponible en la sesión
 * @returns {boolean}
 */
function hayFielEnSesion() {
    return sessionStorage.getItem(FIEL_SESSION_KEY) !== null;
}

/**
 * Obtiene el RFC de la sesión
 * @returns {string|null}
 */
function obtenerRfcDeSesion() {
    const fiel = obtenerFielDeSesion();
    return fiel ? fiel.rfc : null;
}

/**
 * Prepara los datos de FIEL para enviar al servidor
 * @returns {Object|null} Objeto con certificadoCer, clavePrivadaKey, password en base64
 */
function prepararFielParaEnvio() {
    const fiel = obtenerFielDeSesion();
    if (!fiel) {
        return null;
    }
    
    return {
        certificadoCer: arrayBufferToBase64(fiel.certificadoCer),
        clavePrivadaKey: arrayBufferToBase64(fiel.clavePrivadaKey),
        password: fiel.password
    };
}

// Funciones auxiliares para conversión
function arrayBufferToBase64(buffer) {
    const bytes = new Uint8Array(buffer);
    let binary = '';
    for (let i = 0; i < bytes.byteLength; i++) {
        binary += String.fromCharCode(bytes[i]);
    }
    return btoa(binary);
}

function base64ToArrayBuffer(base64) {
    const binary = atob(base64);
    const bytes = new Uint8Array(binary.length);
    for (let i = 0; i < binary.length; i++) {
        bytes[i] = binary.charCodeAt(i);
    }
    return bytes.buffer;
}

