using Bilreg.Domain.AdmPasienContext.AgamaAgg;
using Nuna.Lib.CleanArchHelper;

namespace Bilreg.Application.AdmPasienContext.AgamaContext
{
    public interface IAgamaWriter : INunaWriterWithReturn<AgamaModel>
    {
        void Delete(IAgamaKey agamaKey);
    }
    
    public class AgamaWriter : IAgamaWriter
    {
        private readonly IAgamaDal _agamaDal;

        public AgamaWriter(IAgamaDal agamaDal)
        {
            _agamaDal = agamaDal;
        }

        public AgamaModel Save(AgamaModel model)
        {
            var agamaDb = _agamaDal.GetData(model);
            if (agamaDb == null)
                _agamaDal.Insert(model);
            else
                _agamaDal.Update(model);

            return model;
        }

        public void Delete(IAgamaKey agamaKey)
        {
            _agamaDal.Delete(agamaKey);
        }
    }
}
