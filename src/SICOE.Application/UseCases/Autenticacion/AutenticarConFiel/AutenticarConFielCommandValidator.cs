using FluentValidation;

namespace SICOE.Application.UseCases.Autenticacion.AutenticarConFiel;

public class AutenticarConFielCommandValidator : AbstractValidator<AutenticarConFielCommand>
{
    public AutenticarConFielCommandValidator()
    {
        RuleFor(v => v.CertificadoCer)
            .NotEmpty().WithMessage("El certificado .cer es requerido");

        RuleFor(v => v.ClavePrivadaKey)
            .NotEmpty().WithMessage("La clave privada .key es requerida");

        RuleFor(v => v.PasswordFiel)
            .NotEmpty().WithMessage("La contraseña de la FIEL es requerida");
    }
}
