using Bilreg.Domain.AdmisiContext.LayananSub.TipeLayananDkAgg;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.LayananSub.TipeLayananDkAgg
{
    public interface ITipeLayananDkDal :
        IGetData<TipeLayananDkModel, ITipeLayananDkKey>,
        IListData<TipeLayananDkModel>
    {

    }
}
