using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using SICOE.Application.Interfaces.Services;

namespace SICOE.Infrastructure.Services.Encryption;

/// <summary>
/// Implementación del servicio de encriptación usando ASP.NET Core Data Protection API
/// Usa las claves de protección de datos configuradas en la aplicación
/// </summary>
public class DataProtectionEncryptionService : IEncryptionService
{
    private readonly IDataProtector _protector;
    private readonly ILogger<DataProtectionEncryptionService> _logger;

    public DataProtectionEncryptionService(
        IDataProtectionProvider dataProtectionProvider,
        ILogger<DataProtectionEncryptionService> logger)
    {
        // Crear un protector específico para tokens SAT
        _protector = dataProtectionProvider.CreateProtector("SICOE.TokenSat");
        _logger = logger;
    }

    public string Encrypt(string plainText)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(plainText))
            {
                return plainText;
            }

            var protectedData = _protector.Protect(plainText);
            return protectedData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al encriptar texto");
            throw new InvalidOperationException("Error al encriptar datos", ex);
        }
    }

    public string Decrypt(string encryptedText)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(encryptedText))
            {
                return encryptedText;
            }

            var unprotectedData = _protector.Unprotect(encryptedText);
            return unprotectedData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al desencriptar texto");
            throw new InvalidOperationException("Error al desencriptar datos. El token puede estar corrupto o haber sido encriptado con una clave diferente.", ex);
        }
    }

    public byte[] EncryptBytes(byte[] plainBytes)
    {
        try
        {
            if (plainBytes == null || plainBytes.Length == 0)
            {
                return plainBytes ?? Array.Empty<byte>();
            }

            var plainText = Convert.ToBase64String(plainBytes);
            var encryptedText = Encrypt(plainText);
            return System.Text.Encoding.UTF8.GetBytes(encryptedText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al encriptar bytes");
            throw new InvalidOperationException("Error al encriptar datos", ex);
        }
    }

    public byte[] DecryptBytes(byte[] encryptedBytes)
    {
        try
        {
            if (encryptedBytes == null || encryptedBytes.Length == 0)
            {
                return encryptedBytes ?? Array.Empty<byte>();
            }

            var encryptedText = System.Text.Encoding.UTF8.GetString(encryptedBytes);
            var decryptedText = Decrypt(encryptedText);
            return Convert.FromBase64String(decryptedText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al desencriptar bytes");
            throw new InvalidOperationException("Error al desencriptar datos. El token puede estar corrupto o haber sido encriptado con una clave diferente.", ex);
        }
    }
}

