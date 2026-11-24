using Bilreg.Application.AdmisiContext.AktivasiBerkasFeature.Integration;
using MediatR;

namespace Bilreg.Application.AdmisiContext.AktivasiBerkasFeature.UseCases;

/// <summary>
/// Query MediatR untuk mengambil daftar register yang sedang aktif kembali terkelompok per unit layanan.
/// </summary>
public record AktivasiBerkasListReaktifQuery(string? LayananId = null) : IRequest<IEnumerable<AktivasiBerkasListReaktifDto>>;

public class AktivasiBerkasListReaktifHandler : IRequestHandler<AktivasiBerkasListReaktifQuery, IEnumerable<AktivasiBerkasListReaktifDto>>
{
    private readonly IAktivasiBerkasEmrGateway _gateway;

    public AktivasiBerkasListReaktifHandler(IAktivasiBerkasEmrGateway gateway)
    {
        _gateway = gateway;
    }

    public async Task<IEnumerable<AktivasiBerkasListReaktifDto>> Handle(
        AktivasiBerkasListReaktifQuery request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return [];

        return await _gateway.ListReaktif(request.LayananId, cancellationToken);
    }
}
