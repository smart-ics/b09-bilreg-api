using Nuna.Lib.CleanArchHelper;

namespace Bilreg.Application.AdmisiContext.JaminanFeature;

public interface IGrupjaminanMapGetService : INunaService<GrupJaminanMapGetResponse, GrupJaminanMapGetParam>
{
}

public record GrupJaminanMapGetParam(string TipeJaminanId);
public record GrupJaminanMapGetResponse(string tipeJaminanId, string groupJaminanId, string groupJaminanName);
