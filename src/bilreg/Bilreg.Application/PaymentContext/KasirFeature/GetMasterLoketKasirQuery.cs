using Ardalis.GuardClauses;
using MediatR;

namespace Bilreg.Application.PaymentContext.KasirFeature;

public record GetMasterLoketKasirQuery() : IRequest<IEnumerable<LoketKasirDto>>;

public class GetMasterLoketKasirHandler : IRequestHandler<GetMasterLoketKasirQuery, IEnumerable<LoketKasirDto>>
{
    private readonly ILoketKasirDal _loketKasirDal;

    public GetMasterLoketKasirHandler(ILoketKasirDal loketKasirDal)
    {
        _loketKasirDal = loketKasirDal;
    }

    public Task<IEnumerable<LoketKasirDto>> Handle(GetMasterLoketKasirQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));
        var result = _loketKasirDal.ListAktif() ?? Enumerable.Empty<LoketKasirDto>();
        return Task.FromResult(result);
    }
}
