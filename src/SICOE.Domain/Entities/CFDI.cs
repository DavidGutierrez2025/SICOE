using SICOE.Domain.Enums;
using SICOE.Domain.Interfaces;
using SICOE.Domain.ValueObjects;

namespace SICOE.Domain.Entities;

/// <summary>
/// Entidad CFDI - Representa un Comprobante Fiscal Digital por Internet
/// </summary>
public class CFDI : IEntity
{
    public int Id { get; private set; }
    public int SolicitudDescargaId { get; private set; }
    public UUID Uuid { get; private set; } = null!;
    public RFC RfcEmisor { get; private set; } = null!;
    public RFC RfcReceptor { get; private set; } = null!;
    public DateTime FechaEmision { get; private set; }
    public DateTime? FechaTimbrado { get; private set; }
    public Monto Total { get; private set; } = null!;
    public decimal SubTotal { get; private set; }
    public decimal TotalImpuestosTrasladados { get; private set; }
    public TipoComprobante TipoComprobante { get; private set; }
    public EstatusCFDI Estatus { get; private set; }
    public string? Serie { get; private set; }
    public string? Folio { get; private set; }
    public string? NombreEmisor { get; private set; }
    public string? NombreReceptor { get; private set; }
    public string? RegimenFiscalEmisor { get; private set; }
    public string? RegimenFiscalReceptor { get; private set; }
    public string? DomicilioFiscalReceptor { get; private set; }
    public string? UsoCFDI { get; private set; }
    public string? FormaPago { get; private set; }
    public string? MetodoPago { get; private set; }
    public string? LugarExpedicion { get; private set; }
    public int? ArchivoId { get; private set; }
    public DateTime FechaCreacion { get; private set; }
    public DateTime? FechaActualizacion { get; private set; }

    // Navegación
    public SolicitudDescarga SolicitudDescarga { get; private set; } = null!;
    public Archivo? Archivo { get; private set; }

    // Constructor privado para EF Core
    private CFDI() { }

    // Constructor público
    public CFDI(
        int solicitudDescargaId,
        UUID uuid,
        RFC rfcEmisor,
        RFC rfcReceptor,
        DateTime fechaEmision,
        Monto total,
        TipoComprobante tipoComprobante,
        int? archivoId = null,
        decimal subTotal = 0,
        decimal totalImpuestosTrasladados = 0,
        DateTime? fechaTimbrado = null,
        string? serie = null,
        string? folio = null,
        string? nombreEmisor = null,
        string? nombreReceptor = null,
        string? regimenFiscalEmisor = null,
        string? regimenFiscalReceptor = null,
        string? domicilioFiscalReceptor = null,
        string? usoCFDI = null,
        string? formaPago = null,
        string? metodoPago = null,
        string? lugarExpedicion = null)
    {
        SolicitudDescargaId = solicitudDescargaId;
        Uuid = uuid ?? throw new ArgumentNullException(nameof(uuid));
        RfcEmisor = rfcEmisor ?? throw new ArgumentNullException(nameof(rfcEmisor));
        RfcReceptor = rfcReceptor ?? throw new ArgumentNullException(nameof(rfcReceptor));
        FechaEmision = fechaEmision;
        FechaTimbrado = fechaTimbrado;
        Total = total ?? throw new ArgumentNullException(nameof(total));
        SubTotal = subTotal;
        TotalImpuestosTrasladados = totalImpuestosTrasladados;
        TipoComprobante = tipoComprobante;
        Estatus = EstatusCFDI.Vigente;
        ArchivoId = archivoId;
        Serie = serie;
        Folio = folio;
        NombreEmisor = nombreEmisor;
        NombreReceptor = nombreReceptor;
        RegimenFiscalEmisor = regimenFiscalEmisor;
        RegimenFiscalReceptor = regimenFiscalReceptor;
        DomicilioFiscalReceptor = domicilioFiscalReceptor;
        UsoCFDI = usoCFDI;
        FormaPago = formaPago;
        MetodoPago = metodoPago;
        LugarExpedicion = lugarExpedicion;
        FechaCreacion = DateTime.UtcNow;
    }

    // Lógica de negocio
    public void AsignarArchivo(int archivoId)
    {
        ArchivoId = archivoId;
        FechaActualizacion = DateTime.UtcNow;
    }

    public void MarcarComoCancelado()
    {
        Estatus = EstatusCFDI.Cancelado;
        FechaActualizacion = DateTime.UtcNow;
    }

    public void MarcarComoNoEncontrado()
    {
        Estatus = EstatusCFDI.NoEncontrado;
        FechaActualizacion = DateTime.UtcNow;
    }
}

