using Bilreg.Domain.AdmPasienContext.StatusKawinDkAgg;
using Nuna.Lib.DataAccessHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bilreg.Application.AdmPasienContext.StatusKawinAgg
{
    public interface IStatusKawinDkDal :
                IInsert<StatusKawinDkModel>,
                IUpdate<StatusKawinDkModel>,
                IDelete<IStatusKawinDkKey>,
                IGetData<StatusKawinDkModel, IStatusKawinDkKey>,
                IListData<StatusKawinDkModel>

    {

    }
}
