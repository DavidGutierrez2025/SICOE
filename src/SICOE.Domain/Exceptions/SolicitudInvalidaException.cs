namespace SICOE.Domain.Exceptions;

/// <summary>
/// Excepción de dominio: Solicitud inválida
/// </summary>
public class SolicitudInvalidaException : DomainException
{
    public SolicitudInvalidaException() : base("La solicitud no es válida") { }
    
    public SolicitudInvalidaException(string message) : base(message) { }
}

