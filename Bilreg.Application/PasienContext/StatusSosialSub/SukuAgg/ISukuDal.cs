using Bilreg.Domain.PasienContext.StatusSosialSub.SukuAgg;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.PasienContext.StatusSosialSub.SukuAgg
{
    public interface ISukuDal :
        IInsert<SukuModel>,
        IUpdate<SukuModel>,
        IDelete<ISukuKey>,
        IGetData2<SukuModel, ISukuKey>,
        IListData2<SukuModel>
    {
    }
}
