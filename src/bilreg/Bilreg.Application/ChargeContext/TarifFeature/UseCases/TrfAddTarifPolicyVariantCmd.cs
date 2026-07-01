using Ardalis.GuardClauses;
using Bilreg.Domain.ChargeContext.TarifFeature;
using MediatR;

namespace Bilreg.Application.ChargeContext.TarifFeature.UseCases;

public record TrfAddTarifPolicyVariantCmd(
    string TarifPolicyId,
    string TarifId,
    string KelasId,
    string TipeTarifId,
    IReadOnlyList<TrfVariantKomponenInput> Komponen,
    string UserId) : IRequest<TrfAddTarifPolicyVariantResponse>, ITarifPolicyKey;

public record TrfAddTarifPolicyVariantResponse(int ItemNo);

public class TrfAddTarifPolicyVariantHandler
    : IRequestHandler<TrfAddTarifPolicyVariantCmd, TrfAddTarifPolicyVariantResponse>
{
    private readonly ITarifPolicyRepo _tarifPolicyRepo;

    public TrfAddTarifPolicyVariantHandler(ITarifPolicyRepo tarifPolicyRepo) =>
        _tarifPolicyRepo = tarifPolicyRepo;

    public Task<TrfAddTarifPolicyVariantResponse> Handle(
        TrfAddTarifPolicyVariantCmd request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.UserId);

        var policy = TrfTarifPolicySupport.LoadPolicy(_tarifPolicyRepo, request);
        var lines = TrfTarifPolicySupport.ToKomponenLines(request.Komponen);
        var nilai = lines.Sum(x => x.Nilai);

        var updated = policy.AddVariant(
            request.TarifId,
            request.KelasId,
            request.TipeTarifId,
            nilai,
            lines);

        _tarifPolicyRepo.SaveChanges(updated);

        var itemNo = updated.Variants.Max(v => v.ItemNo);
        return Task.FromResult(new TrfAddTarifPolicyVariantResponse(itemNo));
    }
}
