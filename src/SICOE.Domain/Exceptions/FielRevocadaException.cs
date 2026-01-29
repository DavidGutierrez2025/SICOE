namespace SICOE.Domain.Exceptions;

/// <summary>
/// Excepción de dominio: FIEL revocada
/// </summary>
public class FielRevocadaException : DomainException
{
    public FielRevocadaException() : base("La FIEL ha sido revocada o no es válida") { }
    
    public FielRevocadaException(string message) : base(message) { }
}

