using Ardalis.GuardClauses;

namespace Bilreg.Domain.InventoryContext.OpnameFeature;

public class StockOpnameSessionModel : IStockOpnameSessionKey
{
    private readonly List<StockOpnameScopeItemModel> _scopeItems = [];
    private readonly List<StockOpnameCountedItemModel> _countedItems = [];
    private readonly List<StockOpnameVarianceItemModel> _varianceItems = [];
    private readonly List<StockOpnameApprovedEvent> _domainEvents = [];

    #region PROPERTIES
    public string SessionId { get; private set; }
    public string LayananId { get; private set; }
    public SessionScopeEnum ScopeMode { get; private set; }
    public SessionStatusEnum Status { get; private set; }
    public DateTime CutoffDate { get; private set; }
    public string CreatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public string CountingSubmittedBy { get; private set; }
    public DateTime? CountingSubmittedAt { get; private set; }
    public string VerifiedBy { get; private set; }
    public DateTime? VerifiedAt { get; private set; }
    public string ApprovedBy { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public string RejectedBy { get; private set; }
    public DateTime? RejectedAt { get; private set; }
    public string CancelledBy { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public string CancelReason { get; private set; }
    public string RevisionNotes { get; private set; }

    public IReadOnlyList<StockOpnameScopeItemModel> ScopeItems => _scopeItems.AsReadOnly();
    public IReadOnlyList<StockOpnameCountedItemModel> CountedItems => _countedItems.AsReadOnly();
    public IReadOnlyList<StockOpnameVarianceItemModel> VarianceItems => _varianceItems.AsReadOnly();
    public IReadOnlyList<StockOpnameApprovedEvent> DomainEvents => _domainEvents.AsReadOnly();

    public decimal SurplusValuation =>
        _varianceItems.Where(v => v.FinalVarianceQty > 0).Sum(v => v.FinalValuation);

    public decimal DeficitValuation =>
        _varianceItems.Where(v => v.FinalVarianceQty < 0).Sum(v => Math.Abs(v.FinalValuation));

    public decimal NetValuation => SurplusValuation - DeficitValuation;
    #endregion

    #region CREATION
    public StockOpnameSessionModel(
        string sessionId,
        string layananId,
        SessionScopeEnum scopeMode,
        SessionStatusEnum status,
        DateTime cutoffDate,
        string createdBy,
        DateTime createdAt,
        string countingSubmittedBy = "",
        DateTime? countingSubmittedAt = null,
        string verifiedBy = "",
        DateTime? verifiedAt = null,
        string approvedBy = "",
        DateTime? approvedAt = null,
        string rejectedBy = "",
        DateTime? rejectedAt = null,
        string cancelledBy = "",
        DateTime? cancelledAt = null,
        string cancelReason = "",
        string revisionNotes = "",
        IEnumerable<StockOpnameScopeItemModel>? scopeItems = null,
        IEnumerable<StockOpnameCountedItemModel>? countedItems = null,
        IEnumerable<StockOpnameVarianceItemModel>? varianceItems = null)
    {
        SessionId = sessionId?.Trim() ?? string.Empty;
        if (SessionId.Length > 20)
            throw new ArgumentException("SessionId length cannot exceed 20 characters.", nameof(sessionId));

        LayananId = layananId?.Trim() ?? string.Empty;
        if (LayananId.Length > 5)
            throw new ArgumentException("LayananId length cannot exceed 5 characters.", nameof(layananId));

        ScopeMode = scopeMode;
        Status = status;
        CutoffDate = cutoffDate <= new DateTime(1753, 1, 1) ? DateTime.Now : cutoffDate;
        CreatedBy = createdBy?.Trim() ?? string.Empty;
        CreatedAt = createdAt;
        CountingSubmittedBy = countingSubmittedBy?.Trim() ?? string.Empty;
        CountingSubmittedAt = countingSubmittedAt;
        VerifiedBy = verifiedBy?.Trim() ?? string.Empty;
        VerifiedAt = verifiedAt;
        ApprovedBy = approvedBy?.Trim() ?? string.Empty;
        ApprovedAt = approvedAt;
        RejectedBy = rejectedBy?.Trim() ?? string.Empty;
        RejectedAt = rejectedAt;
        CancelledBy = cancelledBy?.Trim() ?? string.Empty;
        CancelledAt = cancelledAt;
        CancelReason = cancelReason?.Trim() ?? string.Empty;
        RevisionNotes = revisionNotes?.Trim() ?? string.Empty;

        if (scopeItems != null)
            _scopeItems.AddRange(scopeItems);
        if (countedItems != null)
            _countedItems.AddRange(countedItems);
        if (varianceItems != null)
            _varianceItems.AddRange(varianceItems);
    }

    public static StockOpnameSessionModel Create(
        string sessionId,
        string layananId,
        SessionScopeEnum scopeMode,
        DateTime cutoffDate,
        string createdBy,
        DateTime? createdAt = null,
        IEnumerable<StockOpnameScopeItemModel>? scopeItems = null)
    {
        Guard.Against.NullOrWhiteSpace(sessionId, nameof(sessionId));
        Guard.Against.NullOrWhiteSpace(layananId, nameof(layananId));
        Guard.Against.NullOrWhiteSpace(createdBy, nameof(createdBy));

        if (sessionId.Trim().Length > 20)
            throw new ArgumentException("SessionId length cannot exceed 20 characters.", nameof(sessionId));

        if (layananId.Trim().Length > 5)
            throw new ArgumentException("LayananId length cannot exceed 5 characters.", nameof(layananId));

        var safeCutoff = cutoffDate <= new DateTime(1753, 1, 1) ? DateTime.Now : cutoffDate;

        return new StockOpnameSessionModel(
            sessionId: sessionId,
            layananId: layananId,
            scopeMode: scopeMode,
            status: SessionStatusEnum.Draft,
            cutoffDate: safeCutoff,
            createdBy: createdBy,
            createdAt: createdAt ?? DateTime.Now,
            scopeItems: scopeItems);
    }

    public static StockOpnameSessionModel Load(
        string sessionId,
        string layananId,
        SessionScopeEnum scopeMode,
        SessionStatusEnum status,
        DateTime cutoffDate,
        string createdBy,
        DateTime createdAt,
        string countingSubmittedBy = "",
        DateTime? countingSubmittedAt = null,
        string verifiedBy = "",
        DateTime? verifiedAt = null,
        string approvedBy = "",
        DateTime? approvedAt = null,
        string rejectedBy = "",
        DateTime? rejectedAt = null,
        string cancelledBy = "",
        DateTime? cancelledAt = null,
        string cancelReason = "",
        string revisionNotes = "",
        IEnumerable<StockOpnameScopeItemModel>? scopeItems = null,
        IEnumerable<StockOpnameCountedItemModel>? countedItems = null,
        IEnumerable<StockOpnameVarianceItemModel>? varianceItems = null)
    {
        return new StockOpnameSessionModel(
            sessionId,
            layananId,
            scopeMode,
            status,
            cutoffDate,
            createdBy,
            createdAt,
            countingSubmittedBy,
            countingSubmittedAt,
            verifiedBy,
            verifiedAt,
            approvedBy,
            approvedAt,
            rejectedBy,
            rejectedAt,
            cancelledBy,
            cancelledAt,
            cancelReason,
            revisionNotes,
            scopeItems,
            countedItems,
            varianceItems);
    }
    #endregion

    #region BEHAVIOR / INVARIANTS
    public void StartCounting()
    {
        if (Status != SessionStatusEnum.Draft)
            throw new InvalidOperationException($"Cannot start counting from status {Status}. Expected Draft.");

        Status = SessionStatusEnum.Counting;
    }

    public void SubmitCounting(bool hasVariance, string submittedBy = "", DateTime? submittedAt = null)
    {
        if (Status != SessionStatusEnum.Counting)
            throw new InvalidOperationException($"Cannot submit counting from status {Status}. Expected Counting.");

        CountingSubmittedBy = submittedBy?.Trim() ?? string.Empty;
        CountingSubmittedAt = submittedAt ?? DateTime.Now;

        // BR-SO-010: Zero-Variance Verification Bypass
        Status = hasVariance ? SessionStatusEnum.WaitingVerification : SessionStatusEnum.WaitingApproval;
    }

    public void StartVerification()
    {
        if (Status != SessionStatusEnum.WaitingVerification && Status != SessionStatusEnum.Verifying)
            throw new InvalidOperationException($"Cannot start verification from status {Status}. Expected WaitingVerification.");

        Status = SessionStatusEnum.Verifying;
    }

    public void SubmitVerification(string verifiedBy = "", DateTime? verifiedAt = null)
    {
        if (Status != SessionStatusEnum.Verifying)
            throw new InvalidOperationException($"Cannot submit verification from status {Status}. Expected Verifying.");

        // BR-SO-015: Complete Verification Precondition
        if (_varianceItems.Any(v => !v.IsVerified))
            throw new InvalidOperationException("All variance items must be verified before submitting verification.");

        VerifiedBy = verifiedBy?.Trim() ?? string.Empty;
        VerifiedAt = verifiedAt ?? DateTime.Now;
        Status = SessionStatusEnum.WaitingApproval;
    }

    public StockOpnameApprovedEvent Approve(string approvedBy, DateTime? approvedAt = null)
    {
        if (Status != SessionStatusEnum.WaitingApproval)
            throw new InvalidOperationException($"Cannot approve session from status {Status}. Expected WaitingApproval.");

        Guard.Against.NullOrWhiteSpace(approvedBy, nameof(approvedBy));

        ApprovedBy = approvedBy.Trim();
        ApprovedAt = approvedAt ?? DateTime.Now;
        Status = SessionStatusEnum.Approved;

        var items = _varianceItems
            .Where(v => v.FinalVarianceQty != 0)
            .Select(v => new StockOpnameApprovedItem(
                v.BrgId,
                v.BatchNo,
                v.ExpiredDate,
                v.FinalVarianceQty,
                v.Hpp,
                v.BaseSatuanId))
            .ToList();

        var evt = new StockOpnameApprovedEvent(
            SessionId,
            LayananId,
            ApprovedBy,
            ApprovedAt.Value,
            items);

        _domainEvents.Add(evt);
        return evt;
    }

    public void Reject(
        RejectionDispositionEnum disposition,
        string notes,
        string rejectedBy,
        DateTime? rejectedAt = null)
    {
        if (Status != SessionStatusEnum.WaitingApproval)
            throw new InvalidOperationException($"Cannot reject session from status {Status}. Expected WaitingApproval.");

        Guard.Against.NullOrWhiteSpace(notes, nameof(notes));
        Guard.Against.NullOrWhiteSpace(rejectedBy, nameof(rejectedBy));

        RevisionNotes = notes.Trim();

        if (disposition == RejectionDispositionEnum.Rework)
        {
            // BR-SO-017 & TD-08: Rework returns to Verifying preserving recount drafts
            Status = SessionStatusEnum.Verifying;
        }
        else
        {
            RejectedBy = rejectedBy.Trim();
            RejectedAt = rejectedAt ?? DateTime.Now;
            Status = SessionStatusEnum.Rejected;
        }
    }

    public void Cancel(string reason, string cancelledBy, DateTime? cancelledAt = null)
    {
        if (Status == SessionStatusEnum.Approved)
            throw new InvalidOperationException("Cannot cancel an approved session.");
        if (Status == SessionStatusEnum.Cancelled)
            throw new InvalidOperationException("Session is already cancelled.");
        if (Status == SessionStatusEnum.Rejected)
            throw new InvalidOperationException("Cannot cancel a rejected session.");

        Guard.Against.NullOrWhiteSpace(reason, nameof(reason));
        Guard.Against.NullOrWhiteSpace(cancelledBy, nameof(cancelledBy));

        CancelReason = reason.Trim();
        CancelledBy = cancelledBy.Trim();
        CancelledAt = cancelledAt ?? DateTime.Now;
        Status = SessionStatusEnum.Cancelled;
    }

    public void AddScopeItem(StockOpnameScopeItemModel item)
    {
        Guard.Against.Null(item, nameof(item));
        _scopeItems.Add(item);
    }

    public void AddCountedItem(StockOpnameCountedItemModel item)
    {
        Guard.Against.Null(item, nameof(item));
        _countedItems.Add(item);
    }

    public void AddVarianceItem(StockOpnameVarianceItemModel item)
    {
        Guard.Against.Null(item, nameof(item));
        _varianceItems.Add(item);
    }

    public void SetVarianceItems(IEnumerable<StockOpnameVarianceItemModel> items)
    {
        _varianceItems.Clear();
        if (items != null)
            _varianceItems.AddRange(items);
    }

    public void ClearEvents() => _domainEvents.Clear();
    #endregion
}
