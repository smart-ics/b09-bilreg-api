using Bilreg.Domain.BillContext.TindakanSub.KomponenTarifAgg;
using Nuna.Lib.CleanArchHelper;

namespace Bilreg.Application.BillContext.TindakanSub.KomponenTarifAgg
{
    public interface IGrupKomponenWriter : INunaWriterWithReturn<GrupKomponenModel>
    {
        public void Delete(IGrupKomponenKey key);
    }
    public class GrupKomponenWriter : IGrupKomponenWriter
    {
        private readonly IGrupKomponenDal _grupKomponenDal;
        public GrupKomponenWriter(IGrupKomponenDal grupKomponenDal)
        {
            _grupKomponenDal = grupKomponenDal;
        }
        public GrupKomponenModel Save(GrupKomponenModel model)
        {
            var grupKomponenDb = _grupKomponenDal.GetData(model);
            if (grupKomponenDb is null)
                _grupKomponenDal.Insert(model);
            else
                _grupKomponenDal.Update(model);
            return model;
        }
        public void Delete(IGrupKomponenKey key)
        {
            _grupKomponenDal.Delete(key);
        }
    }
}