using Bilreg.Domain.AdmisiContext.JaminanSub.JaminanAgg;
using Bilreg.Domain.AdmisiContext.JaminanSub.TipeJaminanAgg;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.JaminanSub.TipeJaminanAgg
{
    public interface ITipeJaminanDal:
        IInsert<TipeJaminanModel>,
        IUpdate<TipeJaminanModel>,
        IDelete<ITipeJaminanKey>,
        IGetData2<TipeJaminanModel, ITipeJaminanKey>,
        IListData2<TipeJaminanModel>,
        IListData2<TipeJaminanModel, IJaminanKey>
    {
    }
}
