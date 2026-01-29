using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SICOE.Domain.Entities;

namespace SICOE.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuración de Entity Framework para Cliente
/// </summary>
public class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.ToTable("Clientes");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .ValueGeneratedOnAdd();

        // Value Objects como propiedades owned
        builder.OwnsOne(c => c.Rfc, rfc =>
        {
            rfc.Property(r => r.Valor)
                .HasColumnName("RFC")
                .HasMaxLength(13)
                .IsRequired();
        });

        builder.OwnsOne(c => c.Email, email =>
        {
            email.Property(e => e.Valor)
                .HasColumnName("Email")
                .HasMaxLength(255);
        });

        builder.Property(c => c.RazonSocial)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(c => c.Activo)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(c => c.FechaCreacion)
            .IsRequired();

        // Relaciones
        builder.HasMany(c => c.SolicitudesDescarga)
            .WithOne(s => s.Cliente)
            .HasForeignKey(s => s.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

