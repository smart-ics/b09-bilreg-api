using Bilreg.Domain.AdmPasienContext.SukuAgg;
using Nuna.Lib.CleanArchHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bilreg.Application.AdmPasienContext.SukuAgg
{
    public interface ISukuWriter : INunaWriterWithReturn<SukuModel>
    {
    }

    public class SukuWriter : ISukuWriter
    {
        private readonly ISukuDal _sukuDal;

        public SukuWriter(ISukuDal sukuDal)
        {
            _sukuDal = sukuDal;
        }

        public SukuModel Save(SukuModel model)
        {
            var sukuDb = _sukuDal.GetData(model);
            if (sukuDb is null)
                _sukuDal.Insert(model);
            else
                _sukuDal.Update(model);

            return model;
        }
    }


}
