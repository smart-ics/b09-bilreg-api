using Ardalis.GuardClauses;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Domain.IgdContext.BedIgdFeature;

public class BedIgdModel : IBedIgdKey
{
    private const string EMPTY_VISIT = "-";

    #region CREATION
    public BedIgdModel(
        string bedIgdId,
        string bedIgdName,
        string kamarName,
        BedStateEnum bedState,
        string currentIgdVisitId,
        AuditInfoType occupyAudit,
        AuditTrailType auditTrail)
    {
        BedIgdId = bedIgdId;
        BedIgdName = bedIgdName;
        KamarName = kamarName;
        BedState = bedState;
        CurrentIgdVisitId = currentIgdVisitId;
        OccupyAudit = occupyAudit;
        AuditTrail = auditTrail;

        BedStateSnapshot = bedState;
        CurrentIgdVisitIdSnapshot = currentIgdVisitId;
    }

    public static BedIgdModel Default => new(
        bedIgdId: "-",
        bedIgdName: "-",
        kamarName: "-",
        bedState: BedStateEnum.Active,
        currentIgdVisitId: EMPTY_VISIT,
        occupyAudit: AuditInfoType.Default,
        auditTrail: AuditTrailType.Default);

    public static IBedIgdKey Key(string id) => new BedIgdModel(
        bedIgdId: id,
        bedIgdName: "-",
        kamarName: "-",
        bedState: BedStateEnum.Active,
        currentIgdVisitId: EMPTY_VISIT,
        occupyAudit: AuditInfoType.Default,
        auditTrail: AuditTrailType.Default);

    public static BedIgdModel CreateMaster(
        string bedIgdId, string bedIgdName, string kamarName, AuditInfoType audit)
    {
        Guard.Against.NullOrWhiteSpace(bedIgdId);
        Guard.Against.NullOrWhiteSpace(bedIgdName);

        return new BedIgdModel(
            bedIgdId: bedIgdId,
            bedIgdName: bedIgdName,
            kamarName: string.IsNullOrWhiteSpace(kamarName) ? "-" : kamarName,
            bedState: BedStateEnum.Active,
            currentIgdVisitId: EMPTY_VISIT,
            occupyAudit: AuditInfoType.Default,
            auditTrail: AuditTrailType.Create(audit.UserId, audit.Timestamp));
    }
    #endregion

    #region PROPERTIES
    public string BedIgdId { get; init; }
    public string BedIgdName { get; init; }
    public string KamarName { get; init; }
    public BedStateEnum BedState { get; private set; }
    public string CurrentIgdVisitId { get; private set; }
    public AuditInfoType OccupyAudit { get; private set; }
    public AuditTrailType AuditTrail { get; init; }

    public BedStateEnum BedStateSnapshot { get; init; }
    public string CurrentIgdVisitIdSnapshot { get; init; }

    public bool IsAvailable => BedState == BedStateEnum.Active && !IsOccupied;
    public bool IsOccupied => CurrentIgdVisitId != EMPTY_VISIT && CurrentIgdVisitId.Length > 0;
    #endregion

    #region BEHAVIOUR
    public BedIgdReff ToReff() => new(BedIgdId, BedIgdName);

    public void Occupy(string igdVisitId, AuditInfoType audit)
    {
        Guard.Against.NullOrWhiteSpace(igdVisitId, nameof(igdVisitId));
        if (IsOccupied)
            throw new InvalidOperationException(
                $"Bed {BedIgdId} sudah ditempati oleh visit {CurrentIgdVisitId}.");
        if (BedState != BedStateEnum.Active)
            throw new InvalidOperationException(
                $"Bed {BedIgdId} tidak dalam state Active (current: {BedState.ToCode()}).");

        BedState = BedStateEnum.Occupied;
        CurrentIgdVisitId = igdVisitId;
        OccupyAudit = audit;
        AuditTrail.Modif(audit.UserId, audit.Timestamp);
    }

    public void Release(AuditInfoType audit)
    {
        if (!IsOccupied)
            throw new InvalidOperationException($"Bed {BedIgdId} tidak sedang ditempati.");

        BedState = BedStateEnum.Dirty;
        CurrentIgdVisitId = EMPTY_VISIT;
        OccupyAudit = AuditInfoType.Default;
        AuditTrail.Modif(audit.UserId, audit.Timestamp);
    }

    public void MarkClean(AuditInfoType audit)
    {
        if (BedState != BedStateEnum.Dirty)
            throw new InvalidOperationException(
                $"Bed {BedIgdId} tidak dalam state Dirty (current: {BedState.ToCode()}).");
        BedState = BedStateEnum.Active;
        AuditTrail.Modif(audit.UserId, audit.Timestamp);
    }

    public void MarkMaintenance(AuditInfoType audit)
    {
        if (IsOccupied)
            throw new InvalidOperationException(
                $"Bed {BedIgdId} sedang ditempati; tidak dapat di-maintenance.");
        BedState = BedStateEnum.Maintenance;
        AuditTrail.Modif(audit.UserId, audit.Timestamp);
    }

    public void Activate(AuditInfoType audit)
    {
        if (BedState == BedStateEnum.Occupied)
            throw new InvalidOperationException(
                $"Bed {BedIgdId} sedang Occupied; tidak dapat di-activate.");
        BedState = BedStateEnum.Active;
        AuditTrail.Modif(audit.UserId, audit.Timestamp);
    }
    #endregion
}

public record BedIgdReff(string BedIgdId, string BedIgdName);
