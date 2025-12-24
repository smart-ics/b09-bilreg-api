using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PasienContext.PasienFeature;

namespace Bilreg.Domain.BedUsageContext.KamarOperasiFeature;

public class OpCaseModel : IOrderOpKey
{
    private readonly List<OpCaseStateHistType> _listStateHistory;
    private readonly List<OpCasePpaType> _listPpa;
    
    #region CREATION
    public OpCaseModel(string orderOpId, OrderOpReff orderOp, 
        PasienReff pasien, string operasiName, 
        RegReff reg, UrgencyLevelEnum urgencyLevel,
        ScheduleOpReff scheduleOp, DischergeOpReff dischargeOp, OpCaseStateEnum opState,
        IEnumerable<OpCaseStateHistType> listStateHistory, 
        IEnumerable<OpCasePpaType> listPpa)
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
        _listPpa = listPpa?.ToList() ?? [];
        _listStateHistory = listStateHistory?.ToList() ?? [];
    }
    public static OpCaseModel Default => new OpCaseModel(
        "-", OrderOpModel.Default.ToReff(), PasienModel.Default.ToReff(), "-", 
        RegModel.Default.ToReff(), UrgencyLevelEnum.Elective, ScheduleOpReff.Default, 
        DischergeOpReff.Default, OpCaseStateEnum.Requested, [], []);

    public static OpCaseModel Create(OrderOpModel orderOp)
    {
        var listStateHist = new List<OpCaseStateHistType>
        {
            new(0, OpCaseStateEnum.Requested, DateTime.Now)
        };
        var dokterRequester = new OpCasePpaType(0, orderOp.Dokter, "REQUESTER", DateTime.Now);
        var result = new OpCaseModel(orderOp.OrderOpId, orderOp.ToReff(),
            orderOp.Pasien, orderOp.NamaOperasi, orderOp.Reg, orderOp.UrgencyLevel,
            ScheduleOpReff.Default, DischergeOpReff.Default, 
            OpCaseStateEnum.Requested, listStateHist, [dokterRequester]);
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
    public IEnumerable<OpCasePpaType> ListPpa => _listPpa;
    #endregion

    #region BEHAVIOUR
    public void Schedule(ScheduleOpReff schedule)
    {
        ScheduleOp = schedule;
        var stateHistory = _listStateHistory
            .FirstOrDefault(x => x.OpCaseState == OpCaseStateEnum.Scheduled);
        OrderOpState = OpCaseStateEnum.Scheduled;
        if (stateHistory is null)
        {
            var noUrut = _listStateHistory.Max(x => x.NoUrut) + 1;
            _listStateHistory.Add(new OpCaseStateHistType(noUrut, OrderOpState, DateTime.Now));
        }
    }

    public void Discharge(DischergeOpReff discharge)
    {
        DischargeOp = discharge;
    }

    public void CancelSchedule()
    {
        ScheduleOp = ScheduleOpReff.Default;
        OrderOpState = OpCaseStateEnum.Requested;
        var stateHistory = _listStateHistory
            .FirstOrDefault(x => x.OpCaseState == OpCaseStateEnum.Scheduled);
        if (stateHistory != null)
            _listStateHistory.Remove(stateHistory);
    }

    public void SetListPpa(IEnumerable<OpCasePpaType> listPpa)
    {
        if (listPpa == null)
            return;

        // Index incoming data by Ppa
        var incomingMap = listPpa.ToDictionary(x => x.Ppa);

        // 1. Remove PPAs that no longer exist in incoming list
        //    EXCEPT those with Role == "REQUESTER"
        _listPpa.RemoveAll(x =>
            x.Role != "REQUESTER" &&
            !incomingMap.ContainsKey(x.Ppa)
        );

        // 2. Add missing PPAs
        foreach (var incoming in listPpa)
        {
            if (_listPpa.Any(x => x.Ppa == incoming.Ppa))
                continue;

            var newItem = new OpCasePpaType(
                NoUrut: 0, // will be normalized later
                Ppa: incoming.Ppa,
                Role: incoming.Role,
                AssignDate: DateTime.Now // ALWAYS current datetime
            );

            _listPpa.Add(newItem);
        }

        // 3. Normalize NoUrut
        //    - REQUESTER => NoUrut = 0
        //    - Others => contiguous starting from 1
        var requesterItems = _listPpa
            .Where(x => x.Role == "REQUESTER")
            .Select(x => x with { NoUrut = 0 })
            .ToList();

        var nonRequesterItems = _listPpa
            .Where(x => x.Role != "REQUESTER")
            .OrderBy(x => x.NoUrut)
            .ToList();

        _listPpa.Clear();

        _listPpa.AddRange(requesterItems);

        int noUrut = 1;
        foreach (var item in nonRequesterItems)
        {
            _listPpa.Add(item with { NoUrut = noUrut++ });
        }
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