using Bilreg.Domain.PasienContext.SukuFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.PasienContext.StatusSosialSub.SukuAgg
{
    public interface ISukuDal :
        IInsert<SukuType>,
        IUpdate<SukuType>,
        IDelete<ISukuKey>,
        IGetData2<SukuType, ISukuKey>,
        IListData2<SukuType>
    {
    }
}
