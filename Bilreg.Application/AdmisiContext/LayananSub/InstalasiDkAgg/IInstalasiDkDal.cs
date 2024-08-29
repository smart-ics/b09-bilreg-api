using Bilreg.Domain.AdmisiContext.LayananSub.InstalasiDkAgg;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.LayananSub.InstalasiDkAgg
{
    public interface IInstalasiDkDal :
        IGetData<InstalasiDkModel, IInstalasiDkKey>,
        IListData<InstalasiDkModel>
    {
    }
}
