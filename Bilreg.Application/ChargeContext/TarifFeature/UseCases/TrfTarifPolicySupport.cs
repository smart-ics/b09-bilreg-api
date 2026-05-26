using Ardalis.GuardClauses;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Application.ChargeContext.TarifFeature.UseCases;

public record TrfVariantKomponenInput(string KomponenId, decimal Nilai);

internal static class TrfTarifPolicySupport
{
    public static TarifPolicyType LoadPolicy(ITarifPolicyRepo repo, ITarifPolicyKey key) =>
        repo.LoadEntity(key).Match(
            onSome: p => p,
            onNone: () => throw new KeyNotFoundException(
                $"TarifPolicy '{key.TarifPolicyId}' tidak ditemukan."));

    public static IReadOnlyList<TarifVariantKomponenType> ToKomponenLines(
        IEnumerable<TrfVariantKomponenInput> komponen)
    {
        Guard.Against.Null(komponen);
        var list = komponen.ToList();
        if (list.Count == 0)
            throw new ArgumentException("Minimal satu baris komponen diperlukan.");

        var lines = new List<TarifVariantKomponenType>();
        var noUrut = 1;
        foreach (var item in list)
        {
            Guard.Against.NullOrWhiteSpace(item.KomponenId);
            var komponenReff = new KomponenReff(item.KomponenId, "-");
            lines.Add(new TarifVariantKomponenType(noUrut, komponenReff, item.Nilai));
            noUrut++;
        }

        return lines;
    }

    public static decimal SumKomponen(IEnumerable<TrfVariantKomponenInput> komponen) =>
        ToKomponenLines(komponen).Sum(x => x.Nilai);
}
