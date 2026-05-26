using Bilreg.Domain.ChargeContext.TarifFeature;
using MediatR;

namespace Bilreg.Application.ChargeContext.TarifFeature.UseCases;

public record TrfRemoveTarifPolicyVariantCmd(
    string TarifPolicyId,
    int ItemNo) : IRequest, ITarifPolicyKey, ITarifVariantKey;

public class TrfRemoveTarifPolicyVariantHandler : IRequestHandler<TrfRemoveTarifPolicyVariantCmd>
{
    private readonly ITarifPolicyRepo _tarifPolicyRepo;

    public TrfRemoveTarifPolicyVariantHandler(ITarifPolicyRepo tarifPolicyRepo) =>
        _tarifPolicyRepo = tarifPolicyRepo;

    public Task Handle(TrfRemoveTarifPolicyVariantCmd request, CancellationToken cancellationToken)
    {
        var policy = TrfTarifPolicySupport.LoadPolicy(_tarifPolicyRepo, request);
        var updated = policy.RemoveVariant(request.ItemNo);
        _tarifPolicyRepo.SaveChanges(updated);
        return Task.CompletedTask;
    }
}
