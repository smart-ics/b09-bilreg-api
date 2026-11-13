using Bilreg.Application.AdmisiContext.AktivasiBerkasFeature.Integration;
using MediatR;

namespace Bilreg.Application.AdmisiContext.AktivasiBerkasFeature.UseCases;

/// <summary>
/// Command MediatR untuk membersihkan/menonaktifkan seluruh registrasi aktif di semua unit layanan pada EMR.
/// </summary>
public record AktivasiBerkasCleanUpCommand : IRequest<AktivasiBerkasMutationResult>;

public class AktivasiBerkasCleanUpCommandHandler : IRequestHandler<AktivasiBerkasCleanUpCommand, AktivasiBerkasMutationResult>
{
    private readonly IAktivasiBerkasEmrGateway _gateway;

    public AktivasiBerkasCleanUpCommandHandler(IAktivasiBerkasEmrGateway gateway)
    {
        _gateway = gateway;
    }

    public async Task<AktivasiBerkasMutationResult> Handle(
        AktivasiBerkasCleanUpCommand request,
        CancellationToken cancellationToken)
    {
        return await _gateway.CleanUp(cancellationToken);
    }
}
