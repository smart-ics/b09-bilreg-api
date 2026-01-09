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
        ScheduleOpReff scheduleOp, DischergeOpReff dischargeOp,
        OnProgressOpReff onProgressOp,
        OpCaseStateEnum opState,
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
        OnProgressOp = onProgressOp;
        OrderOpState = opState;
        _listPpa = listPpa?.ToList() ?? [];
        _listStateHistory = listStateHistory?.ToList() ?? [];
    }
    public static OpCaseModel Default => new OpCaseModel(
        "-", OrderOpModel.Default.ToReff(), PasienModel.Default.ToReff(), "-", 
        RegModel.Default.ToReff(), UrgencyLevelEnum.Elective, ScheduleOpReff.Default, 
        DischergeOpReff.Default, OnProgressOpReff.Default, OpCaseStateEnum.Requested, [], []);

    public static OpCaseModel Create(OrderOpModel orderOp)
    {
        var listStateHist = new List<OpCaseStateHistType>
        {
            new(0, OpCaseStateEnum.Requested, DateTime.Now)
        };
        var dokterRequester = new OpCasePpaType(0, orderOp.Dokter, "REQUESTER", DateTime.Now);
        var result = new OpCaseModel(orderOp.OrderOpId, orderOp.ToReff(),
            orderOp.Pasien, orderOp.NamaOperasi, orderOp.Reg, orderOp.UrgencyLevel,
            ScheduleOpReff.Default, DischergeOpReff.Default, OnProgressOpReff.Default,
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
    public OnProgressOpReff OnProgressOp { get; private set; }
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
        if ((int)OrderOpState >= (int)OpCaseStateEnum.OpStarted)
            throw new ArgumentException("Operasi sedang dilakukan!");

        ScheduleOp = schedule;
        OnProgressOp = OnProgressOpReff.Default;
        OrderOpState = OpCaseStateEnum.Scheduled;

        UpsertStateHistory(OrderOpState);
    }

    public void Start(DateTime startTime)
    {
        if ((int)OrderOpState >= (int)OpCaseStateEnum.RecoveryStarted)
            throw new ArgumentException("Dalam tahap recovery!");

        OnProgressOp = new OnProgressOpReff(startTime, DateTime.MaxValue);
        OrderOpState = OpCaseStateEnum.OpStarted;

        UpsertStateHistory(OrderOpState);
    }

    public void CancelStart()
    {
        if ((int)OrderOpState >= (int)OpCaseStateEnum.RecoveryStarted)
            throw new ArgumentException("Dalam tahap recovery!");

        // Remove OpStarted state
        _listStateHistory.RemoveAll(x =>
            x.OpCaseState == OpCaseStateEnum.OpStarted);

        NormalizeStateHistoryNoUrut();

        // Prefer PreOpCleared if it exists, otherwise Scheduled
        OrderOpState = _listStateHistory.Any(x =>
            x.OpCaseState == OpCaseStateEnum.PreOpCleared)
                ? OpCaseStateEnum.PreOpCleared
                : OpCaseStateEnum.Scheduled;

        OnProgressOp = OnProgressOpReff.Default;
    }

    public void Finish(DateTime finishTime)
    {
        OnProgressOp = new OnProgressOpReff(OnProgressOp.StartTime, finishTime);
        OrderOpState = OpCaseStateEnum.RecoveryStarted;

        UpsertStateHistory(OrderOpState);
    }

    public void Discharge(DischergeOpReff discharge)
    {
        DischargeOp = discharge;
    }

    public void CancelSchedule()
    {
        if ((int)OrderOpState >= (int)OpCaseStateEnum.OpStarted)
            throw new ArgumentException("Operasi sedang dilakukan!");

        _listStateHistory.RemoveAll(x => x.OpCaseState == OpCaseStateEnum.Scheduled);

        NormalizeStateHistoryNoUrut();

        OrderOpState = OpCaseStateEnum.Requested;
        ScheduleOp = ScheduleOpReff.Default;
        OnProgressOp = OnProgressOpReff.Default;
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

    #region PRIVATE METHODS
    private void UpsertStateHistory(OpCaseStateEnum newState)
    {
        var now = DateTime.Now;

        // Find existing state
        var existingIndex = _listStateHistory
            .FindIndex(x => x.OpCaseState == newState);

        if (existingIndex >= 0)
        {
            // Update timestamp only
            _listStateHistory[existingIndex] =
                _listStateHistory[existingIndex] with
                {
                    StateTimestamp = now
                };
        }
        else
        {
            // Append new state
            _listStateHistory.Add(
                new OpCaseStateHistType(
                    NoUrut: 0, // normalized later
                    OpCaseState: newState,
                    StateTimestamp: now
                )
            );
        }

        NormalizeStateHistoryNoUrut();
    }

    private void NormalizeStateHistoryNoUrut()
    {
        // Order by logical state progression, not insertion order
        var ordered = _listStateHistory
            .OrderBy(x => x.OpCaseState)
            .ToList();

        _listStateHistory.Clear();

        for (int i = 0; i < ordered.Count; i++)
        {
            _listStateHistory.Add(
                ordered[i] with { NoUrut = i }
            );
        }
    }
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

public record OnProgressOpReff(DateTime StartTime, DateTime FinishTime)
{
    public static OnProgressOpReff Default => new OnProgressOpReff(new DateTime(3000, 1, 1), new DateTime(3000, 1, 1));
}

public record OpCaseReff(string OrderOpId, DateTime OrderOpDate,
    PasienReff Pasien, OpCaseStateEnum OpCaseState);