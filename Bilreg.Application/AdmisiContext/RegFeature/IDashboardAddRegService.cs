using Nuna.Lib.CleanArchHelper;

namespace Bilreg.Application.AdmisiContext.RegFeature;

public interface IDashboardAddRegService : INunaServiceVoid<AddRegCmd>
{
}

public record AddRegCmd(string regId);
