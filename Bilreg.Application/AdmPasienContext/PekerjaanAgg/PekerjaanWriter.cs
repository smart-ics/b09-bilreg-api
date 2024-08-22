using Bilreg.Application.AdmPasienContext.PekerjaanAgg;
using Bilreg.Domain.AdmPasienContext.PekerjaanAgg;
using Nuna.Lib.CleanArchHelper;

namespace Bilreg.Application.AdmPasienContext.PekerjaanContext
{
    public interface IPekerjaanWriter : INunaWriterWithReturn<PekerjaanModel>
    {
        void Delete(IPekerjaanKey pekerjaanKey);
    }

    public class PekerjaanWriter : IPekerjaanWriter
    {
        private readonly IPekerjaanDal _pekerjaanDal;

        public PekerjaanWriter(IPekerjaanDal pekerjaanDal)
        {
            _pekerjaanDal = pekerjaanDal;
        }

        public PekerjaanModel Save(PekerjaanModel model)
        {
            var pekerjaanDb = _pekerjaanDal.GetData(model);
            if (pekerjaanDb == null)
                _pekerjaanDal.Insert(model);
            else
                _pekerjaanDal.Update(model);

            return model;
        }

        public void Delete(IPekerjaanKey pekerjaanKey)
        {
            _pekerjaanDal.Delete(pekerjaanKey);
        }
    }
}
