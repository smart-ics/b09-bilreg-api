using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Nuna.Lib.CleanArchHelper;

namespace Bilreg.Application.AdmisiContext.JaminanFeature;

public interface IGetGrupJaminanJetliService : INunaService<GetGrupJaminanJetliResponse, GetGrupJaminanJetliRequest>
{
}

public record GetGrupJaminanJetliRequest(ITipeJaminanKey TipeJaminanKey);
public record GetGrupJaminanJetliResponse(ITipeJaminanKey TipeJaminanKey, string GroupJaminanId, string GroupJaminanName);
