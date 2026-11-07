using Bilreg.Domain.AdmisiContext.JaminanFeature;

namespace Bilreg.Domain.AdmisiRanapContext.InpatientOrderFeature;

public record InpatientOrderInsuranceModel
{
    public InpatientOrderInsuranceModel(TipeJaminanReff tipeJaminan, string jaminanReffId)
    {
        TipeJaminan = tipeJaminan ?? new TipeJaminanReff("-", "");
        JaminanReffId = string.IsNullOrWhiteSpace(jaminanReffId) ? "-" : jaminanReffId;
    }

    public InpatientOrderInsuranceModel(string tipeJaminanId, string tipeJaminanName, string jaminanReffId)
        : this(new TipeJaminanReff(string.IsNullOrWhiteSpace(tipeJaminanId) ? "-" : tipeJaminanId, tipeJaminanName ?? ""), jaminanReffId)
    {
    }

    public static InpatientOrderInsuranceModel Default =>
        new(new TipeJaminanReff("-", ""), "-");

    public TipeJaminanReff TipeJaminan { get; init; }
    public string JaminanReffId { get; init; }
}
