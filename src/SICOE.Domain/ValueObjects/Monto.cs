namespace SICOE.Domain.ValueObjects;

/// <summary>
/// Value Object para Monto (con moneda)
/// </summary>
public class Monto : ValueObject
{
    public decimal Valor { get; private set; }
    public string Moneda { get; private set; } = null!;

    // Constructor privado para EF Core
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. EF Core will initialize this property.
    private Monto() { }
#pragma warning restore CS8618

    public Monto(decimal valor, string moneda = "MXN")
    {
        if (valor < 0)
            throw new ArgumentException("El monto no puede ser negativo", nameof(valor));

        if (string.IsNullOrWhiteSpace(moneda))
            throw new ArgumentException("La moneda no puede estar vacía", nameof(moneda));

        Valor = valor;
        Moneda = moneda.ToUpper();
    }

    public Monto Sumar(Monto otro)
    {
        if (Moneda != otro.Moneda)
            throw new InvalidOperationException("No se pueden sumar montos de diferentes monedas");

        return new Monto(Valor + otro.Valor, Moneda);
    }

    public Monto Restar(Monto otro)
    {
        if (Moneda != otro.Moneda)
            throw new InvalidOperationException("No se pueden restar montos de diferentes monedas");

        return new Monto(Valor - otro.Valor, Moneda);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Valor;
        yield return Moneda;
    }
}

