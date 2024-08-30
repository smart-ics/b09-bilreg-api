using Bilreg.Domain.BillContext.RoomChargeSub.KelasAgg;
using Nuna.Lib.DataAccessHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bilreg.Application.BillContext.RoomChargeSub.KelasAgg
{
    public interface IKelasDal :
        IInsert<KelasModel>,
        IUpdate<KelasModel>,
        IDelete<IKelasKey>,
        IGetData<KelasModel,IKelasKey>,
        IListData<KelasModel>
        ;
}
