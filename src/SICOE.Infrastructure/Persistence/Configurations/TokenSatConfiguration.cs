using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SICOE.Domain.Entities;

namespace SICOE.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuración de Entity Framework para la entidad TokenSat
/// </summary>
public class TokenSatConfiguration : IEntityTypeConfiguration<TokenSat>
{
    public void Configure(EntityTypeBuilder<TokenSat> builder)
    {
        builder.ToTable("TokenSat");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasColumnName("Id")
            .ValueGeneratedOnAdd();

        builder.Property(t => t.ClienteId)
            .HasColumnName("ClienteId")
            .IsRequired();

        builder.Property(t => t.Token)
            .HasColumnName("Token")
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(t => t.FechaCreacion)
            .HasColumnName("FechaCreacion")
            .IsRequired();

        builder.Property(t => t.FechaExpiracion)
            .HasColumnName("FechaExpiracion")
            .IsRequired();

        builder.Property(t => t.Activo)
            .HasColumnName("Activo")
            .IsRequired()
            .HasDefaultValue(true);

        // Relación con Cliente
        builder.HasOne(t => t.Cliente)
            .WithMany()
            .HasForeignKey(t => t.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);

        // Índices
        builder.HasIndex(t => t.ClienteId)
            .HasDatabaseName("IX_TokenSat_ClienteId");

        builder.HasIndex(t => new { t.ClienteId, t.Activo, t.FechaExpiracion })
            .HasDatabaseName("IX_TokenSat_ClienteId_Activo_Expiracion");

        builder.HasIndex(t => t.FechaExpiracion)
            .HasDatabaseName("IX_TokenSat_FechaExpiracion");
    }
}

