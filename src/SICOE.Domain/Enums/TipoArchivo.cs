namespace SICOE.Domain.Enums;

/// <summary>
/// Tipos de archivo
/// </summary>
public enum TipoArchivo
{
    CFDI = 1,      // Archivos XML de CFDI
    Reporte = 2,   // Reportes (PDF, XLSX, CSV)
    Log = 3,       // Archivos de log
    XML = 4,       // Archivos XML genéricos
    PDF = 5,       // Archivos PDF
    XLSX = 6,      // Archivos Excel
    CSV = 7,       // Archivos CSV
    Otro = 99      // Otros tipos
}

