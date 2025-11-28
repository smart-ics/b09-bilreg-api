using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.Helpers.CommonValueObjects;
using Bilreg.Domain.PasienContext.PasienFeature;

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
    int EstimasiDurasi,
    DateTime PreferedDate,
    string SpecialEquipment,
    int OrderOpState,
    //
    string CreateUserId,
    DateTime CreateTimestamp,
    string UpdateUserId,
    DateTime UpdateTimestamp,
    string VoidUserId,
    DateTime VoidTimestamp,
    //
    string PasienName,
    string TglLahir,
    string Gender,
    string fs_ket_icd,
    string fs_nm_jenis_operasi,
    string fs_nm_peg)
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
            model.Dokter.PpaId,
            (int)model.UrgencyLevel,
            model.EstimasiDurasiInMinutes,
            model.PreferedDate,
            model.SpecialEquipment,
            (int)model.OrderOpState,
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
            model.Dokter.PpaName);
        return result;
    }

    public OrderOpModel ToModel()
    {
        var auditTrail = new AuditTrailType(
            new AuditInfoType(CreateUserId, CreateTimestamp),
            new AuditInfoType(UpdateUserId, UpdateTimestamp),
            new AuditInfoType(VoidUserId, VoidTimestamp)
        );
        var pasien = new PasienReff(PasienId, PasienName,
            DateOnly.FromDateTime(DateTime.Parse(TglLahir)), Gender);
        var reg = new RegReff(RegId, PasienId, PasienName);
        var icd10 = new Icd10Type(Icd10Id, fs_ket_icd);
        var jenisOp = new JenisOperasiType(JenisOperasiId, fs_nm_jenis_operasi);
        var dokter = new PpaReff(DokterId, fs_nm_peg);

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
            dokter,
            EstimasiDurasi,
            PreferedDate,
            SpecialEquipment,
            (OpCaseStateEnum)OrderOpState);
        return result;
    }
};
