using Bilreg.Domain.PasienContext.DemografiSub.KabupatenAgg;
using Bilreg.Domain.PasienContext.DemografiSub.KecamatanAgg;
using Bilreg.Domain.PasienContext.DemografiSub.PropinsiAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.PasienContext.DemografiSub.KecamatanAgg;

public record KecamatanGetQuery(string KecamatanId): IRequest<KecamatanGetResponse>, IKecamatanKey;

public record KecamatanGetResponse(
    string KecamatanId,
    string KecamatanName,
    string KabupatenId,
    string KabupatenName,
    string PropinsiId,
    string PropinsiName
    );
    
public class KecamatanGetHandler: IRequestHandler<KecamatanGetQuery, KecamatanGetResponse>
{
    private readonly IKecamatanDal _kecamatanDal;

    public KecamatanGetHandler(IKecamatanDal kecamatanDal)
    {
        _kecamatanDal = kecamatanDal;
    }
    
    public Task<KecamatanGetResponse> Handle(KecamatanGetQuery request, CancellationToken cancellationToken)
    {
        // QUERY
        var result = _kecamatanDal.GetData(request)
            ?? throw new KeyNotFoundException($"Kecamatan id: {request.KecamatanId} not found");

        // RESPONSE
        var response = new KecamatanGetResponse(result.KecamatanId, result.KecamatanName, 
            result.Kabupaten.KabupatenId, result.Kabupaten.KabupatenName,
            result.Kabupaten.Propinsi.PropinsiId, result.Kabupaten.Propinsi.PropinsiName);
        return Task.FromResult(response);
    }
}