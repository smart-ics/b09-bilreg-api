namespace Bilreg.Domain.ChargeContext.TarifFeature;

public record TarifVariantType : ITarifVariantKey
{
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
    }

    public static TarifVariantType Default => new(
        "-", 0, "", "", "", 0, "", []);

    public static ITarifVariantKey Key(string policyId, int itemNo) =>
        Default with { TarifPolicyId = policyId, ItemNo = itemNo };

    public string TarifPolicyId { get; init; }
    public int ItemNo { get; init; }
    public string TarifId { get; init; }
    public string KelasId { get; init; }
    public string TipeTarifId { get; init; }
    public decimal Nilai { get; init; }
    public string PublishedNilaiTarifId { get; init; }
    public IEnumerable<TarifVariantKomponenType> ListKomponen => _komponen;
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
