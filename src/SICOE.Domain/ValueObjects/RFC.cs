using System.Text.RegularExpressions;

namespace SICOE.Domain.ValueObjects;

/// <summary>
/// Value Object para RFC (Registro Federal de Contribuyentes)
/// </summary>
public class RFC : ValueObject
{
    public string Valor { get; private set; } = null!;

    // Constructor privado para EF Core
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. EF Core will initialize this property.
    private RFC() { }
#pragma warning restore CS8618

    public RFC(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            throw new ArgumentException("RFC no puede estar vacío", nameof(valor));

        if (!EsValido(valor))
            throw new ArgumentException("RFC no válido", nameof(valor));

        Valor = valor.ToUpper().Trim();
    }

    private static bool EsValido(string rfc)
    {
        // Validación de formato RFC (persona física o moral)
        var pattern = @"^[A-ZÑ&]{3,4}\d{6}[A-Z0-9]{3}$";
        return Regex.IsMatch(rfc, pattern);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Valor;
    }

    public static implicit operator string(RFC rfc) => rfc.Valor;
}

