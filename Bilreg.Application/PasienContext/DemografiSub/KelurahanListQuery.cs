using Bilreg.Domain.PasienContext.DemografiFeature;
using JetBrains.Annotations;
using MediatR;

namespace Bilreg.Application.PasienContext.DemografiSub;

public record KelurahanListQuery(string KecamatanId) : IRequest<IEnumerable<KelurahanListResponse>>;

[PublicAPI]
public record KelurahanListResponse(string KelurahanId, string KelurahanName, 
    KecamatanReff Kecamatan, KabupatenReff Kabupaten, PropinsiType Propinsi);

public class KelurahanListHandler : IRequestHandler<KelurahanListQuery, IEnumerable<KelurahanListResponse>>
{
    private readonly IKelurahanDal _kelurahanDal;

    public KelurahanListHandler(IKelurahanDal kelurahanDal)
    {
        _kelurahanDal = kelurahanDal;
    }

    public Task<IEnumerable<KelurahanListResponse>> Handle(KelurahanListQuery request, CancellationToken cancellationToken)
        => _kelurahanDal.ListData(KecamatanType.Key(request.KecamatanId))
            .Match(
                onSome: x => Task.FromResult(x.Select(y 
                    => new KelurahanListResponse(y.KelurahanId, y.KelurahanName, y.Kecamatan, y.Kabupaten, y.Propinsi))),
                onNone: () => throw new KeyNotFoundException($"Kelurahan not found"));
}