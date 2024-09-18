namespace Bilreg.Domain.AdmisiContext.RegSub;

public record TindakanObj(
    string TindakanId,
    string TarifId,
    string TarifName,
    string Nilai,
    IEnumerable<KomponenObj> ListKomponen);