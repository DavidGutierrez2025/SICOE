using MediatR;
using Microsoft.Extensions.Logging;
using SICOE.Application.Common;
using SICOE.Application.Interfaces.Services;
using SICOE.Application.Interfaces.UnitOfWork;
using SICOE.Domain.ValueObjects;

namespace SICOE.Application.UseCases.CFDI.DescargarCFDI;

/// <summary>
/// Handler para descargar el XML de un CFDI
/// </summary>
public class DescargarCFDIHandler : IRequestHandler<DescargarCFDIQuery, Result<DescargarCFDIResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IArchivoService _archivoService;
    private readonly ILogger<DescargarCFDIHandler> _logger;

    public DescargarCFDIHandler(
        IUnitOfWork unitOfWork,
        IArchivoService archivoService,
        ILogger<DescargarCFDIHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _archivoService = archivoService;
        _logger = logger;
    }

    public async Task<Result<DescargarCFDIResponse>> Handle(DescargarCFDIQuery request, CancellationToken cancellationToken)
    {
        try
        {
            Domain.Entities.CFDI? cfdi = null;

            if (!string.IsNullOrWhiteSpace(request.Uuid))
            {
                var uuid = new UUID(request.Uuid);
                cfdi = await _unitOfWork.CFDIs.GetByUuidAsync(uuid, cancellationToken);
            }
            else if (request.Id.HasValue)
            {
                cfdi = await _unitOfWork.CFDIs.GetByIdAsync(request.Id.Value, cancellationToken);
            }
            else
            {
                return Result<DescargarCFDIResponse>.Failure("Se debe proporcionar UUID o ID del CFDI");
            }

            if (cfdi == null)
            {
                return Result<DescargarCFDIResponse>.Failure("CFDI no encontrado");
            }

            if (!cfdi.ArchivoId.HasValue)
            {
                return Result<DescargarCFDIResponse>.Failure("El CFDI no tiene archivo XML asociado");
            }

            // Descargar archivo usando ArchivoService
            var downloadResult = await _archivoService.DownloadFileAsync(cfdi.ArchivoId.Value, cancellationToken);
            
            if (downloadResult.IsFailure)
            {
                return Result<DescargarCFDIResponse>.Failure($"Error al descargar archivo: {downloadResult.Error}");
            }

            var (stream, contentType, fileName) = downloadResult.Value;

            // Leer el stream a bytes
            byte[] contenidoXml;
            using (var memoryStream = new MemoryStream())
            {
                await stream.CopyToAsync(memoryStream, cancellationToken);
                contenidoXml = memoryStream.ToArray();
            }

            _logger.LogInformation("CFDI descargado. UUID={Uuid}, Tamaño={Tamaño} bytes", cfdi.Uuid.Valor, contenidoXml.Length);

            var response = new DescargarCFDIResponse
            {
                ContenidoXml = contenidoXml,
                NombreArchivo = $"{cfdi.Uuid.Valor}.xml",
                ContentType = contentType,
                Uuid = cfdi.Uuid.Valor
            };

            return Result<DescargarCFDIResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al descargar CFDI. Uuid={Uuid}, Id={Id}", request.Uuid, request.Id);
            return Result<DescargarCFDIResponse>.Failure($"Error al descargar CFDI: {ex.Message}");
        }
    }
}

