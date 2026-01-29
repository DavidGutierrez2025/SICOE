namespace SICOE.Application.Common.Errors;

/// <summary>
/// Representa un error de aplicación
/// </summary>
public class Error
{
    public string Code { get; }
    public string Message { get; }

    public Error(string code, string message)
    {
        Code = code;
        Message = message;
    }

    public static Error None => new Error(string.Empty, string.Empty);
    public static Error NullValue => new Error("Error.NullValue", "El valor no puede ser nulo");
}

