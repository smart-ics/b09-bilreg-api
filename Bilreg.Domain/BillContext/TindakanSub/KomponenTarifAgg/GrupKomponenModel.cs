namespace Bilreg.Domain.BillContext.TindakanSub.KomponenTarifAgg
{
    public class GrupKomponenModel(string GrupKomponenId, string GrupKomponenName) : IGrupKomponenKey
    {
        public string GrupKomponenId { get; protected set; } = string.Empty;
        public string GrupKomponenName { get; protected set; } = string.Empty;

        public void Set(GrupKomponenModel grupKomponen)
        {
            GrupKomponenId = grupKomponen.GrupKomponenId;
            GrupKomponenName = grupKomponen.GrupKomponenName;
        }
        public void SetName(string name)
        {
            GrupKomponenName = name;
        }



    }

    public interface IGrupKomponenKey
    {
        public string GrupKomponenId { get; }

    }
}
