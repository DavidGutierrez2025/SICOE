namespace SICOE.Application.UseCases.Autenticacion.AutenticarConFiel;

public class AutenticacionResponseDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime Expiration { get; set; }
    public string Rfc { get; set; } = string.Empty;
}
