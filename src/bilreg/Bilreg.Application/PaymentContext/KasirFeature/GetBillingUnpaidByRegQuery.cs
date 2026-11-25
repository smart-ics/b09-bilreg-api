using Ardalis.GuardClauses;
using MediatR;

namespace Bilreg.Application.PaymentContext.KasirFeature;

public record GetBillingUnpaidByRegQuery(string RegId) : IRequest<IEnumerable<BillingUnpaidItemDto>>;

public class GetBillingUnpaidByRegHandler : IRequestHandler<GetBillingUnpaidByRegQuery, IEnumerable<BillingUnpaidItemDto>>
{
    private readonly ITrsKasirItemDal _trsKasirItemDal;

    public GetBillingUnpaidByRegHandler(ITrsKasirItemDal trsKasirItemDal)
    {
        _trsKasirItemDal = trsKasirItemDal;
    }

    public Task<IEnumerable<BillingUnpaidItemDto>> Handle(GetBillingUnpaidByRegQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));
        if (string.IsNullOrWhiteSpace(request.RegId))
        {
            return Task.FromResult(Enumerable.Empty<BillingUnpaidItemDto>());
        }

        var result = _trsKasirItemDal.ListUnpaidBillingByReg(request.RegId.Trim()) ?? Enumerable.Empty<BillingUnpaidItemDto>();
        return Task.FromResult(result);
    }
}
