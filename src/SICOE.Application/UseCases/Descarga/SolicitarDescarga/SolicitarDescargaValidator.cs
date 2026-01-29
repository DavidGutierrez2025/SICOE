using FluentValidation;

namespace SICOE.Application.UseCases.Descarga.SolicitarDescarga;

/// <summary>
/// Validador para SolicitarDescargaCommand usando FluentValidation
/// </summary>
public class SolicitarDescargaValidator : AbstractValidator<SolicitarDescargaCommand>
{
    public SolicitarDescargaValidator()
    {
        // ClienteId ya no es requerido - se obtiene automáticamente del RFC del certificado FIEL
        // No se valida aquí porque el handler lo obtiene/crea automáticamente

        // Validar certificado FIEL (requerido - se usa para obtener token y firmar)
        // Si se proporciona CertificadoCer, también debe proporcionarse ClavePrivadaKey y PasswordFiel
        RuleFor(x => x.ClavePrivadaKey)
            .NotEmpty()
            .When(x => !string.IsNullOrWhiteSpace(x.CertificadoCer))
            .WithMessage("Si se proporciona el certificado .cer, también debe proporcionarse la clave privada .key");
        
        RuleFor(x => x.PasswordFiel)
            .NotEmpty()
            .When(x => !string.IsNullOrWhiteSpace(x.CertificadoCer))
            .WithMessage("Si se proporciona el certificado FIEL, también debe proporcionarse la contraseña");

        RuleFor(x => x.FechaInicial)
            .NotEmpty()
            .WithMessage("La fecha inicial es requerida")
            .LessThanOrEqualTo(x => x.FechaFinal)
            .WithMessage("La fecha inicial no puede ser mayor que la fecha final");

        RuleFor(x => x.FechaFinal)
            .NotEmpty()
            .WithMessage("La fecha final es requerida")
            .GreaterThanOrEqualTo(x => x.FechaInicial)
            .WithMessage("La fecha final no puede ser menor que la fecha inicial");

        // NOTA: El SAT permite solicitar descargas de fechas pasadas
        // La validación de fecha final <= ahora puede ser muy restrictiva
        // Se comenta temporalmente para permitir solicitudes de fechas recientes
        // RuleFor(x => x.FechaFinal)
        //     .LessThanOrEqualTo(DateTime.UtcNow)
        //     .WithMessage("La fecha final no puede ser mayor que la fecha actual");

        // NOTA: La validación de CFDI + Todos se maneja mediante auto-normalización en SatService.cs
        // para evitar errores 400 al usuario, permitiendo que el flujo continúe como "Vigente" automáticamente.

        // Validación opcional para RFCs
        When(x => !string.IsNullOrWhiteSpace(x.RfcEmisor), () =>
        {
            RuleFor(x => x.RfcEmisor)
                .Matches(@"^[A-ZÑ&]{3,4}\d{6}[A-Z0-9]{3}$")
                .WithMessage("El RFC del emisor no tiene un formato válido");
        });

        When(x => !string.IsNullOrWhiteSpace(x.RfcReceptor), () =>
        {
            RuleFor(x => x.RfcReceptor)
                .Matches(@"^[A-ZÑ&]{3,4}\d{6}[A-Z0-9]{3}$")
                .WithMessage("El RFC del receptor no tiene un formato válido");
        });
    }
}

