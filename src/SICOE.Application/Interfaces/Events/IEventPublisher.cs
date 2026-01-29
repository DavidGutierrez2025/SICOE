using SICOE.Domain.Events;

namespace SICOE.Application.Interfaces.Events;

/// <summary>
/// Interface para publicación de eventos de dominio
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync<T>(T domainEvent, CancellationToken cancellationToken = default) where T : DomainEvent;
}

