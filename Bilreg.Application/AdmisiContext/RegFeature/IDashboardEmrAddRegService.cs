using Nuna.Lib.CleanArchHelper;

namespace Bilreg.Application.AdmisiContext.RegFeature;

public interface IDashboardEMrAddRegService : INunaServiceVoid<AddRegCmd>
{
}

//public record AddRegCmd(string RegId);


public record AddRegCmd
(



    string regId,
    string bookingId,
    string pasienId,
    string pasienName,
    string layananId,
    string dokterId,
    string tglBerobat,
    int noAntrian
);
