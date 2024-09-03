using Bilreg.Domain.AdmisiContext.JaminanSub.JaminanAgg;
using Bilreg.Domain.AdmisiContext.JaminanSub.TipeJaminanAgg;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.JaminanSub.TipeJaminanAgg
{
    public interface ITipeJaminanDal:
        IInsert<TipeJaminanModel>,
        IUpdate<TipeJaminanModel>,
        IDelete<ITipeJaminanKey>,
        IGetData<TipeJaminanModel, ITipeJaminanKey>,
        IListData<TipeJaminanModel>,
        IListData<TipeJaminanModel, IJaminanKey>
    {
    }
}
