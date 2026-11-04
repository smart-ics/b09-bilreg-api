using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiRanapContext.InpatientOrderFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Infrastructure.AdmisiRanapContext.InpatientOrderFeature;

public record InpatientOrderDto(
    string InpatientOrderId,
    int OrderSource,
    int OrderStatus,
    string PasienId,
    string PasienName,
    DateTime TglLahir,
    string Gender,
    string DokterId,
    string DokterName,
    DateTime PlannedDate,
    string KelasId,
    string KelasName,
    string BangsalId,
    string BangsalName,
    string ClinicalNotes,
    string TipeJaminanId,
    string TipeJaminanName,
    string JaminanReffId,
    string FulfilledRegId,
    string ExternalReferenceId,
    string ExternalSystemSource,
    string CrtUser,
    DateTime CrtDate,
    string UpdUser,
    DateTime UpdDate,
    string VodUser,
    DateTime VodDate)
{
    public static InpatientOrderDto FromModel(InpatientOrderModel model) =>
        new(
            model.InpatientOrderId,
            (int)model.OrderSource,
            (int)model.OrderStatus,
            model.Pasien?.PasienId ?? "-",
            model.Pasien?.PasienName ?? "",
            model.Pasien?.TglLahir.ToDateTime(TimeOnly.MinValue) ?? new DateTime(3000, 1, 1),
            model.Pasien?.Gender ?? "",
            model.Dokter?.PpaId ?? "-",
            model.Dokter?.PpaName ?? "",
            model.PlannedDate,
            model.KelasRawat?.KelasId ?? "-",
            model.KelasRawat?.KelasName ?? "",
            model.Bangsal?.BangsalId ?? "-",
            model.Bangsal?.BangsalName ?? "",
            model.ClinicalNotes ?? "",
            model.Insurance?.TipeJaminan?.TipeJaminanId ?? "-",
            model.Insurance?.TipeJaminan?.TipeJaminanName ?? "",
            model.Insurance?.JaminanReffId ?? "-",
            model.FulfilledRegId ?? "-",
            model.ExternalReferenceId ?? "-",
            model.ExternalSystemSource ?? "",
            model.AuditTrail.Created.UserId,
            model.AuditTrail.Created.Timestamp,
            model.AuditTrail.Modified.UserId,
            model.AuditTrail.Modified.Timestamp,
            model.AuditTrail.Voided.UserId,
            model.AuditTrail.Voided.Timestamp);

    public InpatientOrderModel ToModel()
    {
        var audit = new AuditTrailType(
            new AuditInfoType(CrtUser, CrtDate),
            new AuditInfoType(UpdUser, UpdDate),
            new AuditInfoType(VodUser, VodDate));

        var tglLahirDateOnly = TglLahir == default ? new DateOnly(3000, 1, 1) : DateOnly.FromDateTime(TglLahir);
        var pasien = new PasienReff(
            string.IsNullOrWhiteSpace(PasienId) ? "-" : PasienId,
            PasienName ?? "",
            tglLahirDateOnly,
            Gender ?? "");

        var dokter = new PpaReff(
            string.IsNullOrWhiteSpace(DokterId) ? "-" : DokterId,
            DokterName ?? "");

        var kelasRawat = new KelasReff(
            string.IsNullOrWhiteSpace(KelasId) ? "-" : KelasId,
            KelasName ?? "");

        var bangsal = new BangsalReff(
            string.IsNullOrWhiteSpace(BangsalId) ? "-" : BangsalId,
            BangsalName ?? "");

        var insurance = new InpatientOrderInsuranceModel(
            new TipeJaminanReff(
                string.IsNullOrWhiteSpace(TipeJaminanId) ? "-" : TipeJaminanId,
                TipeJaminanName ?? ""),
            string.IsNullOrWhiteSpace(JaminanReffId) ? "-" : JaminanReffId);

        return new InpatientOrderModel(
            InpatientOrderId,
            (OrderSourceEnum)OrderSource,
            (InpatientOrderStatusEnum)OrderStatus,
            pasien,
            dokter,
            PlannedDate,
            kelasRawat,
            bangsal,
            ClinicalNotes ?? "",
            insurance,
            FulfilledRegId ?? "-",
            ExternalReferenceId ?? "-",
            ExternalSystemSource ?? "",
            audit);
    }
}
