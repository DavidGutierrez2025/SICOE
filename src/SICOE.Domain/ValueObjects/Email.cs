using System.Text.RegularExpressions;

namespace SICOE.Domain.ValueObjects;

/// <summary>
/// Value Object para Email
/// </summary>
public class Email : ValueObject
{
    public string Valor { get; private set; } = null!;

    // Constructor privado para EF Core
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. EF Core will initialize this property.
    private Email() { }
#pragma warning restore CS8618

    public Email(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            throw new ArgumentException("Email no puede estar vacío", nameof(valor));

        if (!EsValido(valor))
            throw new ArgumentException("Email no válido", nameof(valor));

        Valor = valor.ToLower().Trim();
    }

    private static bool EsValido(string email)
    {
        var pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
        return Regex.IsMatch(email, pattern);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Valor;
    }

    public static implicit operator string(Email email) => email.Valor;
}

