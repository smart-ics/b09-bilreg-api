using Bilreg.Domain.ChargeContext.TarifFeature;
using MediatR;

namespace Bilreg.Application.ChargeContext.TarifFeature.UseCases;

public record TrfListTarifPolicyQry(
    TarifPolicyStatus? PolicyStatus = null,
    string Keyword = "") : IRequest<IReadOnlyList<TarifPolicySummaryView>>;

public class TrfListTarifPolicyHandler : IRequestHandler<TrfListTarifPolicyQry, IReadOnlyList<TarifPolicySummaryView>>
{
    private readonly ITarifPolicyRepo _tarifPolicyRepo;

    public TrfListTarifPolicyHandler(ITarifPolicyRepo tarifPolicyRepo) =>
        _tarifPolicyRepo = tarifPolicyRepo;

    public Task<IReadOnlyList<TarifPolicySummaryView>> Handle(
        TrfListTarifPolicyQry request,
        CancellationToken cancellationToken)
    {
        var filter = new TarifPolicyListFilter(request.PolicyStatus, request.Keyword ?? "");
        var list = _tarifPolicyRepo.ListData(filter).ToList();
        return Task.FromResult<IReadOnlyList<TarifPolicySummaryView>>(list);
    }
}
