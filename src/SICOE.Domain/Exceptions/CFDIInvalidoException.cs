namespace SICOE.Domain.Exceptions;

/// <summary>
/// Excepción de dominio: CFDI inválido
/// </summary>
public class CFDIInvalidoException : DomainException
{
    public CFDIInvalidoException() : base("El CFDI no es válido") { }
    
    public CFDIInvalidoException(string message) : base(message) { }
}

