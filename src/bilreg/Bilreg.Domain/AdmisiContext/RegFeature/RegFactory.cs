using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Nuna.Lib.AutoNumberHelper;
using Nuna.Lib.CleanArchHelper;

namespace Bilreg.Domain.AdmisiContext.RegFeature;

public interface IGetKelasRajalService : INunaService<KelasType>
{
}

public interface IGetKelasRadarService : INunaService<KelasType> { }
public interface IRegFactory 
{
    RegModel CreateRegRajal(PasienModel pasien, 
        AuditInfoType regMasukAudit, TipeJaminanType tipeJaminan, PolisModel polis,
        CaraMasukDkType caraMasukDk, RujukanType rujukan, 
        PpaType dokter, LayananType layanan, KarcisType karcis, string pesertaJaminanId);

    RegModel CreateRegDarurat(PasienModel pasien,
        AuditInfoType regMasukAudit, TipeJaminanType tipeJaminan, PolisModel polis,
        CaraMasukDkType caraMasukDk, PpaType dokter, LayananType layanan,
        KarcisType karcis, string pesertaJaminanId);
}



public class RegFactory : IRegFactory
{
    private readonly ISequencerManual _sequencer;
    private readonly IGetKelasRajalService _getKelasRajalService;
    private readonly IGetKelasRadarService _getKelasRadarService;

    private const string SEQUENCE_TAG = "NOREG";

    public RegFactory(ISequencerManual sequencer,
        IGetKelasRajalService getKelasRajalService,
        IGetKelasRadarService getKelasRadarService)
    {
        _sequencer = sequencer;
        _getKelasRajalService = getKelasRajalService;
        _getKelasRadarService = getKelasRadarService;
    }

    public RegModel CreateRegRajal(PasienModel pasien, 
        AuditInfoType regMasukAudit, TipeJaminanType tipeJaminan, 
        PolisModel polis, CaraMasukDkType caraMasukDk, RujukanType rujukan, 
        PpaType dokter, LayananType layanan, KarcisType karcis, string pesertaJaminanId)
    {
        var newNo = _sequencer.GetNextNoUrut(SEQUENCE_TAG, "No Urut Reg Masuk");
        var regId = $"RG{newNo:D8}";
        var tglMasuk = DateOnly.FromDateTime(regMasukAudit.Timestamp);
        var kelasRajal = _getKelasRajalService.Execute();
        var eligibility = new RegEligibilityType("-", "-", pesertaJaminanId);
        var reg = new RegModel(regId, tglMasuk, regMasukAudit,
            AuditInfoType.Default, AuditInfoType.Default, AuditInfoType.Default, JenisRegEnum.RegJalan,
            pasien.ToReff(), TipeJaminanType.Default.ToReff(),
            PolisModel.Default.ToReff(), kelasRajal.ToReff(), CaraMasukDkType.Default,
            RujukanType.Default.ToReff(), PpaType.Default.ToReff(),  
            LayananType.Default.ToReff(), KarcisType.Default.ToReff(), 
            eligibility, []);

        reg.ApplyJaminan(tipeJaminan, polis);
        reg.SpecifyCaraMasuk (caraMasukDk, rujukan);
        reg.AssignVisitTo(dokter, layanan, karcis);

        return reg;
    }

    public RegModel CreateRegDarurat(PasienModel pasien, 
        AuditInfoType regMasukAudit, TipeJaminanType tipeJaminan, PolisModel polis,
        CaraMasukDkType caraMasukDk,PpaType dokter, LayananType layanan, 
        KarcisType karcis, string pesertaJaminanId)
    {
        var newNo = _sequencer.GetNextNoUrut(SEQUENCE_TAG, "No Urut Reg Masuk");
        var regId = $"RG{newNo:D8}";
        var tglMasuk = DateOnly.FromDateTime(regMasukAudit.Timestamp);
        var kelasRajal = _getKelasRadarService.Execute();
        var eligibility = new RegEligibilityType("-", "-", pesertaJaminanId);

        var reg = new RegModel(regId, tglMasuk, regMasukAudit,
            AuditInfoType.Default, AuditInfoType.Default, AuditInfoType.Default, JenisRegEnum.Darurat,
            pasien.ToReff(), TipeJaminanType.Default.ToReff(),
            PolisModel.Default.ToReff(), kelasRajal.ToReff(), CaraMasukDkType.Default,
            RujukanType.Default.ToReff(), PpaType.Default.ToReff(),
            LayananType.Default.ToReff(), KarcisType.Default.ToReff(), eligibility, []);

        reg.ApplyJaminan(tipeJaminan, polis);
        reg.SpecifyCaraMasuk(caraMasukDk, RujukanType.Default);
        reg.AssignVisitTo(dokter, layanan, karcis);

        return reg;
    }
    
}