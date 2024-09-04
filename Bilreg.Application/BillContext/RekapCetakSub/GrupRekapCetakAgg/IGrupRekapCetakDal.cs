using Bilreg.Domain.BillContext.RekapCetakSub.GrupRekapCetakAgg;
using Nuna.Lib.DataAccessHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bilreg.Application.BillContext.RekapCetakSub.GrupRekapCetakAgg
{
    public interface IGrupRekapCetakDal :
        IInsert<GrupRekapCetakModel>,
        IUpdate<GrupRekapCetakModel>,
        IDelete<IGrupRekapCetakKey>,
        IGetData<GrupRekapCetakModel,IGrupRekapCetakKey>,
        IListData<GrupRekapCetakModel>
    {
    }
}
