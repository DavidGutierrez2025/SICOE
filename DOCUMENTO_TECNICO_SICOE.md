# SICOE - Sistema de Integración y Control de Operaciones Electrónicas

Este documento resume el flujo del sistema, sus componentes principales y la separación entre clases propias del sistema vs clases compartidas (si aplica).

## Diagrama General del Sistema

El sistema SICOE se estructura en tres capas principales siguiendo Clean Architecture:

```
┌─────────────────────────────────────────────────────────────┐
│                    PRESENTATION LAYER                        │
│  ┌──────────────┐              ┌──────────────┐            │
│  │  Razor Pages │              │   REST API   │            │
│  │  (Frontend)  │◄────────────►│  (Backend)   │            │
│  └──────────────┘              └──────────────┘            │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                    APPLICATION LAYER                        │
│  ┌──────────────────────────────────────────────────────┐  │
│  │         Use Cases (CQRS Pattern - MediatR)           │  │
│  │  • SolicitarDescarga  • VerificarDescarga            │  │
│  │  • DescargarPaquete   • ProcesarCFDI                 │  │
│  │  • ListarSolicitudes  • ObtenerCFDI                  │  │
│  │  • ConciliarCFDI      • AutenticarConFiel            │  │
│  └──────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                   INFRASTRUCTURE LAYER                       │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐     │
│  │  SatService  │  │ FielService │  │ArchivoService│     │
│  │  (SOAP/SAT)  │  │  (FIEL)     │  │  (Storage)   │     │
│  └──────────────┘  └──────────────┘  └──────────────┘     │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐     │
│  │ Hangfire     │  │ Entity       │  │  Logging     │     │
│  │ (Jobs)       │  │ Framework   │  │  (Serilog)   │     │
│  └──────────────┘  └──────────────┘  └──────────────┘     │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                      DOMAIN LAYER                            │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐     │
│  │  Entities    │  │  Value       │  │   Enums      │     │
│  │  (Domain)    │  │  Objects     │  │              │     │
│  └──────────────┘  └──────────────┘  └──────────────┘     │
└─────────────────────────────────────────────────────────────┘
```

## Flujo Principal del Sistema

### Flujo de Descarga Masiva de CFDI

```
1. AUTENTICACIÓN
   └─► Usuario carga FIEL (.cer + .key) en frontend
       └─► Frontend almacena en sessionStorage (NUNCA se envía al servidor permanentemente)
       └─► Backend obtiene token SAT usando FIEL
       └─► Token se almacena encriptado en BD (TokenSat) con expiración

2. SOLICITUD DE DESCARGA
   └─► Usuario selecciona rango de fechas y filtros
       └─► Backend crea SolicitudDescarga (Estado: Pendiente)
       └─► Backend envía solicitud SOAP al SAT
       └─► SAT responde con IdSolicitudSat
       └─► SolicitudDescarga actualizada (Estado: EnProceso)

3. VERIFICACIÓN DE ESTADO
   └─► Sistema verifica estado en SAT periódicamente (Hangfire Job)
       └─► Si estado = "Completada" → Obtiene IdsPaquetes
       └─► Actualiza SolicitudDescarga (Estado: Completada, TotalSolicitado)

4. DESCARGA DE PAQUETES
   └─► Usuario solicita descarga de paquete
       └─► Backend descarga paquete ZIP del SAT usando FIEL
       └─► Extrae XMLs del ZIP
       └─► Guarda archivos físicos (ArchivoService)
       └─► Procesa y guarda CFDI en BD (CFDIs)

5. CONCILIACIÓN
   └─► Sistema compara TotalSolicitado vs TotalRecibido
       └─► Si hay faltantes → Crea nueva solicitud automáticamente
       └─► Registra en ConciliacionesCFDI
```

### Vertientes del Flujo

**Vertiente "Usuario Final":**
- Carga FIEL en frontend (sessionStorage)
- Solicita descarga masiva
- Consulta estado de solicitudes
- Descarga paquetes completados
- Visualiza CFDI procesados

**Vertiente "Sistema Automático":**
- Verificación periódica de estado (Hangfire)
- Procesamiento automático de paquetes
- Conciliación automática
- Limpieza de tokens expirados

**Vertiente "Integración SAT":**
- Autenticación con servicio SAT (SOAP)
- Solicitud de descarga masiva (SOAP)
- Verificación de estado (SOAP)
- Descarga de paquetes (SOAP)

## Diagrama de Secuencia (Frontend → Backend → SAT)

```
Frontend (Razor)          Backend (API)              SAT (SOAP)
    │                         │                          │
    │──POST /api/Autenticacion/obtener-token───────────►│
    │                         │                          │
    │                         │──POST Autenticacion────►│
    │                         │◄──Token WRAP────────────│
    │◄──Token SAT─────────────│                          │
    │                         │                          │
    │──POST /api/Descarga/solicitar────────────────────►│
    │                         │                          │
    │                         │──POST SolicitaDescarga──►│
    │                         │◄──IdSolicitudSat────────│
    │◄──IdSolicitudSat────────│                          │
    │                         │                          │
    │──POST /api/Descarga/verificar-estado─────────────►│
    │                         │                          │
    │                         │──POST VerificaSolicitud►│
    │                         │◄──Estado + IdsPaquetes──│
    │◄──Estado────────────────│                          │
    │                         │                          │
    │──POST /api/Descarga/descargar-paquete─────────────►│
    │                         │                          │
    │                         │──POST Descargar─────────►│
    │                         │◄──Paquete ZIP (Base64)──│
    │◄──ZIP File───────────────│                          │
```

## Estados Relacionados al Sistema SICOE

A continuación se detallan los estados específicos que rigen el flujo del sistema:

### EstadoSolicitud (SolicitudesDescarga.Estado)

| Valor | Nombre Constante | Descripción en el Flujo |
| :--- | :--- | :--- |
| 1 | `Pendiente` | Solicitud creada localmente, esperando ser enviada al SAT o en cola de procesamiento. |
| 2 | `EnProceso` | Solicitud enviada al SAT y siendo procesada. El SAT está generando los paquetes. |
| 3 | `Completada` | El SAT ha completado la generación de paquetes. Los paquetes están listos para descargar. |
| 4 | `Error` | Error al procesar la solicitud (error de comunicación con SAT, firma inválida, etc.). |
| 5 | `Cancelada` | Solicitud cancelada manualmente por el usuario o por el sistema. |

### EstatusCFDI (CFDIs.Estatus)

| Valor | Nombre Constante | Descripción |
| :--- | :--- | :--- |
| 1 | `Vigente` | CFDI vigente y válido en el SAT. |
| 2 | `Cancelado` | CFDI cancelado en el SAT. |
| 3 | `NoEncontrado` | CFDI no encontrado en el SAT (posible error en UUID). |

### EstadoConciliacion (ConciliacionesCFDI.Estado)

| Valor | Nombre Constante | Descripción |
| :--- | :--- | :--- |
| 0 | `Pendiente` | Pendiente de conciliación. |
| 1 | `EnProceso` | Conciliación en curso. |
| 2 | `Completada` | Conciliación completada (todo coincide). |
| 3 | `ConFaltantes` | Hay faltantes detectados (TotalSolicitado > TotalRecibido). |
| 4 | `Error` | Error en la conciliación. |
| 5 | `Cancelada` | Conciliación cancelada. |

### TipoComprobante (CFDIs.TipoComprobante)

| Valor | Nombre Constante | Descripción |
| :--- | :--- | :--- |
| 1 | `Ingreso` | Comprobante de ingreso. |
| 2 | `Egreso` | Comprobante de egreso. |
| 3 | `Traslado` | Comprobante de traslado. |
| 4 | `Pago` | Comprobante de pago. |
| 5 | `Nomina` | Comprobante de nómina. |
| 99 | `Otro` | Otro tipo de comprobante. |

## Comunicación Frontend → Backend (APIs del Sistema)

Endpoints usados por el frontend y su controlador en backend:

### Autenticación

- `POST /api/Autenticacion/extraer-rfc`
  - Backend: `AutenticacionController.ExtraerRfc`
  - Descripción: Extrae el RFC del certificado .cer sin necesidad de la clave privada.

- `POST /api/Autenticacion/obtener-token`
  - Backend: `AutenticacionController.ObtenerToken`
  - Descripción: Obtiene un token SAT usando el certificado FIEL proporcionado. La FIEL se usa temporalmente y se descarta inmediatamente.

### Descarga de CFDI

- `POST /api/Descarga/solicitar`
  - Backend: `DescargaController.SolicitarDescarga`
  - Descripción: Solicita una descarga masiva de CFDI's al SAT.

- `GET /api/Descarga/listar`
  - Backend: `DescargaController.ListarSolicitudes`
  - Descripción: Lista las solicitudes de descarga de los últimos N días. Requiere RFC o ClienteId para filtrar.

- `POST /api/Descarga/descargar-paquete/{solicitudId}`
  - Backend: `DescargaController.DescargarPaquete`
  - Descripción: Descarga el paquete ZIP de una solicitud completada. Requiere certificado FIEL.

- `POST /api/Descarga/verificar-estado/{solicitudId}`
  - Backend: `DescargaController.VerificarEstado`
  - Descripción: Verifica el estado de una solicitud de descarga en el SAT. Requiere certificado FIEL.

- `POST /api/Descarga/procesar`
  - Backend: `DescargaController.ProcesarCFDI`
  - Descripción: Procesa y guarda los CFDI's descargados del SAT. Descarga paquetes, extrae XML y guarda archivos.

### Consulta de CFDI

- `GET /api/CFDI/listar`
  - Backend: `CFDIController.ListarCFDI`
  - Descripción: Lista CFDI procesados con filtros y paginación.

- `GET /api/CFDI/{uuidOrId}`
  - Backend: `CFDIController.ObtenerCFDI`
  - Descripción: Obtiene detalles de un CFDI por UUID o ID.

- `GET /api/CFDI/{uuidOrId}/descargar`
  - Backend: `CFDIController.DescargarCFDI`
  - Descripción: Descarga el XML de un CFDI por UUID o ID.

- `POST /api/CFDI/descargar-lote`
  - Backend: `CFDIController.DescargarCFDILote`
  - Descripción: Descarga múltiples CFDI en un archivo ZIP.

## Payloads (Request/Response) por API

### Extraer RFC del Certificado

- `POST /api/Autenticacion/extraer-rfc`
```json
{
  "certificadoCer": "MIIFvTCCA6WgAwIBAgIUMzAwMDEwMDAwMDAzMDAwMjkwODEwDQYJKoZIhvcNAQELBQAw..."
}
```

Respuesta esperada: `200`
```json
{
  "success": true,
  "data": {
    "rfc": "XAXX010101000"
  }
}
```

### Obtener Token SAT

- `POST /api/Autenticacion/obtener-token`
```json
{
  "certificadoCer": "MIIFvTCCA6WgAwIBAgIUMzAwMDEwMDAwMDAzMDAwMjkwODEwDQYJKoZIhvcNAQELBQAw...",
  "clavePrivadaKey": "MIIEvgIBADANBgkqhkiG9w0BAQEFAASCBKgwggSkAgEAAoIBAQC...",
  "passwordFiel": "contraseña123",
  "clienteId": 1
}
```

Respuesta esperada: `200`
```json
{
  "success": true,
  "message": "Token obtenido exitosamente",
  "token": "eyJhbGciOiJodHRwOi8vd3d3LnczLm9yZy8yMDAxLzA0L3htbGRzaWctbW9yZSNobWFjLXNoYTI1NiIsInR5cCI6IkpXVCJ9...",
  "expiration": "2025-01-23T10:00:00Z",
  "rfc": "XAXX010101000"
}
```

### Solicitar Descarga Masiva

- `POST /api/Descarga/solicitar`
```json
{
  "clienteId": 1,
  "certificadoCer": "MIIFvTCCA6WgAwIBAgIUMzAwMDEwMDAwMDAzMDAwMjkwODEwDQYJKoZIhvcNAQELBQAw...",
  "clavePrivadaKey": "MIIEvgIBADANBgkqhkiG9w0BAQEFAASCBKgwggSkAgEAAoIBAQC...",
  "passwordFiel": "contraseña123",
  "fechaInicial": "2024-12-01T00:00:00Z",
  "fechaFinal": "2024-12-31T23:59:59Z",
  "tipoSolicitud": "CFDI",
  "tipoComprobante": "I",
  "estadoComprobante": "Vigente",
  "tipoDescarga": "recibidos"
}
```

Respuesta esperada: `200`
```json
{
  "success": true,
  "message": "Operación exitosa",
  "data": {
    "solicitudId": 123,
    "idSolicitudSat": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "codigoEstado": 5000,
    "mensaje": "Solicitud Aceptada",
    "fechaEstimadaTermino": "2025-01-22T15:30:00Z"
  }
}
```

### Listar Solicitudes

- `GET /api/Descarga/listar?rfc=XAXX010101000&diasAtras=30`
Respuesta esperada: `200`
```json
{
  "success": true,
  "message": "Operación exitosa",
  "data": {
    "solicitudes": [
      {
        "id": 123,
        "clienteId": 1,
        "idSolicitudSat": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
        "fechaInicial": "2024-12-01T00:00:00Z",
        "fechaFinal": "2024-12-31T23:59:59Z",
        "estado": 3,
        "estadoTexto": "Completada",
        "totalSolicitado": 150,
        "totalRecibido": 150,
        "cantidadDocumentos": 150,
        "fechaCreacion": "2025-01-21T10:00:00Z"
      }
    ],
    "total": 1
  }
}
```

### Descargar Paquete

- `POST /api/Descarga/descargar-paquete/123`
```json
{
  "certificadoCer": "MIIFvTCCA6WgAwIBAgIUMzAwMDEwMDAwMDAzMDAwMjkwODEwDQYJKoZIhvcNAQELBQAw...",
  "clavePrivadaKey": "MIIEvgIBADANBgkqhkiG9w0BAQEFAASCBKgwggSkAgEAAoIBAQC...",
  "passwordFiel": "contraseña123"
}
```

Respuesta esperada: `200` (File Stream)
- Content-Type: `application/zip`
- Content-Disposition: `attachment; filename="paquete_123.zip"`

### Verificar Estado

- `POST /api/Descarga/verificar-estado/123`
```json
{
  "certificadoCer": "MIIFvTCCA6WgAwIBAgIUMzAwMDEwMDAwMDAzMDAwMjkwODEwDQYJKoZIhvcNAQELBQAw...",
  "clavePrivadaKey": "MIIEvgIBADANBgkqhkiG9w0BAQEFAASCBKgwggSkAgEAAoIBAQC...",
  "passwordFiel": "contraseña123"
}
```

Respuesta esperada: `200`
```json
{
  "success": true,
  "message": "Estado verificado exitosamente",
  "data": {
    "solicitudId": 123,
    "codigoEstado": "5000",
    "mensaje": "Solicitud Aceptada",
    "totalPaquetes": 1,
    "totalCFDIs": 150,
    "requiereProcesamiento": true
  }
}
```

### Listar CFDI

- `GET /api/CFDI/listar?solicitudDescargaId=123&pageNumber=1&pageSize=50`
Respuesta esperada: `200`
```json
{
  "success": true,
  "message": "Operación exitosa",
  "data": {
    "cfdis": [
      {
        "id": 456,
        "uuid": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
        "rfcEmisor": "XAXX010101000",
        "rfcReceptor": "GODE561231GR8",
        "fechaEmision": "2024-12-15T10:30:00Z",
        "total": 1000.00,
        "moneda": "MXN",
        "tipoComprobante": 1,
        "tipoComprobanteTexto": "Ingreso",
        "estatus": 1,
        "estatusTexto": "Vigente",
        "serie": "A",
        "folio": "12345",
        "nombreEmisor": "Empresa Emisora S.A. de C.V.",
        "nombreReceptor": "Cliente Receptor"
      }
    ],
    "total": 150,
    "pageNumber": 1,
    "pageSize": 50,
    "totalPages": 3
  }
}
```

### Obtener CFDI

- `GET /api/CFDI/a1b2c3d4-e5f6-7890-abcd-ef1234567890`
Respuesta esperada: `200`
```json
{
  "success": true,
  "message": "Operación exitosa",
  "data": {
    "id": 456,
    "uuid": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "rfcEmisor": "XAXX010101000",
    "rfcReceptor": "GODE561231GR8",
    "fechaEmision": "2024-12-15T10:30:00Z",
    "fechaTimbrado": "2024-12-15T10:31:00Z",
    "total": 1000.00,
    "moneda": "MXN",
    "subTotal": 862.07,
    "totalImpuestosTrasladados": 137.93,
    "tipoComprobante": 1,
    "estatus": 1,
    "serie": "A",
    "folio": "12345",
    "nombreEmisor": "Empresa Emisora S.A. de C.V.",
    "nombreReceptor": "Cliente Receptor",
    "regimenFiscalEmisor": "601",
    "regimenFiscalReceptor": "605",
    "usoCFDI": "G03",
    "formaPago": "03",
    "metodoPago": "PUE",
    "lugarExpedicion": "12345",
    "archivoId": 789,
    "fechaCreacion": "2025-01-21T11:00:00Z"
  }
}
```

### Descargar CFDI

- `GET /api/CFDI/a1b2c3d4-e5f6-7890-abcd-ef1234567890/descargar`
Respuesta esperada: `200` (File Stream)
- Content-Type: `application/xml`
- Content-Disposition: `attachment; filename="CFDI_a1b2c3d4-e5f6-7890-abcd-ef1234567890.xml"`

### Descargar CFDI en Lote

- `POST /api/CFDI/descargar-lote`
```json
{
  "uuids": [
    "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "b2c3d4e5-f6a7-8901-bcde-f12345678901"
  ]
}
```

Respuesta esperada: `200` (File Stream)
- Content-Type: `application/zip`
- Content-Disposition: `attachment; filename="CFDI_Lote_20250122.zip"`

## Clases Propias del Sistema SICOE

### Backend (.NET Core / C#)

#### Controllers
- `src/SICOE.API/Controllers/AutenticacionController.cs`
- `src/SICOE.API/Controllers/DescargaController.cs`
- `src/SICOE.API/Controllers/CFDIController.cs`
- `src/SICOE.API/Controllers/Base/SICOEBaseController.cs`

#### Use Cases (CQRS Pattern - MediatR)
- `src/SICOE.Application/UseCases/Autenticacion/AutenticarConFiel/`
- `src/SICOE.Application/UseCases/Descarga/SolicitarDescarga/`
- `src/SICOE.Application/UseCases/Descarga/VerificarDescarga/`
- `src/SICOE.Application/UseCases/Descarga/DescargarPaquete/`
- `src/SICOE.Application/UseCases/Descarga/ProcesarCFDI/`
- `src/SICOE.Application/UseCases/Descarga/ListarSolicitudes/`
- `src/SICOE.Application/UseCases/CFDI/ObtenerCFDI/`
- `src/SICOE.Application/UseCases/CFDI/ListarCFDI/`
- `src/SICOE.Application/UseCases/CFDI/DescargarCFDI/`
- `src/SICOE.Application/UseCases/CFDI/DescargarCFDILote/`
- `src/SICOE.Application/UseCases/Conciliacion/ConciliarCFDI/`

#### Services (Infrastructure)
- `src/SICOE.Infrastructure/Services/Sat/SatService.cs`
- `src/SICOE.Infrastructure/Services/Sat/Soap/SatSoapMessageBuilder.cs`
- `src/SICOE.Infrastructure/Services/Sat/Soap/SatSoapHttpClient.cs`
- `src/SICOE.Infrastructure/Services/Sat/Soap/SatSoapResponseParser.cs`
- `src/SICOE.Infrastructure/Services/Sat/Soap/XmlSignatureService.cs`
- `src/SICOE.Infrastructure/Services/Fiel/FielService.cs`
- `src/SICOE.Infrastructure/Services/Fiel/FielValidationService.cs`
- `src/SICOE.Infrastructure/Services/Archivo/ArchivoService.cs`
- `src/SICOE.Infrastructure/Services/TokenSat/TokenSatService.cs`
- `src/SICOE.Infrastructure/Services/Encryption/DataProtectionEncryptionService.cs`

#### Repositories
- `src/SICOE.Infrastructure/Persistence/Repositories/ClienteRepository.cs`
- `src/SICOE.Infrastructure/Persistence/Repositories/SolicitudDescargaRepository.cs`
- `src/SICOE.Infrastructure/Persistence/Repositories/CFDIRepository.cs`
- `src/SICOE.Infrastructure/Persistence/Repositories/ArchivoRepository.cs`
- `src/SICOE.Infrastructure/Persistence/Repositories/TokenSatRepository.cs`
- `src/SICOE.Infrastructure/Persistence/Repositories/ConciliacionCFDIRepository.cs`

#### Entities (Domain)
- `src/SICOE.Domain/Entities/Cliente.cs`
- `src/SICOE.Domain/Entities/SolicitudDescarga.cs`
- `src/SICOE.Domain/Entities/CFDI.cs`
- `src/SICOE.Domain/Entities/Archivo.cs`
- `src/SICOE.Domain/Entities/TokenSat.cs`
- `src/SICOE.Domain/Entities/ConciliacionCFDI.cs`

#### Value Objects
- `src/SICOE.Domain/ValueObjects/RFC.cs`
- `src/SICOE.Domain/ValueObjects/Email.cs`
- `src/SICOE.Domain/ValueObjects/UUID.cs`
- `src/SICOE.Domain/ValueObjects/Monto.cs`

#### Enums
- `src/SICOE.Domain/Enums/EstadoSolicitud.cs`
- `src/SICOE.Domain/Enums/EstatusCFDI.cs`
- `src/SICOE.Domain/Enums/TipoComprobante.cs`
- `src/SICOE.Domain/Enums/TipoArchivo.cs`
- `src/SICOE.Domain/Enums/EstadoConciliacion.cs`

### Frontend (Razor Pages / JavaScript)

#### Pages
- `src/SICOE.Razor/Pages/Index.cshtml` - Página principal de solicitudes
- `src/SICOE.Razor/Pages/Error.cshtml` - Página de errores

#### JavaScript
- `src/SICOE.Razor/wwwroot/js/descarga-handler.js` - Manejo de UI para descargas
- `src/SICOE.Razor/wwwroot/js/fiel-handler.js` - Manejo de carga de FIEL

## Clases Compartidas / Dependencias Externas

### Frameworks y Librerías (.NET)
- **MediatR**: Patrón CQRS y mediador para casos de uso
- **Entity Framework Core**: ORM para acceso a datos
- **FluentValidation**: Validación de comandos y queries
- **Serilog**: Sistema de logging
- **Hangfire**: Procesamiento de trabajos en segundo plano
- **AutoMapper**: Mapeo de objetos

### Servicios Externos
- **SAT Web Services**: Servicios SOAP del SAT de México
  - Autenticación: `https://cfdidescargamasivasolicitud.clouda.sat.gob.mx/Autenticacion/Autenticacion.svc`
  - Solicitud: `https://cfdidescargamasivasolicitud.clouda.sat.gob.mx/SolicitaDescargaService.svc`
  - Verificación: `https://cfdidescargamasivasolicitud.clouda.sat.gob.mx/VerificaSolicitudDescargaService.svc`
  - Descarga: `https://cfdidescargamasiva.clouda.sat.gob.mx/DescargaMasivaService.svc`

## Modelo de Datos (Simplificado)

El sistema SICOE gestiona la descarga masiva de CFDI a través de las siguientes entidades:

### Tablas Principales

- **`Clientes`**: Almacena información de los clientes del sistema (RFC, Razón Social, Email).
- **`SolicitudesDescarga`**: Es la tabla central del sistema. Almacena las solicitudes de descarga masiva, su estado, y los totales de documentos solicitados vs recibidos.
- **`CFDIs`**: Almacena los CFDI procesados con toda su información fiscal (UUID, RFCs, montos, fechas, etc.).
- **`Archivos`**: Almacena la metadata de los archivos XML de CFDI almacenados físicamente.
- **`TokenSat`**: Almacena tokens del SAT encriptados temporalmente (con expiración).
- **`ConciliacionesCFDI`**: Almacena el historial y estado de las conciliaciones entre documentos solicitados vs recibidos.

### Relaciones Clave

- **SolicitudesDescarga → Clientes**: Una solicitud pertenece a un cliente (FK: ClienteId).
- **CFDIs → SolicitudesDescarga**: Múltiples CFDI pertenecen a una solicitud (FK: SolicitudDescargaId, CASCADE DELETE).
- **CFDIs → Archivos**: Un CFDI puede tener un archivo asociado (FK: ArchivoId, SET NULL).
- **TokenSat → Clientes**: Múltiples tokens pueden pertenecer a un cliente (FK: ClienteId, NO ACTION).
- **ConciliacionesCFDI → SolicitudesDescarga**: Una conciliación pertenece a una solicitud (FK: SolicitudDescargaId, NO ACTION).

## Configuración y Seguridad

### Integración con Servicios SAT

La conexión con los servicios SOAP del SAT se configura en `appsettings.json`:

```json
{
  "Sat": {
    "BaseUrl": "https://cfdidescargamasivasolicitud.clouda.sat.gob.mx",
    "DescargaBaseUrl": "https://cfdidescargamasiva.clouda.sat.gob.mx"
  }
}
```

Implementación principal: `SICOE.Infrastructure.Services.Sat.SatService`.

### Seguridad FIEL

**Política de Seguridad:**
- La FIEL NUNCA se almacena en el servidor.
- El usuario captura su FIEL en cada sesión (frontend).
- La FIEL permanece en el navegador (sessionStorage).
- Solo se almacenan tokens SAT (encriptados y con expiración automática).

**Flujo de Seguridad:**
1. Usuario carga FIEL en frontend → Almacenada en `sessionStorage`.
2. Frontend envía FIEL al backend solo cuando es necesario (obtener token, firmar SOAP).
3. Backend usa FIEL temporalmente y la descarta inmediatamente.
4. Token SAT se almacena encriptado en BD con expiración.

### Encriptación de Tokens

Los tokens SAT se encriptan usando **ASP.NET Core Data Protection**:
- Claves almacenadas en: `keys/` (local) o Azure Key Vault (producción).
- Expiración automática de tokens.
- Rotación de claves cada 90 días.

### Roles y Permisos

Actualmente el sistema no implementa roles específicos. Todos los usuarios autenticados pueden:
- Solicitar descargas masivas.
- Consultar sus propias solicitudes (filtradas por RFC/ClienteId).
- Descargar paquetes de sus solicitudes completadas.
- Consultar CFDI procesados.

**Nota:** El sistema valida que las solicitudes pertenezcan al cliente autenticado mediante filtros por RFC o ClienteId.

## Diccionario de Errores Comunes

| Código Error | Descripción | Solución |
| :--- | :--- | :--- |
| `FIEL_INVALIDA` | El certificado FIEL no es válido o está corrupto. | Verificar que los archivos .cer y .key sean válidos y que la contraseña sea correcta. |
| `FIEL_REVOCADA` | El certificado FIEL ha sido revocado por el SAT. | Obtener un nuevo certificado FIEL del SAT. |
| `TOKEN_SAT_EXPIRADO` | El token SAT ha expirado. | Obtener un nuevo token usando la FIEL. |
| `SOLICITUD_NO_ENCONTRADA` | No se encontró la solicitud especificada. | Verificar que el ID de solicitud sea correcto y que pertenezca al cliente autenticado. |
| `NO_HAY_PAQUETES_DISPONIBLES` | La solicitud no tiene paquetes disponibles para descargar. | Verificar que el estado de la solicitud sea "Completada" y que tenga IdsPaquetes asignados. |
| `SOAP_ACTION_NOT_SUPPORTED` | El SOAPAction enviado al SAT no es reconocido. | Verificar la configuración del SOAPAction en `SatService.cs` (debe ser `Descargar`). |
| `SELLO_MAL_FORMADO` | La firma digital del mensaje SOAP es inválida. | Verificar que la FIEL sea válida y que el algoritmo de canonicalización sea C14N Normal. |
| `XML_MAL_FORMADO` | El XML enviado al SAT tiene errores de formato. | Verificar que los atributos estén ordenados alfabéticamente y que el XML sea válido. |
| `PAQUETE_EXPIRADO` | El paquete del SAT ha expirado (72 horas de vida útil). | Crear una nueva solicitud de descarga. |
| `MAXIMO_DESCARGAS_ALCANZADO` | El paquete ya fue descargado el máximo de veces permitido (2 veces). | No es posible descargar este paquete nuevamente. |
| `RFC_NO_COINCIDE` | El RFC del certificado FIEL no coincide con el RFC de la solicitud. | Verificar que se use la FIEL correcta para la solicitud. |

## Consideraciones de Infraestructura

### Almacenamiento de Archivos

Las actas constitutivas (archivos XML de CFDI) se gestionan mediante el `ArchivoService`:
- **Patrón**: Sistema de archivos + Base de datos (homologado con GEDINET).
- **Ubicación física**: Configurada en `appsettings.json` bajo `Archivo:BasePath`.
- **Metadata**: Almacenada en tabla `Archivos` con ruta completa, tamaño, tipo, etc.

### Procesamiento en Segundo Plano

El sistema utiliza **Hangfire** para:
- Verificación periódica del estado de solicitudes.
- Procesamiento automático de paquetes completados.
- Conciliación automática de CFDI.
- Limpieza de tokens expirados.

**Configuración:**
- Dashboard disponible en: `/hangfire`
- Base de datos: SQL Server (tablas Hangfire se crean automáticamente).

### Logging

El sistema utiliza **Serilog** para logging estructurado:
- **Archivos de log**: `logs/sicoe-api-{date}.log`
- **Niveles**: Information, Warning, Error
- **Formato**: JSON estructurado para fácil análisis

### Base de Datos

- **Motor**: SQL Server / Azure SQL Database
- **ORM**: Entity Framework Core
- **Migraciones**: EF Migrations para cambios de esquema
- **Script de creación**: `scripts/CreateDatabase_SICOE.sql`

### Notificaciones

Actualmente el sistema no implementa notificaciones automáticas. Las notificaciones se pueden agregar mediante:
- Hangfire Jobs para verificación periódica.
- Eventos de dominio (`DomainEvent`) para disparar notificaciones.
- Integración con servicios de email (Azure Communication Services, SendGrid, etc.).

## Particularidades del Sistema

### Integración con SAT (Servicios SOAP)

El sistema depende completamente de los servicios SOAP del SAT para:
- Autenticación con tokens WRAP.
- Solicitud de descarga masiva.
- Verificación de estado de solicitudes.
- Descarga de paquetes ZIP.

**Impactos:**
- **Disponibilidad**: Dependencia externa fuera del control del sistema.
- **Latencia**: Los servicios del SAT pueden tener latencia variable.
- **Versionamiento**: Cambios en el contrato SOAP del SAT requieren actualización del código.
- **Credenciales**: Los tokens SAT tienen expiración (típicamente 2 horas).

### Restricciones del SAT

- **Vida útil de paquetes**: Los paquetes tienen 72 horas de vida útil desde su creación.
- **Límite de descargas**: Cada paquete solo puede descargarse máximo 2 veces.
- **Firma digital requerida**: Todos los mensajes SOAP deben estar firmados digitalmente con FIEL.
- **Canonicalización**: El XML debe usar C14N Normal (no Exclusive C14N).

### Arquitectura Clean Architecture

El sistema sigue principios de Clean Architecture:
- **Separación de capas**: Domain → Application → Infrastructure → Presentation
- **Dependency Inversion**: Las capas internas no dependen de las externas.
- **CQRS**: Separación de comandos (escritura) y queries (lectura).
- **SOLID**: Principios aplicados en cada capa.

---

**Última actualización:** 2025-01-22  
**Versión del Sistema:** 2.2  
**Estado:** Desarrollo - Integración con SAT en progreso
