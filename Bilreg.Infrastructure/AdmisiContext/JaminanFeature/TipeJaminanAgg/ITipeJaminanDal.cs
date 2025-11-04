using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.AdmisiContext.JaminanFeature.TipeJaminanAgg;

public interface ITipeJaminanDal :
    IInsert<TipeJaminanDto>,
    IUpdate<TipeJaminanDto>,
    IDelete<ITipeJaminanKey>,
    IGetData<TipeJaminanDto, ITipeJaminanKey>,
    IListData<TipeJaminanDto>
{
}