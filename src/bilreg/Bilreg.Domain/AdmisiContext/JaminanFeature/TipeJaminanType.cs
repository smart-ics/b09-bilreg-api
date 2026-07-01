using Ardalis.GuardClauses;

namespace Bilreg.Domain.AdmisiContext.JaminanFeature;

public record TipeJaminanType : ITipeJaminanKey
{
    #region CREATION
    public TipeJaminanType(string tipeJaminanId, string tipeJaminanName, 
        bool isAktif, JaminanReff jaminan, CaraBayarDkType caraBayarDk)
    {
        TipeJaminanId = tipeJaminanId;
        TipeJaminanName = tipeJaminanName;
        IsAktif = isAktif;
        Jaminan = jaminan;
        CaraBayarDk = caraBayarDk;
    }

    public static TipeJaminanType Create(string tipeJaminanId, string tipeJaminanName,
        JaminanType jaminan)
    {
        var result = new TipeJaminanType(tipeJaminanId, tipeJaminanName, true, 
            jaminan.ToReff(), jaminan.CaraBayarDk);
        return result;
    }
    public static TipeJaminanType Default => new("-", "-", true, 
        JaminanType.Default.ToReff(), CaraBayarDkType.Default);
    public static ITipeJaminanKey Key(string id) => Default with { TipeJaminanId = id };
    public static TipeJaminanType BayarSendiri =>
        new("00000", "Membayar Sendiri", true, 
            JaminanType.Umum.ToReff(), CaraBayarDkType.BayarSendiri);

    #endregion
    
    public string TipeJaminanId { get; init; }
    public string TipeJaminanName { get; init; }
    public bool IsAktif { get; init; }
    public JaminanReff Jaminan { get; init; }
    public CaraBayarDkType CaraBayarDk { get; init; }
    
    public TipeJaminanReff ToReff() => new TipeJaminanReff(TipeJaminanId, TipeJaminanName); 
}

public interface ITipeJaminanKey
{
    string TipeJaminanId {get;}
}

public record TipeJaminanReff(string TipeJaminanId, string TipeJaminanName)
    : ITipeJaminanKey;