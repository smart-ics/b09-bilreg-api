using Bilreg.Domain.PasienContext.DemografiFeature;
using JetBrains.Annotations;
using MediatR;

namespace Bilreg.Application.PasienContext.DemografiFeature;

public record KecamatanListQuery(string KabupatenId) : IRequest<IEnumerable<KecamatanListResponse>>;

[PublicAPI]
public record KecamatanListResponse(string KecamatanId, string KecamatanName, 
    KabupatenReff Kabupaten, PropinsiType Propinsi);

public class KecamatanListHandler : IRequestHandler<KecamatanListQuery, IEnumerable<KecamatanListResponse>>
{
    private readonly IKecamatanDal _kecamatanDal;

    public KecamatanListHandler(IKecamatanDal kecamatanDal)
    {
        _kecamatanDal = kecamatanDal;
    }

    public Task<IEnumerable<KecamatanListResponse>> Handle(KecamatanListQuery request, CancellationToken cancellationToken)
        => _kecamatanDal.ListData(KabupatenType.Key(request.KabupatenId))
            .Match(
                onSome: x => Task.FromResult(x.Select(y 
                    => new KecamatanListResponse(y.KecamatanId, y.KecamatanName, y.Kabupaten, y.Propinsi))),
                onNone: () => throw new KeyNotFoundException($"Kecamatan not found"));
}