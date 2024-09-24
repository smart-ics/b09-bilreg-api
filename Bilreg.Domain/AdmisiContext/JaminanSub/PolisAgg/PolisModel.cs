using Bilreg.Domain.AdmisiContext.JaminanSub.TipeJaminanAgg;
using Bilreg.Domain.BillContext.RoomChargeSub.KelasAgg;
using CommunityToolkit.Diagnostics;

namespace Bilreg.Domain.AdmisiContext.JaminanSub.PolisAgg;

public partial class PolisModel : IPolisKey, ITipeJaminanKey, IKelasKey
{
    public PolisModel(string id, string noPolis, string atasNama, DateTime expiredDate)
        => (PolisId, NoPolis, AtasNama, ExpiredDate) = (id, noPolis, atasNama, ExpiredDate);
    public string PolisId { get; protected set; }
    public string NoPolis { get; protected set; }
    public string AtasNama { get; protected set; }
    public DateTime ExpiredDate { get; protected set; }

    public string TipeJaminanId { get; protected set; } = string.Empty;
    public string TipeJaminanName { get; protected set; } = string.Empty;
    
    public string KelasId { get; protected set; } = string.Empty;
    public string KelasName { get; protected set; } = string.Empty;
    public bool IsCoverRajal { get; protected set; } = false;
}

