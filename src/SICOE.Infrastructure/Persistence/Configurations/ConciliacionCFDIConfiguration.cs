using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SICOE.Domain.Entities;
using SICOE.Domain.Enums;

namespace SICOE.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuración de Entity Framework para ConciliacionCFDI
/// </summary>
public class ConciliacionCFDIConfiguration : IEntityTypeConfiguration<ConciliacionCFDI>
{
    public void Configure(EntityTypeBuilder<ConciliacionCFDI> builder)
    {
        builder.ToTable("ConciliacionesCFDI");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .ValueGeneratedOnAdd();

        builder.Property(c => c.Estado)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(c => c.TotalSolicitado)
            .IsRequired();

        builder.Property(c => c.TotalRecibido)
            .IsRequired();

        builder.Property(c => c.TotalFaltantes)
            .IsRequired();

        builder.Property(c => c.IntentosReconciliacion)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(c => c.MensajeError)
            .HasMaxLength(1000)
            .IsRequired(false);

        builder.Property(c => c.FechaCreacion)
            .IsRequired();

        // Relaciones
        builder.HasOne(c => c.SolicitudDescarga)
            .WithMany(s => s.Conciliaciones)
            .HasForeignKey(c => c.SolicitudDescargaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.NuevaSolicitud)
            .WithMany()
            .HasForeignKey(c => c.NuevaSolicitudId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        // Índices
        builder.HasIndex(c => c.SolicitudDescargaId)
            .HasDatabaseName("IX_ConciliacionesCFDI_SolicitudDescargaId");

        builder.HasIndex(c => c.Estado)
            .HasDatabaseName("IX_ConciliacionesCFDI_Estado");

        builder.HasIndex(c => new { c.Estado, c.IntentosReconciliacion })
            .HasDatabaseName("IX_ConciliacionesCFDI_Estado_Intentos");
    }
}

