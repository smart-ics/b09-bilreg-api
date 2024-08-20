using Bilreg.Domain.AdmPasienContext.AgamaAgg;
using Nuna.Lib.DataAccessHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bilreg.Application.AdmPasienContext.AgamaContext
{
    public interface IAgamaDal : 
        IInsert<AgamaModel>,
        IUpdate<AgamaModel>,
        IDelete<IAgamaKey>,
        IGetData<AgamaModel, IAgamaKey>,
        IListData<AgamaModel>
    {
    }
}
