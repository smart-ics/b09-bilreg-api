using Bilreg.Domain.AdmisiContext.LayananSub.InstalasiAgg;
using Nuna.Lib.CleanArchHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bilreg.Application.AdmisiContext.LayananSub.InstalasiAgg
{
    public interface InstalasiWriter : INunaWriterWithReturn<InstalasiModel>
    {
        public void Delete(IInstalasiKey key);
    }
    public class IInstalasiWriter : InstalasiWriter
    {
        private readonly IInstalasiDal _instalasiDal;
        public IInstalasiWriter(IInstalasiDal instalasiDal)
        {
            _instalasiDal = instalasiDal;
        }
        public InstalasiModel Save(InstalasiModel model)
        {
            var instalasiDb = _instalasiDal.GetData(model);
            if (instalasiDb is null)
                _instalasiDal.Insert(model);
            else
                _instalasiDal.Update(model);
            return model;
        }
        public void Delete(IInstalasiKey key)
        {
            _instalasiDal.Delete(key);
        }
    }
}
