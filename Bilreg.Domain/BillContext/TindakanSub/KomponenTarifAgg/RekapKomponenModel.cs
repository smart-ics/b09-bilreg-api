namespace Bilreg.Domain.BillContext.TindakanSub.KomponenTarifAgg
{
    public class RekapKomponenModel(string rekapKomponenId, string rekapKomponenName, decimal rekapKomponenUrut) :IRekapKomponenKey
    {
        

        public string RekapKomponenId { get; protected set; } = rekapKomponenId;
        public string RekapKomponenName { get; protected set; } = rekapKomponenName;
        public decimal RekapKomponenUrut { get; protected set; } = rekapKomponenUrut;

        public void Set(RekapKomponenModel rekapKomponen)
        {
            RekapKomponenId = rekapKomponen.RekapKomponenId;
            RekapKomponenName = rekapKomponen.RekapKomponenName;
            SetUrut(rekapKomponen.RekapKomponenUrut);
        }

        public void SetName(string name)
        {
            RekapKomponenName = name;
        }
        public void SetUrut(decimal urut)
        {
            RekapKomponenUrut = urut > 0
        ? urut
        : throw new ArgumentException("RekapKomponenUrut must be greater than zero.", nameof(urut));

        }

    }

    public interface IRekapKomponenKey
    {
        public string RekapKomponenId { get; }
    }
}
