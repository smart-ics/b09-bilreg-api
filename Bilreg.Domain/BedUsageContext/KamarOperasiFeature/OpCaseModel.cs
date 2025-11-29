using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PasienContext.PasienFeature;

namespace Bilreg.Domain.BedUsageContext.KamarOperasiFeature;

public class OpCaseModel : IOrderOpKey
{
    private readonly List<OpCaseStateHistType> _listStateHistory;
    
    #region CREATION
    public OpCaseModel(string orderOpId, OrderOpReff orderOp, 
        PasienReff pasien, string operasiName, 
        RegReff reg, UrgencyLevelEnum urgencyLevel,
        ScheduleOpReff scheduleOp, DischergeOpReff dischargeOp, OpCaseStateEnum opState,
        IEnumerable<OpCaseStateHistType> listStateHistory)
    {
        OrderOpId = orderOpId;
        OrderOp = orderOp;
        Pasien = pasien;
        OperasiName = operasiName;
        
        Reg = reg;
        UrgencyLevel = urgencyLevel;
        ScheduleOp = scheduleOp;
        DischargeOp = dischargeOp;
        OrderOpState = opState;
        
        _listStateHistory = listStateHistory?.ToList() ?? [];
    }
    public static OpCaseModel Default => new OpCaseModel(
        "-", OrderOpModel.Default.ToReff(), PasienModel.Default.ToReff(), "-", 
        RegModel.Default.ToReff(), UrgencyLevelEnum.Elective, ScheduleOpReff.Default, 
        DischergeOpReff.Default, OpCaseStateEnum.Requested, []);

    public static OpCaseModel Create(OrderOpModel orderOp)
    {
        var listStateHist = new List<OpCaseStateHistType>
        {
            new(0, OpCaseStateEnum.Requested, DateTime.Now)
        };
        var result = new OpCaseModel(orderOp.OrderOpId, orderOp.ToReff(),
            orderOp.Pasien, orderOp.NamaOperasi, orderOp.Reg, orderOp.UrgencyLevel,
            ScheduleOpReff.Default, DischergeOpReff.Default, 
            OpCaseStateEnum.Requested, listStateHist);
        return result;
    }
    #endregion
    
    #region PROPERTIES
    public string OrderOpId { get; init; }
    public OrderOpReff OrderOp { get; init; }
    public PasienReff Pasien { get; init; }
    public string OperasiName { get; init; }

    public RegReff Reg { get; private set; }
    public UrgencyLevelEnum UrgencyLevel { get; private set; }

    public ScheduleOpReff ScheduleOp { get; private set; }
    public DischergeOpReff DischargeOp { get; private set; }
    public OpCaseStateEnum OrderOpState { get; private set; }

    public OpCaseReff? ActiveOpCase
    {
        get
        {
            return OrderOpState switch
            {
                OpCaseStateEnum.Requested => ToReff(),
                OpCaseStateEnum.Scheduled => ToReff(),
                OpCaseStateEnum.PreOpCleared => ToReff(),
                OpCaseStateEnum.OpStarted => ToReff(),
                OpCaseStateEnum.RecoveryStarted => ToReff(),
                OpCaseStateEnum.Discharged => null,
                OpCaseStateEnum.Cancelled => null,
                _ => null
            };
        }
    }
    public IEnumerable<OpCaseStateHistType> ListStateHistory => _listStateHistory;
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
        Pasien, OrderOpState);
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
    PasienReff Pasien, OpCaseStateEnum OpCaseState);
    
public record OpCaseStateHistType(int NoUrut, OpCaseStateEnum OpCaseState, DateTime StateTimestamp)
{
    public static OpCaseStateHistType Default 
        => new OpCaseStateHistType(0, OpCaseStateEnum.Requested, new DateTime(3000,1,1));
};