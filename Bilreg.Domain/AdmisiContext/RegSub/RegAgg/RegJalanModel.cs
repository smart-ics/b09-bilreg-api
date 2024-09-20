using Bilreg.Domain.PasienContext.DataSosialPasienSub.PasienAgg;

namespace Bilreg.Domain.AdmisiContext.RegSub.RegAgg;

public class RegJalanModel : AbstractRegModel
{
    public RegJalanModel(string regId, PasienModel pasien)
    {
        JenisReg = JenisRegEnum.RegJalan;
        RegDate = DateTime.Now;
        PasienId = pasien.PasienId;
        PasienName = pasien.PasienId;
        NoMedRec = pasien.GetNoMedrec();
        TglLahir = pasien.TglLahir;
        Gender = pasien.Gender;
    }

    public string KarcisId { get; protected set; } = string.Empty;
    public string KarcisName { get; protected set; } = string.Empty;
    public string TindakanId { get; protected set; } = string.Empty;
    public string TindakanName { get; protected set; } = string.Empty;
    public List<RegJalanLayananModel> ListLayanan { get; protected set; }
}

public class RegJalanLayananModel
{
    public string LayananId { get; protected set; } = string.Empty;
    public string LayananName { get; protected set; } = string.Empty;
    public string DokterId { get; protected set; } = string.Empty;
    public string DokterName { get; protected set; } = string.Empty;
    public string JamBerobat { get; protected set; } = string.Empty;
    public int NoAntrian { get; protected set; }
}