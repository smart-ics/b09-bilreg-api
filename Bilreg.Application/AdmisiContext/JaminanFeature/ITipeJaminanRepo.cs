using Bilreg.Domain.AdmisiContext.JaminanSub;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.JaminanFeature;

public interface ITipeJaminanRepo : 
    ISaveChange<TipeJaminanType>,
    ILoadEntity<TipeJaminanType, ITipeJaminanKey>,
    IDeleteEntity<ITipeJaminanKey>,
    IListData<TipeJaminanView>
{
}

public record TipeJaminanView(
    string TipeJaminanId, string TipeJaminanName, 
    string JaminanName, string CaraBayarDkName);