using Bilreg.Application.AdmPasienContext.StatusKawinAgg;
using Bilreg.Domain.AdmPasienContext.StatusKawinAgg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bilreg.Infrastructure.AdmPasienContext.StatusKawinAgg
{
    public class StatusKawinDal : IStatusKawinDal
    {
        public void Delete(IStatusKawinKey key)
        {
            throw new NotImplementedException();
        }

        public StatusKawinModel GetData(IStatusKawinKey key)
        {
            throw new NotImplementedException();
        }

        public void Insert(StatusKawinModel model)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IStatusKawinKey> ListData()
        {
            throw new NotImplementedException();
        }

        public void Update(StatusKawinModel model)
        {
            throw new NotImplementedException();
        }
    }
}
