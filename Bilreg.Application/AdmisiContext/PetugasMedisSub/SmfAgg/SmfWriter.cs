using Bilreg.Domain.AdmisiContext.PetugasMedisSub.SmfAgg;
using Nuna.Lib.CleanArchHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bilreg.Application.AdmisiContext.PetugasMedisSub.SmfAgg
{
    public interface ISmfWriter : INunaWriterWithReturn<SmfModel>
    {
        void Delete(ISmfKey smfKey);
    }
    public class SmfWriter : ISmfWriter
    {
        private readonly ISmfDal _smfDal;

        public SmfWriter(ISmfDal smfDal)
        {
            _smfDal = smfDal;
        }

        public SmfModel Save(SmfModel model)
        {
            var smfDb = _smfDal.GetData(model);
            if (smfDb == null)
                _smfDal.Insert(model);
            else
                _smfDal.Update(model);

            return model;
        }

        public void Delete(ISmfKey smfKey)
        {
            _smfDal.Delete(smfKey);
        }
    }

}
