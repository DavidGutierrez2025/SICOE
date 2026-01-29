using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SICOE.Application.Common;
using SICOE.Application.Interfaces.Services;
using SICOE.Infrastructure.Services.Sat;
using SICOE.Infrastructure.Services.Sat.Soap;

namespace SICOE.Infrastructure.Services.Fiel;

/// <summary>
/// Implementación del servicio FIEL para autenticación con SAT
/// NOTA: El SAT solicita .cer y .key por separado, pero este servicio recibe un .pfx
/// porque System.Security.Cryptography requiere este formato.
/// El controller combina .cer y .key en .pfx temporalmente antes de llamar a este servicio.
/// 
/// IMPORTANTE: Este servicio NUNCA almacena la FIEL.
/// La FIEL se recibe como parámetro, se usa para obtener el token SAT,
/// y se descarta inmediatamente. El usuario debe capturar su FIEL en cada sesión.
/// </summary>
public class FielService : IFielService
{
    private readonly ILogger<FielService> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IConfiguration _configuration;
    private readonly FielValidationService _validationService;
    private readonly SatAutenticacionSoapBuilder _authSoapBuilder;
    private readonly SatSoapHttpClient _soapHttpClient;
    private readonly IFielCertificateProvider _certificateProvider;

    public FielService(
        ILogger<FielService> logger,
        ILoggerFactory loggerFactory,
        IConfiguration configuration,
        FielValidationService validationService,
        SatAutenticacionSoapBuilder authSoapBuilder,
        SatSoapHttpClient soapHttpClient,
        IFielCertificateProvider certificateProvider)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        _configuration = configuration;
        _validationService = validationService;
        _authSoapBuilder = authSoapBuilder;
        _soapHttpClient = soapHttpClient;
        _certificateProvider = certificateProvider;
    }

    public async Task<Result<bool>> ValidarFielAsync(
        byte[] pfxFile,
        string password,
        CancellationToken cancellationToken = default)
    {
        X509Certificate2? certificate = null;
        try
        {
            if (pfxFile == null || pfxFile.Length == 0)
                return Result<bool>.Failure($"{FielValidationService.ErrorCodes.CERT_INVALID_FORMAT}: El archivo FIEL no puede estar vacío");

            if (string.IsNullOrWhiteSpace(password))
                return Result<bool>.Failure($"{FielValidationService.ErrorCodes.WRONG_PASSWORD}: La contraseña del certificado FIEL es requerida");

            // Cargar certificado desde bytes
            // IMPORTANTE: Usar UserKeySet en lugar de MachineKeySet para evitar problemas de permisos
            certificate = new X509Certificate2(pfxFile, password, X509KeyStorageFlags.UserKeySet | X509KeyStorageFlags.Exportable);

            // Validar que el certificado sea válido
            if (!certificate.HasPrivateKey)
            {
                _logger.LogWarning("El certificado FIEL no contiene una llave privada");
                return Result<bool>.Failure($"{FielValidationService.ErrorCodes.CERT_NO_PRIVATE_KEY}: {FielValidationService.ErrorMessages.GetMessage(FielValidationService.ErrorCodes.CERT_NO_PRIVATE_KEY)}");
            }

            // Validar fecha de vigencia
            var ahora = DateTime.UtcNow;
            if (ahora > certificate.NotAfter)
            {
                _logger.LogWarning("El certificado FIEL está expirado. NotAfter: {NotAfter}", certificate.NotAfter);
                return Result<bool>.Failure($"{FielValidationService.ErrorCodes.CERT_EXPIRED}: {FielValidationService.ErrorMessages.GetMessage(FielValidationService.ErrorCodes.CERT_EXPIRED)}. Válido hasta {certificate.NotAfter:yyyy-MM-dd}");
            }

            if (ahora < certificate.NotBefore)
            {
                _logger.LogWarning("El certificado FIEL aún no es válido. NotBefore: {NotBefore}", certificate.NotBefore);
                return Result<bool>.Failure($"{FielValidationService.ErrorCodes.CERT_NOT_YET_VALID}: {FielValidationService.ErrorMessages.GetMessage(FielValidationService.ErrorCodes.CERT_NOT_YET_VALID)}. Válido desde {certificate.NotBefore:yyyy-MM-dd}");
            }

            // Validar que el certificado tenga el uso correcto (firma digital)
            var keyUsage = certificate.Extensions.OfType<X509KeyUsageExtension>().FirstOrDefault();
            if (keyUsage != null && !keyUsage.KeyUsages.HasFlag(X509KeyUsageFlags.DigitalSignature))
            {
                _logger.LogWarning("El certificado FIEL no tiene el uso de firma digital habilitado");
                return Result<bool>.Failure($"{FielValidationService.ErrorCodes.INVALID_SIGNATURE}: El certificado FIEL no tiene el uso de firma digital habilitado");
            }

            // Validar cadena de certificación
            var cadenaResult = _validationService.ValidarCadenaCertificacion(certificate);
            if (cadenaResult.IsFailure)
            {
                _logger.LogWarning("La cadena de certificación no es válida: {Error}", cadenaResult.Error);
                return Result<bool>.Failure($"{FielValidationService.ErrorCodes.CERT_CHAIN_INVALID}: {cadenaResult.Error}");
            }

            // Validar desafío criptográfico (firmar y verificar)
            var desafioResult = _validationService.ValidarDesafioCriptografico(certificate, password);
            if (desafioResult.IsFailure)
            {
                _logger.LogWarning("El desafío criptográfico falló: {Error}", desafioResult.Error);
                return Result<bool>.Failure($"{FielValidationService.ErrorCodes.INVALID_SIGNATURE}: {desafioResult.Error}");
            }

            // Intentar validar revocación (no bloqueante si falla)
            var crlResult = await _validationService.ValidarRevocacionCRLAsync(certificate, cancellationToken);
            if (crlResult.IsFailure)
            {
                _logger.LogWarning("No se pudo validar revocación CRL: {Error}. Continuando con validación básica.", crlResult.Error);
                // No fallamos si no hay conexión a CRL, solo registramos la advertencia
            }

            _logger.LogInformation("Certificado FIEL validado correctamente. RFC: {SubjectName}, Válido hasta: {NotAfter}", 
                certificate.SubjectName.Name, certificate.NotAfter);

            return Result<bool>.Success(true);
        }
        catch (CryptographicException ex)
        {
            _logger.LogError(ex, "Error criptográfico al validar certificado FIEL. Posible contraseña incorrecta");
            return Result<bool>.Failure($"{FielValidationService.ErrorCodes.WRONG_PASSWORD}: {FielValidationService.ErrorMessages.GetMessage(FielValidationService.ErrorCodes.WRONG_PASSWORD)}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al validar certificado FIEL");
            return Result<bool>.Failure($"Error al validar el certificado FIEL: {ex.Message}");
        }
        finally
        {
            certificate?.Dispose();
        }
    }

    public async Task<Result<string>> ObtenerTokenSatAsync(
        byte[] pfxFile,
        string password,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Primero validar el certificado
            var validacion = await ValidarFielAsync(pfxFile, password, cancellationToken);
            if (validacion.IsFailure)
            {
                return Result<string>.Failure(validacion.Error);
            }

            // Cargar certificado usando el nuevo proveedor
            var certResult = _certificateProvider.GetFromPfx(pfxFile, password);
            if (certResult.IsFailure)
            {
                return Result<string>.Failure(certResult.Error);
            }
            
            var certificate = certResult.Value;

            // Extraer RFC del certificado
            var rfcResult = await ExtraerRfcDelCertificadoAsync(pfxFile, password, cancellationToken);
            if (rfcResult.IsFailure)
            {
                return Result<string>.Failure(rfcResult.Error);
            }

            var rfc = rfcResult.Value;
            _logger.LogInformation("Iniciando obtención de token SAT para RFC: {Rfc}", rfc);

            // Intentar primero con WCF (implementación legacy)
            try
            {
                _logger.LogInformation("Intentando autenticación con WCF para RFC: {Rfc}", rfc);
                
                var clientFactory = new SatSoapClientFactory(
                    _configuration,
                    _loggerFactory.CreateLogger<SatSoapClientFactory>());
                
                var authClient = clientFactory.CreateAutenticacionClient(certificate);
                var token = authClient.Autentica();

                if (!string.IsNullOrWhiteSpace(token))
                {
                    // CRÍTICO: Limpiar el token - eliminar saltos de línea y tabulaciones, pero MANTENER &wrap_subject
                    // IMPORTANTE: El token del SAT DEBE incluir &wrap_subject=... como parte del token completo
                    // El formato correcto según documentación SAT es: JWT&wrap_subject=valor
                    // NO eliminar el &wrap_subject, solo limpiar caracteres de control
                    token = token.Trim()
                        .Replace("\r", string.Empty)
                        .Replace("\n", string.Empty)
                        .Replace("\t", string.Empty);
                    // NO eliminar espacios - el token puede tener espacios válidos antes de &wrap_subject

                    if (string.IsNullOrWhiteSpace(token))
                    {
                        _logger.LogError("El token de WCF quedó vacío después de limpiarlo. Token original tenía caracteres inválidos.");
                        // Continuar al fallback SOAP Manual
                    }
                    else
                    {
                        _logger.LogInformation("Token SAT obtenido exitosamente con WCF para RFC: {Rfc}, Longitud: {Length}", rfc, token.Length);
                        _logger.LogDebug("Token SAT preview (primeros 50 chars): {Preview}", token.Length > 50 ? token.Substring(0, 50) : token);
                        if (token.Contains("&wrap_subject"))
                        {
                            _logger.LogDebug("Token WCF incluye wrap_subject como se espera");
                        }
                        return Result<string>.Success(token);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("La autenticación con WCF falló: {Message}. Reintentando con SOAP Manual.", ex.Message);
            }

            // FALLBACK: Autenticación Manual con HttpClient y firma personalizada
            // Esto es más robusto ante problemas de cadena de confianza en WCF
            try
            {
                _logger.LogInformation("Iniciando autenticación manual con SOAP para RFC: {Rfc}", rfc);
                
                var authBody = _authSoapBuilder.BuildAutenticacionSoap(certificate);
                var endpoint = _configuration["Sat:AutenticacionUrl"] 
                    ?? "https://cfdidescargamasivasolicitud.clouda.sat.gob.mx/Autenticacion/Autenticacion.svc";
                
                var responseXml = await _soapHttpClient.SendSoapRequestAsync(
                    endpoint,
                    "http://DescargaMasivaTerceros.gob.mx/IAutenticacion/Autentica",
                    authBody,
                    token: null, // No hay token aún
                    cancellationToken);

                // Parsea la respuesta manual del token
                var doc = new System.Xml.XmlDocument();
                doc.LoadXml(responseXml);
                
                var nsmgr = new System.Xml.XmlNamespaceManager(doc.NameTable);
                nsmgr.AddNamespace("s", "http://schemas.xmlsoap.org/soap/envelope/");
                nsmgr.AddNamespace("h", "http://DescargaMasivaTerceros.gob.mx");
                
                var tokenNode = doc.SelectSingleNode("//h:AutenticaResponse/h:AutenticaResult", nsmgr);
                var token = tokenNode?.InnerText;

                if (string.IsNullOrWhiteSpace(token))
                {
                    _logger.LogError("El SAT retornó un token vacío en la respuesta manual");
                    return Result<string>.Failure("El SAT no retornó un token válido");
                }

                // CRÍTICO: Limpiar el token - eliminar saltos de línea y tabulaciones, pero MANTENER &wrap_subject
                // IMPORTANTE: El token del SAT DEBE incluir &wrap_subject=... como parte del token completo
                // El formato correcto según documentación SAT es: JWT&wrap_subject=valor
                // NO eliminar el &wrap_subject, solo limpiar caracteres de control
                token = token.Trim()
                    .Replace("\r", string.Empty)
                    .Replace("\n", string.Empty)
                    .Replace("\t", string.Empty);
                // NO eliminar espacios - el token puede tener espacios válidos antes de &wrap_subject

                if (string.IsNullOrWhiteSpace(token))
                {
                    _logger.LogError("El token quedó vacío después de limpiarlo. Token original tenía caracteres inválidos.");
                    return Result<string>.Failure("El token obtenido del SAT contiene solo caracteres inválidos");
                }

                _logger.LogInformation("Token SAT obtenido exitosamente con SOAP Manual para RFC: {Rfc}, Longitud: {Length}", rfc, token.Length);
                _logger.LogDebug("Token SAT preview (primeros 50 chars): {Preview}", token.Length > 50 ? token.Substring(0, 50) : token);
                if (token.Contains("&wrap_subject"))
                {
                    _logger.LogDebug("Token incluye wrap_subject como se espera");
                }
                return Result<string>.Success(token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error crítico: fallaron ambos métodos de autenticación (WCF y Manual)");
                return Result<string>.Failure($"Error al obtener token del SAT: {ex.Message}");
            }
        }
        catch (CryptographicException ex)
        {
            _logger.LogError(ex, "Error criptográfico al obtener token SAT");
            return Result<string>.Failure("Error al procesar el certificado FIEL. Verifique que la contraseña sea correcta");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al obtener token SAT");
            return Result<string>.Failure($"Error al obtener token del SAT: {ex.Message}");
        }
        finally
        {
            // Note: certificate is disposed inside provider or locally if needed
        }
    }

    public async Task<Result<bool>> ValidarFielRevocadaOCaducadaAsync(
        byte[] pfxFile,
        string password,
        CancellationToken cancellationToken = default)
    {
        X509Certificate2? certificate = null;
        try
        {
            if (pfxFile == null || pfxFile.Length == 0)
                return Result<bool>.Failure("El archivo FIEL no puede estar vacío");

            if (string.IsNullOrWhiteSpace(password))
                return Result<bool>.Failure("La contraseña del certificado FIEL es requerida");

            certificate = new X509Certificate2(pfxFile, password, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet);

            var ahora = DateTime.UtcNow;

            // Verificar si está caducado
            if (ahora > certificate.NotAfter)
            {
                _logger.LogWarning("Certificado FIEL caducado. Fecha de expiración: {NotAfter}", certificate.NotAfter);
                return Result<bool>.Success(true); // Está caducado
            }

            // Verificar si aún no es válido
            if (ahora < certificate.NotBefore)
            {
                _logger.LogWarning("Certificado FIEL aún no válido. Fecha de inicio: {NotBefore}", certificate.NotBefore);
                return Result<bool>.Success(true); // No es válido aún
            }

            // Verificar revocación consultando la CRL (Certificate Revocation List) del SAT
            var crlResult = await _validationService.ValidarRevocacionCRLAsync(certificate, cancellationToken);
            if (crlResult.IsFailure)
            {
                _logger.LogWarning("No se pudo validar revocación CRL: {Error}. Intentando OCSP.", crlResult.Error);
                
                // Intentar OCSP como respaldo
                var ocspResult = await _validationService.ValidarRevocacionOCSPAsync(certificate, cancellationToken);
                if (ocspResult.IsFailure)
                {
                    _logger.LogWarning("No se pudo validar revocación OCSP: {Error}. Asumiendo que no está revocado (no se pudo verificar).", ocspResult.Error);
                    // No fallamos si no hay conexión, solo registramos la advertencia
                    return Result<bool>.Success(false); // No está revocado (no se pudo verificar)
                }
            }

            _logger.LogInformation("Certificado FIEL válido. No caducado ni revocado");
            return Result<bool>.Success(false); // No está caducado ni revocado
        }
        catch (CryptographicException ex)
        {
            _logger.LogError(ex, "Error criptográfico al validar revocación/caducidad");
            return Result<bool>.Failure("Error al procesar el certificado FIEL");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al validar revocación/caducidad");
            return Result<bool>.Failure($"Error al validar certificado FIEL: {ex.Message}");
        }
        finally
        {
            certificate?.Dispose();
        }
    }

    public Task<Result<string>> ExtraerRfcDelCertificadoAsync(
        byte[] pfxFile,
        string password,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var certificate = new X509Certificate2(pfxFile, password, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet);

            // El RFC del SAT está en el campo con OID 2.5.4.45 (unstructuredName)
            // Este es el campo correcto según la estructura de certificados del SAT
            // Personas físicas: 13 caracteres (ej: GUCD730207CY3)
            // Personas morales: 12 caracteres (ej: ABC123456XY1)
            // IMPORTANTE: Algunos certificados tienen múltiples RFCs separados por "/": "OID.2.5.4.45=ATD170809GE1 / GUCD730207CY3"
            // En estos casos, se debe usar el último RFC (que es el RFC del solicitante real)
            
            string? rfc = null;

            // Método 1: Buscar directamente en el Subject usando el OID 2.5.4.45
            // El Subject puede tener el formato: "2.5.4.45=GUCD730207CY3, CN=..."
            // O con múltiples RFCs: "OID.2.5.4.45=ATD170809GE1 / GUCD730207CY3"
            var subject = certificate.Subject;
            
            // Buscar específicamente el campo 2.5.4.45 en el Subject (formato más común)
            // Usar Regex.Matches para capturar TODOS los RFCs cuando hay múltiples separados por "/"
            var oidMatches = Regex.Matches(subject, @"(?:OID\.)?2\.5\.4\.45\s*=\s*([^,]+)", RegexOptions.IgnoreCase);
            if (oidMatches.Count > 0)
            {
                // Obtener el valor completo del campo OID 2.5.4.45
                var oidValue = oidMatches[0].Groups[1].Value.Trim();
                
                // Si contiene "/", hay múltiples RFCs - extraer todos y usar el PRIMERO
                // IMPORTANTE: El primer RFC (12 caracteres) es el RFC de la persona moral
                // El segundo RFC (13 caracteres) es el RFC del apoderado legal/representante
                // Para certificados de persona moral, debemos usar el PRIMER RFC (persona moral)
                if (oidValue.Contains("/"))
                {
                    // Extraer todos los RFCs del valor (separados por "/")
                    var rfcMatches = Regex.Matches(oidValue, @"([A-ZÑ&]{3,4}\d{6}[A-Z0-9]{2,3})", RegexOptions.IgnoreCase);
                    if (rfcMatches.Count > 0)
                    {
                        // Usar el PRIMER RFC cuando hay múltiples (RFC de la persona moral)
                        // El primer RFC generalmente tiene 12 caracteres (persona moral)
                        rfc = rfcMatches[0].Groups[1].Value.Trim();
                        _logger.LogInformation("Múltiples RFCs encontrados en certificado. Usando el primer RFC (persona moral): {Rfc}", rfc);
                        if (rfcMatches.Count > 1)
                        {
                            _logger.LogDebug("RFC adicional (apoderado legal/representante): {Rfc}", rfcMatches[1].Groups[1].Value.Trim());
                        }
                    }
                }
                else
                {
                    // Un solo RFC
                    var singleRfcMatch = Regex.Match(oidValue, @"([A-ZÑ&]{3,4}\d{6}[A-Z0-9]{2,3})", RegexOptions.IgnoreCase);
                    if (singleRfcMatch.Success)
                    {
                        rfc = singleRfcMatch.Groups[1].Value.Trim();
                    }
                }
            }

            // Método 2: Si no se encuentra, buscar en el SubjectName usando el formato alternativo
            if (string.IsNullOrEmpty(rfc))
            {
                try
                {
                    // El SubjectName puede tener el formato con el OID explícito
                    var subjectName = certificate.SubjectName;
                    var subjectFormatted = subjectName.Format(false);
                    
                    // Buscar el OID 2.5.4.45 en el SubjectName formateado
                    // Manejar múltiples RFCs separados por "/"
                    var oidMatch2 = Regex.Match(subjectFormatted, @"(?:OID\.)?2\.5\.4\.45\s*=\s*([^,]+)", RegexOptions.IgnoreCase);
                    if (oidMatch2.Success && string.IsNullOrEmpty(rfc))
                    {
                        var oidValue2 = oidMatch2.Groups[1].Value.Trim();
                        
                        // Si contiene "/", hay múltiples RFCs - usar el PRIMERO (persona moral)
                        if (oidValue2.Contains("/"))
                        {
                            var rfcMatches2 = Regex.Matches(oidValue2, @"([A-ZÑ&]{3,4}\d{6}[A-Z0-9]{2,3})", RegexOptions.IgnoreCase);
                            if (rfcMatches2.Count > 0)
                            {
                                // Usar el PRIMER RFC (persona moral), no el último
                                rfc = rfcMatches2[0].Groups[1].Value.Trim();
                            }
                        }
                        else
                        {
                            var singleRfcMatch2 = Regex.Match(oidValue2, @"([A-ZÑ&]{3,4}\d{6}[A-Z0-9]{2,3})", RegexOptions.IgnoreCase);
                            if (singleRfcMatch2.Success)
                            {
                                rfc = singleRfcMatch2.Groups[1].Value.Trim();
                            }
                        }
                    }
                    
                    if (string.IsNullOrEmpty(rfc))
                    {
                        // Buscar en cada parte del SubjectName
                        var subjectParts = subjectFormatted.Split(',');
                        foreach (var part in subjectParts)
                        {
                            var trimmedPart = part.Trim();
                            if (trimmedPart.StartsWith("2.5.4.45", StringComparison.OrdinalIgnoreCase) ||
                                trimmedPart.StartsWith("OID.2.5.4.45", StringComparison.OrdinalIgnoreCase) || 
                                trimmedPart.Contains("2.5.4.45", StringComparison.OrdinalIgnoreCase))
                            {
                                // Manejar múltiples RFCs en la parte
                                var rfcMatches3 = Regex.Matches(trimmedPart, @"([A-ZÑ&]{3,4}\d{6}[A-Z0-9]{2,3})");
                                if (rfcMatches3.Count > 0)
                                {
                                    // Usar el PRIMER RFC cuando hay múltiples (persona moral)
                                    rfc = rfcMatches3[0].Groups[1].Value;
                                    break;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error al buscar RFC en SubjectName, continuando con otros métodos");
                }
            }

            // Método 3: Buscar en las extensiones del certificado usando el OID 2.5.4.45
            if (string.IsNullOrEmpty(rfc))
            {
                foreach (var extension in certificate.Extensions)
                {
                    if (extension.Oid?.Value == "2.5.4.45")
                    {
                        // El valor puede estar en formato ASN.1, necesitamos parsearlo
                        var rawData = extension.Format(false);
                        // Intentar extraer el RFC del formato
                        var rfcMatch = Regex.Match(rawData, @"([A-ZÑ&]{3,4}\d{6}[A-Z0-9]{2,3})");
                        if (rfcMatch.Success)
                        {
                            rfc = rfcMatch.Groups[1].Value;
                            break;
                        }
                    }
                }
            }

            // Validar que el RFC tenga el formato correcto (12 o 13 caracteres)
            if (!string.IsNullOrEmpty(rfc))
            {
                // Personas físicas: 13 caracteres, Personas morales: 12 caracteres
                if (rfc.Length == 12 || rfc.Length == 13)
                {
                    // Validar formato: 3-4 letras, 6 dígitos, 2-3 caracteres alfanuméricos
                    var rfcPattern = @"^[A-ZÑ&]{3,4}\d{6}[A-Z0-9]{2,3}$";
                    if (Regex.IsMatch(rfc, rfcPattern))
                    {
                        _logger.LogInformation("RFC extraído del certificado (OID 2.5.4.45): {Rfc}", rfc);
                        return Task.FromResult(Result<string>.Success(rfc));
                    }
                }
            }

            // Si no se encontró en el OID, intentar búsqueda alternativa en el Subject
            var fallbackMatch = Regex.Match(subject, @"([A-ZÑ&]{3,4}\d{6}[A-Z0-9]{2,3})");
            if (fallbackMatch.Success)
            {
                var fallbackRfc = fallbackMatch.Groups[1].Value;
                if (fallbackRfc.Length == 12 || fallbackRfc.Length == 13)
                {
                    _logger.LogWarning("RFC extraído del certificado usando búsqueda alternativa (no se encontró en OID 2.5.4.45): {Rfc}", fallbackRfc);
                    return Task.FromResult(Result<string>.Success(fallbackRfc));
                }
            }

            _logger.LogWarning("No se pudo extraer el RFC del certificado. Subject: {Subject}", subject);
            return Task.FromResult(Result<string>.Failure("No se pudo extraer el RFC del certificado FIEL. Verifique que el certificado sea válido y contenga el campo 2.5.4.45"));
        }
        catch (CryptographicException ex)
        {
            _logger.LogError(ex, "Error criptográfico al extraer RFC del certificado");
            return Task.FromResult(Result<string>.Failure("Error al procesar el certificado FIEL"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al extraer RFC del certificado");
            return Task.FromResult(Result<string>.Failure($"Error al extraer RFC del certificado: {ex.Message}"));
        }
    }

    public Task<Result<string>> ExtraerRfcDelCertificadoCerAsync(
        byte[] certificadoCer,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Cargar certificado público (.cer) - no requiere password ni clave privada
            var certificate = new X509Certificate2(certificadoCer);

            // El RFC del SAT está en el campo con OID 2.5.4.45 (unstructuredName)
            // Este es el campo correcto según la estructura de certificados del SAT
            // Personas físicas: 13 caracteres (ej: GUCD730207CY3)
            // Personas morales: 12 caracteres (ej: ABC123456XY1)
            
            string? rfc = null;

            // Método 1: Buscar directamente en el Subject usando el OID 2.5.4.45
            // El Subject puede tener el formato: "2.5.4.45=GUCD730207CY3, CN=..."
            // O con múltiples RFCs: "OID.2.5.4.45=ATD170809GE1 / GUCD730207CY3"
            var subject = certificate.Subject;
            
            // Buscar específicamente el campo 2.5.4.45 en el Subject (formato más común)
            // Usar Regex.Matches para capturar TODOS los RFCs cuando hay múltiples separados por "/"
            var oidMatches = Regex.Matches(subject, @"(?:OID\.)?2\.5\.4\.45\s*=\s*([^,]+)", RegexOptions.IgnoreCase);
            if (oidMatches.Count > 0)
            {
                // Obtener el valor completo del campo OID 2.5.4.45
                var oidValue = oidMatches[0].Groups[1].Value.Trim();
                
                // Si contiene "/", hay múltiples RFCs - extraer todos y usar el PRIMERO
                // IMPORTANTE: El primer RFC (12 caracteres) es el RFC de la persona moral
                // El segundo RFC (13 caracteres) es el RFC del apoderado legal/representante
                // Para certificados de persona moral, debemos usar el PRIMER RFC (persona moral)
                if (oidValue.Contains("/"))
                {
                    // Extraer todos los RFCs del valor (separados por "/")
                    var rfcMatches = Regex.Matches(oidValue, @"([A-ZÑ&]{3,4}\d{6}[A-Z0-9]{2,3})", RegexOptions.IgnoreCase);
                    if (rfcMatches.Count > 0)
                    {
                        // Usar el PRIMER RFC cuando hay múltiples (RFC de la persona moral)
                        // El primer RFC generalmente tiene 12 caracteres (persona moral)
                        rfc = rfcMatches[0].Groups[1].Value.Trim();
                        _logger.LogInformation("Múltiples RFCs encontrados en certificado .cer. Usando el primer RFC (persona moral): {Rfc}", rfc);
                        if (rfcMatches.Count > 1)
                        {
                            _logger.LogDebug("RFC adicional (apoderado legal/representante): {Rfc}", rfcMatches[1].Groups[1].Value.Trim());
                        }
                    }
                }
                else
                {
                    // Un solo RFC
                    var singleRfcMatch = Regex.Match(oidValue, @"([A-ZÑ&]{3,4}\d{6}[A-Z0-9]{2,3})", RegexOptions.IgnoreCase);
                    if (singleRfcMatch.Success)
                    {
                        rfc = singleRfcMatch.Groups[1].Value.Trim();
                    }
                }
            }

            // Método 2: Si no se encuentra, buscar en el SubjectName usando el formato alternativo
            if (string.IsNullOrEmpty(rfc))
            {
                try
                {
                    // El SubjectName puede tener el formato con el OID explícito
                    var subjectName = certificate.SubjectName;
                    var subjectFormatted = subjectName.Format(false);
                    
                    // Buscar el OID 2.5.4.45 en el SubjectName formateado
                    // Manejar múltiples RFCs separados por "/"
                    var oidMatch2 = Regex.Match(subjectFormatted, @"(?:OID\.)?2\.5\.4\.45\s*=\s*([^,]+)", RegexOptions.IgnoreCase);
                    if (oidMatch2.Success && string.IsNullOrEmpty(rfc))
                    {
                        var oidValue2 = oidMatch2.Groups[1].Value.Trim();
                        
                        // Si contiene "/", hay múltiples RFCs - usar el PRIMERO (persona moral)
                        if (oidValue2.Contains("/"))
                        {
                            var rfcMatches2 = Regex.Matches(oidValue2, @"([A-ZÑ&]{3,4}\d{6}[A-Z0-9]{2,3})", RegexOptions.IgnoreCase);
                            if (rfcMatches2.Count > 0)
                            {
                                // Usar el PRIMER RFC (persona moral), no el último
                                rfc = rfcMatches2[0].Groups[1].Value.Trim();
                            }
                        }
                        else
                        {
                            var singleRfcMatch2 = Regex.Match(oidValue2, @"([A-ZÑ&]{3,4}\d{6}[A-Z0-9]{2,3})", RegexOptions.IgnoreCase);
                            if (singleRfcMatch2.Success)
                            {
                                rfc = singleRfcMatch2.Groups[1].Value.Trim();
                            }
                        }
                    }
                    
                    if (string.IsNullOrEmpty(rfc))
                    {
                        // Buscar en cada parte del SubjectName
                        var subjectParts = subjectFormatted.Split(',');
                        foreach (var part in subjectParts)
                        {
                            var trimmedPart = part.Trim();
                            if (trimmedPart.StartsWith("2.5.4.45", StringComparison.OrdinalIgnoreCase) ||
                                trimmedPart.StartsWith("OID.2.5.4.45", StringComparison.OrdinalIgnoreCase) || 
                                trimmedPart.Contains("2.5.4.45", StringComparison.OrdinalIgnoreCase))
                            {
                                // Manejar múltiples RFCs en la parte
                                var rfcMatches3 = Regex.Matches(trimmedPart, @"([A-ZÑ&]{3,4}\d{6}[A-Z0-9]{2,3})");
                                if (rfcMatches3.Count > 0)
                                {
                                    // Usar el PRIMER RFC cuando hay múltiples (persona moral)
                                    rfc = rfcMatches3[0].Groups[1].Value;
                                    break;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error al buscar RFC en SubjectName, continuando con otros métodos");
                }
            }

            // Método 3: Buscar en las extensiones del certificado usando el OID 2.5.4.45
            if (string.IsNullOrEmpty(rfc))
            {
                foreach (var extension in certificate.Extensions)
                {
                    if (extension.Oid?.Value == "2.5.4.45")
                    {
                        // El valor puede estar en formato ASN.1, necesitamos parsearlo
                        var rawData = extension.Format(false);
                        // Intentar extraer el RFC del formato
                        var rfcMatch = Regex.Match(rawData, @"([A-ZÑ&]{3,4}\d{6}[A-Z0-9]{2,3})");
                        if (rfcMatch.Success)
                        {
                            rfc = rfcMatch.Groups[1].Value;
                            break;
                        }
                    }
                }
            }

            // Validar que el RFC tenga el formato correcto (12 o 13 caracteres)
            if (!string.IsNullOrEmpty(rfc))
            {
                // Personas físicas: 13 caracteres, Personas morales: 12 caracteres
                if (rfc.Length == 12 || rfc.Length == 13)
                {
                    // Validar formato: 3-4 letras, 6 dígitos, 2-3 caracteres alfanuméricos
                    var rfcPattern = @"^[A-ZÑ&]{3,4}\d{6}[A-Z0-9]{2,3}$";
                    if (System.Text.RegularExpressions.Regex.IsMatch(rfc, rfcPattern))
                    {
                        _logger.LogInformation("RFC extraído del certificado .cer (OID 2.5.4.45): {Rfc}", rfc);
                        return Task.FromResult(Result<string>.Success(rfc));
                    }
                }
            }

            // Si no se encontró en el OID, intentar búsqueda alternativa en el Subject
            // Usar la variable 'subject' ya declarada anteriormente
            var fallbackMatch = Regex.Match(subject, @"([A-ZÑ&]{3,4}\d{6}[A-Z0-9]{2,3})");
            if (fallbackMatch.Success)
            {
                var fallbackRfc = fallbackMatch.Groups[1].Value;
                if (fallbackRfc.Length == 12 || fallbackRfc.Length == 13)
                {
                    _logger.LogWarning("RFC extraído del certificado .cer usando búsqueda alternativa (no se encontró en OID 2.5.4.45): {Rfc}", fallbackRfc);
                    return Task.FromResult(Result<string>.Success(fallbackRfc));
                }
            }

            _logger.LogWarning("No se pudo extraer el RFC del certificado .cer. Subject: {Subject}", subject);
            return Task.FromResult(Result<string>.Failure("No se pudo extraer el RFC del certificado .cer. Verifique que el certificado sea válido y contenga el campo 2.5.4.45"));
        }
        catch (CryptographicException ex)
        {
            _logger.LogError(ex, "Error criptográfico al extraer RFC del certificado .cer");
            return Task.FromResult(Result<string>.Failure("Error al procesar el certificado .cer"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al extraer RFC del certificado .cer");
            return Task.FromResult(Result<string>.Failure($"Error al extraer RFC del certificado .cer: {ex.Message}"));
        }
    }

}

