using Bilreg.Application.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;

public record ScheduleOpDto(
    string ScheduleOpId,
    DateTime ScheduleOpDate,
    string OrderOpId,
    string PasienId,
    int UrgencyLevel,
    int Durasi,
    DateTime TglOp,
    string KamarId,
    string RegId,
    string PpaId,

    string CrtUser,
    DateTime CrtDate,
    string UpdUser,
    DateTime UpdDate,
    string VodUser,
    DateTime VodDate,

    DateTime OrderDate,
    string NamaOperasi,
    string PasienName,
    string TglLahir,
    string Gender,
    string KamarName,
    string PpaName,
    DateTime StartedDate,
    DateTime FinishedDate,
    int OrderOpState)
{
    public static ScheduleOpDto FromModel(ScheduleOpModel model)
    {
        var result = new ScheduleOpDto(
            model.ScheduleOpId,
            model.ScheduleOpDate,
            model.OrderOp.OrderOpId,
            model.Pasien.PasienId,
            (int)model.UrgencyLevel,
            model.Durasi,
            model.TglOp,
            model.KamarOp.KamarId,
            model.Reg.RegId,
            model.TeamLead.PpaId,

            model.AuditTrail.Created.UserId,
            model.AuditTrail.Created.Timestamp,
            model.AuditTrail.Modified.UserId,
            model.AuditTrail.Modified.Timestamp,
            model.AuditTrail.Voided.UserId,
            model.AuditTrail.Voided.Timestamp,

            model.OrderOp.OrderDate,
            model.OrderOp.NamaOperasi,
            model.Pasien.PasienName,
            model.Pasien.TglLahir.ToString("yyyy-MM-dd"),
            model.Pasien.Gender,
            model.KamarOp.KamarName,
            model.TeamLead.PpaName,
            model.StartOpDate,
            model.EndOpDate,
            0 // OrderOpState
        );
        return result;
    }

    public ScheduleOpModel ToModel(IEnumerable<ScheduleOpPpaType> listPpa)
    {
        var auditTrail = new AuditTrailType(
            new AuditInfoType(CrtUser, CrtDate),
            new AuditInfoType(UpdUser, UpdDate),
            new AuditInfoType(VodUser, VodDate)
        );

        var orderOp = new OrderOpReff(OrderOpId, OrderDate, NamaOperasi);
        var pasien = new PasienReff(PasienId, PasienName, DateOnly.ParseExact(TglLahir, "yyyy-MM-dd"), Gender);
        var kamarOp = new KamarReff(KamarId, KamarName);
        var reg = new RegReff(RegId, PasienId, PasienName);
        var teamLead = new PpaReff(PpaId, PpaName);
        
        var result = new ScheduleOpModel(
            ScheduleOpId,
            ScheduleOpDate,
            auditTrail,
            orderOp,
            pasien,
            (UrgencyLevelEnum)UrgencyLevel,
            Durasi,
            TglOp,
            kamarOp,
            reg,
            teamLead,
            StartedDate, FinishedDate,
            (OpCaseStateEnum)OrderOpState,
            listPpa
        );
        return result;
    }
    
    public ScheduleOpView ToView()
    {
        var pasien = new PasienReff(PasienId, PasienName, DateOnly.ParseExact(TglLahir, "yyyy-MM-dd"), Gender);
        var kamarOp = new KamarReff(KamarId, KamarName);
        var teamLead = new PpaReff(PpaId, PpaName);
        var orderOp = new OrderOpReff(OrderOpId, OrderDate, NamaOperasi);
        var result = new ScheduleOpView(
            ScheduleOpId,
            pasien,
            orderOp,
            (UrgencyLevelEnum)UrgencyLevel,
            TglOp,
            Durasi,
            teamLead,
            kamarOp,
            (OpCaseStateEnum)OrderOpState,
            VodDate.ToString(DateFormatEnum.YMD) != "3000-01-01"
        );
        return result;
    }
}