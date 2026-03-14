using Nuna.Lib.CleanArchHelper;

namespace Bilreg.Application.AdmisiContext.RegFeature;

public interface IDashboardEMrAddRegService : INunaServiceVoid<AddRegCmd>
{
}

public record AddRegCmd(string RegId);
