using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SICOE.Domain.Entities;
using SICOE.Domain.Enums;

namespace SICOE.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuración de Entity Framework para Archivo
/// </summary>
public class ArchivoConfiguration : IEntityTypeConfiguration<Archivo>
{
    public void Configure(EntityTypeBuilder<Archivo> builder)
    {
        builder.ToTable("Archivos");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .ValueGeneratedOnAdd();

        builder.Property(a => a.NombreOriginal)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(a => a.NombreAlmacenado)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(a => a.RutaCompleta)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(a => a.ContentType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.TamanioBytes)
            .IsRequired();

        builder.Property(a => a.TipoArchivo)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(a => a.Descripcion)
            .HasMaxLength(1000);

        builder.Property(a => a.FechaCreacion)
            .IsRequired();

        // Índices
        builder.HasIndex(a => a.RutaCompleta)
            .IsUnique();
    }
}

