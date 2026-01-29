# Scripts SQL para Base de Datos SICOE

## 📋 Descripción

Este directorio contiene los scripts SQL necesarios para crear y gestionar la base de datos **SICOE** (Sistema de Integración y Control de Operaciones Electrónicas).

## 📁 Archivos

### 1. `CreateDatabase_SICOE.sql`
Script principal para crear la base de datos completa con todas las tablas, índices y relaciones.

**Contenido:**
- Creación de la base de datos SICOE
- Tabla `Clientes`
- Tabla `SolicitudesDescarga`
- Tabla `Archivos`
- Tabla `CFDIs` (con todos los campos completos)
- Tabla `TokenSat`
- Tabla `ConciliacionesCFDI`
- Índices y Foreign Keys
- Comentarios sobre enums
- Valores por defecto y constraints

**Uso:**
```sql
-- Ejecutar en SQL Server Management Studio o Azure Data Studio
-- Asegúrate de tener permisos de administrador
```

### 2. `DropDatabase_SICOE.sql`
Script para eliminar la base de datos SICOE (útil para desarrollo y pruebas).

**Uso:**
```sql
-- Ejecutar cuando necesites eliminar y recrear la base de datos
-- ⚠️ ADVERTENCIA: Esto eliminará todos los datos
```

## 🗄️ Estructura de la Base de Datos

### Tablas Principales

#### 1. Clientes
- **Id** (PK, Identity)
- **RFC** (NVARCHAR(13), NOT NULL, UNIQUE)
- **RazonSocial** (NVARCHAR(500), NOT NULL)
- **Email** (NVARCHAR(255), NULL)
- **Activo** (BIT, DEFAULT 1)
- **FechaCreacion** (DATETIME2, NOT NULL)
- **FechaActualizacion** (DATETIME2, NULL)

#### 2. SolicitudesDescarga
- **Id** (PK, Identity)
- **ClienteId** (FK → Clientes)
- **IdSolicitudSat** (NVARCHAR(100), NULL)
- **FechaInicial** (DATETIME2, NOT NULL)
- **FechaFinal** (DATETIME2, NOT NULL)
- **Estado** (INT, NOT NULL) - Enum: EstadoSolicitud
- **TotalSolicitado** (INT, DEFAULT 0)
- **TotalRecibido** (INT, DEFAULT 0)
- **MensajeError** (NVARCHAR(1000), NULL)
- **FechaCreacion** (DATETIME2, NOT NULL)
- **FechaActualizacion** (DATETIME2, NULL)

#### 3. Archivos
- **Id** (PK, Identity)
- **NombreOriginal** (NVARCHAR(500), NOT NULL)
- **NombreAlmacenado** (NVARCHAR(500), NOT NULL)
- **RutaCompleta** (NVARCHAR(1000), NOT NULL, UNIQUE)
- **ContentType** (NVARCHAR(100), NOT NULL)
- **TamanioBytes** (BIGINT, NOT NULL)
- **TipoArchivo** (INT, NOT NULL) - Enum: TipoArchivo
- **Descripcion** (NVARCHAR(1000), NULL)
- **FechaCreacion** (DATETIME2, NOT NULL)
- **FechaActualizacion** (DATETIME2, NULL)

#### 4. CFDIs
- **Id** (PK, Identity)
- **SolicitudDescargaId** (FK → SolicitudesDescarga, CASCADE DELETE)
- **UUID** (NVARCHAR(36), NOT NULL, UNIQUE)
- **RfcEmisor** (NVARCHAR(13), NOT NULL)
- **RfcReceptor** (NVARCHAR(13), NOT NULL)
- **FechaEmision** (DATETIME2, NOT NULL)
- **FechaTimbrado** (DATETIME2, NULL)
- **Total** (DECIMAL(18,2), NOT NULL)
- **Moneda** (NVARCHAR(3), DEFAULT 'MXN')
- **SubTotal** (DECIMAL(18,2), DEFAULT 0)
- **TotalImpuestosTrasladados** (DECIMAL(18,2), DEFAULT 0)
- **TipoComprobante** (INT, NOT NULL) - Enum: TipoComprobante
- **Estatus** (INT, NOT NULL, DEFAULT 1) - Enum: EstatusCFDI
- **Serie** (NVARCHAR(25), NULL)
- **Folio** (NVARCHAR(40), NULL)
- **NombreEmisor** (NVARCHAR(300), NULL)
- **NombreReceptor** (NVARCHAR(300), NULL)
- **RegimenFiscalEmisor** (NVARCHAR(10), NULL)
- **RegimenFiscalReceptor** (NVARCHAR(10), NULL)
- **DomicilioFiscalReceptor** (NVARCHAR(5), NULL)
- **UsoCFDI** (NVARCHAR(3), NULL)
- **FormaPago** (NVARCHAR(2), NULL)
- **MetodoPago** (NVARCHAR(3), NULL)
- **LugarExpedicion** (NVARCHAR(5), NULL)
- **ArchivoId** (FK → Archivos, SET NULL)
- **FechaCreacion** (DATETIME2, NOT NULL)
- **FechaActualizacion** (DATETIME2, NULL)

#### 5. TokenSat
- **Id** (PK, Identity)
- **ClienteId** (FK → Clientes, NO ACTION)
- **Token** (NVARCHAR(1000), NOT NULL) - Token encriptado
- **FechaCreacion** (DATETIME2, NOT NULL)
- **FechaExpiracion** (DATETIME2, NOT NULL)
- **Activo** (BIT, DEFAULT 1)

#### 6. ConciliacionesCFDI
- **Id** (PK, Identity)
- **SolicitudDescargaId** (FK → SolicitudesDescarga, NO ACTION)
- **Estado** (INT, NOT NULL, DEFAULT 0) - Enum: EstadoConciliacion
- **TotalSolicitado** (INT, NOT NULL)
- **TotalRecibido** (INT, NOT NULL)
- **TotalFaltantes** (INT, NOT NULL)
- **NuevaSolicitudId** (FK → SolicitudesDescarga, SET NULL, NULL)
- **IntentosReconciliacion** (INT, NOT NULL, DEFAULT 0)
- **MensajeError** (NVARCHAR(1000), NULL)
- **FechaCreacion** (DATETIME2, NOT NULL)
- **FechaActualizacion** (DATETIME2, NULL)
- **FechaCompletada** (DATETIME2, NULL)

### Índices

#### Clientes
- `IX_Clientes_RFC` (UNIQUE) - Sobre RFC

#### SolicitudesDescarga
- `IX_SolicitudesDescarga_ClienteId` - Sobre ClienteId
- `IX_SolicitudesDescarga_Estado` - Sobre Estado
- `IX_SolicitudesDescarga_IdSolicitudSat` - Sobre IdSolicitudSat (filtrado)

#### Archivos
- `IX_Archivos_RutaCompleta` (UNIQUE) - Sobre RutaCompleta

#### CFDIs
- `IX_CFDIs_UUID` (UNIQUE) - Sobre UUID
- `IX_CFDIs_SolicitudDescargaId` - Sobre SolicitudDescargaId
- `IX_CFDIs_RfcEmisor` - Sobre RfcEmisor
- `IX_CFDIs_RfcReceptor` - Sobre RfcReceptor
- `IX_CFDIs_FechaEmision` - Sobre FechaEmision
- `IX_CFDIs_Estatus` - Sobre Estatus

#### TokenSat
- `IX_TokenSat_ClienteId` - Sobre ClienteId
- `IX_TokenSat_ClienteId_Activo_Expiracion` - Compuesto (ClienteId, Activo, FechaExpiracion)
- `IX_TokenSat_FechaExpiracion` - Sobre FechaExpiracion

#### ConciliacionesCFDI
- `IX_ConciliacionesCFDI_SolicitudDescargaId` - Sobre SolicitudDescargaId
- `IX_ConciliacionesCFDI_Estado` - Sobre Estado
- `IX_ConciliacionesCFDI_Estado_Intentos` - Compuesto (Estado, IntentosReconciliacion)

### Relaciones (Foreign Keys)

1. **SolicitudesDescarga → Clientes**
   - `FK_SolicitudesDescarga_Clientes`
   - ON DELETE NO ACTION
   - ON UPDATE NO ACTION

2. **CFDIs → SolicitudesDescarga**
   - `FK_CFDIs_SolicitudesDescarga`
   - ON DELETE CASCADE
   - ON UPDATE NO ACTION

3. **CFDIs → Archivos**
   - `FK_CFDIs_Archivos`
   - ON DELETE SET NULL
   - ON UPDATE NO ACTION

4. **TokenSat → Clientes**
   - `FK_TokenSat_Clientes`
   - ON DELETE NO ACTION
   - ON UPDATE NO ACTION

5. **ConciliacionesCFDI → SolicitudesDescarga** (SolicitudDescargaId)
   - `FK_ConciliacionesCFDI_SolicitudDescarga`
   - ON DELETE NO ACTION
   - ON UPDATE NO ACTION

6. **ConciliacionesCFDI → SolicitudesDescarga** (NuevaSolicitudId)
   - `FK_ConciliacionesCFDI_NuevaSolicitud`
   - ON DELETE SET NULL
   - ON UPDATE NO ACTION

## 🔧 Configuración Local

### Ubicación de Archivos de Base de Datos

Por defecto, los archivos se crean en:
```
C:\Sistemas\GEDINET\SICOE\SICOE\Database\
```

**Nota:** Si esta ruta no existe, créala antes de ejecutar el script, o modifica las rutas en el script según tu preferencia.

### Connection String

Para desarrollo local, usa:
```json
{
  "ConnectionStrings": {
    "SICOEConnectionString": "Server=(localdb)\\mssqllocaldb;Database=SICOE;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

O para SQL Server Express:
```json
{
  "ConnectionStrings": {
    "SICOEConnectionString": "Server=.\\SQLEXPRESS;Database=SICOE;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

## 📝 Notas Importantes

### Hangfire Tables

Las tablas de Hangfire se crean automáticamente cuando la aplicación se ejecuta por primera vez y se configura Hangfire con:
```csharp
services.AddHangfire(config => config
    .UseSqlServerStorage(connectionString));
```

No es necesario crear estas tablas manualmente.

### Value Objects

Los Value Objects del dominio (RFC, Email, UUID, Monto) se mapean como columnas simples en las tablas:
- `RFC` → Columna `RFC` (NVARCHAR)
- `Email` → Columna `Email` (NVARCHAR)
- `UUID` → Columna `UUID` (NVARCHAR)
- `Monto` → Columnas `Total` (DECIMAL) y `Moneda` (NVARCHAR)

### Enums

Los enums de C# se almacenan como INT en la base de datos. Ver comentarios en el script para los valores correspondientes.

## 🚀 Pasos para Crear la Base de Datos

1. **Crear el directorio de base de datos** (si no existe):
   ```powershell
   New-Item -ItemType Directory -Path "C:\Sistemas\GEDINET\SICOE\SICOE\Database" -Force
   ```

2. **Abrir SQL Server Management Studio** o Azure Data Studio

3. **Conectarse al servidor local** (LocalDB, SQL Express, o SQL Server)

4. **Ejecutar el script** `CreateDatabase_SICOE.sql`

5. **Verificar la creación**:
   ```sql
   USE SICOE;
   SELECT * FROM INFORMATION_SCHEMA.TABLES;
   ```

## 🔄 Migraciones con Entity Framework

Una vez creada la base de datos, puedes usar Entity Framework Migrations para futuras actualizaciones:

```powershell
# Crear una migración
dotnet ef migrations add NombreMigracion --project src/SICOE.Infrastructure --startup-project src/SICOE.API

# Aplicar migraciones
dotnet ef database update --project src/SICOE.Infrastructure --startup-project src/SICOE.API
```

**Nota:** Para usar EF Migrations, necesitas instalar las herramientas:
```powershell
dotnet tool install --global dotnet-ef
```

## ⚠️ Advertencias

- **Desarrollo:** Estos scripts están diseñados para desarrollo local
- **Producción:** Para Azure SQL Database, ajusta las rutas de archivos y usa scripts de migración
- **Backup:** Siempre haz backup antes de ejecutar scripts de eliminación
- **Permisos:** Asegúrate de tener permisos de administrador en SQL Server

---

**Última actualización:** 2025-01-22  
**Versión:** 2.0

