using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SICOE.Application.Interfaces.Repositories;
using SICOE.Application.Interfaces.Services;
using SICOE.Application.Interfaces.UnitOfWork;
using SICOE.Infrastructure.Persistence;
using SICOE.Infrastructure.Persistence.Repositories;
using SICOE.Infrastructure.Services.Archivo;
using SICOE.Infrastructure.Services.Encryption;
using SICOE.Infrastructure.Services.Fiel;
using SICOE.Infrastructure.Services.Sat;
using System.IO;
using SICOE.Infrastructure.UnitOfWork;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.Extensions;
using Serilog;

// ======================
// Configure Serilog
// ======================
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
        .AddEnvironmentVariables()
        .Build())
    .CreateLogger();

try
{
    Log.Information("Iniciando aplicación SICOE Razor");

    var builder = WebApplication.CreateBuilder(args);

    // Use Serilog for logging
    builder.Host.UseSerilog();

// ======================
// Database Context
// ======================
builder.Services.AddDbContext<SICOEDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SICOEConnectionString")));

// ======================
// Repositories (DIP)
// ======================
builder.Services.AddScoped<IClienteRepository, ClienteRepository>();
builder.Services.AddScoped<ISolicitudDescargaRepository, SolicitudDescargaRepository>();
builder.Services.AddScoped<ICFDIRepository, CFDIRepository>();
builder.Services.AddScoped<IArchivoRepository, ArchivoRepository>();
builder.Services.AddScoped<ITokenSatRepository, TokenSatRepository>();

// ======================
// Unit of Work
// ======================
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// ======================
// Services (Infrastructure)
// ======================
builder.Services.AddHttpClient(); // Para FielValidationService
builder.Services.AddHttpClient<SICOE.Infrastructure.Services.Sat.Soap.SatSoapHttpClient>()
    .ConfigureHttpClient(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(300); // 5 minutos
    });
builder.Services.AddScoped<SICOE.Infrastructure.Services.Fiel.FielValidationService>();
builder.Services.AddScoped<SICOE.Infrastructure.Services.Sat.Soap.XmlSignatureService>();
builder.Services.AddScoped<SICOE.Infrastructure.Services.Sat.Soap.SatSoapMessageBuilder>();
builder.Services.AddScoped<SICOE.Infrastructure.Services.Sat.Soap.SatAutenticacionSoapBuilder>();
builder.Services.AddScoped<IFielCertificateProvider, SICOE.Infrastructure.Services.Sat.FielCertificateProvider>();
builder.Services.AddScoped<SICOE.Infrastructure.Services.Sat.Soap.SatSoapClientFactory>();
builder.Services.AddScoped<SICOE.Infrastructure.Services.Sat.Soap.SatSoapResponseParser>();
builder.Services.AddScoped<IArchivoService, ArchivoService>();
builder.Services.AddScoped<IFielService, FielService>();
builder.Services.AddScoped<ISatService, SatService>();

// ======================
// Data Protection (para encriptación de tokens)
// ======================
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "keys")))
    .SetApplicationName("SICOE")
    .SetDefaultKeyLifetime(TimeSpan.FromDays(90)); // Las claves expiran después de 90 días

builder.Services.AddScoped<IEncryptionService, SICOE.Infrastructure.Services.Encryption.DataProtectionEncryptionService>();
builder.Services.AddScoped<ITokenSatService, SICOE.Infrastructure.Services.TokenSat.TokenSatService>();

// ======================
// Hangfire (Message Queue)
// ======================
var hangfireConnectionString = builder.Configuration.GetConnectionString("SICOEConnectionString");
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(hangfireConnectionString, new SqlServerStorageOptions
    {
        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
        QueuePollInterval = TimeSpan.Zero,
        UseRecommendedIsolationLevel = true,
        DisableGlobalLocks = true
    }));

builder.Services.AddHangfireServer();

// ======================
// Razor Pages
// ======================
builder.Services.AddRazorPages();

// ======================
// Build App
// ======================
var app = builder.Build();

// ======================
// Middleware
// ======================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
// Configurar Razor Pages
app.MapRazorPages();

// ======================
// Hangfire Dashboard
// ======================
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() }
});

app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "La aplicación falló al iniciar");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

// ======================
// Hangfire Authorization Filter (Simple - para desarrollo)
// ======================
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        // En desarrollo, permitir acceso a todos
        // En producción, implementar autenticación adecuada
        return true;
    }
}
