using Bilreg.Domain.PasienContext.DemografiSub.KotaAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.PasienContext.DemografiSub.KotaAgg;

public record KotaGetQuery(string KotaId): IRequest<KotaGetResponse>, IKotaKey;

public record KotaGetResponse(string KotaId, string KotaName);

public class KotaGetHandler: IRequestHandler<KotaGetQuery, KotaGetResponse>
{
    private readonly IKotaDal _kotaDal;

    public KotaGetHandler(IKotaDal kotaDal)
    {
        _kotaDal = kotaDal;
    }
    
    public Task<KotaGetResponse> Handle(KotaGetQuery request, CancellationToken cancellationToken)
    {
        // QUERY
        var result = _kotaDal.GetData(request)
            ?? throw new KeyNotFoundException($"Kota id {request.KotaId} not found");

        // RESPONSE
        var response = new KotaGetResponse(result.KotaId, result.KotaName);
        return Task.FromResult(response);
    }
}