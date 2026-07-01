using Ardalis.GuardClauses;
using Bilreg.Domain.ChargeContext.TarifFeature;
using MediatR;

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

    public TrfCopyTarifPolicyHandler(ITarifPolicyRepo tarifPolicyRepo) =>
        _tarifPolicyRepo = tarifPolicyRepo;

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
            request.UserId);

        _tarifPolicyRepo.SaveChanges(copy);

        return Task.FromResult(new TrfCopyTarifPolicyResponse(
            copy.TarifPolicyId,
            copy.PolicyNo,
            copy.PolicyName,
            copy.PolicyStatus,
            copy.Variants.Count()));
    }
}
