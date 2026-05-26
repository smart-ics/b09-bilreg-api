using Ardalis.GuardClauses;
using Bilreg.Domain.ChargeContext.TarifFeature;
using MediatR;

namespace Bilreg.Application.ChargeContext.TarifFeature.UseCases;

public record TrfReviewTarifPolicyCmd(
    string TarifPolicyId,
    string UserId) : IRequest<TrfReviewTarifPolicyResponse>, ITarifPolicyKey;

public record TrfReviewTarifPolicyResponse(
    string TarifPolicyId,
    TarifPolicyStatus PolicyStatus);

public class TrfReviewTarifPolicyHandler : IRequestHandler<TrfReviewTarifPolicyCmd, TrfReviewTarifPolicyResponse>
{
    private readonly ITarifPolicyRepo _tarifPolicyRepo;

    public TrfReviewTarifPolicyHandler(ITarifPolicyRepo tarifPolicyRepo) =>
        _tarifPolicyRepo = tarifPolicyRepo;

    public Task<TrfReviewTarifPolicyResponse> Handle(
        TrfReviewTarifPolicyCmd request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.UserId);

        var policy = TrfTarifPolicySupport.LoadPolicy(_tarifPolicyRepo, request);
        var reviewed = policy.MarkReviewed(request.UserId);
        _tarifPolicyRepo.SaveChanges(reviewed);

        return Task.FromResult(new TrfReviewTarifPolicyResponse(
            reviewed.TarifPolicyId,
            reviewed.PolicyStatus));
    }
}
