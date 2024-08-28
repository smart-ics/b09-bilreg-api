using Bilreg.Domain.AdmisiContext.RujukanSub.TipeRujukanAgg;
using Nuna.Lib.DataAccessHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bilreg.Application.AdmisiContext.RujukanSub.TipeRujukanAgg
{
    public interface ITipeRujukanDal :
        IInsert<TipeRujukanModel>,
        IUpdate<TipeRujukanModel>,
        IDelete<ITipeRujukanKey>,
        IGetData<TipeRujukanModel,ITipeRujukanKey>,
        IListData<TipeRujukanModel>
    {
    }
}
