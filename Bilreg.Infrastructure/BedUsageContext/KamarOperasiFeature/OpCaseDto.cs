using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.PasienContext.PasienFeature;

namespace Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;

public record OpCaseDto(
    string OrderOpId,
    DateTime OrderDate,
    string NamaOperasi,
    string PasienId,
    string RegId,

    string ScheduleOpId,
    DateTime ScheduledDate,
    string DischargeOpId,
    DateTime DischargedDate,
    DateTime StartedDate,
    DateTime FinishedDate,
    int OrderOpState,
    //
    string PasienName,
    string TglLahir,
    string Gender,
    int UrgencyLevel)
{
    public static OpCaseDto FromModel(OpCaseModel model)
    {
        var tglLahir = model.Pasien.TglLahir.ToString("yyyy-MM-dd");
        var result = new OpCaseDto(model.OrderOpId,
            model.OrderOp.OrderDate,
            model.OrderOp.NamaOperasi,
            model.Pasien.PasienId,
            model.Reg.RegId,
            model.ScheduleOp.ScheduleOpId,
            model.ScheduleOp.ScheduledDate,
            model.DischargeOp.DischargeOpId,
            model.DischargeOp.DischargedDate,
            model.OnProgressOp.StartTime,
            model.OnProgressOp.FinishTime,
            (int)model.OrderOpState,
            model.Pasien.PasienName, tglLahir, model.Pasien.Gender,
            (int)model.UrgencyLevel);
        return result;
    }

    public OpCaseModel ToModel(
        IEnumerable<OpCaseStateHistType> listHist,
        IEnumerable<OpCasePpaType> listPpa)
    {
        var orderOp = new OrderOpReff(OrderOpId, OrderDate, NamaOperasi);
        var pasien = new PasienReff(PasienId, PasienName, DateOnly.Parse(TglLahir), Gender);
        var reg = RegId == "" ?
            RegModel.Default.ToReff() :
            new RegReff(RegId, PasienId, PasienName);
        var schedule = new ScheduleOpReff(ScheduleOpId, ScheduledDate);
        var discharge = new DischargeOpReff(DischargeOpId, DischargedDate);
        var onProgress = new OnProgressOpReff(StartedDate, FinishedDate);
        var result = new OpCaseModel(OrderOpId, orderOp, pasien, NamaOperasi,
            reg, (UrgencyLevelEnum)UrgencyLevel, schedule, discharge, onProgress,
            (OpCaseStateEnum)OrderOpState, listHist, listPpa);
        return result;
    }
}