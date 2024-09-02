using Bilreg.Domain.AdmisiContext.RujukanSub.RujukanAgg;
using Nuna.Lib.DataAccessHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Bilreg.Domain.AdmisiContext.RujukanSub.RujukanAgg.RujukanModel;

namespace Bilreg.Application.AdmisiContext.RujukanSub.RujukanAgg
{
    public interface IRujukanDal :
        IInsert<RujukanModel>,
        IUpdate<RujukanModel>,
        IGetData<RujukanModel, IRujukanKey>,
        IListData<RujukanModel>
    {
    }
}
