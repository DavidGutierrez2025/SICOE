using SICOE.Domain.Enums;
using SICOE.Domain.Interfaces;

namespace SICOE.Domain.Entities;

/// <summary>
/// Entidad ConciliacionCFDI - Representa una conciliación de CFDI solicitados vs recibidos
/// </summary>
public class ConciliacionCFDI : IEntity
{
    public int Id { get; private set; }
    public int SolicitudDescargaId { get; private set; }
    public EstadoConciliacion Estado { get; private set; }
    public int TotalSolicitado { get; private set; }
    public int TotalRecibido { get; private set; }
    public int TotalFaltantes { get; private set; }
    public int? NuevaSolicitudId { get; private set; } // ID de la nueva solicitud creada para faltantes
    public int IntentosReconciliacion { get; private set; }
    public string? MensajeError { get; private set; }
    public DateTime FechaCreacion { get; private set; }
    public DateTime? FechaActualizacion { get; private set; }
    public DateTime? FechaCompletada { get; private set; }

    // Navegación
    public SolicitudDescarga SolicitudDescarga { get; private set; } = null!;
    public SolicitudDescarga? NuevaSolicitud { get; private set; }

    // Constructor privado para EF Core
    private ConciliacionCFDI() { }

    // Constructor público
    public ConciliacionCFDI(
        int solicitudDescargaId,
        int totalSolicitado,
        int totalRecibido)
    {
        if (totalSolicitado < 0)
            throw new ArgumentException("El total solicitado no puede ser negativo", nameof(totalSolicitado));
        if (totalRecibido < 0)
            throw new ArgumentException("El total recibido no puede ser negativo", nameof(totalRecibido));

        SolicitudDescargaId = solicitudDescargaId;
        TotalSolicitado = totalSolicitado;
        TotalRecibido = totalRecibido;
        TotalFaltantes = Math.Max(0, totalSolicitado - totalRecibido);
        Estado = TotalFaltantes == 0 ? EstadoConciliacion.Completada : EstadoConciliacion.ConFaltantes;
        IntentosReconciliacion = 0;
        FechaCreacion = DateTime.UtcNow;
    }

    // Lógica de negocio
    public void MarcarComoCompletada()
    {
        Estado = EstadoConciliacion.Completada;
        TotalFaltantes = 0;
        FechaCompletada = DateTime.UtcNow;
        FechaActualizacion = DateTime.UtcNow;
        MensajeError = null;
    }

    public void MarcarConFaltantes(int totalFaltantes)
    {
        if (totalFaltantes < 0)
            throw new ArgumentException("El total de faltantes no puede ser negativo", nameof(totalFaltantes));

        TotalFaltantes = totalFaltantes;
        Estado = EstadoConciliacion.ConFaltantes;
        FechaActualizacion = DateTime.UtcNow;
    }

    public void AsignarNuevaSolicitud(int nuevaSolicitudId)
    {
        if (nuevaSolicitudId <= 0)
            throw new ArgumentException("El ID de nueva solicitud debe ser mayor que cero", nameof(nuevaSolicitudId));

        NuevaSolicitudId = nuevaSolicitudId;
        Estado = EstadoConciliacion.EnProceso;
        IncrementarIntentos();
        FechaActualizacion = DateTime.UtcNow;
    }

    public void IncrementarIntentos()
    {
        IntentosReconciliacion++;
        FechaActualizacion = DateTime.UtcNow;
    }

    public void MarcarError(string mensajeError)
    {
        if (string.IsNullOrWhiteSpace(mensajeError))
            throw new ArgumentException("El mensaje de error no puede estar vacío", nameof(mensajeError));

        Estado = EstadoConciliacion.Error;
        MensajeError = mensajeError;
        IncrementarIntentos();
        FechaActualizacion = DateTime.UtcNow;
    }

    public void Cancelar()
    {
        Estado = EstadoConciliacion.Cancelada;
        FechaActualizacion = DateTime.UtcNow;
    }

    public void ActualizarTotales(int totalSolicitado, int totalRecibido)
    {
        if (totalSolicitado < 0)
            throw new ArgumentException("El total solicitado no puede ser negativo", nameof(totalSolicitado));
        if (totalRecibido < 0)
            throw new ArgumentException("El total recibido no puede ser negativo", nameof(totalRecibido));

        TotalSolicitado = totalSolicitado;
        TotalRecibido = totalRecibido;
        TotalFaltantes = Math.Max(0, totalSolicitado - totalRecibido);

        // Actualizar estado según faltantes
        if (TotalFaltantes == 0 && Estado != EstadoConciliacion.Completada)
        {
            MarcarComoCompletada();
        }
        else if (TotalFaltantes > 0 && Estado == EstadoConciliacion.Completada)
        {
            Estado = EstadoConciliacion.ConFaltantes;
        }

        FechaActualizacion = DateTime.UtcNow;
    }

    public bool TieneFaltantes()
    {
        return TotalFaltantes > 0;
    }

    public bool PuedeReconciliar(int maxIntentos = 3)
    {
        return Estado == EstadoConciliacion.ConFaltantes && IntentosReconciliacion < maxIntentos;
    }
}

