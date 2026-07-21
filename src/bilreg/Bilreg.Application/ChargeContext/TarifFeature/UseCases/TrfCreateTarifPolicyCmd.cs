using Ardalis.GuardClauses;
using Bilreg.Domain.ChargeContext.TarifFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.ChargeContext.TarifFeature.UseCases;

public record TrfCreateTarifPolicyCmd(
    string PolicyNo,
    string PolicyName,
    DateTime EffectiveDateInfo,
    string Description,
    string UserId) : IRequest<TrfCreateTarifPolicyResponse>;

public record TrfCreateTarifPolicyResponse(
    string TarifPolicyId,
    string PolicyNo,
    string PolicyName,
    TarifPolicyStatus PolicyStatus);

public class TrfCreateTarifPolicyHandler : IRequestHandler<TrfCreateTarifPolicyCmd, TrfCreateTarifPolicyResponse>
{
    private readonly ITarifPolicyRepo _tarifPolicyRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public TrfCreateTarifPolicyHandler(ITarifPolicyRepo tarifPolicyRepo, ITglJamProvider tglJamProvider)
    {
        _tarifPolicyRepo = tarifPolicyRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task<TrfCreateTarifPolicyResponse> Handle(
        TrfCreateTarifPolicyCmd request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.UserId);

        var policy = TarifPolicyType.Create(
            request.PolicyNo,
            request.PolicyName,
            request.EffectiveDateInfo,
            request.Description,
            request.UserId,
            _tglJamProvider.Now);

        _tarifPolicyRepo.SaveChanges(policy);

        return Task.FromResult(new TrfCreateTarifPolicyResponse(
            policy.TarifPolicyId,
            policy.PolicyNo,
            policy.PolicyName,
            policy.PolicyStatus));
    }
}
