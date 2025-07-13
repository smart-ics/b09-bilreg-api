using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.JaminanSub.JaminanAgg;

namespace Bilreg.Domain.AdmisiContext.JaminanSub;

public record TipeJaminanType : ITipeJaminanKey
{
    public TipeJaminanType(string tipeJaminanId, string tipeJaminanName)
    {
        Guard.Against.NullOrWhiteSpace(tipeJaminanId, nameof(tipeJaminanId));
        Guard.Against.NullOrWhiteSpace(tipeJaminanName, nameof(tipeJaminanName));

        TipeJaminanId = tipeJaminanId;
        TipeJaminanName = tipeJaminanName;
    }
    
    public string TipeJaminanId { get; init; }
    public string TipeJaminanName { get; init; }
    public bool IsAktif { get; init; }
    public JaminanReff Jaminan { get; init; }
    
    public TipeJaminanReff ToReff() => new TipeJaminanReff(TipeJaminanId, TipeJaminanName); 
    public static ITipeJaminanKey Key(string id) => new TipeJaminanType(id, "-");
    public static TipeJaminanType Default => new("-", "-");
}

public interface ITipeJaminanKey
{
    string TipeJaminanId {get;}
}

public record TipeJaminanReff(string TipeJaminanId, string TipeJaminanName);