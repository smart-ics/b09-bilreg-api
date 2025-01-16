using Bilreg.Domain.AdmisiContext.JaminanSub.TipeJaminanAgg;
using Bilreg.Domain.BillContext.RoomChargeSub.KelasAgg;

namespace Bilreg.Domain.AdmisiContext.JaminanSub.PolisAgg;

public partial class PolisModel : IPolisKey, ITipeJaminanKey, IKelasKey
{
    public string PolisId { get; protected set; } = string.Empty;
    public string NoPolis { get; protected set; } = string.Empty;
    public string AtasNama { get; protected set; } = string.Empty;
    public DateTime ExpiredDate { get; protected set; } = new DateTime(3000, 1, 1);

    public string TipeJaminanId { get; protected set; } = string.Empty;
    public string TipeJaminanName { get; protected set; } = string.Empty;
    public string JaminanId { get; protected set; } = string.Empty;
    public string JaminanName { get; protected set; } = string.Empty;
    
    public string KelasId { get; protected set; } = string.Empty;
    public string KelasName { get; protected set; } = string.Empty;
    public bool IsCoverRajal { get; protected set; } = false;
    public List<PolisCoverModel> ListCover { get; protected set; } = [];
}


