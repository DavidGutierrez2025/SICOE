using SICOE.Domain.Enums;
using SICOE.Domain.Interfaces;

namespace SICOE.Domain.Entities;

/// <summary>
/// Entidad SolicitudDescarga - Representa una solicitud de descarga masiva de CFDI
/// </summary>
public class SolicitudDescarga : IEntity
{
    public int Id { get; private set; }
    public int ClienteId { get; private set; }
    public string? IdSolicitudSat { get; private set; }
    public DateTime FechaInicial { get; private set; }
    public DateTime FechaFinal { get; private set; }
    public EstadoSolicitud Estado { get; private set; }
    public int TotalSolicitado { get; private set; }
    public int TotalRecibido { get; private set; }
    public DateTime FechaCreacion { get; private set; }
    public DateTime? FechaActualizacion { get; private set; }
    public string? MensajeError { get; private set; }

    // Navegación
    public Cliente Cliente { get; private set; } = null!;
    public ICollection<CFDI> CFDIs { get; private set; } = new List<CFDI>();
    public ICollection<ConciliacionCFDI> Conciliaciones { get; private set; } = new List<ConciliacionCFDI>();

    // Constructor privado para EF Core
    private SolicitudDescarga() { }

    // Constructor público
    public SolicitudDescarga(
        int clienteId,
        DateTime fechaInicial,
        DateTime fechaFinal,
        int totalSolicitado = 0)
    {
        if (fechaInicial > fechaFinal)
            throw new ArgumentException("La fecha inicial no puede ser mayor que la fecha final");

        ClienteId = clienteId;
        FechaInicial = fechaInicial;
        FechaFinal = fechaFinal;
        TotalSolicitado = totalSolicitado;
        Estado = EstadoSolicitud.Pendiente;
        FechaCreacion = DateTime.UtcNow;
    }

    // Lógica de negocio
    public void AsignarIdSolicitudSat(string idSolicitudSat)
    {
        if (string.IsNullOrWhiteSpace(idSolicitudSat))
            throw new ArgumentException("El ID de solicitud SAT no puede estar vacío", nameof(idSolicitudSat));

        IdSolicitudSat = idSolicitudSat;
        FechaActualizacion = DateTime.UtcNow;
    }

    public void MarcarComoEnProceso()
    {
        if (Estado != EstadoSolicitud.Pendiente)
            throw new InvalidOperationException("Solo se puede marcar como en proceso si está pendiente");

        Estado = EstadoSolicitud.EnProceso;
        FechaActualizacion = DateTime.UtcNow;
    }

    public void Completar(int totalRecibido)
    {
        if (totalRecibido < 0)
            throw new ArgumentException("El total recibido no puede ser negativo", nameof(totalRecibido));

        TotalRecibido = totalRecibido;
        Estado = EstadoSolicitud.Completada;
        FechaActualizacion = DateTime.UtcNow;
        MensajeError = null;
    }

    public void MarcarError(string mensajeError)
    {
        if (string.IsNullOrWhiteSpace(mensajeError))
            throw new ArgumentException("El mensaje de error no puede estar vacío", nameof(mensajeError));

        Estado = EstadoSolicitud.Error;
        MensajeError = mensajeError;
        FechaActualizacion = DateTime.UtcNow;
    }

    public void Cancelar()
    {
        Estado = EstadoSolicitud.Cancelada;
        FechaActualizacion = DateTime.UtcNow;
    }

    public bool EstaCompleta()
    {
        return TotalRecibido >= TotalSolicitado && TotalSolicitado > 0;
    }

    public int ObtenerFaltantes()
    {
        return Math.Max(0, TotalSolicitado - TotalRecibido);
    }

    public void ActualizarIdSolicitudSat(string idSolicitudSat)
    {
        AsignarIdSolicitudSat(idSolicitudSat);
    }

    public void ActualizarTotalSolicitado(int totalSolicitado)
    {
        if (totalSolicitado < 0)
            throw new ArgumentException("El total solicitado no puede ser negativo", nameof(totalSolicitado));

        TotalSolicitado = totalSolicitado;
        FechaActualizacion = DateTime.UtcNow;
    }

    public void ActualizarTotalRecibido(int totalRecibido)
    {
        if (totalRecibido < 0)
            throw new ArgumentException("El total recibido no puede ser negativo", nameof(totalRecibido));

        TotalRecibido = totalRecibido;
        FechaActualizacion = DateTime.UtcNow;
    }

    public void ActualizarEstado(EstadoSolicitud nuevoEstado)
    {
        Estado = nuevoEstado;
        FechaActualizacion = DateTime.UtcNow;
    }

    public void MarcarComoError(string mensajeError)
    {
        MarcarError(mensajeError);
    }
}

