using SICOE.Application.Common;
using SICOE.Domain.Entities;
using SICOE.Domain.Enums;

namespace SICOE.Application.Interfaces.Services;

/// <summary>
/// Interface específica para servicio de archivos (ISP: Interface específica para archivos)
/// </summary>
public interface IArchivoService
{
    Task<Result<Archivo>> UploadFileAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        TipoArchivo tipoArchivo,
        CancellationToken cancellationToken = default);

    Task<Result<(Stream Stream, string ContentType, string FileName)>> DownloadFileAsync(
        int archivoId,
        CancellationToken cancellationToken = default);

    Task<Result<Archivo?>> GetFileAsync(
        int archivoId,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteFileAsync(
        int archivoId,
        CancellationToken cancellationToken = default);

    Task<Result<string>> GenerateTemporaryUrlAsync(
        int archivoId,
        int minutes = 10,
        CancellationToken cancellationToken = default);
}

