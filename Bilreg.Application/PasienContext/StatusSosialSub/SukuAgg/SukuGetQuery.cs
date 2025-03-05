using Bilreg.Domain.PasienContext.StatusSosialSub.SukuAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.PasienContext.StatusSosialSub.SukuAgg;

public record SukuGetQuery(string SukuId) : IRequest<SukuGetResponse>, ISukuKey;

public record SukuGetResponse(string SukuId, string SukuName);

public class SukuGetHandler : IRequestHandler<SukuGetQuery, SukuGetResponse>
{
    private readonly ISukuDal _SukuDal;

    public SukuGetHandler(ISukuDal SukuDal)
    {
        _SukuDal = SukuDal;
    }

    public Task<SukuGetResponse> Handle(SukuGetQuery request, CancellationToken cancellationToken)
    {
        //  QUERY
        var result = _SukuDal
            .GetData2(request)
            .OrThrowNotFoundException()
            .Value; 

        //  RESPONSE
        var response = new SukuGetResponse(result.SukuId, result.SukuName);
        return Task.FromResult(response);
    }
}