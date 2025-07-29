using Bilreg.Domain.AdmisiContext.JaminanSub;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.JaminanSub.TipeJaminanAgg
{
    public interface ITipeJaminanDal :
        IInsert<TipeJaminanType>,
        IUpdate<TipeJaminanType>,
        IDelete<ITipeJaminanKey>,
        IGetDataMayBe<TipeJaminanType, ITipeJaminanKey>,
        IListDataMayBe<TipeJaminanType>,
        IListDataMayBe<TipeJaminanType, IJaminanKey>
    {
    }
}
