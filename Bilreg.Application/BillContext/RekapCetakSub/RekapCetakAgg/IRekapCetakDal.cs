using Bilreg.Domain.BillContext.RekapCetakSub.RekapCetakAgg;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.BillContext.RekapCetakSub.RekapCetakAgg;

public interface IRekapCetakDal :
    IInsert<RekapCetakModel>,
    IUpdate<RekapCetakModel>,
    IDelete<IRekapCetakKey>,
    IGetData<RekapCetakModel, IRekapCetakKey>,
    IListData<RekapCetakModel>
{
    
}