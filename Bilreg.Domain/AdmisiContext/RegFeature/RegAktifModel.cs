using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.PasienContext.PasienFeature;

namespace Bilreg.Domain.AdmisiContext.RegFeature;

public class RegAktifModel : IRegKey
{
    #region CREATION
    public RegAktifModel(string regId, DateOnly regDate,  
        PasienReff pasien, JenisRegEnum jenisReg, LayananReff layanan,
        PpaReff dokter, TipeJaminanReff tipeJaminan)
    {
        RegId = regId;
        RegDate = regDate;
        Pasien = pasien;
        JenisReg = jenisReg;
        TipeJaminan = tipeJaminan;
        Layanan = layanan;
        Dokter = dokter;
    }

    public static RegAktifModel CreateFromReg(RegModel reg)
    {
        var result = new RegAktifModel(reg.RegId, reg.RegDate,
            reg.Pasien, reg.JenisReg, reg.Layanan,
            reg.Dokter, reg.TipeJaminan);
        return result;
    }
    public static RegAktifModel Default => new RegAktifModel("-", new DateOnly(3000, 1, 1),
        PasienModel.Default.ToReff(), JenisRegEnum.RegJalan, LayananType.Default.ToReff(),
        PpaType.Default.ToReff(), TipeJaminanType.Default.ToReff());
    #endregion
    
    #region PROPERTIES
    public string RegId { get; init; }
    public DateOnly RegDate { get; init; }
    public PasienReff Pasien { get; init; }
    public JenisRegEnum JenisReg { get; init; }
    public LayananReff Layanan { get; private set; }
    public PpaReff Dokter { get; private set; }
    public TipeJaminanReff TipeJaminan { get; private set; }
    #endregion
    
    #region BEHAVIOUR
    public void ChangeJaminan(TipeJaminanReff tipeJaminan)
    {
        TipeJaminan = tipeJaminan;
    }
    
    public void TransferLayanan(LayananReff layanan)
    {
        Layanan = layanan;
    }

    public void AssignDokter(PpaReff dokter)
    {
        Dokter = dokter;
    }
    #endregion
}
