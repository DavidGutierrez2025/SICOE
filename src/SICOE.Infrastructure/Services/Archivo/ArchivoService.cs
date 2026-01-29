using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SICOE.Application.Common;
using SICOE.Application.Interfaces.Repositories;
using SICOE.Application.Interfaces.Services;
using SICOE.Application.Interfaces.UnitOfWork;
using SICOE.Domain.Entities;
using SICOE.Domain.Enums;
using ArchivoEntity = SICOE.Domain.Entities.Archivo;

namespace SICOE.Infrastructure.Services.Archivo;

/// <summary>
/// Implementación del servicio de archivos (homologado con GEDINET)
/// Almacena archivos en sistema de archivos y metadata en BD
/// </summary>
public class ArchivoService : IArchivoService
{
    private readonly IArchivoRepository _archivoRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ArchivoService> _logger;
    private readonly string _basePath;

    public ArchivoService(
        IArchivoRepository archivoRepository,
        IUnitOfWork unitOfWork,
        IConfiguration configuration,
        ILogger<ArchivoService> logger)
    {
        _archivoRepository = archivoRepository;
        _unitOfWork = unitOfWork;
        _configuration = configuration;
        _logger = logger;

        // Obtener ruta base desde configuración o usar wwwroot/uploads por defecto
        _basePath = _configuration["Storage:BasePath"] 
            ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

        // Crear directorio base si no existe
        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
            _logger.LogInformation("Directorio base de archivos creado: {BasePath}", _basePath);
        }
    }

    public async Task<Result<ArchivoEntity>> UploadFileAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        TipoArchivo tipoArchivo,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (fileStream == null || fileStream.Length == 0)
                return Result<ArchivoEntity>.Failure("El stream del archivo no puede estar vacío");

            if (string.IsNullOrWhiteSpace(fileName))
                return Result<ArchivoEntity>.Failure("El nombre del archivo no puede estar vacío");

            // Validar tamaño máximo
            var maxFileSizeStr = _configuration["Storage:MaxFileSize"];
            var maxFileSize = long.TryParse(maxFileSizeStr, out var size) ? size : 10_485_760; // 10MB por defecto
            if (fileStream.Length > maxFileSize)
                return Result<ArchivoEntity>.Failure($"El archivo excede el tamaño máximo permitido: {maxFileSize} bytes");

            // Obtener extensión y validar
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            var allowedExtensionsSection = _configuration.GetSection("Storage:AllowedExtensions");
            var allowedExtensions = allowedExtensionsSection.GetChildren().Select(c => c.Value).Where(v => !string.IsNullOrEmpty(v)).ToArray();
            if (allowedExtensions.Length == 0)
                allowedExtensions = new[] { ".xml", ".pdf", ".xlsx", ".csv" };

            if (!allowedExtensions.Contains(extension))
                return Result<ArchivoEntity>.Failure($"Extensión no permitida: {extension}. Extensiones permitidas: {string.Join(", ", allowedExtensions)}");

            // Generar nombre único para almacenamiento
            var nombreAlmacenado = $"{Guid.NewGuid()}{extension}";

            // Crear estructura de carpetas por año/mes (YYYYMM)
            var fechaActual = DateTime.UtcNow;
            var carpetaMes = fechaActual.ToString("yyyyMM");
            var carpetaCompleta = Path.Combine(_basePath, carpetaMes);

            if (!Directory.Exists(carpetaCompleta))
            {
                Directory.CreateDirectory(carpetaCompleta);
                _logger.LogInformation("Directorio mensual creado: {Carpeta}", carpetaCompleta);
            }

            // Ruta completa del archivo (con backslashes para Windows)
            var rutaCompleta = Path.Combine(carpetaCompleta, nombreAlmacenado);

            // Guardar archivo físico
            using (var fileStreamWrite = new FileStream(rutaCompleta, FileMode.Create, FileAccess.Write))
            {
                await fileStream.CopyToAsync(fileStreamWrite, cancellationToken);
            }

            _logger.LogInformation("Archivo físico guardado: {RutaCompleta}, Tamaño: {Tamanio} bytes", 
                rutaCompleta, fileStream.Length);

            // Crear entidad Archivo
            var archivo = new ArchivoEntity(
                nombreOriginal: fileName,
                nombreAlmacenado: nombreAlmacenado,
                rutaCompleta: rutaCompleta,
                contentType: contentType ?? "application/octet-stream",
                tamanioBytes: fileStream.Length,
                tipoArchivo: tipoArchivo,
                descripcion: null);

            // Guardar en BD
            await _archivoRepository.AddAsync(archivo, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Archivo guardado en BD con ID: {ArchivoId}, Ruta: {RutaCompleta}", 
                archivo.Id, rutaCompleta);

            return Result<ArchivoEntity>.Success(archivo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al subir archivo: {FileName}", fileName);
            return Result<ArchivoEntity>.Failure($"Error al subir archivo: {ex.Message}");
        }
    }

    public async Task<Result<(Stream Stream, string ContentType, string FileName)>> DownloadFileAsync(
        int archivoId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var archivo = await _archivoRepository.GetByIdAsync(archivoId, cancellationToken);
            if (archivo == null)
            {
                return Result<(Stream Stream, string ContentType, string FileName)>.Failure($"Archivo no encontrado con ID: {archivoId}");
            }

            if (!File.Exists(archivo.RutaCompleta))
            {
                return Result<(Stream Stream, string ContentType, string FileName)>.Failure($"Archivo físico no encontrado: {archivo.RutaCompleta}");
            }

            var memoryStream = new MemoryStream();
            using (var fileStream = new FileStream(archivo.RutaCompleta, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                await fileStream.CopyToAsync(memoryStream, cancellationToken);
            }
            memoryStream.Position = 0;

            _logger.LogInformation("Archivo descargado: {ArchivoId}, Ruta: {RutaCompleta}", 
                archivoId, archivo.RutaCompleta);

            return Result<(Stream Stream, string ContentType, string FileName)>.Success(
                (memoryStream, archivo.ContentType, archivo.NombreOriginal));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al descargar archivo: {ArchivoId}", archivoId);
            return Result<(Stream Stream, string ContentType, string FileName)>.Failure($"Error al descargar archivo: {ex.Message}");
        }
    }

    public async Task<Result<ArchivoEntity?>> GetFileAsync(
        int archivoId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var archivo = await _archivoRepository.GetByIdAsync(archivoId, cancellationToken);
            return Result<ArchivoEntity?>.Success(archivo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener archivo: {ArchivoId}", archivoId);
            return Result<ArchivoEntity?>.Failure($"Error al obtener archivo: {ex.Message}");
        }
    }

    public async Task<Result> DeleteFileAsync(
        int archivoId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var archivo = await _archivoRepository.GetByIdAsync(archivoId, cancellationToken);
            if (archivo == null)
            {
                _logger.LogWarning("Intento de eliminar archivo inexistente: {ArchivoId}", archivoId);
                return Result.Failure($"Archivo no encontrado con ID: {archivoId}");
            }

            // Eliminar archivo físico si existe
            if (File.Exists(archivo.RutaCompleta))
            {
                File.Delete(archivo.RutaCompleta);
                _logger.LogInformation("Archivo físico eliminado: {RutaCompleta}", archivo.RutaCompleta);
            }
            else
            {
                _logger.LogWarning("Archivo físico no encontrado para eliminar: {RutaCompleta}", archivo.RutaCompleta);
            }

            // Eliminar de BD (hard delete)
            await _archivoRepository.DeleteAsync(archivo, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Archivo eliminado de BD: {ArchivoId}", archivoId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar archivo: {ArchivoId}", archivoId);
            return Result.Failure($"Error al eliminar archivo: {ex.Message}");
        }
    }

    public Task<Result<string>> GenerateTemporaryUrlAsync(
        int archivoId,
        int minutes = 10,
        CancellationToken cancellationToken = default)
    {
        // Implementación futura para generar URLs temporales seguras
        _logger.LogWarning("GenerateTemporaryUrlAsync no implementado completamente. Retornando URL de ejemplo.");
        var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://localhost";
        return Task.FromResult(Result<string>.Success($"{baseUrl}/api/Archivo/secure-download/{Guid.NewGuid()}"));
    }
}

