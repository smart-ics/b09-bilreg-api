using Bilreg.Domain.PasienContext.DemografiSub.KabupatenAgg;
using Bilreg.Domain.PasienContext.DemografiSub.KecamatanAgg;
using Bilreg.Domain.PasienContext.DemografiSub.KelurahanAgg;
using Bilreg.Domain.PasienContext.DemografiSub.PropinsiAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.PasienContext.DemografiSub.KelurahanAgg;

public record KelurahanGetQuery(string KelurahanId): IRequest<KelurahanGetResponse>, IKelurahanKey;

public record KelurahanGetResponse(
    string KelurahanId,
    string KelurahanName,
    string KecamatanId,
    string KecamatanName,
    string KabupatenId,
    string KabupatenName,
    string PropinsiId,
    string PropinsiName,
    string KodePos
    );

public class KelurahanGetHandler : IRequestHandler<KelurahanGetQuery, KelurahanGetResponse>
{
    private readonly IKelurahanDal _kelurahanDal;

    public KelurahanGetHandler(IKelurahanDal kelurahanDal)
    {
        _kelurahanDal = kelurahanDal;
    }

    public Task<KelurahanGetResponse> Handle(KelurahanGetQuery request, CancellationToken cancellationToken)
    {
        // QUERY
        var result = _kelurahanDal.GetData(request)
                     ?? throw new KeyNotFoundException($"Kelurahan id {request.KelurahanId} not found");

        // RESPONSE
        var response = new KelurahanGetResponse(
            result.KelurahanId, result.KelurahanName, 
            result.Kecamatan.KecamatanId, result.Kecamatan.KecamatanName,
            result.Kecamatan.Kabupaten.KabupatenId, result.Kecamatan.Kabupaten.KabupatenName, 
            result.Kecamatan.Kabupaten.Propinsi.PropinsiId, result.Kecamatan.Kabupaten.Propinsi.PropinsiName, 
            result.KodePos);
        return Task.FromResult(response);
    }
}