using Bilreg.Domain.PasienContext.DemografiSub.KabupatenAgg;
using Bilreg.Domain.PasienContext.DemografiSub.KecamatanAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.PasienContext.DemografiSub.KecamatanAgg;

public record KecamatanListQuery(string KabupatenId) : IRequest<IEnumerable<KecamatanListResponse>>, IKabupatenKey;

public record KecamatanListResponse(
    string KecamatanId,
    string KecamatanName,
    string KabupatenId,
    string KabupatenName,
    string PropinsiId,
    string PropinsiName
);

public class KecamatanListHandler : IRequestHandler<KecamatanListQuery, IEnumerable<KecamatanListResponse>>
{
    private readonly IKecamatanDal _kecamatanDal;

    public KecamatanListHandler(IKecamatanDal kecamatanDal)
    {
        _kecamatanDal = kecamatanDal;
    }

    public Task<IEnumerable<KecamatanListResponse>> Handle(KecamatanListQuery request,
        CancellationToken cancellationToken)
    {
        // QUERY
        var result = _kecamatanDal.ListData(request)
                     ?? throw new KeyNotFoundException("Kecamatan not found");

        // RESPONSE
        var response = result.Select(x
            => new KecamatanListResponse(x.KecamatanId, x.KecamatanName, 
                x.Kabupaten.KabupatenId, x.Kabupaten.KabupatenName, 
                x.Kabupaten.Propinsi.PropinsiId,
                x.Kabupaten.Propinsi.PropinsiName));
        return Task.FromResult(response);
    }
}