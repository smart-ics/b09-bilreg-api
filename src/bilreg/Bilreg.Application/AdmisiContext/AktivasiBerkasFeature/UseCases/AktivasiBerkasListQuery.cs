using Bilreg.Application.AdmisiContext.AktivasiBerkasFeature.Integration;
using MediatR;

namespace Bilreg.Application.AdmisiContext.AktivasiBerkasFeature.UseCases;

/// <summary>
/// Query MediatR untuk mengambil daftar riwayat transaksi aktivasi berkas berdasarkan rentang tanggal.
/// </summary>
public record AktivasiBerkasListQuery(string TglAwal, string TglAkhir) : IRequest<IEnumerable<AktivasiBerkasListItemDto>>;

public class AktivasiBerkasListHandler : IRequestHandler<AktivasiBerkasListQuery, IEnumerable<AktivasiBerkasListItemDto>>
{
    private readonly IAktivasiBerkasEmrGateway _gateway;

    public AktivasiBerkasListHandler(IAktivasiBerkasEmrGateway gateway)
    {
        _gateway = gateway;
    }

    public async Task<IEnumerable<AktivasiBerkasListItemDto>> Handle(
        AktivasiBerkasListQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.TglAwal) || string.IsNullOrWhiteSpace(request.TglAkhir))
            return [];

        return await _gateway.ListData(request.TglAwal, request.TglAkhir, cancellationToken);
    }
}
