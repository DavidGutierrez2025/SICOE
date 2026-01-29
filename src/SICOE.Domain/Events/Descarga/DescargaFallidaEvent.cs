using SICOE.Domain.Events;

namespace SICOE.Domain.Events.Descarga;

/// <summary>
/// Evento de dominio: Descarga fallida
/// </summary>
public class DescargaFallidaEvent : DomainEvent
{
    public int SolicitudDescargaId { get; }
    public int ClienteId { get; }
    public string MensajeError { get; }

    public DescargaFallidaEvent(int solicitudDescargaId, int clienteId, string mensajeError)
    {
        SolicitudDescargaId = solicitudDescargaId;
        ClienteId = clienteId;
        MensajeError = mensajeError;
    }
}

