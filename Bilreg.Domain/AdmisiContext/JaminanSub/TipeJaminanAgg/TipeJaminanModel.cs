using Bilreg.Domain.AdmisiContext.JaminanSub.JaminanAgg;
using CommunityToolkit.Diagnostics;

namespace Bilreg.Domain.AdmisiContext.JaminanSub.TipeJaminanAgg;

public class TipeJaminanModel(string tipeJaminanId, string tipeJaminanName) : ITipeJaminanKey, IJaminanKey
{
    #region PROPERTY
    public string TipeJaminanId { get; protected set; } = tipeJaminanId;
    public string TipeJaminanName { get; protected set; } = tipeJaminanName;
    public bool IsAktif { get; protected set; } = true;
    public string JaminanId { get; protected set; } = string.Empty;
    public string JaminanName { get; protected set; } = string.Empty;
    #endregion

    #region BEHAVIOUR
    public void Activate() => IsAktif = true;

    public void Deactivate() => IsAktif = false;
    
    public void Set(JaminanModel jaminan)
    {
        Guard.IsNotNull(jaminan);

        JaminanId = jaminan.JaminanId;
        JaminanName = jaminan.JaminanName;
    }
    
    public void SetName(string name)
    {
        TipeJaminanName = name;
    }
    #endregion
}

public interface ITipeJaminanKey
{
    string TipeJaminanId { get; }
}