namespace Bilreg.Domain.BillContext.TindakanSub.KomponenTarifAgg;

public class KomponenTarifModel(string id, string name): IKomponenTarifKey
{
    #region PROPERTIES

    public string KomponenId { get; protected set; } = id;
    public string KomponenName { get; protected set; } = name;

    #endregion
}

public interface IKomponenTarifKey
{
    string KomponenId { get; }
}