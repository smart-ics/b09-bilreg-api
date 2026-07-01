using Bilreg.Application.ChargeContext.TindakanFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using MediatR;

namespace Bilreg.Application.ChargeContext.TarifFeature.UseCases;

public record TrfGetTarifPolicyQry(string TarifPolicyId) : IRequest<TrfGetTarifPolicyResponse>, ITarifPolicyKey;

public record TrfGetTarifPolicyResponse(
    string TarifPolicyId,
    string PolicyNo,
    string PolicyName,
    DateTime EffectiveDateInfo,
    string Description,
    TarifPolicyStatus PolicyStatus,
    IReadOnlyList<TrfGetTarifPolicyVariantResponse> Variants);

public record TrfGetTarifPolicyVariantResponse(
    int ItemNo,
    string TarifId,
    string KelasId,
    string TipeTarifId,
    decimal Nilai,
    string PublishedNilaiTarifId,
    IReadOnlyList<TrfGetTarifPolicyKomponenResponse> Komponen);

public record TrfGetTarifPolicyKomponenResponse(
    int NoUrut,
    string KomponenId,
    string KomponenName,
    decimal Nilai);

public class TrfGetTarifPolicyHandler : IRequestHandler<TrfGetTarifPolicyQry, TrfGetTarifPolicyResponse>
{
    private readonly ITarifPolicyRepo _tarifPolicyRepo;
    private readonly IKomponenRepo _komponenRepo;

    public TrfGetTarifPolicyHandler(ITarifPolicyRepo tarifPolicyRepo, IKomponenRepo komponenRepo)
    {
        _tarifPolicyRepo = tarifPolicyRepo;
        _komponenRepo = komponenRepo;
    }

    public Task<TrfGetTarifPolicyResponse> Handle(
        TrfGetTarifPolicyQry request,
        CancellationToken cancellationToken)
    {
        var policy = TrfTarifPolicySupport.LoadPolicy(_tarifPolicyRepo, request);

        var komponenKeys = policy.Variants
            .SelectMany(v => v.ListKomponen)
            .Select(k => k.Komponen)
            .DistinctBy(k => k.KomponenId)
            .ToList();
        var komponenMasters = komponenKeys.Count == 0
            ? []
            : _komponenRepo.ListData(komponenKeys)?.ToList() ?? [];

        var variants = policy.Variants
            .OrderBy(v => v.ItemNo)
            .Select(v => MapVariant(v, komponenMasters))
            .ToList();

        return Task.FromResult(new TrfGetTarifPolicyResponse(
            policy.TarifPolicyId,
            policy.PolicyNo,
            policy.PolicyName,
            policy.EffectiveDateInfo,
            policy.Description,
            policy.PolicyStatus,
            variants));
    }

    private static TrfGetTarifPolicyVariantResponse MapVariant(
        TarifVariantType variant,
        IReadOnlyList<KomponenType> komponenMasters)
    {
        var komponen = variant.ListKomponen
            .Select(line =>
            {
                var master = komponenMasters.FirstOrDefault(m => m.KomponenId == line.Komponen.KomponenId);
                return new TrfGetTarifPolicyKomponenResponse(
                    line.NoUrut,
                    line.Komponen.KomponenId,
                    master?.KomponenName ?? line.Komponen.KomponenName,
                    line.Nilai);
            })
            .ToList();

        return new TrfGetTarifPolicyVariantResponse(
            variant.ItemNo,
            variant.TarifId,
            variant.KelasId,
            variant.TipeTarifId,
            variant.Nilai,
            variant.PublishedNilaiTarifId,
            komponen);
    }
}
