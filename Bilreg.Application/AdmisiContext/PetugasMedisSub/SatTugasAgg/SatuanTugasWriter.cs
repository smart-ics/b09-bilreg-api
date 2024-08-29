using Bilreg.Domain.AdmisiContext.PetugasMedisSub.SatTugasAgg;
using Nuna.Lib.CleanArchHelper;

namespace Bilreg.Application.AdmisiContext.PetugasMedisSub.SatTugasAgg
{
    public interface ISatuanTugasWriter : INunaWriterWithReturn<SatuanTugasModel>
    {
        void Delete(ISatuanTugasKey satuanTugasKey);
    }

    public class SatuanTugasWriter : ISatuanTugasWriter
    {
        private readonly ISatuanTugasDal _satuanTugasDal;

        public SatuanTugasWriter(ISatuanTugasDal satuanTugasDal)
        {
            _satuanTugasDal = satuanTugasDal;
        }

        public SatuanTugasModel Save(SatuanTugasModel model)
        {
            var satuanTugasDb = _satuanTugasDal.GetData(model);
            if (satuanTugasDb == null)
                _satuanTugasDal.Insert(model);
            else
                _satuanTugasDal.Update(model);

            return model;
        }

        public void Delete(ISatuanTugasKey satuanTugasKey)
        {
            _satuanTugasDal.Delete(satuanTugasKey);
        }
    }

}
