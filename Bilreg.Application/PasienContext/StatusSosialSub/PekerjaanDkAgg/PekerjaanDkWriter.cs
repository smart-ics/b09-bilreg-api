using Bilreg.Domain.PasienContext.StatusSosialSub.PekerjaanDkAgg;
using Nuna.Lib.CleanArchHelper;

namespace Bilreg.Application.PasienContext.StatusSosialSub.PekerjaanDkAgg
{
    public interface IPekerjaanDkWriter : INunaWriterWithReturn<PekerjaanDkModel>
    {
        void Delete(IPekerjaanDkKey pekerjaanDkKey);
    }

    public class PekerjaanDkWriter : IPekerjaanDkWriter
    {
        private readonly IPekerjaanDkDal _pekerjaanDkDal;

        public PekerjaanDkWriter(IPekerjaanDkDal pekerjaanDkDal)
        {
            _pekerjaanDkDal = pekerjaanDkDal;
        }

        public PekerjaanDkModel Save(PekerjaanDkModel model)
        {
            var pekerjaanDb = _pekerjaanDkDal.GetData(model);
            if (pekerjaanDb == null)
                _pekerjaanDkDal.Insert(model);
            else
                _pekerjaanDkDal.Update(model);

            return model;
        }

        public void Delete(IPekerjaanDkKey pekerjaanDkKey)
        {
            _pekerjaanDkDal.Delete(pekerjaanDkKey);
        }
    }
}
