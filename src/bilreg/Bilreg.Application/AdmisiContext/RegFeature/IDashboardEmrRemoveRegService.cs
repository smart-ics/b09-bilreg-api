using Nuna.Lib.CleanArchHelper;

namespace Bilreg.Application.AdmisiContext.RegFeature;

public interface IDashboardEmrRemoveRegService : INunaServiceVoid<RemoveRegCmd>
{
}

public record RemoveRegCmd(string RegId);
