namespace SICOE.Application.Interfaces.Services;

/// <summary>
/// Interface para servicio de encriptación/desencriptación
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Encripta un texto plano
    /// </summary>
    string Encrypt(string plainText);

    /// <summary>
    /// Desencripta un texto encriptado
    /// </summary>
    string Decrypt(string encryptedText);

    /// <summary>
    /// Encripta bytes
    /// </summary>
    byte[] EncryptBytes(byte[] plainBytes);

    /// <summary>
    /// Desencripta bytes
    /// </summary>
    byte[] DecryptBytes(byte[] encryptedBytes);
}

