using Bilreg.Application.AdmisiContext.AktivasiBerkasFeature.Integration;
using MediatR;

namespace Bilreg.Application.AdmisiContext.AktivasiBerkasFeature.UseCases;

/// <summary>
/// Command MediatR untuk menonaktifkan kembali registrasi yang sedang aktif di EMR.
/// </summary>
public record AktivasiBerkasDeactivateCommand(
    string RegId,
    string UserrId = ""
) : IRequest<AktivasiBerkasMutationResult>
{
    public AktivasiBerkasDeactivateCommand() : this(string.Empty, string.Empty) { }
}

public class AktivasiBerkasDeactivateHandler : IRequestHandler<AktivasiBerkasDeactivateCommand, AktivasiBerkasMutationResult>
{
    private readonly IAktivasiBerkasEmrGateway _gateway;

    public AktivasiBerkasDeactivateHandler(IAktivasiBerkasEmrGateway gateway)
    {
        _gateway = gateway;
    }

    public async Task<AktivasiBerkasMutationResult> Handle(
        AktivasiBerkasDeactivateCommand request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return AktivasiBerkasMutationResult.Fail("Data permintaan penonaktifan berkas tidak boleh kosong.");

        if (string.IsNullOrWhiteSpace(request.RegId))
            return AktivasiBerkasMutationResult.Fail("Nomor register (RegId) wajib diisi.");

        if (string.IsNullOrWhiteSpace(request.UserrId))
            return AktivasiBerkasMutationResult.Fail("User penonaktif (UserrId) wajib diisi.");

        return await _gateway.Deactivate(request.RegId.Trim(), request.UserrId.Trim(), cancellationToken);
    }
}
