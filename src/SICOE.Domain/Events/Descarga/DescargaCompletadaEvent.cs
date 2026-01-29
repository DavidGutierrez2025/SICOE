using SICOE.Domain.Events;

namespace SICOE.Domain.Events.Descarga;

/// <summary>
/// Evento de dominio: Descarga completada
/// </summary>
public class DescargaCompletadaEvent : DomainEvent
{
    public int SolicitudDescargaId { get; }
    public int ClienteId { get; }
    public int TotalCFDI { get; }

    public DescargaCompletadaEvent(int solicitudDescargaId, int clienteId, int totalCFDI)
    {
        SolicitudDescargaId = solicitudDescargaId;
        ClienteId = clienteId;
        TotalCFDI = totalCFDI;
    }
}

