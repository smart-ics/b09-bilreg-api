namespace Bilreg.Domain.AdmisiContext.RegSub.RegAgg;

public record KarcisObj(
    string KarcisId,
    string KarcisName,
    string Nilai,
    IEnumerable<KomponenObj> ListKomponen);