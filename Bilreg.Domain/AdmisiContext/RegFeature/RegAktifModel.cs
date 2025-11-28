using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.PasienContext.PasienFeature;

namespace Bilreg.Domain.AdmisiContext.RegFeature;

public class RegAktifModel : IRegKey
{
    #region CREATION
    public RegAktifModel(string regId, DateTime regDate,  
        PasienReff pasien, string jenisRawat, LayananReff layanan,
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
    #endregion
    
    #region PROPERTIES
    public string RegId { get; init; }
    public DateTime RegDate { get; init; }
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
