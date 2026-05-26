using Ardalis.GuardClauses;
using Bilreg.Domain.ChargeContext.TarifFeature;
using MediatR;

namespace Bilreg.Application.ChargeContext.TarifFeature.UseCases;

public record TrfUpdateTarifPolicyVariantCmd(
    string TarifPolicyId,
    int ItemNo,
    string TarifId,
    string KelasId,
    string TipeTarifId,
    IReadOnlyList<TrfVariantKomponenInput> Komponen,
    string UserId) : IRequest, ITarifPolicyKey, ITarifVariantKey;

public class TrfUpdateTarifPolicyVariantHandler : IRequestHandler<TrfUpdateTarifPolicyVariantCmd>
{
    private readonly ITarifPolicyRepo _tarifPolicyRepo;

    public TrfUpdateTarifPolicyVariantHandler(ITarifPolicyRepo tarifPolicyRepo) =>
        _tarifPolicyRepo = tarifPolicyRepo;

    public Task Handle(TrfUpdateTarifPolicyVariantCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.UserId);

        var policy = TrfTarifPolicySupport.LoadPolicy(_tarifPolicyRepo, request);
        var lines = TrfTarifPolicySupport.ToKomponenLines(request.Komponen);

        var updated = policy.UpdateVariant(
            request.ItemNo,
            request.TarifId,
            request.KelasId,
            request.TipeTarifId,
            lines);

        _tarifPolicyRepo.SaveChanges(updated);
        return Task.CompletedTask;
    }
}
