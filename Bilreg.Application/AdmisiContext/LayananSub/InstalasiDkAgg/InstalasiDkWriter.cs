using Bilreg.Domain.AdmisiContext.LayananSub.InstalasiAgg;
using Bilreg.Domain.AdmisiContext.LayananSub.InstalasiDkAgg;
using Nuna.Lib.CleanArchHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bilreg.Application.AdmisiContext.LayananSub.InstalasiDkAgg
{
    public interface IInstalasiDkWriter: INunaWriterWithReturn<InstalasiDkModel>
    {
        void Delete(IInstalasiDkKey instalasiDkKey);
    }

    public class InstalasiDkWriter : IInstalasiDkWriter
    {
        private readonly IInstalasiDkDal _instalasiDkDal;

        public InstalasiDkWriter(IInstalasiDkDal instalasiDkDal)
        {
            _instalasiDkDal = instalasiDkDal;
        }

        public InstalasiDkModel Save(InstalasiDkModel model)
        {
            var instalasiDkDb = _instalasiDkDal.GetData(model);
            if (instalasiDkDb == null)
                _instalasiDkDal.Insert(model);
            else
                _instalasiDkDal.Update(model);

            return model;
        }

        public void Delete(IInstalasiDkKey instalasiDkkey)
        {
            _instalasiDkDal.Delete(instalasiDkkey);
        }
    }
}
