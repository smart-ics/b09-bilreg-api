namespace Bilreg.Domain.BillContext.TindakanSub.KomponenTarifAgg
{
    public class TipeRekeningModel(string tipeRekeningId, string tipeRekeningName, bool isNeraca, decimal noUrut, string debetKredit) : ITipeRekeningKey
    {
        public string TipeRekeningId { get; protected set; } = tipeRekeningId;
        public string TipeRekeningName { get; protected set; } = tipeRekeningName;
        public bool IsNeraca { get; protected set; } = isNeraca;
        public decimal NoUrut { get; protected set; } = noUrut;
        public string DebetKredit { get; protected set; } = debetKredit;

        public void Set(TipeRekeningModel model)
        {
         TipeRekeningId = model.TipeRekeningId;
         TipeRekeningName = model.TipeRekeningName;
         IsNeraca = model.IsNeraca;
         NoUrut = model.NoUrut;
         DebetKredit = model.DebetKredit;

        }

    }
    public interface ITipeRekeningKey
    {
        public string TipeRekeningId { get; }
    }
}
