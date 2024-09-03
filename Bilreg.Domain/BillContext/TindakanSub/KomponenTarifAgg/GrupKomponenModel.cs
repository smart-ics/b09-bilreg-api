namespace Bilreg.Domain.BillContext.TindakanSub.KomponenTarifAgg
{
    public class GrupKomponenModel(string grupKomponenId, string grupKomponenName) : IGrupKomponenKey
    {
        public string GrupKomponenId { get; protected set; } = grupKomponenId;
        public string GrupKomponenName { get; protected set; } = grupKomponenName;

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
