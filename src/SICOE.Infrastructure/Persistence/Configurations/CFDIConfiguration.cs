using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SICOE.Domain.Entities;
using SICOE.Domain.Enums;

namespace SICOE.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuración de Entity Framework para CFDI
/// </summary>
public class CFDIConfiguration : IEntityTypeConfiguration<CFDI>
{
    public void Configure(EntityTypeBuilder<CFDI> builder)
    {
        builder.ToTable("CFDIs");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .ValueGeneratedOnAdd();

        // Value Objects como propiedades owned
        builder.OwnsOne(c => c.Uuid, uuid =>
        {
            uuid.Property(u => u.Valor)
                .HasColumnName("UUID")
                .HasMaxLength(36)
                .IsRequired();
            
            // Índice único en la propiedad owned - debe estar dentro del OwnsOne
            uuid.HasIndex(u => u.Valor)
                .IsUnique()
                .HasDatabaseName("IX_CFDIs_UUID");
        });

        builder.OwnsOne(c => c.RfcEmisor, rfc =>
        {
            rfc.Property(r => r.Valor)
                .HasColumnName("RfcEmisor")
                .HasMaxLength(13)
                .IsRequired();
        });

        builder.OwnsOne(c => c.RfcReceptor, rfc =>
        {
            rfc.Property(r => r.Valor)
                .HasColumnName("RfcReceptor")
                .HasMaxLength(13)
                .IsRequired();
        });

        builder.OwnsOne(c => c.Total, monto =>
        {
            monto.Property(m => m.Valor)
                .HasColumnName("Total")
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            monto.Property(m => m.Moneda)
                .HasColumnName("Moneda")
                .HasMaxLength(3)
                .IsRequired()
                .HasDefaultValue("MXN");
        });

        builder.Property(c => c.FechaEmision)
            .IsRequired();

        builder.Property(c => c.TipoComprobante)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(c => c.Estatus)
            .HasConversion<int>()
            .IsRequired();

        // Campos adicionales del CFDI
        builder.Property(c => c.SubTotal)
            .HasColumnType("decimal(18,2)")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(c => c.TotalImpuestosTrasladados)
            .HasColumnType("decimal(18,2)")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(c => c.FechaTimbrado)
            .IsRequired(false);

        builder.Property(c => c.Serie)
            .HasMaxLength(25)
            .IsRequired(false);

        builder.Property(c => c.Folio)
            .HasMaxLength(40)
            .IsRequired(false);

        builder.Property(c => c.NombreEmisor)
            .HasMaxLength(300)
            .IsRequired(false);

        builder.Property(c => c.NombreReceptor)
            .HasMaxLength(300)
            .IsRequired(false);

        builder.Property(c => c.RegimenFiscalEmisor)
            .HasMaxLength(10)
            .IsRequired(false);

        builder.Property(c => c.RegimenFiscalReceptor)
            .HasMaxLength(10)
            .IsRequired(false);

        builder.Property(c => c.DomicilioFiscalReceptor)
            .HasMaxLength(5)
            .IsRequired(false);

        builder.Property(c => c.UsoCFDI)
            .HasMaxLength(3)
            .IsRequired(false);

        builder.Property(c => c.FormaPago)
            .HasMaxLength(2)
            .IsRequired(false);

        builder.Property(c => c.MetodoPago)
            .HasMaxLength(3)
            .IsRequired(false);

        builder.Property(c => c.LugarExpedicion)
            .HasMaxLength(5)
            .IsRequired(false);

        // Relaciones
        builder.HasOne(c => c.Archivo)
            .WithMany(a => a.CFDIs)
            .HasForeignKey(c => c.ArchivoId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

