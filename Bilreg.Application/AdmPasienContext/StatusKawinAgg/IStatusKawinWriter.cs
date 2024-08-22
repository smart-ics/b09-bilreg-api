using Bilreg.Domain.AdmPasienContext.StatusKawinAgg;
using Nuna.Lib.CleanArchHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bilreg.Application.AdmPasienContext.StatusKawinAgg
{
    public interface IStatusKawinWriter : INunaWriterWithReturn<StatusKawinModel>
    {
        void Delete(IStatusKawinKey statuskawinKey);
    }
    public class StatusKawinWriter : IStatusKawinWriter
    {
        private readonly IStatusKawinDal _statuskawinDal;

        public StatusKawinWriter(IStatusKawinDal statusKawinDal)
        {
            _statuskawinDal = statusKawinDal;
        }

        public StatusKawinModel Save(StatusKawinModel model)
        {
            var statuskawinDb = _statuskawinDal.GetData(model);
            if (statuskawinDb is null)
            {
                _statuskawinDal.Insert(model);
            }
            else
            {
                _statuskawinDal.Update(model);
            }
            return model;
        }

        public void Delete(IStatusKawinKey statuskawinKey)
        {
            _statuskawinDal.Delete(statuskawinKey);
        }

    }
}

