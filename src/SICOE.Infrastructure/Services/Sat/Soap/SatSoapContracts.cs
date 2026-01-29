using System.Runtime.Serialization;
using System.ServiceModel;

namespace SICOE.Infrastructure.Services.Sat.Soap;

/// <summary>
/// Contratos SOAP para los servicios del SAT
/// Basados en la documentación oficial del SAT v1.5
/// </summary>

// ======================
// AUTENTICACIÓN
// ======================

[ServiceContract(Namespace = "http://DescargaMasivaTerceros.gob.mx")]
public interface IAutenticacionService
{
    [OperationContract(Action = "http://DescargaMasivaTerceros.gob.mx/IAutenticacion/Autentica")]
    string Autentica();
}

// ======================
// SOLICITUD DE DESCARGA
// ======================

[ServiceContract(Namespace = "http://DescargaMasivaTerceros.sat.gob.mx")]
public interface ISolicitaDescargaService
{
    [OperationContract(Action = "http://DescargaMasivaTerceros.sat.gob.mx/ISolicitaDescargaService/SolicitaDescargaEmitidos")]
    SolicitaDescargaEmitidosResponse SolicitaDescargaEmitidos(SolicitaDescargaEmitidosRequest request);

    [OperationContract(Action = "http://DescargaMasivaTerceros.sat.gob.mx/ISolicitaDescargaService/SolicitaDescargaRecibidos")]
    SolicitaDescargaRecibidosResponse SolicitaDescargaRecibidos(SolicitaDescargaRecibidosRequest request);

    [OperationContract(Action = "http://DescargaMasivaTerceros.sat.gob.mx/ISolicitaDescargaService/SolicitaDescargaFolio")]
    SolicitaDescargaFolioResponse SolicitaDescargaFolio(SolicitaDescargaFolioRequest request);
}

[DataContract(Namespace = "http://DescargaMasivaTerceros.sat.gob.mx")]
public class SolicitaDescargaEmitidosRequest
{
    [DataMember]
    public SolicitudDescargaEmitidos Solicitud { get; set; } = new();
}

[DataContract(Namespace = "http://DescargaMasivaTerceros.sat.gob.mx")]
public class SolicitudDescargaEmitidos
{
    [DataMember(Order = 1)]
    public string? Complemento { get; set; }

    [DataMember(Order = 2)]
    public string? EstadoComprobante { get; set; }

    [DataMember(Order = 3)]
    public DateTime FechaInicial { get; set; }

    [DataMember(Order = 4)]
    public DateTime FechaFinal { get; set; }

    [DataMember(Order = 5)]
    public string? RfcACuentaTerceros { get; set; }

    [DataMember(Order = 6)]
    public string RfcEmisor { get; set; } = string.Empty;

    [DataMember(Order = 7)]
    public string? RfcSolicitante { get; set; }

    [DataMember(Order = 8)]
    public string? TipoComprobante { get; set; }

    [DataMember(Order = 9)]
    public string TipoSolicitud { get; set; } = string.Empty;

    [DataMember(Order = 10)]
    public string[]? RfcReceptores { get; set; }
}

[DataContract(Namespace = "http://DescargaMasivaTerceros.sat.gob.mx")]
public class SolicitaDescargaEmitidosResponse
{
    [DataMember]
    public SolicitaDescargaEmitidosResult SolicitaDescargaEmitidosResult { get; set; } = new();
}

[DataContract(Namespace = "http://DescargaMasivaTerceros.sat.gob.mx")]
public class SolicitaDescargaEmitidosResult
{
    [DataMember]
    public string IdSolicitud { get; set; } = string.Empty;

    [DataMember]
    public string RfcSolicitante { get; set; } = string.Empty;

    [DataMember]
    public string CodEstatus { get; set; } = string.Empty;

    [DataMember]
    public string Mensaje { get; set; } = string.Empty;
}

[DataContract(Namespace = "http://DescargaMasivaTerceros.sat.gob.mx")]
public class SolicitaDescargaRecibidosRequest
{
    [DataMember]
    public SolicitudDescargaRecibidos Solicitud { get; set; } = new();
}

[DataContract(Namespace = "http://DescargaMasivaTerceros.sat.gob.mx")]
public class SolicitudDescargaRecibidos
{
    [DataMember(Order = 1)]
    public string? Complemento { get; set; }

    [DataMember(Order = 2)]
    public string? EstadoComprobante { get; set; }

    [DataMember(Order = 3)]
    public DateTime FechaInicial { get; set; }

    [DataMember(Order = 4)]
    public DateTime FechaFinal { get; set; }

    [DataMember(Order = 5)]
    public string? RfcACuentaTerceros { get; set; }

    [DataMember(Order = 6)]
    public string? RfcEmisor { get; set; }

    [DataMember(Order = 7)]
    public string RfcReceptor { get; set; } = string.Empty;

    [DataMember(Order = 8)]
    public string? RfcSolicitante { get; set; }

    [DataMember(Order = 9)]
    public string? TipoComprobante { get; set; }

    [DataMember(Order = 10)]
    public string TipoSolicitud { get; set; } = string.Empty;

    [DataMember(Order = 11)]
    public string[]? UUIDs { get; set; }
}

[DataContract(Namespace = "http://DescargaMasivaTerceros.sat.gob.mx")]
public class SolicitaDescargaRecibidosResponse
{
    [DataMember]
    public SolicitaDescargaRecibidosResult SolicitaDescargaRecibidosResult { get; set; } = new();
}

[DataContract(Namespace = "http://DescargaMasivaTerceros.sat.gob.mx")]
public class SolicitaDescargaRecibidosResult
{
    [DataMember]
    public string IdSolicitud { get; set; } = string.Empty;

    [DataMember]
    public string RfcSolicitante { get; set; } = string.Empty;

    [DataMember]
    public string CodEstatus { get; set; } = string.Empty;

    [DataMember]
    public string Mensaje { get; set; } = string.Empty;
}

[DataContract(Namespace = "http://DescargaMasivaTerceros.sat.gob.mx")]
public class SolicitaDescargaFolioRequest
{
    [DataMember]
    public SolicitudDescargaFolio Solicitud { get; set; } = new();
}

[DataContract(Namespace = "http://DescargaMasivaTerceros.sat.gob.mx")]
public class SolicitudDescargaFolio
{
    [DataMember(Order = 1)]
    public string Folio { get; set; } = string.Empty;

    [DataMember(Order = 2)]
    public string RfcSolicitante { get; set; } = string.Empty;
}

[DataContract(Namespace = "http://DescargaMasivaTerceros.sat.gob.mx")]
public class SolicitaDescargaFolioResponse
{
    [DataMember]
    public SolicitaDescargaFolioResult SolicitaDescargaFolioResult { get; set; } = new();
}

[DataContract(Namespace = "http://DescargaMasivaTerceros.sat.gob.mx")]
public class SolicitaDescargaFolioResult
{
    [DataMember]
    public string IdSolicitud { get; set; } = string.Empty;

    [DataMember]
    public string RfcSolicitante { get; set; } = string.Empty;

    [DataMember]
    public string CodEstatus { get; set; } = string.Empty;

    [DataMember]
    public string Mensaje { get; set; } = string.Empty;
}

// ======================
// VERIFICACIÓN
// ======================

[ServiceContract(Namespace = "http://DescargaMasivaTerceros.sat.gob.mx")]
public interface IVerificaSolicitudDescargaService
{
    [OperationContract(Action = "http://DescargaMasivaTerceros.sat.gob.mx/IVerificaSolicitudDescargaService/VerificaSolicitudDescarga")]
    VerificaSolicitudDescargaResponse VerificaSolicitudDescarga(VerificaSolicitudDescargaRequest request);
}

[DataContract(Namespace = "http://DescargaMasivaTerceros.sat.gob.mx")]
public class VerificaSolicitudDescargaRequest
{
    [DataMember]
    public SolicitudVerificacion Solicitud { get; set; } = new();
}

[DataContract(Namespace = "http://DescargaMasivaTerceros.sat.gob.mx")]
public class SolicitudVerificacion
{
    [DataMember(Order = 1)]
    public string IdSolicitud { get; set; } = string.Empty;

    [DataMember(Order = 2)]
    public string RfcSolicitante { get; set; } = string.Empty;
}

[DataContract(Namespace = "http://DescargaMasivaTerceros.sat.gob.mx")]
public class VerificaSolicitudDescargaResponse
{
    [DataMember]
    public VerificaSolicitudDescargaResult VerificaSolicitudDescargaResult { get; set; } = new();
}

[DataContract(Namespace = "http://DescargaMasivaTerceros.sat.gob.mx")]
public class VerificaSolicitudDescargaResult
{
    [DataMember]
    public string CodEstatus { get; set; } = string.Empty;

    [DataMember]
    public int EstadoSolicitud { get; set; }

    [DataMember]
    public string CodigoEstadoSolicitud { get; set; } = string.Empty;

    [DataMember]
    public int NumeroCFDIS { get; set; }

    [DataMember]
    public string Mensaje { get; set; } = string.Empty;

    [DataMember]
    public string[]? IdsPaquetes { get; set; }
}

// ======================
// DESCARGA DE PAQUETES
// ======================

[ServiceContract(Namespace = "http://DescargaMasivaTerceros.sat.gob.mx")]
public interface IDescargaMasivaService
{
    [OperationContract(Action = "http://DescargaMasivaTerceros.sat.gob.mx/IDescargaMasivaService/DescargaMasivaTerceros")]
    byte[] DescargaMasivaTerceros(string idPaquete);
}

