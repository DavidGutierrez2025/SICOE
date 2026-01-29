using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography.X509Certificates;

namespace SICOE.Infrastructure.Services.Sat.Soap;

/// <summary>
/// Factory para crear clientes SOAP del SAT
/// </summary>
public class SatSoapClientFactory
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SatSoapClientFactory> _logger;
    private readonly string _autenticacionUrl;
    private readonly string _solicitudUrl;
    private readonly string _verificacionUrl;
    private readonly string _descargaUrl;

    public SatSoapClientFactory(IConfiguration configuration, ILogger<SatSoapClientFactory> logger)
    {
        _configuration = configuration;
        _logger = logger;

        var baseUrl = _configuration["Sat:BaseUrl"] 
            ?? "https://cfdidescargamasivasolicitud.clouda.sat.gob.mx";

        _autenticacionUrl = $"{baseUrl}/Autenticacion/Autenticacion.svc";
        _solicitudUrl = $"{baseUrl}/SolicitaDescargaService.svc";
        _verificacionUrl = $"{baseUrl}/VerificaSolicitudDescargaService.svc";
        
        // URL de descarga puede ser diferente
        var descargaBaseUrl = _configuration["Sat:DescargaBaseUrl"] 
            ?? "https://cfdidescargamasiva.clouda.sat.gob.mx";
        _descargaUrl = $"{descargaBaseUrl}/DescargaMasivaService.svc";
    }

    /// <summary>
    /// Crea un cliente SOAP para autenticación con certificado FIEL
    /// El SAT requiere WS-Security v1.0 con certificado en el header
    /// </summary>
    public IAutenticacionService CreateAutenticacionClient(X509Certificate2 certificate)
    {
        try
        {
            // BasicHttpBinding con TransportWithMessageCredential para WS-Security
            var binding = CreateBasicHttpBindingForAuth();
            var endpoint = new EndpointAddress(new Uri(_autenticacionUrl));

            var factory = new ChannelFactory<IAutenticacionService>(binding, endpoint);
            
            // Configurar certificado para WS-Security (esto agrega el certificado al header WS-Security)
            factory.Credentials.ClientCertificate.Certificate = certificate;
            
            // Configurar validación del certificado del servidor
            factory.Credentials.ServiceCertificate.SslCertificateAuthentication = 
                new X509ServiceCertificateAuthentication
                {
                    CertificateValidationMode = X509CertificateValidationMode.None // En producción, usar ChainTrust
                };

            var client = factory.CreateChannel();
            return client;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear cliente SOAP de autenticación");
            throw;
        }
    }

    /// <summary>
    /// Crea un cliente SOAP para solicitud de descarga con token de autorización
    /// </summary>
    public ISolicitaDescargaService CreateSolicitudClient(string token)
    {
        try
        {
            var binding = CreateBasicHttpBinding();
            var endpoint = new EndpointAddress(_solicitudUrl);

            var factory = new ChannelFactory<ISolicitaDescargaService>(binding, endpoint);
            
            // Agregar token en header Authorization
            factory.Endpoint.EndpointBehaviors.Add(new AuthorizationHeaderBehavior(token));

            var client = factory.CreateChannel();
            return client;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear cliente SOAP de solicitud");
            throw;
        }
    }

    /// <summary>
    /// Crea un cliente SOAP para verificación con token de autorización
    /// </summary>
    public IVerificaSolicitudDescargaService CreateVerificacionClient(string token)
    {
        try
        {
            var binding = CreateBasicHttpBinding();
            var endpoint = new EndpointAddress(_verificacionUrl);

            var factory = new ChannelFactory<IVerificaSolicitudDescargaService>(binding, endpoint);
            
            // Agregar token en header Authorization
            factory.Endpoint.EndpointBehaviors.Add(new AuthorizationHeaderBehavior(token));

            var client = factory.CreateChannel();
            return client;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear cliente SOAP de verificación");
            throw;
        }
    }

    /// <summary>
    /// Crea un cliente SOAP para descarga de paquetes con token de autorización
    /// </summary>
    public IDescargaMasivaService CreateDescargaClient(string token)
    {
        try
        {
            var binding = CreateBasicHttpBinding();
            var endpoint = new EndpointAddress(_descargaUrl);

            var factory = new ChannelFactory<IDescargaMasivaService>(binding, endpoint);
            
            // Agregar token en header Authorization
            factory.Endpoint.EndpointBehaviors.Add(new AuthorizationHeaderBehavior(token));

            var client = factory.CreateChannel();
            return client;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear cliente SOAP de descarga");
            throw;
        }
    }

    /// <summary>
    /// Crea un BasicHttpBinding configurado para los servicios del SAT
    /// </summary>
    private BasicHttpBinding CreateBasicHttpBinding()
    {
        var binding = new BasicHttpBinding(BasicHttpSecurityMode.Transport)
        {
            MaxReceivedMessageSize = 2147483647, // 2GB
            MaxBufferSize = 2147483647,
            ReaderQuotas = new System.Xml.XmlDictionaryReaderQuotas
            {
                MaxStringContentLength = 2147483647,
                MaxArrayLength = 2147483647,
                MaxBytesPerRead = 2147483647,
                MaxDepth = 2147483647,
                MaxNameTableCharCount = 2147483647
            },
            SendTimeout = TimeSpan.FromSeconds(300), // 5 minutos
            ReceiveTimeout = TimeSpan.FromSeconds(300),
            OpenTimeout = TimeSpan.FromSeconds(30),
            CloseTimeout = TimeSpan.FromSeconds(30)
        };

        return binding;
    }

    /// <summary>
    /// Crea un BasicHttpBinding configurado para autenticación con WS-Security
    /// El SAT requiere TransportWithMessageCredential para WS-Security v1.0
    /// </summary>
    private BasicHttpBinding CreateBasicHttpBindingForAuth()
    {
        var binding = new BasicHttpBinding(BasicHttpSecurityMode.TransportWithMessageCredential)
        {
            MaxReceivedMessageSize = 2147483647, // 2GB
            MaxBufferSize = 2147483647,
            ReaderQuotas = new System.Xml.XmlDictionaryReaderQuotas
            {
                MaxStringContentLength = 2147483647,
                MaxArrayLength = 2147483647,
                MaxBytesPerRead = 2147483647,
                MaxDepth = 2147483647,
                MaxNameTableCharCount = 2147483647
            },
            SendTimeout = TimeSpan.FromSeconds(300), // 5 minutos
            ReceiveTimeout = TimeSpan.FromSeconds(300),
            OpenTimeout = TimeSpan.FromSeconds(30),
            CloseTimeout = TimeSpan.FromSeconds(30)
        };

        // Configurar WS-Security con certificado
        binding.Security.Message.ClientCredentialType = BasicHttpMessageCredentialType.Certificate;
        binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.None;

        return binding;
    }
}

/// <summary>
/// Behavior para agregar header Authorization con token WRAP
/// </summary>
public class AuthorizationHeaderBehavior : System.ServiceModel.Description.IEndpointBehavior
{
    private readonly string _token;

    public AuthorizationHeaderBehavior(string token)
    {
        _token = token;
    }

    public void AddBindingParameters(System.ServiceModel.Description.ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
    {
    }

    public void ApplyClientBehavior(System.ServiceModel.Description.ServiceEndpoint endpoint, System.ServiceModel.Dispatcher.ClientRuntime clientRuntime)
    {
        clientRuntime.ClientMessageInspectors.Add(new AuthorizationHeaderMessageInspector(_token));
    }

    public void ApplyDispatchBehavior(System.ServiceModel.Description.ServiceEndpoint endpoint, System.ServiceModel.Dispatcher.EndpointDispatcher endpointDispatcher)
    {
    }

    public void Validate(System.ServiceModel.Description.ServiceEndpoint endpoint)
    {
    }
}

/// <summary>
/// Message Inspector para agregar header Authorization
/// </summary>
public class AuthorizationHeaderMessageInspector : System.ServiceModel.Dispatcher.IClientMessageInspector
{
    private readonly string _token;

    public AuthorizationHeaderMessageInspector(string token)
    {
        _token = token;
    }

    public void AfterReceiveReply(ref Message reply, object correlationState)
    {
    }

    public object? BeforeSendRequest(ref Message request, System.ServiceModel.IClientChannel channel)
    {
        var header = MessageHeader.CreateHeader(
            "Authorization",
            string.Empty,
            $"WRAP access_token=\"{_token}\"");

        request.Headers.Add(header);
        return null!; // Este método debe retornar null según la interfaz pero no se usa el valor de retorno
    }
}

