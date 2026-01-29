using System.Text.RegularExpressions;

namespace SICOE.Domain.ValueObjects;

/// <summary>
/// Value Object para UUID (Universal Unique Identifier)
/// </summary>
public class UUID : ValueObject
{
    public string Valor { get; private set; } = null!;

    // Constructor privado para EF Core
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. EF Core will initialize this property.
    private UUID() { }
#pragma warning restore CS8618

    public UUID(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            throw new ArgumentException("UUID no puede estar vacío", nameof(valor));

        if (!EsValido(valor))
            throw new ArgumentException("UUID no válido", nameof(valor));

        Valor = valor.ToUpper().Trim();
    }

    private static bool EsValido(string uuid)
    {
        // Validación de formato UUID (GUID)
        var pattern = @"^[0-9A-F]{8}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{12}$";
        return Regex.IsMatch(uuid, pattern, RegexOptions.IgnoreCase);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Valor;
    }

    public static implicit operator string(UUID uuid) => uuid.Valor;
}

