using System.IO.Compression;
using MediatR;
using Microsoft.Extensions.Logging;
using SICOE.Application.Common;
using SICOE.Application.Interfaces.Services;
using SICOE.Application.Interfaces.UnitOfWork;
using SICOE.Domain.ValueObjects;

namespace SICOE.Application.UseCases.CFDI.DescargarCFDILote;

/// <summary>
/// Handler para descargar múltiples CFDI en un archivo ZIP
/// </summary>
public class DescargarCFDILoteHandler : IRequestHandler<DescargarCFDILoteCommand, Result<DescargarCFDILoteResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IArchivoService _archivoService;
    private readonly ILogger<DescargarCFDILoteHandler> _logger;

    public DescargarCFDILoteHandler(
        IUnitOfWork unitOfWork,
        IArchivoService archivoService,
        ILogger<DescargarCFDILoteHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _archivoService = archivoService;
        _logger = logger;
    }

    public async Task<Result<DescargarCFDILoteResponse>> Handle(DescargarCFDILoteCommand request, CancellationToken cancellationToken)
    {
        try
        {
            List<Domain.Entities.CFDI> cfdis = new();

            // Obtener CFDI según el método especificado
            if (request.SolicitudDescargaId.HasValue)
            {
                // Obtener todos los CFDI de una solicitud
                var cfdisSolicitud = await _unitOfWork.CFDIs.GetBySolicitudDescargaIdAsync(
                    request.SolicitudDescargaId.Value, cancellationToken);
                cfdis.AddRange(cfdisSolicitud);
            }
            else if (request.Uuids != null && request.Uuids.Any())
            {
                // Obtener por UUIDs
                foreach (var uuidStr in request.Uuids)
                {
                    try
                    {
                        var uuid = new UUID(uuidStr);
                        var cfdi = await _unitOfWork.CFDIs.GetByUuidAsync(uuid, cancellationToken);
                        if (cfdi != null)
                        {
                            cfdis.Add(cfdi);
                        }
                    }
                    catch
                    {
                        // UUID inválido, se agregará a la lista de no encontrados
                    }
                }
            }
            else if (request.Ids != null && request.Ids.Any())
            {
                // Obtener por IDs
                foreach (var id in request.Ids)
                {
                    var cfdi = await _unitOfWork.CFDIs.GetByIdAsync(id, cancellationToken);
                    if (cfdi != null)
                    {
                        cfdis.Add(cfdi);
                    }
                }
            }
            else
            {
                return Result<DescargarCFDILoteResponse>.Failure(
                    "Se debe proporcionar UUIDs, IDs o SolicitudDescargaId");
            }

            if (cfdis.Count == 0)
            {
                return Result<DescargarCFDILoteResponse>.Failure("No se encontraron CFDI para descargar");
            }

            // Filtrar solo los que tienen archivo
            var cfdisConArchivo = cfdis.Where(c => c.ArchivoId.HasValue).ToList();
            var cfdisSinArchivo = cfdis.Where(c => !c.ArchivoId.HasValue)
                .Select(c => c.Uuid.Valor)
                .ToList();

            if (cfdisConArchivo.Count == 0)
            {
                return Result<DescargarCFDILoteResponse>.Failure(
                    "Ninguno de los CFDI tiene archivo XML asociado");
            }

            // Crear ZIP en memoria
            using var zipStream = new MemoryStream();
            using (var zip = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                int descargados = 0;
                foreach (var cfdi in cfdisConArchivo)
                {
                    try
                    {
                        // Descargar archivo
                        var downloadResult = await _archivoService.DownloadFileAsync(
                            cfdi.ArchivoId!.Value, cancellationToken);

                        if (downloadResult.IsFailure)
                        {
                            _logger.LogWarning("No se pudo descargar archivo para CFDI {Uuid}: {Error}",
                                cfdi.Uuid.Valor, downloadResult.Error);
                            continue;
                        }

                        var (stream, _, _) = downloadResult.Value;

                        // Agregar al ZIP
                        var entry = zip.CreateEntry($"{cfdi.Uuid.Valor}.xml", CompressionLevel.Optimal);
                        using (var entryStream = entry.Open())
                        {
                            await stream.CopyToAsync(entryStream, cancellationToken);
                        }

                        descargados++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error al agregar CFDI {Uuid} al ZIP", cfdi.Uuid.Valor);
                    }
                }

                if (descargados == 0)
                {
                    return Result<DescargarCFDILoteResponse>.Failure(
                        "No se pudo descargar ningún archivo");
                }
            }

            zipStream.Position = 0;
            var contenidoZip = zipStream.ToArray();

            // Determinar nombre del archivo
            var nombreArchivo = request.SolicitudDescargaId.HasValue
                ? $"CFDI_Solicitud_{request.SolicitudDescargaId}_{DateTime.UtcNow:yyyyMMddHHmmss}.zip"
                : $"CFDI_Lote_{DateTime.UtcNow:yyyyMMddHHmmss}.zip";

            _logger.LogInformation(
                "ZIP de CFDI creado. Total={Total}, Descargados={Descargados}, Tamaño={Tamaño} bytes",
                cfdis.Count, cfdisConArchivo.Count, contenidoZip.Length);

            var response = new DescargarCFDILoteResponse
            {
                ContenidoZip = contenidoZip,
                NombreArchivo = nombreArchivo,
                ContentType = "application/zip",
                TotalCFDIs = cfdisConArchivo.Count,
                UuidsIncluidos = cfdisConArchivo.Select(c => c.Uuid.Valor).ToList(),
                UuidsSinArchivo = cfdisSinArchivo
            };

            return Result<DescargarCFDILoteResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al descargar lote de CFDI");
            return Result<DescargarCFDILoteResponse>.Failure($"Error al descargar lote: {ex.Message}");
        }
    }
}

