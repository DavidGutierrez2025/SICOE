using System.Net.Http.Headers;
using System.Text;
using System.Xml;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography.X509Certificates;

namespace SICOE.Infrastructure.Services.Sat.Soap;

/// <summary>
/// Cliente HTTP para enviar mensajes SOAP al SAT
/// Usa HttpClient para mayor control sobre la construcción y envío de mensajes SOAP con firma digital
/// </summary>
public class SatSoapHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SatSoapHttpClient> _logger;
    private readonly IConfiguration _configuration;

    public SatSoapHttpClient(
        HttpClient httpClient,
        ILogger<SatSoapHttpClient> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Envía un mensaje SOAP al SAT y retorna la respuesta XML
    /// </summary>
    public async Task<string> SendSoapRequestAsync(
        string endpoint,
        string soapAction,
        string soapBody,
        string? token = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            
            // Headers
            request.Headers.Add("SOAPAction", $"\"{soapAction}\"");
            if (!string.IsNullOrWhiteSpace(token))
            {
                // IMPORTANTE: El token debe incluir &wrap_subject=... como parte del token completo
                // NO decodificar URL porque el &wrap_subject debe mantenerse como está
                // El formato del SAT es: JWT&wrap_subject=valor
                // Solo hacer Trim para eliminar espacios al inicio/final
                var tokenLimpio = token.Trim();
                
                // Formato requerido por SAT: "WRAP access_token="Token""
                // El token completo incluye &wrap_subject=... según documentación SAT
                var authHeader = $"WRAP access_token=\"{tokenLimpio}\"";
                request.Headers.Add("Authorization", authHeader);
                
                // Log del token (solo primeros y últimos caracteres por seguridad)
                var tokenPreview = tokenLimpio.Length > 20 
                    ? $"{tokenLimpio.Substring(0, 10)}...{tokenLimpio.Substring(tokenLimpio.Length - 10)}" 
                    : "***";
                _logger.LogInformation("Token SAT enviado en Authorization header. Longitud: {Length}, Preview: {Preview}", 
                    tokenLimpio.Length, tokenPreview);
                _logger.LogDebug("Authorization header completo: {AuthHeader}", authHeader);
            }
            else
            {
                _logger.LogWarning("No se proporcionó token SAT para la solicitud a {Endpoint}", endpoint);
            }
            
            request.Content = new StringContent(soapBody, Encoding.UTF8, "text/xml");
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("text/xml")
            {
                CharSet = "UTF-8"
            };

            _logger.LogDebug("Enviando solicitud SOAP a {Endpoint}, SOAPAction: {SoapAction}", endpoint, soapAction);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            
            _logger.LogDebug("Respuesta recibida del SAT. Status: {StatusCode}, Length: {Length}", 
                response.StatusCode, responseContent.Length);
            
            // CRÍTICO: Si hay error, loguear la respuesta completa para diagnóstico
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("=== RESPUESTA DE ERROR DEL SAT ===");
                _logger.LogError("Status Code: {StatusCode}", response.StatusCode);
                _logger.LogError("Response Content:\n{ResponseContent}", responseContent);
                _logger.LogError("Request SOAP enviado:\n{SoapBody}", soapBody);
            }
            
            response.EnsureSuccessStatusCode();

            return responseContent;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error HTTP al enviar solicitud SOAP a {Endpoint}", endpoint);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al enviar solicitud SOAP");
            throw;
        }
    }

    /// <summary>
    /// Descarga un archivo binario (ZIP) del SAT
    /// </summary>
    public async Task<byte[]> DownloadBinaryAsync(
        string endpoint,
        string? token = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            
            if (!string.IsNullOrWhiteSpace(token))
            {
                // Formato requerido por SAT: "WRAP access_token=Token" (sin comillas alrededor del token)
                request.Headers.Add("Authorization", $"WRAP access_token=\"{token}\"");
            }

            _logger.LogDebug("Descargando archivo binario de {Endpoint}", endpoint);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            
            _logger.LogDebug("Archivo descargado. Tamaño: {Size} bytes", content.Length);

            return content;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error HTTP al descargar archivo de {Endpoint}", endpoint);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al descargar archivo");
            throw;
        }
    }
}

