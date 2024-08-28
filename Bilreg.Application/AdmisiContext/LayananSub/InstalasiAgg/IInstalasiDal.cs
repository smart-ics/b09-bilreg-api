using Bilreg.Domain.AdmisiContext.LayananSub.InstalasiAgg;
using Bilreg.Domain.AdmisiContext.LayananSub.InstalasiDkAgg;
using Nuna.Lib.DataAccessHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bilreg.Application.AdmisiContext.LayananSub.InstalasiAgg
{
    public interface IInstalasiDal:
        IInsert<InstalasiModel>,
        IUpdate<InstalasiModel>,
        IDelete<IInstalasiKey>,
        IGetData<InstalasiModel,IInstalasiKey>,
        IListData<InstalasiModel>

    {
    }
}
