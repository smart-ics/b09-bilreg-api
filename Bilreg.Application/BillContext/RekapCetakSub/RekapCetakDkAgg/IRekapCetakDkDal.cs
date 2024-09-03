using Bilreg.Domain.BillContext.RekapCetakSub.RekapCetakDkAgg;
using Nuna.Lib.DataAccessHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bilreg.Application.BillContext.RekapCetakSub.RekapCetakDkAgg
{
    public interface IRekapCetakDkDal :
        IGetData<RekapCetakDkModel,IRekapCetakDkKey>,
        IListData<RekapCetakDkModel>
    {
    }
}
