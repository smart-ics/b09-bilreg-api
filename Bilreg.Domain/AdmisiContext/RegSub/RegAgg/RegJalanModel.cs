using Bilreg.Domain.AdmisiContext.RegSub.KarcisAgg;
using Bilreg.Domain.AdmisiContext.RegSub.RegAgg.ValueObjects;
using Bilreg.Domain.PasienContext.DataSosialPasienSub.PasienAgg;

namespace Bilreg.Domain.AdmisiContext.RegSub.RegAgg;

public class RegJalanModel : AbstractRegModel
{

    public RegJalanModel(string regId) : base(regId)
    {
    }

    public RegJalanModel(string regId, TglJamTrsVo tglJamTrs, VoidFlagVo voidStamp, 
        RegOutFlagVo regOut, RegPasienVo pasien, RegTipeJaminanVo tipeJaminan, 
        RegCaraMasukVo caraMasuk, LampiranRujukanVo lampiranRujukan) 
        : base(regId, tglJamTrs, voidStamp, 
            regOut, pasien, tipeJaminan, 
            caraMasuk, lampiranRujukan)
    {
    }
    public string KarcisId { get; private set; } = string.Empty;
    public string KarcisName { get; private set; } = string.Empty;
    public string TindakanId { get; private set; } = string.Empty;
    public string TindakanName { get; private set; } = string.Empty;
    public List<RegJalanLayananModel> ListLayanan { get; private set; }

    public void SetKarcis(KarcisModel karcis)
    {
        
    }

    public void SetKarcis(string karcisId, string karcisName, string tindakanId, string tindakanName)
    {
        
    }
}

public class RegJalanLayananModel
{
    public string LayananId { get; private set; } = string.Empty;
    public string LayananName { get; private set; } = string.Empty;
    public string DokterId { get; private set; } = string.Empty;
    public string DokterName { get; private set; } = string.Empty;
    public string JamBerobat { get; private set; } = string.Empty;
    public int NoAntrian { get; private set; }
}