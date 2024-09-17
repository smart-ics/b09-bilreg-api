namespace Bilreg.Domain.AdmisiContext.RegSub;

public record KarcisObj(
    string KarcisId,
    string KarcisName,
    string Nilai,
    IEnumerable<KomponenObj> ListKomponen);