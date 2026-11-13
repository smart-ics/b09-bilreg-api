using Bilreg.Application.AdmisiContext.AktivasiBerkasFeature.Integration;
using MediatR;

namespace Bilreg.Application.AdmisiContext.AktivasiBerkasFeature.UseCases;

/// <summary>
/// Query MediatR untuk mengambil detail transaksi aktivasi berkas berdasarkan ID transaksi.
/// </summary>
public record AktivasiBerkasGetQuery(string Id) : IRequest<AktivasiBerkasDetailDto?>;

public class AktivasiBerkasGetHandler : IRequestHandler<AktivasiBerkasGetQuery, AktivasiBerkasDetailDto?>
{
    private readonly IAktivasiBerkasEmrGateway _gateway;

    public AktivasiBerkasGetHandler(IAktivasiBerkasEmrGateway gateway)
    {
        _gateway = gateway;
    }

    public async Task<AktivasiBerkasDetailDto?> Handle(
        AktivasiBerkasGetQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Id))
            return null;

        return await _gateway.GetData(request.Id, cancellationToken);
    }
}
