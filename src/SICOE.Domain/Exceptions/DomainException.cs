namespace SICOE.Domain.Exceptions;

/// <summary>
/// Excepción base para excepciones de dominio
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
    
    protected DomainException(string message, Exception innerException) 
        : base(message, innerException) { }
}

