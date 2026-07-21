using Ardalis.GuardClauses;
using Bilreg.Domain.ChargeContext.TarifFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.ChargeContext.TarifFeature.UseCases;

public record TrfCopyTarifPolicyCmd(
    string TarifPolicyId,
    string NewPolicyNo,
    string NewPolicyName,
    string UserId) : IRequest<TrfCopyTarifPolicyResponse>, ITarifPolicyKey;

public record TrfCopyTarifPolicyResponse(
    string TarifPolicyId,
    string PolicyNo,
    string PolicyName,
    TarifPolicyStatus PolicyStatus,
    int VariantCount);

public class TrfCopyTarifPolicyHandler : IRequestHandler<TrfCopyTarifPolicyCmd, TrfCopyTarifPolicyResponse>
{
    private readonly ITarifPolicyRepo _tarifPolicyRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public TrfCopyTarifPolicyHandler(ITarifPolicyRepo tarifPolicyRepo, ITglJamProvider tglJamProvider)
    {
        _tarifPolicyRepo = tarifPolicyRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task<TrfCopyTarifPolicyResponse> Handle(
        TrfCopyTarifPolicyCmd request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.UserId);

        var source = TrfTarifPolicySupport.LoadPolicy(_tarifPolicyRepo, request);
        var copy = TarifPolicyType.CopyFrom(
            source,
            request.NewPolicyNo,
            request.NewPolicyName,
            request.UserId,
            _tglJamProvider.Now);

        _tarifPolicyRepo.SaveChanges(copy);

        return Task.FromResult(new TrfCopyTarifPolicyResponse(
            copy.TarifPolicyId,
            copy.PolicyNo,
            copy.PolicyName,
            copy.PolicyStatus,
            copy.Variants.Count()));
    }
}
