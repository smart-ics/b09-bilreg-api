using Bilreg.Domain.AdmisiContext.JaminanSub.GrupJaminanAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.AdmisiContext.JaminanSub.GrupJaminanAgg;

public record GrupJaminanGetQuery(string GrupJaminanId): IRequest<GrupJaminanGetResponse>, IGrupJaminanKey;

public record GrupJaminanGetResponse(
    string GrupJaminanId,
    string GrupJaminanName,
    bool IsKaryawan,
    string Keterangan);
    
public class GrupJaminanGetHandler: IRequestHandler<GrupJaminanGetQuery, GrupJaminanGetResponse>
{
    private readonly IGrupJaminanDal _grupJaminanDal;

    public GrupJaminanGetHandler(IGrupJaminanDal grupJaminanDal)
    {
        _grupJaminanDal = grupJaminanDal;
    }

    public Task<GrupJaminanGetResponse> Handle(GrupJaminanGetQuery request, CancellationToken cancellationToken)
    {
        // QUERY
        var result = _grupJaminanDal
            .GetData2(request)
            .OrThrowNotFoundException()
            .Value;
        
        // RESPONSE
        var response = new GrupJaminanGetResponse(
            result.GrupJaminanId, result.GrupJaminanName, 
            result.IsKaryawan, result.Keterangan);
        return Task.FromResult(response);
    }
}