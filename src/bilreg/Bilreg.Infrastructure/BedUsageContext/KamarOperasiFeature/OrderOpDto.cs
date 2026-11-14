using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;

public record OrderOpDto(
    string OrderOpId,
    DateTime OrderDate,
    string RegId,
    string PasienId,
    string Icd10Id,
    string JenisOperasiId,
    string NamaOperasi,
    string DokterId,
    int UrgencyLevel,
    string TarifId,
    string TarifName,
    int EstimasiDurasi,
    DateTime PreferedDate,
    string SpecialEquipment,
    //
    string DokterPengorderId,
    string LayananPengorderId,
    string DokterOperatorId,
    string ProsedurTambahan,
    int KlasifikasiOperasi,
    int AnestesiPlan,
    int PostOpCareUnit,
    string AlasanBatal,
    //
    string CrtUser,
    DateTime CrtDate,
    string UpdUser,
    DateTime UpdDate,
    string VodUser,
    DateTime VodDate,
    //
    string PasienName = "",
    string TglLahir = "3000-01-01",
    string Gender = "",
    string fs_ket_icd = "",
    string fs_nm_jenis_operasi = "",
    string fs_nm_peg = "",
    string DokterPengorderName = "",
    string LayananPengorderName = "",
    string DokterOperatorName = "")
{
    public static OrderOpDto FromModel(OrderOpModel model)
    {
        var result = new OrderOpDto(
            model.OrderOpId,
            model.OrderDate,
            model.Reg.RegId,
            model.Pasien.PasienId,
            model.Icd10.Icd10Id,
            model.JenisOperasi.JenisOperasiId,
            model.NamaOperasi,
            model.DokterOperator.PpaId,
            (int)model.UrgencyLevel,
            model.TarifOperasi.TarifId,
            model.TarifOperasi.TarifName,
            model.EstimasiDurasiInMinutes,
            model.PreferedDate,
            model.SpecialEquipment,
            //
            model.DokterPengorder.PpaId,
            model.LayananPengorder.LayananId,
            model.DokterOperator.PpaId,
            model.ProsedurTambahan,
            (int)model.KlasifikasiOperasi,
            (int)model.AnestesiPlan,
            (int)model.PostOpCareUnit,
            model.AlasanBatal,
            //
            model.AuditTrail.Created.UserId,
            model.AuditTrail.Created.Timestamp,
            model.AuditTrail.Modified.UserId,
            model.AuditTrail.Modified.Timestamp,
            model.AuditTrail.Voided.UserId,
            model.AuditTrail.Voided.Timestamp,
            //
            model.Pasien.PasienName,
            model.Pasien.TglLahir.ToString("yyyy-MM-dd"),
            model.Pasien.Gender,
            model.Icd10.Icd10Name,
            model.JenisOperasi.JenisOperasiName,
            model.DokterOperator.PpaName,
            model.DokterPengorder.PpaName,
            model.LayananPengorder.LayananName,
            model.DokterOperator.PpaName);
        return result;
    }

    public OrderOpModel ToModel(IEnumerable<OrderOpResourceModel>? resources = null)
    {
        var auditTrail = new AuditTrailType(
            new AuditInfoType(CrtUser, CrtDate),
            new AuditInfoType(UpdUser, UpdDate),
            new AuditInfoType(VodUser, VodDate)
        );
        var pasien = new PasienReff(PasienId, PasienName,
            DateOnly.FromDateTime(DateTime.Parse(TglLahir)), Gender);
        var reg = new RegReff(RegId, PasienId, PasienName);
        var icd10 = new Icd10Type(Icd10Id, fs_ket_icd);
        var jenisOp = new JenisOperasiType(JenisOperasiId, fs_nm_jenis_operasi);
        var tarif = new TarifReff(TarifId, TarifName);
        var dokterId = string.IsNullOrEmpty(DokterOperatorId) ? DokterId : DokterOperatorId;
        var dokterName = string.IsNullOrEmpty(DokterOperatorName) ? fs_nm_peg : DokterOperatorName;
        var dokterOperator = new PpaReff(dokterId, dokterName);
        var dokterPengorder = new PpaReff(
            string.IsNullOrEmpty(DokterPengorderId) ? dokterId : DokterPengorderId,
            string.IsNullOrEmpty(DokterPengorderName) ? dokterName : DokterPengorderName);
        var layananPengorder = new LayananReff(
            string.IsNullOrEmpty(LayananPengorderId) ? "-" : LayananPengorderId,
            string.IsNullOrEmpty(LayananPengorderName) ? "-" : LayananPengorderName);

        var result = new OrderOpModel(
            OrderOpId,
            OrderDate,
            auditTrail,
            pasien,
            reg,
            icd10,
            jenisOp,
            NamaOperasi,
            (UrgencyLevelEnum)UrgencyLevel,
            tarif,
            dokterOperator,
            EstimasiDurasi,
            PreferedDate,
            SpecialEquipment,
            dokterPengorder,
            layananPengorder,
            ProsedurTambahan,
            (KlasifikasiOperasiEnum)(KlasifikasiOperasi <= 0 ? 1 : KlasifikasiOperasi),
            (AnestesiPlanEnum)(AnestesiPlan <= 0 ? 1 : AnestesiPlan),
            (PostOpCareUnitEnum)(PostOpCareUnit <= 0 ? 1 : PostOpCareUnit),
            AlasanBatal,
            resources);
        return result;
    }
}
