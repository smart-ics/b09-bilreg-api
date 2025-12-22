using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using System.Globalization;

namespace Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;

public record StartOpDto(
    string StartOpId,
    DateTime StartOpTime,
    string OrderOpId,
    string ScheduleOpId,
    string RegId,
    string PasienId,
    string KamarOpId,

    string CrtUser, DateTime CrtDate,
    string UpdUser, DateTime UpdDate,
    string VodUser, DateTime VodDate,

    string NamaOperasi,
    string PasienName,
    string TglLahir,
    string Gender,
    string KamarName
    )
{
    public static StartOpDto FromModel(StartOpModel model)
    {
        return new StartOpDto(
            StartOpId: model.StartOpId,
            StartOpTime: model.StartOpTime,
            OrderOpId: model.OrderOp?.OrderOpId ?? string.Empty,
            ScheduleOpId: model.ScheduleOp?.ScheduleOpId ?? string.Empty,
            RegId: model.Reg?.RegId ?? string.Empty,
            PasienId: model.Pasien?.PasienId ?? string.Empty,
            KamarOpId: model.KamarOp?.KamarId ?? string.Empty,

            CrtUser: model.AuditTrail.Created.UserId,
            CrtDate: model.AuditTrail.Created.Timestamp,
            UpdUser: model.AuditTrail.Modified.UserId,
            UpdDate: model.AuditTrail.Modified.Timestamp,
            VodUser: model.AuditTrail.Voided.UserId,
            VodDate: model.AuditTrail.Voided.Timestamp,

            NamaOperasi: model.OrderOp?.NamaOperasi ?? string.Empty,
            PasienName: model.Pasien?.PasienName ?? string.Empty,
            TglLahir: model.Pasien?.TglLahir.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                ?? DateOnly.Parse("3000-01-01").ToString(),
            Gender: model.Pasien?.Gender ?? string.Empty,
            KamarName: model.KamarOp?.KamarName ?? string.Empty
        );
    }

    public StartOpModel ToModel()
    {
        var auditTrail = new AuditTrailType(
            new AuditInfoType(CrtUser, CrtDate),
            new AuditInfoType(UpdUser, UpdDate),
            new AuditInfoType(VodUser, VodDate)
        );
        var orderOpReff = new OrderOpReff(OrderOpId, new DateTime(3000, 1, 1, 0, 0, 0), NamaOperasi);
        var scheduleOpReff = new ScheduleOpReff(ScheduleOpId, new DateTime(3000, 1, 1, 0, 0, 0));
        var kamar = new KamarReff(KamarOpId, KamarName);
        var pasien = new PasienReff(PasienId, PasienName, DateOnly.ParseExact(TglLahir, "yyyy-MM-dd"), Gender);
        var reg = new RegReff(RegId, PasienId, PasienName);

        return new StartOpModel(
            StartOpId,
            StartOpTime,
            auditTrail,
            orderOpReff,
            scheduleOpReff,
            kamar,
            pasien,
            reg
        );
    }
}
