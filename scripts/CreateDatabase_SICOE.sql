-- =============================================
-- Script de Creación de Base de Datos SICOE
-- Sistema de Integración y Control de Operaciones Electrónicas
-- Versión: 2.0
-- Fecha: 2025-01-22
-- =============================================
-- 
-- Este script crea la base de datos completa para SICOE incluyendo:
-- - Tabla Clientes
-- - Tabla SolicitudesDescarga
-- - Tabla Archivos
-- - Tabla CFDIs (con todos los campos)
-- - Tabla TokenSat
-- - Tabla ConciliacionesCFDI
-- - Índices y Foreign Keys
-- 
-- NOTA: Para Azure SQL Database, eliminar las secciones de FILEGROWTH y MAXSIZE
-- =============================================

USE master;
GO

-- Verificar si la base de datos existe y eliminarla si es necesario
IF EXISTS (SELECT name FROM sys.databases WHERE name = 'SICOE')
BEGIN
    PRINT 'Eliminando base de datos SICOE existente...';
    ALTER DATABASE SICOE SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE SICOE;
    PRINT 'Base de datos SICOE eliminada.';
END
GO

-- Crear la base de datos
PRINT 'Creando base de datos SICOE...';
CREATE DATABASE SICOE
ON 
( NAME = 'SICOE_Data',
  FILENAME = 'C:\Sistemas\GEDINET\SICOE\SICOE\Database\SICOE_Data.mdf',
  SIZE = 100MB,
  MAXSIZE = 1GB,
  FILEGROWTH = 10MB )
LOG ON 
( NAME = 'SICOE_Log',
  FILENAME = 'C:\Sistemas\GEDINET\SICOE\SICOE\Database\SICOE_Log.ldf',
  SIZE = 10MB,
  MAXSIZE = 100MB,
  FILEGROWTH = 5MB );
GO

USE SICOE;
GO

-- Configurar opciones de sesión
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

PRINT 'Configurando base de datos SICOE...';
GO

-- =============================================
-- Tabla: Clientes
-- Almacena información de los clientes del sistema
-- =============================================
PRINT 'Creando tabla Clientes...';
CREATE TABLE [dbo].[Clientes] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [RFC] NVARCHAR(13) NOT NULL,
    [RazonSocial] NVARCHAR(500) NOT NULL,
    [Email] NVARCHAR(255) NULL,
    [Activo] BIT NOT NULL DEFAULT 1,
    [FechaCreacion] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [FechaActualizacion] DATETIME2 NULL,
    CONSTRAINT [PK_Clientes] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO

-- Índice único para RFC
CREATE UNIQUE NONCLUSTERED INDEX [IX_Clientes_RFC]
ON [dbo].[Clientes] ([RFC]);
GO

-- =============================================
-- Tabla: SolicitudesDescarga
-- Almacena las solicitudes de descarga masiva de CFDI
-- =============================================
PRINT 'Creando tabla SolicitudesDescarga...';
CREATE TABLE [dbo].[SolicitudesDescarga] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [ClienteId] INT NOT NULL,
    [IdSolicitudSat] NVARCHAR(100) NULL,
    [FechaInicial] DATETIME2 NOT NULL,
    [FechaFinal] DATETIME2 NOT NULL,
    [Estado] INT NOT NULL DEFAULT 1, -- EstadoSolicitud.Pendiente = 1
    [TotalSolicitado] INT NOT NULL DEFAULT 0,
    [TotalRecibido] INT NOT NULL DEFAULT 0,
    [MensajeError] NVARCHAR(1000) NULL,
    [FechaCreacion] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [FechaActualizacion] DATETIME2 NULL,
    CONSTRAINT [PK_SolicitudesDescarga] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_SolicitudesDescarga_Clientes] FOREIGN KEY ([ClienteId])
        REFERENCES [dbo].[Clientes] ([Id])
        ON DELETE NO ACTION
        ON UPDATE NO ACTION
);
GO

-- Índices para SolicitudesDescarga
-- Nota: EF Core crea automáticamente un índice para la foreign key ClienteId
-- Los índices adicionales son optimizaciones para consultas frecuentes
CREATE NONCLUSTERED INDEX [IX_SolicitudesDescarga_ClienteId]
ON [dbo].[SolicitudesDescarga] ([ClienteId]);
GO

CREATE NONCLUSTERED INDEX [IX_SolicitudesDescarga_Estado]
ON [dbo].[SolicitudesDescarga] ([Estado]);
GO

CREATE NONCLUSTERED INDEX [IX_SolicitudesDescarga_IdSolicitudSat]
ON [dbo].[SolicitudesDescarga] ([IdSolicitudSat])
WHERE [IdSolicitudSat] IS NOT NULL;
GO

-- Índice adicional para optimizar consultas por fecha (no requerido por EF)
CREATE NONCLUSTERED INDEX [IX_SolicitudesDescarga_FechaCreacion]
ON [dbo].[SolicitudesDescarga] ([FechaCreacion]);
GO

-- =============================================
-- Tabla: Archivos
-- Almacena información de archivos almacenados en el sistema
-- =============================================
PRINT 'Creando tabla Archivos...';
CREATE TABLE [dbo].[Archivos] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [NombreOriginal] NVARCHAR(500) NOT NULL,
    [NombreAlmacenado] NVARCHAR(500) NOT NULL,
    [RutaCompleta] NVARCHAR(1000) NOT NULL,
    [ContentType] NVARCHAR(100) NOT NULL,
    [TamanioBytes] BIGINT NOT NULL,
    [TipoArchivo] INT NOT NULL,
    [Descripcion] NVARCHAR(1000) NULL,
    [FechaCreacion] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [FechaActualizacion] DATETIME2 NULL,
    CONSTRAINT [PK_Archivos] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO

-- Índice único para RutaCompleta
-- Nota: EF Core crea un índice único simple, sin filtro WHERE
CREATE UNIQUE NONCLUSTERED INDEX [IX_Archivos_RutaCompleta]
ON [dbo].[Archivos] ([RutaCompleta]);
GO

-- =============================================
-- Tabla: CFDIs
-- Almacena información de los Comprobantes Fiscales Digitales por Internet
-- =============================================
PRINT 'Creando tabla CFDIs...';
CREATE TABLE [dbo].[CFDIs] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [SolicitudDescargaId] INT NOT NULL,
    [UUID] NVARCHAR(36) NOT NULL,
    [RfcEmisor] NVARCHAR(13) NOT NULL,
    [RfcReceptor] NVARCHAR(13) NOT NULL,
    [FechaEmision] DATETIME2 NOT NULL,
    [FechaTimbrado] DATETIME2 NULL,
    [Total] DECIMAL(18,2) NOT NULL,
    [Moneda] NVARCHAR(3) NOT NULL DEFAULT 'MXN',
    [SubTotal] DECIMAL(18,2) NOT NULL DEFAULT 0,
    [TotalImpuestosTrasladados] DECIMAL(18,2) NOT NULL DEFAULT 0,
    [TipoComprobante] INT NOT NULL,
    [Estatus] INT NOT NULL DEFAULT 1, -- EstatusCFDI.Vigente = 1
    [Serie] NVARCHAR(25) NULL,
    [Folio] NVARCHAR(40) NULL,
    [NombreEmisor] NVARCHAR(300) NULL,
    [NombreReceptor] NVARCHAR(300) NULL,
    [RegimenFiscalEmisor] NVARCHAR(10) NULL,
    [RegimenFiscalReceptor] NVARCHAR(10) NULL,
    [DomicilioFiscalReceptor] NVARCHAR(5) NULL,
    [UsoCFDI] NVARCHAR(3) NULL,
    [FormaPago] NVARCHAR(2) NULL,
    [MetodoPago] NVARCHAR(3) NULL,
    [LugarExpedicion] NVARCHAR(5) NULL,
    [ArchivoId] INT NULL,
    [FechaCreacion] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [FechaActualizacion] DATETIME2 NULL,
    CONSTRAINT [PK_CFDIs] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_CFDIs_SolicitudesDescarga] FOREIGN KEY ([SolicitudDescargaId])
        REFERENCES [dbo].[SolicitudesDescarga] ([Id])
        ON DELETE CASCADE
        ON UPDATE NO ACTION,
    CONSTRAINT [FK_CFDIs_Archivos] FOREIGN KEY ([ArchivoId])
        REFERENCES [dbo].[Archivos] ([Id])
        ON DELETE SET NULL
        ON UPDATE NO ACTION
);
GO

-- Índice único para UUID
CREATE UNIQUE NONCLUSTERED INDEX [IX_CFDIs_UUID]
ON [dbo].[CFDIs] ([UUID]);
GO

-- Índices adicionales para CFDIs
CREATE NONCLUSTERED INDEX [IX_CFDIs_SolicitudDescargaId]
ON [dbo].[CFDIs] ([SolicitudDescargaId]);
GO

CREATE NONCLUSTERED INDEX [IX_CFDIs_RfcEmisor]
ON [dbo].[CFDIs] ([RfcEmisor]);
GO

CREATE NONCLUSTERED INDEX [IX_CFDIs_RfcReceptor]
ON [dbo].[CFDIs] ([RfcReceptor]);
GO

CREATE NONCLUSTERED INDEX [IX_CFDIs_FechaEmision]
ON [dbo].[CFDIs] ([FechaEmision]);
GO

-- Índice adicional para optimizar consultas por estatus (no requerido por EF)
CREATE NONCLUSTERED INDEX [IX_CFDIs_Estatus]
ON [dbo].[CFDIs] ([Estatus]);
GO

-- =============================================
-- Tabla: TokenSat
-- Almacena tokens del SAT temporalmente (encriptados)
-- Los tokens tienen expiración y se eliminan automáticamente
-- =============================================
PRINT 'Creando tabla TokenSat...';
CREATE TABLE [dbo].[TokenSat] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [ClienteId] INT NOT NULL,
    [Token] NVARCHAR(1000) NOT NULL, -- Token encriptado
    [FechaCreacion] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [FechaExpiracion] DATETIME2 NOT NULL,
    [Activo] BIT NOT NULL DEFAULT 1,
    CONSTRAINT [PK_TokenSat] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_TokenSat_Clientes] FOREIGN KEY ([ClienteId]) 
        REFERENCES [dbo].[Clientes] ([Id]) 
        ON DELETE NO ACTION
        ON UPDATE NO ACTION
);
GO

-- Índices para TokenSat
CREATE NONCLUSTERED INDEX [IX_TokenSat_ClienteId]
ON [dbo].[TokenSat] ([ClienteId]);
GO

CREATE NONCLUSTERED INDEX [IX_TokenSat_ClienteId_Activo_Expiracion]
ON [dbo].[TokenSat] ([ClienteId], [Activo], [FechaExpiracion]);
GO

CREATE NONCLUSTERED INDEX [IX_TokenSat_FechaExpiracion]
ON [dbo].[TokenSat] ([FechaExpiracion]);
GO

-- =============================================
-- Tabla: ConciliacionesCFDI
-- Almacena información de conciliaciones entre CFDI solicitados vs recibidos
-- =============================================
PRINT 'Creando tabla ConciliacionesCFDI...';
CREATE TABLE [dbo].[ConciliacionesCFDI] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [SolicitudDescargaId] INT NOT NULL,
    [Estado] INT NOT NULL DEFAULT 0, -- EstadoConciliacion.Pendiente = 0
    [TotalSolicitado] INT NOT NULL,
    [TotalRecibido] INT NOT NULL,
    [TotalFaltantes] INT NOT NULL,
    [NuevaSolicitudId] INT NULL, -- ID de la nueva solicitud creada para faltantes
    [IntentosReconciliacion] INT NOT NULL DEFAULT 0,
    [MensajeError] NVARCHAR(1000) NULL,
    [FechaCreacion] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [FechaActualizacion] DATETIME2 NULL,
    [FechaCompletada] DATETIME2 NULL,
    CONSTRAINT [PK_ConciliacionesCFDI] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_ConciliacionesCFDI_SolicitudDescarga] FOREIGN KEY ([SolicitudDescargaId])
        REFERENCES [dbo].[SolicitudesDescarga] ([Id])
        ON DELETE NO ACTION
        ON UPDATE NO ACTION,
    CONSTRAINT [FK_ConciliacionesCFDI_NuevaSolicitud] FOREIGN KEY ([NuevaSolicitudId])
        REFERENCES [dbo].[SolicitudesDescarga] ([Id])
        ON DELETE SET NULL
        ON UPDATE NO ACTION
);
GO

-- Índices para ConciliacionesCFDI
CREATE NONCLUSTERED INDEX [IX_ConciliacionesCFDI_SolicitudDescargaId]
ON [dbo].[ConciliacionesCFDI] ([SolicitudDescargaId]);
GO

CREATE NONCLUSTERED INDEX [IX_ConciliacionesCFDI_Estado]
ON [dbo].[ConciliacionesCFDI] ([Estado]);
GO

CREATE NONCLUSTERED INDEX [IX_ConciliacionesCFDI_Estado_Intentos]
ON [dbo].[ConciliacionesCFDI] ([Estado], [IntentosReconciliacion]);
GO

-- =============================================
-- Catálogos de Referencia: Valores de Enums
-- =============================================
-- NOTA: SICOE utiliza enums de C# almacenados como INT en la base de datos.
-- Estos valores son fijos y están definidos en el código. Esta sección sirve
-- como referencia para consultas y reportes.

/*
==============================================
ESTADO SOLICITUD (SolicitudesDescarga.Estado)
==============================================
Valor | Descripción
------|------------
  1   | Pendiente    - Solicitud creada, esperando procesamiento
  2   | EnProceso    - Solicitud siendo procesada por el SAT
  3   | Completada   - Solicitud completada exitosamente
  4   | Error        - Error al procesar la solicitud
  5   | Cancelada    - Solicitud cancelada manualmente

==============================================
TIPO COMPROBANTE (CFDIs.TipoComprobante)
==============================================
Valor | Descripción
------|------------
  1   | Ingreso      - Comprobante de ingreso
  2   | Egreso       - Comprobante de egreso
  3   | Traslado     - Comprobante de traslado
  4   | Pago         - Comprobante de pago
  5   | Nomina       - Comprobante de nómina
 99   | Otro         - Otro tipo de comprobante

==============================================
ESTATUS CFDI (CFDIs.Estatus)
==============================================
Valor | Descripción
------|------------
  1   | Vigente      - CFDI vigente y válido
  2   | Cancelado    - CFDI cancelado
  3   | NoEncontrado - CFDI no encontrado en el SAT

==============================================
TIPO ARCHIVO (Archivos.TipoArchivo)
==============================================
Valor | Descripción
------|------------
  1   | CFDI         - Archivos XML de CFDI
  2   | Reporte      - Reportes (PDF, XLSX, CSV)
  3   | Log          - Archivos de log
  4   | XML          - Archivos XML genéricos
  5   | PDF          - Archivos PDF
  6   | XLSX         - Archivos Excel
  7   | CSV          - Archivos CSV
 99   | Otro         - Otros tipos de archivo

==============================================
ESTADO CONCILIACIÓN (ConciliacionesCFDI.Estado)
==============================================
Valor | Descripción
------|------------
  0   | Pendiente    - Pendiente de conciliación
  1   | EnProceso    - Conciliación en curso
  2   | Completada    - Conciliación completada (todo coincide)
  3   | ConFaltantes  - Hay faltantes detectados
  4   | Error         - Error en la conciliación
  5   | Cancelada     - Conciliación cancelada
*/

-- =============================================
-- Tablas de Hangfire (Message Queue)
-- =============================================
-- NOTA: Hangfire creará sus propias tablas automáticamente al inicializarse
-- Estas tablas se crean mediante el método UseSqlServerStorage() en la configuración
-- Las tablas incluyen: HangFire.Server, HangFire.Job, HangFire.State, etc.
-- No es necesario crearlas manualmente en este script

-- =============================================
-- Datos Iniciales para Poblar el Sistema
-- =============================================
-- NOTA: Estos datos son necesarios para que el sistema funcione correctamente
-- desde el inicio. Se recomienda mantener al menos un cliente activo.

PRINT 'Insertando datos iniciales...';
GO

-- =============================================
-- Cliente de Ejemplo
-- =============================================
-- Este cliente es necesario para poder crear solicitudes de descarga
-- Puedes agregar más clientes según sea necesario
PRINT 'Insertando cliente de ejemplo...';
INSERT INTO [dbo].[Clientes] ([RFC], [RazonSocial], [Email], [Activo], [FechaCreacion])
VALUES 
    ('XAXX010101000', 'Cliente de Prueba SICOE', 'prueba@sicoe.example.com', 1, GETUTCDATE());
GO

PRINT 'Datos iniciales insertados correctamente.';
PRINT 'Cliente de ejemplo creado: XAXX010101000 - Cliente de Prueba SICOE';
PRINT '';
PRINT 'NOTA: Para agregar más clientes, usa el siguiente formato:';
PRINT 'INSERT INTO [dbo].[Clientes] ([RFC], [RazonSocial], [Email], [Activo], [FechaCreacion])';
PRINT 'VALUES (''RFC12345678901'', ''Razón Social'', ''email@example.com'', 1, GETUTCDATE());';
GO

-- =============================================
-- Verificación Final
-- =============================================
PRINT '';
PRINT '=============================================';
PRINT 'Base de datos SICOE creada exitosamente.';
PRINT '=============================================';
PRINT '';
PRINT 'Tablas creadas:';
PRINT '  - Clientes';
PRINT '  - SolicitudesDescarga';
PRINT '  - Archivos';
PRINT '  - CFDIs';
PRINT '  - TokenSat';
PRINT '  - ConciliacionesCFDI';
PRINT '';
PRINT 'NOTA: Las tablas de Hangfire se crearán automáticamente al inicializar la aplicación.';
PRINT '';
GO
