using Bilreg.Domain.BillContext.RoomChargeSub.KelasDkAgg;
using Nuna.Lib.DataAccessHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bilreg.Application.BillContext.RoomChargeSub.KelasDkAgg
{
    public interface IKelasDkDal: 
        IGetData<KelasDkModel,IKelasDkKey>,
        IListData<KelasDkModel>
    {
    }
}
