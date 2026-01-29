namespace SICOE.Domain.Events;

/// <summary>
/// Clase base para eventos de dominio
/// </summary>
public abstract class DomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public Guid EventId { get; } = Guid.NewGuid();
}

