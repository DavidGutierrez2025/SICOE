using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SICOE.Application.Interfaces.Repositories;
using SICOE.Application.Interfaces.Services;
using SICOE.Application.Interfaces.UnitOfWork;
using SICOE.Infrastructure.Persistence;
using SICOE.Infrastructure.Persistence.Repositories;
using SICOE.Infrastructure.MessageQueue.Hangfire;
using SICOE.Infrastructure.Services.Archivo;
using SICOE.Infrastructure.Services.Encryption;
using SICOE.Infrastructure.Services.Fiel;
using SICOE.Infrastructure.Services.Sat;
using SICOE.Infrastructure.UnitOfWork;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.SqlServer;
using FluentValidation;
using FluentValidation.AspNetCore;
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
    Log.Information("Iniciando aplicación SICOE API");

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
builder.Services.AddScoped<IConciliacionCFDIRepository, ConciliacionCFDIRepository>();

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
builder.Services.AddScoped<IEncryptionService, SICOE.Infrastructure.Services.Encryption.DataProtectionEncryptionService>();
builder.Services.AddScoped<ITokenSatService, SICOE.Infrastructure.Services.TokenSat.TokenSatService>();
builder.Services.AddScoped<IMessageQueueService, SICOE.Infrastructure.MessageQueue.Hangfire.HangfireQueueService>();

// ======================
// Data Protection (para encriptación de tokens)
// ======================
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "keys")))
    .SetApplicationName("SICOE")
    .SetDefaultKeyLifetime(TimeSpan.FromDays(90)); // Las claves expiran después de 90 días

// ======================
// Background Jobs (Hangfire)
// ======================
builder.Services.AddScoped<SICOE.Infrastructure.BackgroundJobs.Descarga.VerificarDescargaJob>();
builder.Services.AddScoped<SICOE.Infrastructure.BackgroundJobs.Descarga.VerificarDescargasPendientesJob>();
builder.Services.AddScoped<SICOE.Infrastructure.BackgroundJobs.Descarga.ProcesarCFDIJob>();
builder.Services.AddScoped<SICOE.Infrastructure.BackgroundJobs.TokenSat.LimpiarTokensExpiradosJob>();

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
// MediatR (CQRS)
// ======================
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(SICOE.Application.Common.Result).Assembly);
});

// ======================
// AutoMapper
// ======================
builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddProfile<SICOE.Application.Mappings.DescargaMappingProfile>();
}, typeof(SICOE.Application.Mappings.DescargaMappingProfile).Assembly);

// ======================
// FluentValidation
// ======================
builder.Services.AddValidatorsFromAssemblyContaining<SICOE.Application.UseCases.Descarga.SolicitarDescarga.SolicitarDescargaValidator>();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();

// ======================
// Controllers
// ======================
// ======================
// CORS (para permitir llamadas desde Razor Pages)
// ======================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowRazorPages", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5218",
                "https://localhost:7064",
                "http://localhost:5173",  // HTTP del API
                "https://localhost:7052"   // HTTPS del API
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// ======================
// OpenAPI / Swagger
// ======================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ======================
// Build App
// ======================
var app = builder.Build();

// ======================
// Middleware
// ======================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SICOE API v1");
        c.RoutePrefix = "swagger"; // Swagger UI estará disponible en /swagger
        c.DisplayRequestDuration();
    });
}

app.UseHttpsRedirection();

// CORS debe ir después de UseRouting y antes de UseAuthorization
app.UseRouting();
app.UseCors("AllowRazorPages");

app.UseAuthorization();
app.MapControllers();

// ======================
// Hangfire Dashboard
// ======================
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() }
});

// ======================
// Configure Recurring Jobs
// ======================
using (var scope = app.Services.CreateScope())
{
    var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    // Job recurrente: Verificar todas las solicitudes pendientes cada 10 minutos
    recurringJobManager.AddOrUpdate(
        "verificar-descargas-pendientes",
        () => scope.ServiceProvider.GetRequiredService<SICOE.Infrastructure.BackgroundJobs.Descarga.VerificarDescargasPendientesJob>()
            .VerificarTodasLasSolicitudesPendientesAsync(CancellationToken.None),
        "*/10 * * * *", // Cada 10 minutos
        new RecurringJobOptions
        {
            TimeZone = TimeZoneInfo.Local
        });

    // Job recurrente: Limpiar tokens SAT expirados diariamente a las 2 AM
    recurringJobManager.AddOrUpdate(
        "limpiar-tokens-expirados",
        () => scope.ServiceProvider.GetRequiredService<SICOE.Infrastructure.BackgroundJobs.TokenSat.LimpiarTokensExpiradosJob>()
            .LimpiarTokensAsync(CancellationToken.None),
        "0 2 * * *", // Diariamente a las 2 AM
        new RecurringJobOptions
        {
            TimeZone = TimeZoneInfo.Local
        });

    logger.LogInformation("Jobs recurrentes de Hangfire configurados");
}

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
