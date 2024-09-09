using Bilreg.Domain.AdmisiContext.LayananSub.LayananAgg;
using Nuna.Lib.CleanArchHelper;

namespace Bilreg.Application.AdmisiContext.LayananSub.LayananAgg
{
    public interface ILayananWriter: INunaWriterWithReturn<LayananModel>
    {
    }
    public class LayananWriter : ILayananWriter
    {
        private readonly ILayananDal _layananDal;

        public LayananWriter(ILayananDal layananDal)
        {
            _layananDal = layananDal;
        }

        public LayananModel Save(LayananModel model)
        {
            var existingLayanan = _layananDal.GetData(model);
            if (existingLayanan is null)
                _layananDal.Insert(model);
            else
                _layananDal.Update(model);
            return model;
        }
    }
}
