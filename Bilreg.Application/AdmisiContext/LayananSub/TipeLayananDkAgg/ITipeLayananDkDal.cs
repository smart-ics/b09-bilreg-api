using Bilreg.Domain.AdmisiContext.LayananSub.TipeLayananDkAgg;
using Nuna.Lib.DataAccessHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bilreg.Application.AdmisiContext.LayananSub.TipeLayananDkAgg
{
    public interface ITipeLayananDkDal :
        IGetData<TipeLayananDkModel, ITipeLayananDkKey>,
        IListData<TipeLayananDkModel>
    {

    }
}
