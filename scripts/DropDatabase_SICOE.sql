-- =============================================
-- Script para Eliminar Base de Datos SICOE
-- Sistema de Integración y Control de Operaciones Electrónicas
-- Versión: 1.0
-- Fecha: 2025-01-16
-- =============================================

USE master;
GO

-- Cerrar todas las conexiones a la base de datos
IF EXISTS (SELECT name FROM sys.databases WHERE name = 'SICOE')
BEGIN
    -- Cambiar a modo single user para cerrar conexiones
    ALTER DATABASE SICOE SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    
    -- Eliminar la base de datos
    DROP DATABASE SICOE;
    
    PRINT 'Base de datos SICOE eliminada exitosamente.';
END
ELSE
BEGIN
    PRINT 'La base de datos SICOE no existe.';
END
GO

