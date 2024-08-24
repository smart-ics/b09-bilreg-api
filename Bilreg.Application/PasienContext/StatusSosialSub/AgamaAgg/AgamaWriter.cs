using Bilreg.Domain.PasienContext.StatusSosialSub.AgamaAgg;
using Nuna.Lib.CleanArchHelper;

namespace Bilreg.Application.PasienContext.StatusSosialSub.AgamaAgg
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
