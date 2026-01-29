USE SICOE;
GO

-- Script para crear la tabla ConciliacionesCFDI
-- Migración: AddConciliacionesCFDITable
-- Fecha: 2025-01-01

SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
GO

BEGIN TRANSACTION;

BEGIN TRY
    PRINT 'Iniciando migración: AddConciliacionesCFDITable';
    PRINT 'Creando tabla ConciliacionesCFDI...';
    PRINT '';

    -- Crear tabla si no existe
    IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID('dbo.ConciliacionesCFDI'))
    BEGIN
        CREATE TABLE [dbo].[ConciliacionesCFDI] (
            [Id] INT IDENTITY(1,1) NOT NULL,
            [SolicitudDescargaId] INT NOT NULL,
            [Estado] INT NOT NULL,
            [TotalSolicitado] INT NOT NULL,
            [TotalRecibido] INT NOT NULL,
            [TotalFaltantes] INT NOT NULL,
            [NuevaSolicitudId] INT NULL,
            [IntentosReconciliacion] INT NOT NULL DEFAULT 0,
            [MensajeError] NVARCHAR(1000) NULL,
            [FechaCreacion] DATETIME2 NOT NULL,
            [FechaActualizacion] DATETIME2 NULL,
            [FechaCompletada] DATETIME2 NULL,
            CONSTRAINT [PK_ConciliacionesCFDI] PRIMARY KEY CLUSTERED ([Id] ASC),
            CONSTRAINT [FK_ConciliacionesCFDI_SolicitudesDescarga] FOREIGN KEY ([SolicitudDescargaId]) 
                REFERENCES [dbo].[SolicitudesDescarga] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION,
            CONSTRAINT [FK_ConciliacionesCFDI_NuevaSolicitud] FOREIGN KEY ([NuevaSolicitudId]) 
                REFERENCES [dbo].[SolicitudesDescarga] ([Id]) ON DELETE SET NULL ON UPDATE NO ACTION
        );

        PRINT '✓ Tabla ConciliacionesCFDI creada';
    END
    ELSE
    BEGIN
        PRINT '⚠ Tabla ConciliacionesCFDI ya existe, omitiendo creación...';
    END

    -- Crear índices
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.ConciliacionesCFDI') AND name = 'IX_ConciliacionesCFDI_SolicitudDescargaId')
    BEGIN
        CREATE NONCLUSTERED INDEX [IX_ConciliacionesCFDI_SolicitudDescargaId]
        ON [dbo].[ConciliacionesCFDI] ([SolicitudDescargaId] ASC);
        PRINT '✓ Índice IX_ConciliacionesCFDI_SolicitudDescargaId creado';
    END
    ELSE
        PRINT '⚠ Índice IX_ConciliacionesCFDI_SolicitudDescargaId ya existe, omitiendo...';

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.ConciliacionesCFDI') AND name = 'IX_ConciliacionesCFDI_Estado')
    BEGIN
        CREATE NONCLUSTERED INDEX [IX_ConciliacionesCFDI_Estado]
        ON [dbo].[ConciliacionesCFDI] ([Estado] ASC);
        PRINT '✓ Índice IX_ConciliacionesCFDI_Estado creado';
    END
    ELSE
        PRINT '⚠ Índice IX_ConciliacionesCFDI_Estado ya existe, omitiendo...';

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.ConciliacionesCFDI') AND name = 'IX_ConciliacionesCFDI_Estado_Intentos')
    BEGIN
        CREATE NONCLUSTERED INDEX [IX_ConciliacionesCFDI_Estado_Intentos]
        ON [dbo].[ConciliacionesCFDI] ([Estado] ASC, [IntentosReconciliacion] ASC);
        PRINT '✓ Índice IX_ConciliacionesCFDI_Estado_Intentos creado';
    END
    ELSE
        PRINT '⚠ Índice IX_ConciliacionesCFDI_Estado_Intentos ya existe, omitiendo...';

    PRINT '';
    PRINT '=============================================';
    PRINT 'Migración completada exitosamente.';
    PRINT 'Tabla ConciliacionesCFDI creada con índices.';
    PRINT '=============================================';

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT '';
    PRINT 'Error durante la migración. Se ha revertido la transacción.';
    PRINT 'Mensaje de error: ' + ERROR_MESSAGE();
    THROW;
END CATCH;
GO

