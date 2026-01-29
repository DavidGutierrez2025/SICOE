using Microsoft.EntityFrameworkCore;
using SICOE.Domain.Entities;
using SICOE.Infrastructure.Persistence.Configurations;

namespace SICOE.Infrastructure.Persistence;

/// <summary>
/// DbContext principal de SICOE
/// </summary>
public class SICOEDbContext : DbContext
{
    public SICOEDbContext(DbContextOptions<SICOEDbContext> options) : base(options)
    {
    }

    public DbSet<Cliente> Clientes { get; set; } = null!;
    public DbSet<SolicitudDescarga> SolicitudesDescarga { get; set; } = null!;
    public DbSet<CFDI> CFDIs { get; set; } = null!;
    public DbSet<Archivo> Archivos { get; set; } = null!;
    public DbSet<TokenSat> TokensSat { get; set; } = null!;
    public DbSet<ConciliacionCFDI> ConciliacionesCFDI { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Aplicar configuraciones
        modelBuilder.ApplyConfiguration(new ClienteConfiguration());
        modelBuilder.ApplyConfiguration(new SolicitudDescargaConfiguration());
        modelBuilder.ApplyConfiguration(new CFDIConfiguration());
        modelBuilder.ApplyConfiguration(new ArchivoConfiguration());
        modelBuilder.ApplyConfiguration(new TokenSatConfiguration());
        modelBuilder.ApplyConfiguration(new ConciliacionCFDIConfiguration());
    }
}

