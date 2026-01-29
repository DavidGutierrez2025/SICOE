using SICOE.Application.Common;

namespace SICOE.Application.Interfaces.Services;

/// <summary>
/// Interface específica para servicio FIEL (Firma Electrónica del SAT)
/// Maneja autenticación con certificado FIEL (.pfx) para obtener tokens del SAT
/// </summary>
public interface IFielService
{
    /// <summary>
    /// Valida un certificado FIEL (.pfx) sin obtener token
    /// </summary>
    Task<Result<bool>> ValidarFielAsync(
        byte[] pfxFile,
        string password,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene un token del SAT usando el certificado FIEL
    /// El token se usa para autenticar solicitudes a los Web Services del SAT
    /// </summary>
    Task<Result<string>> ObtenerTokenSatAsync(
        byte[] pfxFile,
        string password,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Valida si un certificado FIEL está revocado o caducado
    /// </summary>
    Task<Result<bool>> ValidarFielRevocadaOCaducadaAsync(
        byte[] pfxFile,
        string password,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Extrae el RFC del certificado FIEL (.pfx)
    /// </summary>
    Task<Result<string>> ExtraerRfcDelCertificadoAsync(
        byte[] pfxFile,
        string password,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Extrae el RFC del certificado .cer (solo certificado público, no requiere clave privada)
    /// </summary>
    Task<Result<string>> ExtraerRfcDelCertificadoCerAsync(
        byte[] certificadoCer,
        CancellationToken cancellationToken = default);
}

