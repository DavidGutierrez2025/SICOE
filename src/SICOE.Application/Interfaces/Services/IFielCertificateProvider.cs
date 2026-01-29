using System.Security.Cryptography.X509Certificates;
using SICOE.Application.Common;

namespace SICOE.Application.Interfaces.Services;

/// <summary>
/// Provee certificados FIEL (e.firma) desde diferentes fuentes (PFX, Almacén de Windows)
/// </summary>
public interface IFielCertificateProvider
{
    /// <summary>
    /// Obtiene un certificado X509Certificate2 a partir de un arreglo de bytes PFX y su contraseña
    /// </summary>
    Result<X509Certificate2> GetFromPfx(byte[] pfxData, string password);

    /// <summary>
    /// Obtiene el certificado configurado por defecto en el Almacén de Windows (usando el Thumbprint de la configuración)
    /// </summary>
    Result<X509Certificate2> GetDefaultCertificate();

    /// <summary>
    /// Obtiene un certificado del Almacén de Windows usando su huella digital (Thumbprint)
    /// </summary>
    Result<X509Certificate2> GetFromStore(string thumbprint);
}
