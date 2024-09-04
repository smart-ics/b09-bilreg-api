using Bilreg.Domain.BillContext.TindakanSub.KomponenTarifAgg;
using Nuna.Lib.CleanArchHelper;

namespace Bilreg.Application.BillContext.TindakanSub.RekapKomponenAgg
{
    public interface IRekapKomponenWriter : INunaWriterWithReturn<RekapKomponenModel>
    {
        public void Delete(IRekapKomponenKey key);
    }
    public class RekapKomponenWriter : IRekapKomponenWriter
    {
        private readonly IRekapKomponenDal _rekapKomponenDal;
        public RekapKomponenWriter(IRekapKomponenDal rekapKomponenDal )
        {
            _rekapKomponenDal = rekapKomponenDal;
        }
        public RekapKomponenModel Save(RekapKomponenModel model)
        {
            var rekapKomponenDb = _rekapKomponenDal.GetData(model);
            if (rekapKomponenDb is null)
            {
                _rekapKomponenDal.Insert(model);
            }
            else
                _rekapKomponenDal.Update(model);
            return model;
        }
        public void Delete(IRekapKomponenKey key)
        {
            _rekapKomponenDal.Delete(key);
        }
    }


}