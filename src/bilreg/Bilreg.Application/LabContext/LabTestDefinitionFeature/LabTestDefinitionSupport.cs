using Bilreg.Application.ChargeContext.TarifFeature;
using Bilreg.Application.LabContext.LabComponentMasterFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.LabContext.LabComponentMasterFeature;
using Bilreg.Domain.LabContext.LabTestDefinitionFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Application.LabContext.LabTestDefinitionFeature;

internal static class LabTestDefinitionSupport
{
    public static IReadOnlyDictionary<string, LabComponentMasterModel> BuildComponentCatalog(
        ILabComponentMasterRepo componentMasterRepo,
        IEnumerable<LabTestComponentInput> inputs)
    {
        var ids = inputs.Select(x => x.ComponentId).Distinct().ToList();
        var catalog = new Dictionary<string, LabComponentMasterModel>(StringComparer.Ordinal);

        foreach (var componentId in ids)
        {
            componentMasterRepo.LoadEntity(LabComponentMasterModel.Key(componentId))
                .Match(
                    onSome: m => catalog[componentId] = m,
                    onNone: () => { });
        }

        return catalog;
    }

    public static IEnumerable<LabTestComponentModel> ToComponentModels(IEnumerable<LabTestComponentInput> inputs) =>
        inputs.Select(x => new LabTestComponentModel(
            x.SequenceNo,
            x.ComponentId,
            x.ReferenceRangeOverride ?? string.Empty,
            x.RequiredFlagging,
            x.IsMandatory));

    public static void EnsureNoActiveTarifConflict(
        ILabTestDefinitionRepo repo,
        string tarifId,
        bool isActive,
        string? excludeTestDefinitionId)
    {
        if (!isActive)
            return;

        if (repo.HasActiveTarifConflict(tarifId, excludeTestDefinitionId))
            throw new InvalidOperationException(
                "LAB_TEST_DEFINITION_TARIF_CONFLICT: Another active LabTestDefinition already exists for this TarifId.");
    }

    public static decimal ResolveTarifPrice(INilaiTarifRepo? nilaiTarifRepo, string tarifId)
    {
        if (nilaiTarifRepo is null || string.IsNullOrWhiteSpace(tarifId))
            return 0m;

        try
        {
            var tarifKey = TarifType.Key(tarifId);

            // Level 1: UMUM + Kelas III
            var umumKey = NilaiTarifType.KeyComposite(
                tarifKey,
                TipeTarifType.Key("UMUM"),
                KelasType.Key("III"));
            var umumOpt = nilaiTarifRepo.LoadEntity(umumKey);
            if (umumOpt.HasValue && umumOpt.Value.Nilai > 0)
                return umumOpt.Value.Nilai;

            // Level 2: RJ + Kelas III
            var rjKey = NilaiTarifType.KeyComposite(
                tarifKey,
                TipeTarifType.Key("RJ"),
                KelasType.Key("III"));
            var rjOpt = nilaiTarifRepo.LoadEntity(rjKey);
            if (rjOpt.HasValue && rjOpt.Value.Nilai > 0)
                return rjOpt.Value.Nilai;

            // Level 3: Default + Default
            var fallbackKey = NilaiTarifType.KeyComposite(
                tarifKey,
                TipeTarifType.Default,
                KelasType.Default);
            var fallbackOpt = nilaiTarifRepo.LoadEntity(fallbackKey);
            if (fallbackOpt.HasValue && fallbackOpt.Value.Nilai > 0)
                return fallbackOpt.Value.Nilai;

            // Level 4: First active rate for this tarif
            var firstActiveOpt = nilaiTarifRepo.LoadFirstActiveRate(tarifKey);
            if (firstActiveOpt.HasValue && firstActiveOpt.Value.Nilai > 0)
                return firstActiveOpt.Value.Nilai;
        }
        catch
        {
            // Fail-safe default
        }

        return 0m;
    }
}
