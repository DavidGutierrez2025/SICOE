-- =============================================
-- Script de Migración: Agregar Tabla TokenSat
-- Sistema de Integración y Control de Operaciones Electrónicas
-- Versión: 1.1
-- Fecha: 2025-01-16
-- =============================================
-- Este script agrega la tabla TokenSat a una base de datos SICOE existente
-- Ejecutar solo si la base de datos ya existe y no tiene la tabla TokenSat

USE SICOE;
GO

-- Verificar si la tabla ya existe
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TokenSat')
BEGIN
    PRINT 'Creando tabla TokenSat...';

    -- =============================================
    -- Tabla: TokenSat
    -- Almacena tokens del SAT temporalmente (encriptados)
    -- =============================================
    CREATE TABLE [dbo].[TokenSat] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [ClienteId] INT NOT NULL,
        [Token] NVARCHAR(1000) NOT NULL,
        [FechaCreacion] DATETIME2 NOT NULL,
        [FechaExpiracion] DATETIME2 NOT NULL,
        [Activo] BIT NOT NULL DEFAULT 1,
        CONSTRAINT [PK_TokenSat] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_TokenSat_Clientes] FOREIGN KEY ([ClienteId]) 
            REFERENCES [dbo].[Clientes] ([Id]) ON DELETE NO ACTION
    );

    -- Índices para TokenSat
    CREATE NONCLUSTERED INDEX [IX_TokenSat_ClienteId]
    ON [dbo].[TokenSat] ([ClienteId]);

    CREATE NONCLUSTERED INDEX [IX_TokenSat_ClienteId_Activo_Expiracion]
    ON [dbo].[TokenSat] ([ClienteId], [Activo], [FechaExpiracion]);

    CREATE NONCLUSTERED INDEX [IX_TokenSat_FechaExpiracion]
    ON [dbo].[TokenSat] ([FechaExpiracion]);

    PRINT 'Tabla TokenSat creada exitosamente.';
END
ELSE
BEGIN
    PRINT 'La tabla TokenSat ya existe. No se realizaron cambios.';
END
GO

