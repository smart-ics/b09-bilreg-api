using Ardalis.GuardClauses;

namespace Bilreg.Domain.ChargeContext.TarifFeature;

public record TarifVariantType : ITarifVariantKey
{
    private const decimal SumTolerance = 0.01m;
    private readonly List<TarifVariantKomponenType> _komponen;

    public TarifVariantType(
        string tarifPolicyId,
        int itemNo,
        string tarifId,
        string kelasId,
        string tipeTarifId,
        decimal nilai,
        string publishedNilaiTarifId,
        IEnumerable<TarifVariantKomponenType> listKomponen)
    {
        TarifPolicyId = tarifPolicyId;
        ItemNo = itemNo;
        TarifId = tarifId;
        KelasId = kelasId;
        TipeTarifId = tipeTarifId;
        Nilai = nilai;
        PublishedNilaiTarifId = publishedNilaiTarifId;
        _komponen = listKomponen?.OrderBy(x => x.NoUrut).ToList() ?? [];
        ValidateKomponenInvariants(_komponen, nilai);
    }

    #region CREATION

    public static TarifVariantType Create(
        string tarifPolicyId,
        int itemNo,
        string tarifId,
        string kelasId,
        string tipeTarifId,
        decimal nilai,
        IEnumerable<TarifVariantKomponenType> listKomponen) =>
        new(tarifPolicyId, itemNo, tarifId, kelasId, tipeTarifId, nilai, "", listKomponen);

    public static TarifVariantType Default => new(
        "-", 0, "", "", "", 0, "", []);

    public static ITarifVariantKey Key(string policyId, int itemNo) =>
        Default with { TarifPolicyId = policyId, ItemNo = itemNo };

    #endregion

    #region PROPERTIES

    public string TarifPolicyId { get; init; }
    public int ItemNo { get; init; }
    public string TarifId { get; init; }
    public string KelasId { get; init; }
    public string TipeTarifId { get; init; }
    public decimal Nilai { get; init; }
    public string PublishedNilaiTarifId { get; init; }
    public IEnumerable<TarifVariantKomponenType> ListKomponen => _komponen;

    public (string TarifId, string KelasId, string TipeTarifId) VariantCompositeKey =>
        (TarifId, KelasId, TipeTarifId);

    #endregion

    #region BEHAVIOUR

    public TarifVariantType SetKomponenLines(IEnumerable<TarifVariantKomponenType> lines)
    {
        var ordered = lines?.OrderBy(x => x.NoUrut).ToList() ?? [];
        var sum = SumKomponen(ordered);
        return new(TarifPolicyId, ItemNo, TarifId, KelasId, TipeTarifId, sum, PublishedNilaiTarifId, ordered);
    }

    public TarifVariantType ToPublishedSnapshot(string nilaiTarifId)
    {
        Guard.Against.NullOrWhiteSpace(nilaiTarifId);
        return this with { PublishedNilaiTarifId = nilaiTarifId };
    }

    public TarifVariantType WithMassAdjustedNilai(decimal percentFactor)
    {
        var multiplier = 1m + percentFactor / 100m;
        var adjustedLines = _komponen
            .Select(line => line with { Nilai = RoundMoney(line.Nilai * multiplier) })
            .ToList();
        var headerNilai = RoundMoney(Nilai * multiplier);
        return new(TarifPolicyId, ItemNo, TarifId, KelasId, TipeTarifId, headerNilai, PublishedNilaiTarifId, adjustedLines);
    }

    internal void EnsureValidForPublish()
    {
        if (_komponen.Count == 0)
            throw new InvalidOperationException(
                $"TarifVariant ItemNo {ItemNo}: minimal satu baris komponen diperlukan.");

        ValidateKomponenInvariants(_komponen, Nilai);
    }

    #endregion

    #region HELPERS

    private static void ValidateKomponenInvariants(
        IReadOnlyList<TarifVariantKomponenType> komponen,
        decimal headerNilai)
    {
        if (komponen.Count == 0)
            return;

        var duplicateNoUrut = komponen
            .GroupBy(x => x.NoUrut)
            .FirstOrDefault(g => g.Count() > 1);
        if (duplicateNoUrut is not null)
            throw new ArgumentException($"NoUrut {duplicateNoUrut.Key} duplikat pada variant komponen.");

        var sum = SumKomponen(komponen);
        if (Math.Abs(sum - headerNilai) > SumTolerance)
            throw new ArgumentException(
                $"Nilai header ({headerNilai}) harus sama dengan jumlah komponen ({sum}).");
    }

    private static decimal SumKomponen(IEnumerable<TarifVariantKomponenType> komponen) =>
        komponen.Sum(x => x.Nilai);

    private static decimal RoundMoney(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);

    #endregion
}

public interface ITarifVariantKey : ITarifPolicyKey
{
    int ItemNo { get; }
}

public record TarifVariantKomponenType(int NoUrut, KomponenReff Komponen, decimal Nilai)
{
    public static TarifVariantKomponenType Default =>
        new(0, KomponenType.Default.ToReff(), 0);
}
