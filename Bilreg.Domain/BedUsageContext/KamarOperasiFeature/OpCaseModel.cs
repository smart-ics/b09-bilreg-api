using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PasienContext.PasienFeature;

namespace Bilreg.Domain.BedUsageContext.KamarOperasiFeature;

public class OpCaseModel : IOrderOpKey
{
    #region CREATION
    public OpCaseModel(string orderOpId, OrderOpReff orderOp, 
        PasienReff pasien, RegReff reg, ScheduleOpReff scheduleOp, 
        DischergeOpReff dischargeOp, OrderOpStateEnum opState)
    {
        OrderOpId = orderOpId;
        OrderOp = orderOp;
        Pasien = pasien;
        Reg = reg;
        ScheduleOp = scheduleOp;
        DischargeOp = dischargeOp;
        OpState = opState;
    }
    public static OpCaseModel Default => new OpCaseModel(
        "-", OrderOpModel.Default.ToReff(), PasienModel.Default.ToReff(), RegModel.Default.ToReff(),
        ScheduleOpReff.Default, DischergeOpReff.Default, OrderOpStateEnum.Requested);

    public static OpCaseModel Create(OrderOpModel orderOp)
    {
        var result = new OpCaseModel(orderOp.OrderOpId, orderOp.ToReff(),
            orderOp.Pasien, orderOp.Reg, ScheduleOpReff.Default,
            DischergeOpReff.Default, OrderOpStateEnum.Requested);
        return result;
    }
    #endregion
    
    #region PROPERTIES
    public string OrderOpId { get; init; }
    public OrderOpReff OrderOp { get; init; }
    public PasienReff Pasien { get; init; }
    public RegReff Reg { get; private set; }
    public ScheduleOpReff ScheduleOp { get; private set; }
    public DischergeOpReff DischargeOp { get; private set; }
    public OrderOpStateEnum OpState { get; private set; }

    public OpCaseReff? ActiveOpCase
    {
        get
        {
            return OpState switch
            {
                OrderOpStateEnum.Requested => ToReff(),
                OrderOpStateEnum.Scheduled => ToReff(),
                OrderOpStateEnum.PreOpCleared => ToReff(),
                OrderOpStateEnum.OpStarted => ToReff(),
                OrderOpStateEnum.RecoveryStarted => ToReff(),
                OrderOpStateEnum.Discharged => null,
                OrderOpStateEnum.Cancelled => null,
                _ => null
            };
        }
    }
    #endregion
    
    #region BEHAVIOUR
    public void Schedule(ScheduleOpReff schedule)
    {
        ScheduleOp = schedule;
    }

    public void Discharge(DischergeOpReff discharge)
    {
        DischargeOp = discharge;
    }
    public OpCaseReff ToReff() => new OpCaseReff(OrderOp.OrderOpId, OrderOp.OrderDate,
        Pasien, OpState);
    #endregion
}

public record ScheduleOpReff(string ScheduleOpId, DateTime ScheduledDate)
{
    public static ScheduleOpReff Default => new ScheduleOpReff("-", new DateTime(3000, 1, 1));
};

public record DischergeOpReff(string DischargeOpId, DateTime DischargedDate)
{
    public static DischergeOpReff Default => new DischergeOpReff("-", new DateTime(3000, 1, 1));
};

public record OpCaseReff(string OrderOpId, DateTime OrderOpDate,
    PasienReff Pasien, OrderOpStateEnum OrderOpState);