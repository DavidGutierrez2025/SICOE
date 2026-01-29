using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Org.BouncyCastle.Security;

namespace SICOE.Application.Common.Helpers.Fiel;

/// <summary>
/// Helper para combinar archivos .cer y .key en un .pfx temporal
/// NOTA: El SAT de México solicita .cer y .key por separado.
/// - El archivo .cer es el certificado público
/// - El archivo .key es la clave privada (está en formato PEM aunque tenga extensión .key)
/// System.Security.Cryptography requiere un formato .pfx para trabajar con la clave privada,
/// por lo que este helper combina ambos archivos temporalmente en memoria.
/// </summary>
public static class FielCerKeyHelper
{
    /// <summary>
    /// Combina un certificado .cer y una clave privada .key en un .pfx en memoria
    /// </summary>
    public static byte[] CombinarCerYKeyEnPfx(byte[] certificadoCer, byte[] clavePrivadaKey, string password)
    {
        try
        {
            // Leer el certificado .cer
            var certificateParser = new X509CertificateParser();
            var cert = certificateParser.ReadCertificate(certificadoCer);

            // Leer la clave privada .key
            // NOTA: Los archivos .key del SAT pueden venir en dos formatos:
            // 1. PEM (Base64 con headers BEGIN/END)
            // 2. DER (Binario puro, generalmente PKCS#8 EncryptedPrivateKeyInfo)
            
            if (clavePrivadaKey == null || clavePrivadaKey.Length == 0)
            {
                throw new CryptographicException("El archivo .key está vacío o no se pudo leer.");
            }

            AsymmetricKeyParameter? privateKey = null;
            bool isPem = false;
            string keyContentIdx = "";

            // 1. Intentar detectar si es PEM
            try
            {
                // Intentar leer como string para buscar headers PEM
                string possiblePem = Encoding.UTF8.GetString(clavePrivadaKey);
                if (possiblePem.Contains("BEGIN", StringComparison.OrdinalIgnoreCase) && 
                    possiblePem.Contains("END", StringComparison.OrdinalIgnoreCase))
                {
                    isPem = true;
                    keyContentIdx = possiblePem;
                }
            }
            catch { /* No es texto válido, probablemente es binario DER */ }

            if (isPem)
            {
                // --- Lógica para PEM ---
                var passwordFinder = new SimplePasswordFinder(password);
                using (var reader = new StringReader(keyContentIdx))
                {
                    var pemReader = new PemReader(reader, passwordFinder);
                    object? obj = pemReader.ReadObject();
                    
                    if (obj == null) 
                        throw new CryptographicException("No se pudo leer el objeto PEM del archivo .key");

                    if (obj is AsymmetricCipherKeyPair keyPair)
                        privateKey = keyPair.Private;
                    else if (obj is AsymmetricKeyParameter keyParam && keyParam.IsPrivate)
                        privateKey = keyParam;
                    else
                        throw new CryptographicException($"Tipo de objeto PEM no esperado: {obj.GetType().Name}");
                }
            }
            else
            {
                // --- Lógica para DER (Binario) ---
                try 
                {
                    // Asumimos que es PKCS#8 EncryptedPrivateKeyInfo (formato estándar SAT para .key binario)
                    var asn1 = Asn1Object.FromByteArray(clavePrivadaKey);
                    var encryptedPrivateKeyInfo = EncryptedPrivateKeyInfo.GetInstance(asn1);
                    
                    // Desencriptar usando la contraseña
                    // Necesitamos pasar la contraseña como char[]
                    privateKey = PrivateKeyFactory.DecryptKey(password.ToCharArray(), encryptedPrivateKeyInfo);
                }
                catch (Exception ex)
                {
                    // Si falla como DER encriptado, podría ser una clave no encriptada (raro para SAT)
                    // o formato inválido.
                     throw new CryptographicException($"No se pudo leer el archivo .key como binario DER ni como PEM. Verifique que la contraseña sea correcta.", ex);
                }
            }

            if (privateKey == null)
            {
                throw new CryptographicException("No se pudo leer la clave privada del archivo .key");
            }

            // Validar que la clave privada corresponde al certificado
            // Esto verifica que el .key y el .cer pertenecen al mismo par de claves
            var certPublicKey = cert.GetPublicKey();
            var privateKeyPublicKey = ExtractPublicKeyFromPrivateKey(privateKey);
            
            if (!KeysMatch(certPublicKey, privateKeyPublicKey))
            {
                throw new CryptographicException("La clave privada del archivo .key no corresponde al certificado .cer. Verifique que ambos archivos pertenezcan al mismo par de claves de la e.firma del SAT.");
            }

            // Crear el store PKCS12
            var store = new Pkcs12StoreBuilder().Build();
            
            // Crear la entrada del certificado con la clave privada
            var certEntry = new X509CertificateEntry(cert);
            store.SetKeyEntry("FIEL", new AsymmetricKeyEntry(privateKey), new[] { certEntry });

            // Exportar a PKCS12 (.pfx) en memoria
            using (var memoryStream = new MemoryStream())
            {
                store.Save(memoryStream, password.ToCharArray(), new Org.BouncyCastle.Security.SecureRandom());
                return memoryStream.ToArray();
            }
        }
        catch (Exception ex)
        {
            throw new CryptographicException($"Error al combinar .cer y .key en .pfx: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Valida si los bytes corresponden a un archivo .cer válido
    /// </summary>
    public static bool EsCertificadoCerValido(byte[] bytes)
    {
        try
        {
            var parser = new X509CertificateParser();
            var cert = parser.ReadCertificate(bytes);
            return cert != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Valida si los bytes corresponden a un archivo .key válido del SAT
    /// NOTA: Los archivos .key del SAT están en formato PEM, por eso usamos PemReader
    /// </summary>
    public static bool EsClavePrivadaKeyValida(byte[] bytes)
    {
        try
        {
            // 1. Intentar como PEM
            try 
            {
                using (var reader = new StringReader(Encoding.UTF8.GetString(bytes)))
                {
                    var pemReader = new PemReader(reader);
                    var obj = pemReader.ReadObject();
                    if (obj != null) return true;
                }
            }
            catch { /* Ignorar error PEM, intentar DER */ }

            // 2. Intentar como DER (ASN.1)
            var asn1 = Asn1Object.FromByteArray(bytes);
            
            // Verificar si es un EncryptedPrivateKeyInfo (común en SAT)
            // o simplemente si es un objeto ASN.1 válido estructurado
            return asn1 != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Extrae la clave pública de una clave privada para compararla con el certificado
    /// </summary>
    private static AsymmetricKeyParameter ExtractPublicKeyFromPrivateKey(AsymmetricKeyParameter privateKey)
    {
        if (privateKey is RsaPrivateCrtKeyParameters rsaPrivate)
        {
            // Extraer la clave pública de la clave privada RSA
            return new RsaKeyParameters(false, rsaPrivate.Modulus, rsaPrivate.PublicExponent);
        }
        
        // Para otros tipos de claves, intentar obtener la clave pública
        // Nota: AsymmetricCipherKeyPair no es un tipo de AsymmetricKeyParameter,
        // por lo que este método solo maneja claves privadas individuales
        throw new CryptographicException("No se pudo extraer la clave pública de la clave privada. Tipo de clave no soportado o formato no reconocido.");
    }

    /// <summary>
    /// Compara si dos claves públicas coinciden (verifica que la clave privada corresponde al certificado)
    /// </summary>
    private static bool KeysMatch(AsymmetricKeyParameter certPublicKey, AsymmetricKeyParameter privateKeyPublicKey)
    {
        try
        {
            // Comparar claves RSA
            if (certPublicKey is RsaKeyParameters certRsa && privateKeyPublicKey is RsaKeyParameters privateRsa)
            {
                return certRsa.Modulus.Equals(privateRsa.Modulus) && 
                       certRsa.Exponent.Equals(privateRsa.Exponent);
            }
            
            // Para otros tipos de claves, comparar directamente
            return certPublicKey.Equals(privateKeyPublicKey);
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Implementación simple de IPasswordFinder para proporcionar la contraseña al PemReader
/// cuando el archivo .key está encriptado
/// </summary>
internal class SimplePasswordFinder : IPasswordFinder
{
    private readonly string _password;

    public SimplePasswordFinder(string password)
    {
        _password = password ?? string.Empty;
    }

    public char[] GetPassword()
    {
        return _password.ToCharArray();
    }
}
