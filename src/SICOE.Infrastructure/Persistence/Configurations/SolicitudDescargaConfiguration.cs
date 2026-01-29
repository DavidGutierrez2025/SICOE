using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SICOE.Domain.Entities;
using SICOE.Domain.Enums;

namespace SICOE.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuración de Entity Framework para SolicitudDescarga
/// </summary>
public class SolicitudDescargaConfiguration : IEntityTypeConfiguration<SolicitudDescarga>
{
    public void Configure(EntityTypeBuilder<SolicitudDescarga> builder)
    {
        builder.ToTable("SolicitudesDescarga");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .ValueGeneratedOnAdd();

        builder.Property(s => s.IdSolicitudSat)
            .HasMaxLength(100);

        builder.Property(s => s.FechaInicial)
            .IsRequired();

        builder.Property(s => s.FechaFinal)
            .IsRequired();

        builder.Property(s => s.Estado)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(s => s.TotalSolicitado)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(s => s.TotalRecibido)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(s => s.MensajeError)
            .HasMaxLength(1000);

        builder.Property(s => s.FechaCreacion)
            .IsRequired();

        // Relaciones
        builder.HasMany(s => s.CFDIs)
            .WithOne(c => c.SolicitudDescarga)
            .HasForeignKey(c => c.SolicitudDescargaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

