using Bilreg.Domain.InventoryContext.OpnameFeature;

namespace Bilreg.Infrastructure.InventoryContext.OpnameFeature.Dtos;

public record StockOpnameSessionDto(
    string SessionId,
    string LayananId,
    string ScopeType,
    string Status,
    DateTime CutoffTime,
    DateTime? StartedAt = null,
    DateTime? CountingFinishedAt = null,
    DateTime? VerifiedAt = null,
    DateTime? ApprovedAt = null,
    string? ApprovedBy = null,
    DateTime? RejectedAt = null,
    string? RejectedBy = null,
    string? RejectionNotes = null,
    string? RejectionDisposition = null,
    DateTime? CancelledAt = null,
    string? CancelledBy = null,
    string? CancellationReason = null,
    string CreatedBy = "",
    DateTime CreatedAt = default)
{
    public SessionStatusEnum StatusEnum => ParseStatus(Status);
    public SessionScopeEnum ScopeEnum => ParseScope(ScopeType);

    public static string StatusToString(SessionStatusEnum status) => status.ToString();
    public static SessionStatusEnum ParseStatus(string status) => Enum.Parse<SessionStatusEnum>(status, true);

    public static string ScopeToString(SessionScopeEnum scope) => scope.ToString();
    public static SessionScopeEnum ParseScope(string scope) => Enum.Parse<SessionScopeEnum>(scope, true);

    public static StockOpnameSessionDto FromModel(StockOpnameSessionModel model) =>
        new(
            SessionId: model.SessionId,
            LayananId: model.LayananId,
            ScopeType: model.ScopeMode.ToString(),
            Status: model.Status.ToString(),
            CutoffTime: model.CutoffDate,
            StartedAt: null,
            CountingFinishedAt: model.CountingSubmittedAt,
            VerifiedAt: model.VerifiedAt,
            ApprovedAt: model.ApprovedAt,
            ApprovedBy: model.ApprovedBy,
            RejectedAt: model.RejectedAt,
            RejectedBy: model.RejectedBy,
            RejectionNotes: model.RevisionNotes,
            RejectionDisposition: model.Status == SessionStatusEnum.Rejected ? "Terminal" : null,
            CancelledAt: model.CancelledAt,
            CancelledBy: model.CancelledBy,
            CancellationReason: model.CancelReason,
            CreatedBy: model.CreatedBy,
            CreatedAt: model.CreatedAt
        );

    public StockOpnameSessionModel ToModel(
        IEnumerable<StockOpnameScopeItemModel>? scopeItems = null,
        IEnumerable<StockOpnameCountedItemModel>? countedItems = null,
        IEnumerable<StockOpnameVarianceItemModel>? varianceItems = null) =>
        StockOpnameSessionModel.Load(
            sessionId: SessionId,
            layananId: LayananId,
            scopeMode: ParseScope(ScopeType),
            status: ParseStatus(Status),
            cutoffDate: CutoffTime,
            createdBy: CreatedBy,
            createdAt: CreatedAt,
            countingSubmittedBy: string.Empty,
            countingSubmittedAt: CountingFinishedAt,
            verifiedBy: string.Empty,
            verifiedAt: VerifiedAt,
            approvedBy: ApprovedBy ?? string.Empty,
            approvedAt: ApprovedAt,
            rejectedBy: RejectedBy ?? string.Empty,
            rejectedAt: RejectedAt,
            cancelledBy: CancelledBy ?? string.Empty,
            cancelledAt: CancelledAt,
            cancelReason: CancellationReason ?? string.Empty,
            revisionNotes: RejectionNotes ?? string.Empty,
            scopeItems: scopeItems,
            countedItems: countedItems,
            varianceItems: varianceItems
        );
}
