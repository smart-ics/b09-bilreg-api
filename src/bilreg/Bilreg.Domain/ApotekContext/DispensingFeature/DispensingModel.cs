using Ardalis.GuardClauses;
using Bilreg.Domain.ApotekContext.Shared;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.ApotekContext.DispensingFeature;

public class DispensingItemModel
{
    public DispensingItemModel(
        int itemNo,
        int salesOrderItemNo,
        string brgId,
        decimal qty,
        string reserveMutasiReff,
        string removeStockMutasiReff,
        string returnMutasiReff,
        DispensingItemOutcomeEnum itemOutcome)
    {
        Guard.Against.NegativeOrZero(itemNo, nameof(itemNo));
        Guard.Against.NegativeOrZero(salesOrderItemNo, nameof(salesOrderItemNo));
        if (qty <= 0)
            throw new ApotekDomainException("Dispensing qty must be greater than zero.");
        ItemNo = itemNo;
        SalesOrderItemNo = salesOrderItemNo;
        BrgId = brgId;
        Qty = qty;
        ReserveMutasiReff = reserveMutasiReff ?? "";
        RemoveStockMutasiReff = removeStockMutasiReff ?? "";
        ReturnMutasiReff = returnMutasiReff ?? "";
        ItemOutcome = itemOutcome;
    }

    public void SetReserveReff(string reff) => ReserveMutasiReff = reff ?? "";
    public void SetRemoveReff(string reff)
    {
        RemoveStockMutasiReff = reff ?? "";
        ItemOutcome = DispensingItemOutcomeEnum.Dispensed;
    }
    public void SetReturnReff(string reff)
    {
        ReturnMutasiReff = reff ?? "";
        ItemOutcome = DispensingItemOutcomeEnum.Returned;
    }
    public void MarkUnfulfilled() => ItemOutcome = DispensingItemOutcomeEnum.Unfulfilled;

    public int ItemNo { get; }
    public int SalesOrderItemNo { get; }
    public string BrgId { get; }
    public decimal Qty { get; }
    public string ReserveMutasiReff { get; private set; }
    public string RemoveStockMutasiReff { get; private set; }
    public string ReturnMutasiReff { get; private set; }
    public DispensingItemOutcomeEnum ItemOutcome { get; private set; }
}

public class FinalReviewModel
{
    public FinalReviewModel(
        int reviewNo,
        FinalReviewOutcomeEnum outcome,
        string reason,
        string pharmacistId,
        DateTime effectiveAt,
        decimal affectedQty)
    {
        Guard.Against.NegativeOrZero(reviewNo, nameof(reviewNo));
        Guard.Against.NullOrWhiteSpace(pharmacistId, nameof(pharmacistId));
        ReviewNo = reviewNo;
        Outcome = outcome;
        Reason = reason ?? "";
        PharmacistId = pharmacistId;
        EffectiveAt = effectiveAt;
        AffectedQty = affectedQty;
    }

    public int ReviewNo { get; }
    public FinalReviewOutcomeEnum Outcome { get; }
    public string Reason { get; }
    public string PharmacistId { get; }
    public DateTime EffectiveAt { get; }
    public decimal AffectedQty { get; }
}

public class DispensingModel : IDispensingKey
{
    public const string IdPrefix = "ADP";
    private readonly List<DispensingItemModel> _items;
    private readonly List<FinalReviewModel> _reviews;

    private DispensingModel(
        string dispensingId,
        string salesOrderId,
        int careSetting,
        DispensingStatusEnum dispensingStatus,
        string pharmacyUnitLayananId,
        string temporaryUnitLayananId,
        DateTime releasedAt,
        DateTime preparationStartedAt,
        DateTime preparedAt,
        DateTime educationAt,
        string educationPharmacistId,
        string educationNote,
        DateTime overrideAt,
        string overridePharmacistId,
        string overrideReason,
        DateTime handoverAt,
        string recipientPhone,
        string recipientRelationship,
        DateTime cancelledAt,
        DateTime expiredAt,
        string cancelReason,
        DateTime pickupCalledAt,
        int version,
        IEnumerable<DispensingItemModel> items,
        IEnumerable<FinalReviewModel> reviews)
    {
        DispensingId = dispensingId;
        SalesOrderId = salesOrderId;
        CareSetting = careSetting;
        DispensingStatus = dispensingStatus;
        PharmacyUnitLayananId = pharmacyUnitLayananId;
        TemporaryUnitLayananId = temporaryUnitLayananId;
        ReleasedAt = releasedAt;
        PreparationStartedAt = preparationStartedAt;
        PreparedAt = preparedAt;
        EducationAt = educationAt;
        EducationPharmacistId = educationPharmacistId ?? "";
        EducationNote = educationNote ?? "";
        OverrideAt = overrideAt;
        OverridePharmacistId = overridePharmacistId ?? "";
        OverrideReason = overrideReason ?? "";
        HandoverAt = handoverAt;
        RecipientPhone = recipientPhone ?? "";
        RecipientRelationship = recipientRelationship ?? "";
        CancelledAt = cancelledAt;
        ExpiredAt = expiredAt;
        CancelReason = cancelReason ?? "";
        PickupCalledAt = pickupCalledAt;
        Version = version;
        _items = items.ToList();
        _reviews = reviews.ToList();
    }

    public static IDispensingKey Key(string dispensingId) => new DispensingKey(dispensingId);

    public static DispensingModel Establish(
        string salesOrderId,
        string pharmacyUnitLayananId,
        string temporaryUnitLayananId,
        IEnumerable<DispensingItemModel> items)
    {
        Guard.Against.NullOrWhiteSpace(salesOrderId, nameof(salesOrderId));
        var list = items.ToList();
        if (list.Count == 0)
            throw new ApotekDomainException("Dispensing requires Sales Order items.");
        return new DispensingModel(
            NunaId.New(IdPrefix),
            salesOrderId,
            0,
            DispensingStatusEnum.Established,
            pharmacyUnitLayananId,
            temporaryUnitLayananId,
            ApotekDate.Empty, ApotekDate.Empty, ApotekDate.Empty, ApotekDate.Empty,
            "", "", ApotekDate.Empty, "", "", ApotekDate.Empty, "", "",
            ApotekDate.Empty, ApotekDate.Empty, "", ApotekDate.Empty, 1, list, []);
    }

    public static DispensingModel Rehydrate(
        string dispensingId,
        string salesOrderId,
        int careSetting,
        DispensingStatusEnum dispensingStatus,
        string pharmacyUnitLayananId,
        string temporaryUnitLayananId,
        DateTime releasedAt,
        DateTime preparationStartedAt,
        DateTime preparedAt,
        DateTime educationAt,
        string educationPharmacistId,
        string educationNote,
        DateTime overrideAt,
        string overridePharmacistId,
        string overrideReason,
        DateTime handoverAt,
        string recipientPhone,
        string recipientRelationship,
        DateTime cancelledAt,
        DateTime expiredAt,
        string cancelReason,
        DateTime pickupCalledAt,
        int version,
        IEnumerable<DispensingItemModel> items,
        IEnumerable<FinalReviewModel> reviews)
        => new(dispensingId, salesOrderId, careSetting, dispensingStatus, pharmacyUnitLayananId, temporaryUnitLayananId,
            releasedAt, preparationStartedAt, preparedAt, educationAt, educationPharmacistId, educationNote,
            overrideAt, overridePharmacistId, overrideReason, handoverAt, recipientPhone, recipientRelationship,
            cancelledAt, expiredAt, cancelReason, pickupCalledAt, version, items, reviews);

    public bool HasStartedPreparation => !ApotekDate.IsEmpty(PreparationStartedAt);
    public bool IsPrepared => DispensingStatus == DispensingStatusEnum.Prepared;
    public bool IsAccountablyResolved =>
        DispensingStatus is DispensingStatusEnum.Completed or DispensingStatusEnum.Expired
            or DispensingStatusEnum.Cancelled or DispensingStatusEnum.Unfulfilled;

    public void AwaitClearance()
    {
        EnsureStatus(DispensingStatusEnum.Established, "await clearance");
        DispensingStatus = DispensingStatusEnum.AwaitingClearance;
        Version++;
    }

    public void Release(DateTime releasedAt, bool authorized)
    {
        if (DispensingStatus is not (DispensingStatusEnum.Established or DispensingStatusEnum.AwaitingClearance))
            throw new ApotekDomainException("Dispensing can release only from Established or AwaitingClearance.");
        if (!authorized)
            throw new ApotekDomainException("Dispense Authorized policy denied release.");
        DispensingStatus = DispensingStatusEnum.Released;
        ReleasedAt = releasedAt;
        Version++;
    }

    public bool StartPreparation(DateTime startedAt, bool authorized)
    {
        if (DispensingStatus == DispensingStatusEnum.Preparing && HasStartedPreparation)
            return false;
        if (DispensingStatus != DispensingStatusEnum.Released)
            throw new ApotekDomainException("Preparation can start only from Released.");
        if (!authorized)
            throw new ApotekDomainException("Dispense Authorized policy denied start.");
        var first = !HasStartedPreparation;
        DispensingStatus = DispensingStatusEnum.Preparing;
        if (first)
            PreparationStartedAt = startedAt;
        Version++;
        return first;
    }

    public void MarkPrepared(DateTime preparedAt)
    {
        if (DispensingStatus != DispensingStatusEnum.Preparing)
            throw new ApotekDomainException("Prepared is allowed only from Preparing.");
        DispensingStatus = DispensingStatusEnum.Prepared;
        PreparedAt = preparedAt;
        Version++;
    }

    public void RecordPickupCall(DateTime pickupCalledAt)
    {
        if (DispensingStatus != DispensingStatusEnum.Prepared && !IsAccountablyResolved)
            throw new ApotekDomainException("Pickup call requires Prepared or accountable resolution.");
        if (ApotekDate.IsEmpty(PickupCalledAt))
            PickupCalledAt = pickupCalledAt;
        Version++;
    }

    public FinalReviewModel AppendFinalReview(FinalReviewOutcomeEnum outcome, string reason, string pharmacistId, DateTime at)
    {
        if (DispensingStatus != DispensingStatusEnum.Prepared && outcome == FinalReviewOutcomeEnum.Fail)
        {
            if (DispensingStatus != DispensingStatusEnum.Preparing)
                throw new ApotekDomainException("Final review applies to Prepared dispensing.");
        }
        if (DispensingStatus != DispensingStatusEnum.Prepared && outcome == FinalReviewOutcomeEnum.Pass)
            throw new ApotekDomainException("Passing final review requires Prepared status.");
        if (outcome == FinalReviewOutcomeEnum.Fail && string.IsNullOrWhiteSpace(reason))
            throw new ApotekDomainException("Failed final review requires a reason.");

        var review = new FinalReviewModel(
            _reviews.Count == 0 ? 1 : _reviews.Max(x => x.ReviewNo) + 1,
            outcome,
            reason,
            pharmacistId,
            at,
            outcome == FinalReviewOutcomeEnum.Fail ? _items.Sum(x => x.Qty) : 0);
        _reviews.Add(review);
        if (outcome == FinalReviewOutcomeEnum.Fail)
            DispensingStatus = DispensingStatusEnum.Preparing;
        Version++;
        return review;
    }

    public void RecordEducation(string pharmacistId, DateTime at, string note)
    {
        Guard.Against.NullOrWhiteSpace(pharmacistId, nameof(pharmacistId));
        EducationPharmacistId = pharmacistId;
        EducationAt = at;
        EducationNote = note ?? "";
        Version++;
    }

    public void OverrideCollectionWindow(string pharmacistId, string reason, DateTime at)
    {
        Guard.Against.NullOrWhiteSpace(pharmacistId, nameof(pharmacistId));
        Guard.Against.NullOrWhiteSpace(reason, nameof(reason));
        OverridePharmacistId = pharmacistId;
        OverrideReason = reason;
        OverrideAt = at;
        Version++;
    }

    public bool IsPickupExpired(DateTime asOf, int collectionWindowDays)
        => IsPrepared
           && ApotekDate.IsEmpty(HandoverAt)
           && asOf > PreparedAt.AddDays(collectionWindowDays)
           && ApotekDate.IsEmpty(OverrideAt);

    public void Handover(DateTime at, string recipientPhone, string recipientRelationship, bool pickupExpired, bool hasOverride)
    {
        if (DispensingStatus != DispensingStatusEnum.Prepared)
            throw new ApotekDomainException("Handover requires Prepared status.");
        if (ApotekDate.IsEmpty(PickupCalledAt))
            throw new ApotekDomainException("Handover requires a coordinated pickup call.");
        if (ApotekDate.IsEmpty(EducationAt) || string.IsNullOrWhiteSpace(EducationPharmacistId))
            throw new ApotekDomainException("Handover requires patient education acknowledgement.");
        if (_reviews.All(x => x.Outcome != FinalReviewOutcomeEnum.Pass)
            || _reviews.Last().Outcome == FinalReviewOutcomeEnum.Fail)
            throw new ApotekDomainException("Handover requires a passing final review.");
        if (pickupExpired && !hasOverride)
            throw new ApotekDomainException("Ordinary handover is blocked when Pickup Expired unless override exists.");

        HandoverAt = at;
        RecipientPhone = recipientPhone ?? "";
        RecipientRelationship = recipientRelationship ?? "";
        DispensingStatus = DispensingStatusEnum.Completed;
        foreach (var item in _items)
            item.SetRemoveReff(item.RemoveStockMutasiReff);
        Version++;
    }

    public void ExpireNoShow(DateTime at, string reason)
    {
        if (DispensingStatus != DispensingStatusEnum.Prepared)
            throw new ApotekDomainException("No-show expiry applies to Prepared dispensing.");
        DispensingStatus = DispensingStatusEnum.Expired;
        ExpiredAt = at;
        CancelReason = reason ?? "";
        foreach (var item in _items)
            item.MarkUnfulfilled();
        Version++;
    }

    public void AssertExpectedVersion(int expectedVersion)
    {
        if (Version != expectedVersion)
            throw new ApotekConcurrencyException(DispensingId, expectedVersion);
    }

    private void EnsureStatus(DispensingStatusEnum expected, string action)
    {
        if (DispensingStatus != expected)
            throw new ApotekDomainException($"Cannot {action} from {DispensingStatus}.");
    }

    public string DispensingId { get; }
    public string SalesOrderId { get; }
    public int CareSetting { get; }
    public DispensingStatusEnum DispensingStatus { get; private set; }
    public string PharmacyUnitLayananId { get; }
    public string TemporaryUnitLayananId { get; }
    public DateTime ReleasedAt { get; private set; }
    public DateTime PreparationStartedAt { get; private set; }
    public DateTime PreparedAt { get; private set; }
    public DateTime EducationAt { get; private set; }
    public string EducationPharmacistId { get; private set; }
    public string EducationNote { get; private set; }
    public DateTime OverrideAt { get; private set; }
    public string OverridePharmacistId { get; private set; }
    public string OverrideReason { get; private set; }
    public DateTime HandoverAt { get; private set; }
    public string RecipientPhone { get; private set; }
    public string RecipientRelationship { get; private set; }
    public DateTime CancelledAt { get; }
    public DateTime ExpiredAt { get; private set; }
    public string CancelReason { get; private set; }
    public DateTime PickupCalledAt { get; private set; }
    public int Version { get; private set; }
    public IReadOnlyList<DispensingItemModel> Items => _items;
    public IReadOnlyList<FinalReviewModel> Reviews => _reviews;

    private sealed record DispensingKey(string DispensingId) : IDispensingKey;
}
