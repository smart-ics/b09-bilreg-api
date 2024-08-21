using Bilreg.Domain.AdmPasienContext.StatusKawinAgg;
using Nuna.Lib.DataAccessHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bilreg.Application.AdmPasienContext.StatusKawinAgg
{
    public interface IStatusKawinDal :
                IInsert<StatusKawinModel>,
                IUpdate<StatusKawinModel>,
                IDelete<IStatusKawinKey>,
                IGetData<StatusKawinModel, IStatusKawinKey>,
                IListData<IStatusKawinKey>

    {

    }
}
