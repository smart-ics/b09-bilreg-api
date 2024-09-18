namespace Bilreg.Domain.BillContext.TindakanSub.KomponenTarifAgg;

public class KomponenModel(string id, string name): IKomponenKey
{
    #region PROPERTIES

    public string KomponenId { get; protected set; } = id;
    public string KomponenName { get; protected set; } = name;

    #endregion
}

public interface IKomponenKey
{
    string KomponenId { get; }
}