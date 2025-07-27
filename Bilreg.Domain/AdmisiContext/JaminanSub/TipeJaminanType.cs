using Ardalis.GuardClauses;

namespace Bilreg.Domain.AdmisiContext.JaminanSub;

public record TipeJaminanType : ITipeJaminanKey
{
    public TipeJaminanType(string tipeJaminanId, string tipeJaminanName, 
        bool isAktif, JaminanReff jaminan)
    {
        Guard.Against.NullOrWhiteSpace(tipeJaminanId, nameof(tipeJaminanId));
        Guard.Against.NullOrWhiteSpace(tipeJaminanName, nameof(tipeJaminanName));
        Guard.Against.Null(jaminan, nameof(jaminan));

        TipeJaminanId = tipeJaminanId;
        TipeJaminanName = tipeJaminanName;
        IsAktif = isAktif;
        Jaminan = jaminan;
    }
    
    public string TipeJaminanId { get; init; }
    public string TipeJaminanName { get; init; }
    public bool IsAktif { get; init; }
    public JaminanReff Jaminan { get; init; }
    
    public TipeJaminanReff ToReff() => new TipeJaminanReff(TipeJaminanId, TipeJaminanName); 
    public static TipeJaminanType Default => new("-", "-", true, JaminanType.Default.ToReff());
    public static ITipeJaminanKey Key(string id) => Default with { TipeJaminanId = id };
}

public interface ITipeJaminanKey
{
    string TipeJaminanId {get;}
}

public record TipeJaminanReff(string TipeJaminanId, string TipeJaminanName);