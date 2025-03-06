namespace Bilreg.Domain.AdmisiContext.JaminanSub.TipeJaminanAgg;

public interface ITipeJaminanKey
{
    string TipeJaminanId { get; }
}

public record TipeJaminanKey(string TipeJaminanId) : ITipeJaminanKey;