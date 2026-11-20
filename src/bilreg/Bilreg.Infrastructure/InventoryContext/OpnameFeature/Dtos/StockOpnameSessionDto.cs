using Bilreg.Domain.InventoryContext.OpnameFeature;

namespace Bilreg.Infrastructure.InventoryContext.OpnameFeature.Dtos;

public record StockOpnameSessionDto(
    string SessionId,
    string LayananId,
    int ScopeMode,
    int State,
    DateTime CutoffDate,
    string CreatedBy,
    DateTime CreatedAt,
    string CountingSubmittedBy,
    DateTime? CountingSubmittedAt,
    string VerifiedBy,
    DateTime? VerifiedAt,
    string ApprovedBy,
    DateTime? ApprovedAt,
    string RejectedBy,
    DateTime? RejectedAt,
    string CancelledBy,
    DateTime? CancelledAt,
    string CancelReason,
    string RevisionNotes)
{
    public static StockOpnameSessionDto FromModel(StockOpnameSessionModel model) =>
        new(
            SessionId: model.SessionId,
            LayananId: model.LayananId,
            ScopeMode: (int)model.ScopeMode,
            State: (int)model.Status,
            CutoffDate: model.CutoffDate,
            CreatedBy: model.CreatedBy,
            CreatedAt: model.CreatedAt,
            CountingSubmittedBy: model.CountingSubmittedBy,
            CountingSubmittedAt: model.CountingSubmittedAt,
            VerifiedBy: model.VerifiedBy,
            VerifiedAt: model.VerifiedAt,
            ApprovedBy: model.ApprovedBy,
            ApprovedAt: model.ApprovedAt,
            RejectedBy: model.RejectedBy,
            RejectedAt: model.RejectedAt,
            CancelledBy: model.CancelledBy,
            CancelledAt: model.CancelledAt,
            CancelReason: model.CancelReason,
            RevisionNotes: model.RevisionNotes
        );

    public StockOpnameSessionModel ToModel(
        IEnumerable<StockOpnameScopeItemModel>? scopeItems = null,
        IEnumerable<StockOpnameCountedItemModel>? countedItems = null,
        IEnumerable<StockOpnameVarianceItemModel>? varianceItems = null) =>
        StockOpnameSessionModel.Load(
            sessionId: SessionId,
            layananId: LayananId,
            scopeMode: (SessionScopeEnum)ScopeMode,
            status: (SessionStatusEnum)State,
            cutoffDate: CutoffDate,
            createdBy: CreatedBy,
            createdAt: CreatedAt,
            countingSubmittedBy: CountingSubmittedBy ?? string.Empty,
            countingSubmittedAt: CountingSubmittedAt,
            verifiedBy: VerifiedBy ?? string.Empty,
            verifiedAt: VerifiedAt,
            approvedBy: ApprovedBy ?? string.Empty,
            approvedAt: ApprovedAt,
            rejectedBy: RejectedBy ?? string.Empty,
            rejectedAt: RejectedAt,
            cancelledBy: CancelledBy ?? string.Empty,
            cancelledAt: CancelledAt,
            cancelReason: CancelReason ?? string.Empty,
            revisionNotes: RevisionNotes ?? string.Empty,
            scopeItems: scopeItems,
            countedItems: countedItems,
            varianceItems: varianceItems
        );
}
