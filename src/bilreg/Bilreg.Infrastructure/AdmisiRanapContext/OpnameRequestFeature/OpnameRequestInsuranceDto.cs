using Bilreg.Domain.AdmisiRanapContext.OpnameRequestFeature;

namespace Bilreg.Infrastructure.AdmisiRanapContext.OpnameRequestFeature;

public record OpnameRequestInsuranceDto(
    string OpnameRequestId,
    string TipeJaminanId,
    string TipeJaminanName,
    string ReffId)
{
    public static OpnameRequestInsuranceDto FromModel(IOpnameRequestKey key, OpnameRequestInsuranceModel model) =>
        new(key.OpnameRequestId,
            model.TipeJaminan.TipeJaminanId,
            model.TipeJaminan.TipeJaminanName,
            model.ReffId);

}
