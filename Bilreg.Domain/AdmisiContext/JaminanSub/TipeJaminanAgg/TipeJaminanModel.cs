using Bilreg.Domain.AdmisiContext.JaminanSub.JaminanAgg;

namespace Bilreg.Domain.AdmisiContext.JaminanSub.TipeJaminanAgg;

public class TipeJaminanModel : ITipeJaminanKey
{
    public TipeJaminanModel(string id, string name, bool isAktif, JaminanModel jaminan)
    {
        if (id == string.Empty ^ name == string.Empty)
            throw new ArgumentException("Tipe Jaminan invalid");

        TipeJaminanId = id;
        TipeJaminanName = name;
        IsAktif = isAktif;
        Jaminan = jaminan.ToViewType();
    }
    public TipeJaminanModel(string id, string name, bool isAktif, JaminanViewType jaminan)
    {
        if (id == string.Empty ^ name == string.Empty)
            throw new ArgumentException("Tipe Jaminan invalid");

        TipeJaminanId = id;
        TipeJaminanName = name;
        IsAktif = isAktif;
        Jaminan = jaminan;
    }

    public static TipeJaminanModel Default => new TipeJaminanModel(
        string.Empty, string.Empty, false, JaminanModel.Default);

    public TipeJaminanViewType ToViewType() => new TipeJaminanViewType(TipeJaminanId, TipeJaminanName);
    
    public string TipeJaminanId { get; private set; }
    public string TipeJaminanName { get; private set; }
    public bool IsAktif { get; private set; }
    public JaminanViewType Jaminan { get; private set; }
}

public record TipeJaminanViewType(
    string TipeJaminanId, string TipeJaminanName);