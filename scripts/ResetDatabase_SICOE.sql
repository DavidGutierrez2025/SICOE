-- =============================================
-- Script de Reset de Base de Datos SICOE
-- Elimina todos los registros y reinicia los autoincrementales
-- ADVERTENCIA: Este script elimina TODOS los datos de la base de datos
-- =============================================

USE SICOE;
GO

-- Configurar opciones de sesión necesarias
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
SET NOCOUNT ON;
GO

BEGIN TRANSACTION;

BEGIN TRY
    PRINT 'Iniciando limpieza de datos...';
    
    -- =============================================
    -- 1. Eliminar registros respetando foreign keys
    -- Orden: De las tablas hijas a las tablas padre
    -- =============================================
    
    -- 1.1. TokenSat (depende de Clientes)
    PRINT 'Eliminando registros de TokenSat...';
    DELETE FROM [dbo].[TokenSat];
    DBCC CHECKIDENT ('[dbo].[TokenSat]', RESEED, 0);
    PRINT 'TokenSat limpiado y autoincremental reiniciado.';
    
    -- 1.2. CFDIs (depende de SolicitudesDescarga y Archivos)
    PRINT 'Eliminando registros de CFDIs...';
    DELETE FROM [dbo].[CFDIs];
    DBCC CHECKIDENT ('[dbo].[CFDIs]', RESEED, 0);
    PRINT 'CFDIs limpiado y autoincremental reiniciado.';
    
    -- 1.3. SolicitudesDescarga (depende de Clientes)
    PRINT 'Eliminando registros de SolicitudesDescarga...';
    DELETE FROM [dbo].[SolicitudesDescarga];
    DBCC CHECKIDENT ('[dbo].[SolicitudesDescarga]', RESEED, 0);
    PRINT 'SolicitudesDescarga limpiado y autoincremental reiniciado.';
    
    -- 1.4. Archivos (no depende de otras tablas del dominio)
    PRINT 'Eliminando registros de Archivos...';
    DELETE FROM [dbo].[Archivos];
    DBCC CHECKIDENT ('[dbo].[Archivos]', RESEED, 0);
    PRINT 'Archivos limpiado y autoincremental reiniciado.';
    
    -- 1.5. Clientes (tabla raíz, sin dependencias de otras tablas del dominio)
    PRINT 'Eliminando registros de Clientes...';
    DELETE FROM [dbo].[Clientes];
    DBCC CHECKIDENT ('[dbo].[Clientes]', RESEED, 0);
    PRINT 'Clientes limpiado y autoincremental reiniciado.';
    
    -- =============================================
    -- 2. Verificar que todas las tablas estén vacías
    -- =============================================
    PRINT '';
    PRINT 'Verificando que todas las tablas estén vacías...';
    
    DECLARE @count INT;
    
    SELECT @count = COUNT(*) FROM [dbo].[Clientes];
    PRINT 'Clientes: ' + CAST(@count AS VARCHAR(10)) + ' registros';
    
    SELECT @count = COUNT(*) FROM [dbo].[SolicitudesDescarga];
    PRINT 'SolicitudesDescarga: ' + CAST(@count AS VARCHAR(10)) + ' registros';
    
    SELECT @count = COUNT(*) FROM [dbo].[CFDIs];
    PRINT 'CFDIs: ' + CAST(@count AS VARCHAR(10)) + ' registros';
    
    SELECT @count = COUNT(*) FROM [dbo].[Archivos];
    PRINT 'Archivos: ' + CAST(@count AS VARCHAR(10)) + ' registros';
    
    SELECT @count = COUNT(*) FROM [dbo].[TokenSat];
    PRINT 'TokenSat: ' + CAST(@count AS VARCHAR(10)) + ' registros';
    
    COMMIT TRANSACTION;
    
    PRINT '';
    PRINT '=============================================';
    PRINT 'Base de datos SICOE resetada exitosamente.';
    PRINT 'Todas las tablas están vacías y los autoincrementales reiniciados.';
    PRINT '=============================================';
    
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    
    PRINT '';
    PRINT 'ERROR: Ocurrió un error durante la limpieza:';
    PRINT ERROR_MESSAGE();
    PRINT 'Transaction revertida.';
    
    THROW;
END CATCH;
GO

SET NOCOUNT OFF;
GO

