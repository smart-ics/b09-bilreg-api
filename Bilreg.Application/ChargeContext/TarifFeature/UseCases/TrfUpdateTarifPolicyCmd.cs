using Ardalis.GuardClauses;
using Bilreg.Domain.ChargeContext.TarifFeature;
using MediatR;

namespace Bilreg.Application.ChargeContext.TarifFeature.UseCases;

public record TrfUpdateTarifPolicyCmd(
    string TarifPolicyId,
    string PolicyNo,
    string PolicyName,
    DateTime EffectiveDateInfo,
    string Description,
    string UserId) : IRequest, ITarifPolicyKey;

public class TrfUpdateTarifPolicyHandler : IRequestHandler<TrfUpdateTarifPolicyCmd>
{
    private readonly ITarifPolicyRepo _tarifPolicyRepo;

    public TrfUpdateTarifPolicyHandler(ITarifPolicyRepo tarifPolicyRepo) =>
        _tarifPolicyRepo = tarifPolicyRepo;

    public Task Handle(TrfUpdateTarifPolicyCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.UserId);

        var policy = TrfTarifPolicySupport.LoadPolicy(_tarifPolicyRepo, request);
        var updated = policy.UpdateMetadata(
            request.PolicyNo,
            request.PolicyName,
            request.EffectiveDateInfo,
            request.Description,
            request.UserId);

        _tarifPolicyRepo.SaveChanges(updated);
        return Task.CompletedTask;
    }
}
