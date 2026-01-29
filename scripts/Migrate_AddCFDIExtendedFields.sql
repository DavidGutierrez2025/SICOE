USE SICOE;
GO

-- Script para agregar campos adicionales a la tabla CFDIs
-- Migración: AddCFDIExtendedFields
-- Fecha: 2025-01-01

SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
GO

BEGIN TRANSACTION;

BEGIN TRY
    PRINT 'Iniciando migración: AddCFDIExtendedFields';
    PRINT 'Agregando campos adicionales a la tabla CFDIs...';
    PRINT '';

    -- Verificar si las columnas ya existen antes de agregarlas
    -- SubTotal
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.CFDIs') AND name = 'SubTotal')
    BEGIN
        ALTER TABLE [dbo].[CFDIs]
        ADD [SubTotal] DECIMAL(18,2) NOT NULL DEFAULT 0;
        PRINT '✓ Columna SubTotal agregada';
    END
    ELSE
        PRINT '⚠ Columna SubTotal ya existe, omitiendo...';

    -- TotalImpuestosTrasladados
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.CFDIs') AND name = 'TotalImpuestosTrasladados')
    BEGIN
        ALTER TABLE [dbo].[CFDIs]
        ADD [TotalImpuestosTrasladados] DECIMAL(18,2) NOT NULL DEFAULT 0;
        PRINT '✓ Columna TotalImpuestosTrasladados agregada';
    END
    ELSE
        PRINT '⚠ Columna TotalImpuestosTrasladados ya existe, omitiendo...';

    -- FechaTimbrado
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.CFDIs') AND name = 'FechaTimbrado')
    BEGIN
        ALTER TABLE [dbo].[CFDIs]
        ADD [FechaTimbrado] DATETIME2 NULL;
        PRINT '✓ Columna FechaTimbrado agregada';
    END
    ELSE
        PRINT '⚠ Columna FechaTimbrado ya existe, omitiendo...';

    -- Serie
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.CFDIs') AND name = 'Serie')
    BEGIN
        ALTER TABLE [dbo].[CFDIs]
        ADD [Serie] NVARCHAR(25) NULL;
        PRINT '✓ Columna Serie agregada';
    END
    ELSE
        PRINT '⚠ Columna Serie ya existe, omitiendo...';

    -- Folio
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.CFDIs') AND name = 'Folio')
    BEGIN
        ALTER TABLE [dbo].[CFDIs]
        ADD [Folio] NVARCHAR(40) NULL;
        PRINT '✓ Columna Folio agregada';
    END
    ELSE
        PRINT '⚠ Columna Folio ya existe, omitiendo...';

    -- NombreEmisor
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.CFDIs') AND name = 'NombreEmisor')
    BEGIN
        ALTER TABLE [dbo].[CFDIs]
        ADD [NombreEmisor] NVARCHAR(300) NULL;
        PRINT '✓ Columna NombreEmisor agregada';
    END
    ELSE
        PRINT '⚠ Columna NombreEmisor ya existe, omitiendo...';

    -- NombreReceptor
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.CFDIs') AND name = 'NombreReceptor')
    BEGIN
        ALTER TABLE [dbo].[CFDIs]
        ADD [NombreReceptor] NVARCHAR(300) NULL;
        PRINT '✓ Columna NombreReceptor agregada';
    END
    ELSE
        PRINT '⚠ Columna NombreReceptor ya existe, omitiendo...';

    -- RegimenFiscalEmisor
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.CFDIs') AND name = 'RegimenFiscalEmisor')
    BEGIN
        ALTER TABLE [dbo].[CFDIs]
        ADD [RegimenFiscalEmisor] NVARCHAR(10) NULL;
        PRINT '✓ Columna RegimenFiscalEmisor agregada';
    END
    ELSE
        PRINT '⚠ Columna RegimenFiscalEmisor ya existe, omitiendo...';

    -- RegimenFiscalReceptor
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.CFDIs') AND name = 'RegimenFiscalReceptor')
    BEGIN
        ALTER TABLE [dbo].[CFDIs]
        ADD [RegimenFiscalReceptor] NVARCHAR(10) NULL;
        PRINT '✓ Columna RegimenFiscalReceptor agregada';
    END
    ELSE
        PRINT '⚠ Columna RegimenFiscalReceptor ya existe, omitiendo...';

    -- DomicilioFiscalReceptor
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.CFDIs') AND name = 'DomicilioFiscalReceptor')
    BEGIN
        ALTER TABLE [dbo].[CFDIs]
        ADD [DomicilioFiscalReceptor] NVARCHAR(5) NULL;
        PRINT '✓ Columna DomicilioFiscalReceptor agregada';
    END
    ELSE
        PRINT '⚠ Columna DomicilioFiscalReceptor ya existe, omitiendo...';

    -- UsoCFDI
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.CFDIs') AND name = 'UsoCFDI')
    BEGIN
        ALTER TABLE [dbo].[CFDIs]
        ADD [UsoCFDI] NVARCHAR(3) NULL;
        PRINT '✓ Columna UsoCFDI agregada';
    END
    ELSE
        PRINT '⚠ Columna UsoCFDI ya existe, omitiendo...';

    -- FormaPago
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.CFDIs') AND name = 'FormaPago')
    BEGIN
        ALTER TABLE [dbo].[CFDIs]
        ADD [FormaPago] NVARCHAR(2) NULL;
        PRINT '✓ Columna FormaPago agregada';
    END
    ELSE
        PRINT '⚠ Columna FormaPago ya existe, omitiendo...';

    -- MetodoPago
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.CFDIs') AND name = 'MetodoPago')
    BEGIN
        ALTER TABLE [dbo].[CFDIs]
        ADD [MetodoPago] NVARCHAR(3) NULL;
        PRINT '✓ Columna MetodoPago agregada';
    END
    ELSE
        PRINT '⚠ Columna MetodoPago ya existe, omitiendo...';

    -- LugarExpedicion
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.CFDIs') AND name = 'LugarExpedicion')
    BEGIN
        ALTER TABLE [dbo].[CFDIs]
        ADD [LugarExpedicion] NVARCHAR(5) NULL;
        PRINT '✓ Columna LugarExpedicion agregada';
    END
    ELSE
        PRINT '⚠ Columna LugarExpedicion ya existe, omitiendo...';

    PRINT '';
    PRINT '=============================================';
    PRINT 'Migración completada exitosamente.';
    PRINT 'Campos adicionales agregados a la tabla CFDIs.';
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

