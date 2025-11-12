using Bilreg.Domain.BillContext.BedUsageFeature;
using Bilreg.Domain.BillContext.TindakanSub.TipeTarifAgg;
using System.Xml;

namespace Bilreg.Domain.BillContext.TindakanSub.TarifFeature;

public class TarifNilaiModel : INilaiTarifKey
{
    private readonly List<TarifNilaiKomponenModel> _listKomponen;
    #region  CREATION
    public TarifNilaiModel(string tarifNilaiId, string tarifId, 
        string tarifName, DateOnly tglBerlaku, DateOnly tglExpired, 
        IEnumerable<TarifNilaiKomponenModel> listKomponen)
    {
        TarifNilaiId = tarifNilaiId;
        TarifId = tarifId;
        TarifName = tarifName;
        TglBerlaku = tglBerlaku;
        TglExpired = tglExpired;
        _listKomponen = listKomponen.ToList();
    }
    #endregion

    #region PROPERTIES
    public string TarifNilaiId { get; init; }
    public string TarifId { get; init; }
    public string TarifName { get; init; }
    public DateOnly TglBerlaku { get; init; }
    public DateOnly TglExpired { get; init; }
    public IEnumerable<TarifNilaiKomponenModel> ListKomponen => _listKomponen;
    #endregion
}

public class TarifNilaiKomponenModel
{
    public string TarifNilaiId { get; init; }
    public string TarifId { get; init; }
    public string TarifName { get; init; }
    public KelasReff Kelas { get; init; }
    public TipeTarifModel TipeTarif { get; init; }
    public DetilTarifType DetilTarif { get; init; }
    public long Nilai {  get; init; }
}


public interface INilaiTarifKey
{
    string TarifNilaiId { get; }
}