namespace Bilreg.Domain.ChargeContext.TindakanFeature;

public record TindakanTarifModel
{
    private readonly List<TindakanKomponenTarifModel> _listKomponen;
    #region CREATION
    public TindakanTarifModel(string tindakanId, int noUrut, string tarifId, string tarifName,
        IEnumerable<TindakanKomponenTarifModel> listKomponen)
    {
        TindakanId = tindakanId;
        NoUrut = noUrut;
        TarifId = tarifId;
        TarifName = tarifName;
        _listKomponen = listKomponen.ToList() ?? new List<TindakanKomponenTarifModel>();
    }
    public static TindakanTarifModel Default => new TindakanTarifModel(
        "-", -1, "-", "-", []);
    #endregion

    public string TindakanId { get; init; }
    public int NoUrut { get; init; }
    public string TarifId { get; init; }
    public string TarifName { get; init; }
    public decimal Total => _listKomponen.Sum(t => t.Nilai);
    public IEnumerable<TindakanKomponenTarifModel> ListKomponen => _listKomponen;
}

public record TindakanKomponenTarifModel(
    string TindakanId,
    string TarifId,
    string KomponenTarifId,
    string KomponenTarifName,
    string PpaId,
    string PpaName,
    decimal Qty,
    decimal Nilai
);
