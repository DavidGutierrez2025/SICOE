using SICOE.Domain.Events;

namespace SICOE.Domain.Events.Descarga;

/// <summary>
/// Evento de dominio: Descarga iniciada
/// </summary>
public class DescargaIniciadaEvent : DomainEvent
{
    public int SolicitudDescargaId { get; }
    public int ClienteId { get; }

    public DescargaIniciadaEvent(int solicitudDescargaId, int clienteId)
    {
        SolicitudDescargaId = solicitudDescargaId;
        ClienteId = clienteId;
    }
}

