using MediatR;
using SICOE.Application.Common;
using SICOE.Application.UseCases.Conciliacion.ConciliarCFDI;

namespace SICOE.Application.UseCases.Conciliacion.ConciliarCFDI;

/// <summary>
/// Command para conciliar CFDI solicitados vs recibidos
/// </summary>
public class ConciliarCFDICommand : IRequest<Result<ConciliarCFDIResponse>>
{
    public int SolicitudDescargaId { get; set; }
    public bool SolicitarFaltantes { get; set; } = true; // Por defecto, crear nueva solicitud para faltantes
}

