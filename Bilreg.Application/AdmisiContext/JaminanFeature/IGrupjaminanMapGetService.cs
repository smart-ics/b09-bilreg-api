using Nuna.Lib.CleanArchHelper;

namespace Bilreg.Application.AdmisiContext.JaminanFeature;

public interface IGetGrupJaminanJetliService : INunaService<GetGrupJaminanJetliResponse, GetGrupJaminanJetliRequest>
{
}

public record GetGrupJaminanJetliRequest(string TipeJaminanId);
public record GetGrupJaminanJetliResponse(string TipeJaminanId, string GroupJaminanId, string GroupJaminanName);
