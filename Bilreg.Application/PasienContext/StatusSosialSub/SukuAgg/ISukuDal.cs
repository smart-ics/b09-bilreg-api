using Bilreg.Domain.PasienContext.StatusSosialSub.SukuAgg;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.PasienContext.StatusSosialSub.SukuAgg
{
    public interface ISukuDal :
        IInsert<SukuModel>,
        IUpdate<SukuModel>,
        IDelete<ISukuKey>,
        IGetData<SukuModel, ISukuKey>,
        IListData<SukuModel>
    {
    }
}
