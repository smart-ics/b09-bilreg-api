using Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.PasienContext.PasienFeature;

namespace Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;

public record OpCaseDto(
    string OrderOpId,
    DateTime OrderDate,
    string PasienId,
    string NamaOperasi,
    
    string RegId,
    int UrgencyLevel,
    string ScheduleOpId,
    DateTime ScheduledDate,
    string DischargeOpId,
    DateTime DischargedDate,
    int OpCaseState,
    //
    string PasienName,
    string TglLahir,
    string Gender)
{
    public static OpCaseDto FromModel(OpCaseModel model)
    {
        var tglLahir = model.Pasien.TglLahir.ToString("yyyy-MM-dd");
        var result = new OpCaseDto(model.OrderOpId,
            model.OrderOp.OrderDate, model.Pasien.PasienId, 
            model.OrderOp.NamaOperasi, 
            model.Reg.RegId, (int)model.UrgencyLevel, 
            model.ScheduleOp.ScheduleOpId,
            model.ScheduleOp.ScheduledDate,
            model.DischargeOp.DischargeOpId,
            model.DischargeOp.DischargedDate,
            (int)model.OrderOpState,
            model.Pasien.PasienName, tglLahir, model.Pasien.Gender);
        return result;
    }

    public OpCaseModel ToModel(IEnumerable<OpCaseStateHistType> listHist)
    {
        var orderOp = new OrderOpReff(OrderOpId, OrderDate, NamaOperasi);
        var pasien = new PasienReff(PasienId, PasienName, DateOnly.Parse(TglLahir), Gender);
        var reg = RegId == "" ?
            RegModel.Default.ToReff() :
            new RegReff(RegId, PasienId, PasienName);
        var schedule = new ScheduleOpReff(ScheduleOpId, ScheduledDate);
        var discharge = new DischergeOpReff(DischargeOpId, DischargedDate);
        var result = new OpCaseModel(OrderOpId, orderOp, pasien, NamaOperasi, 
            reg, (UrgencyLevelEnum)UrgencyLevel, schedule, discharge, 
            (OpCaseStateEnum)OpCaseState, listHist);
        return result;
    }
    
    public OpCaseOrderView ToView()
    {
        var result = new OpCaseOrderView(OrderOpId, 
            new PasienReff(PasienId, PasienName, DateOnly.Parse(TglLahir), Gender), 
            NamaOperasi, (UrgencyLevelEnum)UrgencyLevel, ScheduledDate);
        return result;
    }
}