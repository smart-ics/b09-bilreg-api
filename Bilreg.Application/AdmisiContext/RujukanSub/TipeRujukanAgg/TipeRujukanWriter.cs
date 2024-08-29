using Bilreg.Domain.AdmisiContext.RujukanSub.TipeRujukanAgg;
using Nuna.Lib.CleanArchHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bilreg.Application.AdmisiContext.RujukanSub.TipeRujukanAgg
{
    public interface ITipeRujukanWriter : INunaWriterWithReturn<TipeRujukanModel>
    {
        void Delete(ITipeRujukanKey tipeRujukanDkKey);
    }

    public class TipeRujukanWriter : ITipeRujukanWriter 
    {
        private readonly ITipeRujukanDal _tipeRujukanDal;
        public TipeRujukanWriter(ITipeRujukanDal tipeRujukanDal)
        {
            _tipeRujukanDal = tipeRujukanDal;
        }

        public TipeRujukanModel Save(TipeRujukanModel model)
        {
            var tipeRujukanDb = _tipeRujukanDal.GetData(model);
            if (tipeRujukanDb == null)
                _tipeRujukanDal.Insert(model);
            else
                _tipeRujukanDal.Update(model);

            return model;
        }

        public void Delete(ITipeRujukanKey tipeRujukanKey)
        {
            _tipeRujukanDal.Delete(tipeRujukanKey);
        }

    }
}
