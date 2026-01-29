using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SICOE.Application.Common;
using SICOE.Application.Interfaces.Services;

namespace SICOE.Infrastructure.Services.Sat;

/// <summary>
/// Provee certificados FIEL (e.firma) desde diferentes fuentes
/// </summary>
public class FielCertificateProvider : IFielCertificateProvider
{
    private readonly ILogger<FielCertificateProvider> _logger;
    private readonly IConfiguration _configuration;

    public FielCertificateProvider(ILogger<FielCertificateProvider> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Obtiene el certificado FIEL desde el Almacén de Certificados de Windows por Thumbprint
    /// </summary>
    public Result<X509Certificate2> GetFromStore(string thumbprint)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(thumbprint))
                return Result<X509Certificate2>.Failure("El thumbprint del certificado es requerido");

            // Limpiar thumbprint (quitar espacios si existen)
            var cleanThumbprint = thumbprint.Replace(" ", "").ToUpper();

            using var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);

            var certificates = store.Certificates.Find(X509FindType.FindByThumbprint, cleanThumbprint, false);

            if (certificates.Count == 0)
            {
                // Intentar en User Store como fallback
                using var userStore = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                userStore.Open(OpenFlags.ReadOnly);
                certificates = userStore.Certificates.Find(X509FindType.FindByThumbprint, cleanThumbprint, false);
            }

            if (certificates.Count == 0)
            {
                _logger.LogWarning("No se encontró el certificado con thumbprint {Thumbprint} en los almacenes LocalMachine/My o CurrentUser/My", cleanThumbprint);
                return Result<X509Certificate2>.Failure($"No se encontró el certificado con thumbprint {cleanThumbprint}");
            }

            var certificate = certificates[0];
            
            if (!certificate.HasPrivateKey)
            {
                return Result<X509Certificate2>.Failure("El certificado encontrado en el almacén no tiene una llave privada accesible");
            }

            _logger.LogInformation("Certificado cargado exitosamente desde el almacén. Subject: {Subject}", certificate.Subject);
            return Result<X509Certificate2>.Success(certificate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar certificado desde el almacén de Windows");
            return Result<X509Certificate2>.Failure($"Error al acceder al almacén de certificados: {ex.Message}");
        }
    }

    /// <summary>
    /// Carga el certificado desde bytes de un archivo PFX
    /// </summary>
    public Result<X509Certificate2> GetFromPfx(byte[] pfxData, string password)
    {
        try
        {
            if (pfxData == null || pfxData.Length == 0)
                return Result<X509Certificate2>.Failure("Los datos del PFX son requeridos");

            // IMPORTANTE: Usar UserKeySet en lugar de MachineKeySet para evitar problemas de permisos
            // MachineKeySet requiere permisos de administrador y puede causar que la clave privada no sea accesible
            var certificate = new X509Certificate2(
                pfxData, 
                password, 
                X509KeyStorageFlags.UserKeySet | X509KeyStorageFlags.Exportable);

            if (!certificate.HasPrivateKey)
            {
                _logger.LogError("El certificado PFX cargado no tiene clave privada accesible");
                return Result<X509Certificate2>.Failure("El certificado PFX no contiene una llave privada accesible");
            }

            // Verificar que la clave privada realmente se pueda obtener
            try
            {
                var testKey = certificate.GetRSAPrivateKey();
                if (testKey == null)
                {
                    _logger.LogError("No se pudo obtener la clave privada RSA del certificado después de cargarlo");
                    return Result<X509Certificate2>.Failure("El certificado no tiene una clave privada RSA accesible");
                }
                _logger.LogDebug("Certificado cargado exitosamente. Tiene clave privada RSA de {KeySize} bits", testKey.KeySize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar la clave privada del certificado");
                return Result<X509Certificate2>.Failure($"Error al verificar la clave privada: {ex.Message}");
            }

            return Result<X509Certificate2>.Success(certificate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar certificado desde PFX");
            return Result<X509Certificate2>.Failure($"Error al cargar PFX: {ex.Message}");
        }
    }

    /// <summary>
    /// Obtiene el certificado configurado por defecto (si existe thumbprint en configuración)
    /// </summary>
    public Result<X509Certificate2> GetDefaultCertificate()
    {
        var thumbprint = _configuration["Sat:CertificateThumbprint"];
        if (string.IsNullOrEmpty(thumbprint))
            return Result<X509Certificate2>.Failure("No se ha configurado un Thumbprint en Sat:CertificateThumbprint");

        return GetFromStore(thumbprint);
    }
}
