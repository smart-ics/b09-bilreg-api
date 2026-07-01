using Bilreg.Application.ChargeContext.TarifFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.ChargeContext.TarifFeature;

public class TarifPolicyRepo : ITarifPolicyRepo
{
    private readonly ITarifPolicyDal _tarifPolicyDal;
    private readonly ITarifVariantDal _tarifVariantDal;
    private readonly ITarifVariantKomponenDal _tarifVariantKomponenDal;

    public TarifPolicyRepo(
        ITarifPolicyDal tarifPolicyDal,
        ITarifVariantDal tarifVariantDal,
        ITarifVariantKomponenDal tarifVariantKomponenDal)
    {
        _tarifPolicyDal = tarifPolicyDal;
        _tarifVariantDal = tarifVariantDal;
        _tarifVariantKomponenDal = tarifVariantKomponenDal;
    }

    public void SaveChanges(TarifPolicyType model)
    {
        LoadEntity(model)
            .Match(
                onSome: _ => _tarifPolicyDal.Update(TarifPolicyDto.FromModel(model)),
                onNone: () => _tarifPolicyDal.Insert(TarifPolicyDto.FromModel(model)));

        _tarifVariantKomponenDal.Delete(model);
        _tarifVariantDal.Delete(model);

        var variantDtos = model.Variants.Select(TarifVariantDto.FromModel).ToList();
        if (variantDtos.Count > 0)
            _tarifVariantDal.Insert(variantDtos);

        var komponenDtos = model.Variants
            .SelectMany(variant => variant.ListKomponen
                .Select(line => TarifVariantKomponenDto.FromModel(variant, line)))
            .ToList();
        if (komponenDtos.Count > 0)
            _tarifVariantKomponenDal.Insert(komponenDtos);
    }

    public MayBe<TarifPolicyType> LoadEntity(ITarifPolicyKey key)
    {
        var headerDto = _tarifPolicyDal.GetData(key);
        if (headerDto is null)
            return MayBe<TarifPolicyType>.None;

        var variantDtos = _tarifVariantDal.ListData(key)?.ToList() ?? [];
        var variants = variantDtos.Select(variantDto =>
        {
            var variantKey = TarifVariantType.Key(variantDto.TarifPolicyId, variantDto.ItemNo);
            var komponenDtos = _tarifVariantKomponenDal.ListData(variantKey)?.ToList() ?? [];
            var komponen = komponenDtos.Select(x => x.ToModel());
            return variantDto.ToModel(komponen);
        }).ToList();

        return MayBe.From(headerDto.ToModel(variants));
    }

    public IEnumerable<TarifPolicySummaryView> ListData(TarifPolicyListFilter filter)
    {
        var headers = _tarifPolicyDal.ListData(filter)?.ToList() ?? [];
        return headers.Select(header =>
        {
            var policyKey = TarifPolicyType.Key(header.TarifPolicyId);
            var variantCount = _tarifVariantDal.ListData(policyKey)?.Count() ?? 0;
            return new TarifPolicySummaryView(
                header.TarifPolicyId,
                header.PolicyNo,
                header.PolicyName,
                header.EffectiveDateInfo,
                (TarifPolicyStatus)header.PolicyStatus,
                variantCount);
        });
    }
}
