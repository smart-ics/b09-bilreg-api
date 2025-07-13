using Bilreg.Domain.AdmisiContext.LayananSub;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.LayananSub.TipeLayananDkAgg
{
    public interface ITipeLayananDkDal :
        IGetData<TipeLayananDkModel, ITipeLayananDkKey>,
        IListData<TipeLayananDkModel>
    {

    }
}
