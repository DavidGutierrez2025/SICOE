using MediatR;
using SICOE.Application.Common;

namespace SICOE.Application.UseCases.Autenticacion.AutenticarConFiel;

public class AutenticarConFielCommand : IRequest<Result<AutenticacionResponseDto>>
{
    public string CertificadoCer { get; set; } = string.Empty;
    public string ClavePrivadaKey { get; set; } = string.Empty;
    public string PasswordFiel { get; set; } = string.Empty;
    public int? ClienteId { get; set; }
}
