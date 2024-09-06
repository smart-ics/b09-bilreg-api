namespace Bilreg.Domain.BillContext.TindakanSub.KomponenTarifAgg
{
    public class GrupKomponenModel(string grupKomponenId, string grupKomponenName) : IGrupKomponenKey
    {
        public string GrupKomponenId { get; protected set; } = grupKomponenId;
        public string GrupKomponenName { get; protected set; } = grupKomponenName;
    }

    public interface IGrupKomponenKey
    {
        public string GrupKomponenId { get; }

    }
}
