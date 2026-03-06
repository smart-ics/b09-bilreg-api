using Nuna.Lib.CleanArchHelper;

namespace Bilreg.Application.AdmisiContext.RegFeature;

public interface IDashboardAddRegService : INunaServiceVoid<AddRegCmd>
{
}

public record AddRegCmd(string RegId, string BookingId, string PasienId, string PasienName,
    string LayananId, string DokterId, string TglBerobat, int NoAntrian);
