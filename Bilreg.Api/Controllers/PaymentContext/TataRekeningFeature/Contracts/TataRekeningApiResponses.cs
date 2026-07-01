using Bilreg.Application.PaymentContext.TataRekeningFeature.Dtos;
using Bilreg.Application.PaymentContext.TataRekeningFeature.UseCases;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;

namespace Bilreg.Api.Controllers.PaymentContext.TataRekeningFeature.Contracts;

public record OpenTataRekeningApiResponse(
    TataRekeningSummaryDto Summary,
    IReadOnlyList<TrsBillSummaryDto> Bills,
    IReadOnlyList<PaymentProjectionDto> Projection,
    IReadOnlyList<MergeRequestSummaryDto> PendingMergeRequests);

public record TataRekeningMutationApiResponse(TataRekeningSummaryDto Summary);

public record MergeBillingApiResponse(
    MergeRequestSummaryDto MergeRequest,
    TataRekeningSummaryDto SourceSummary,
    TataRekeningSummaryDto TargetSummary);

public record AllocateFinancialResponsibilityApiResponse(
    TataRekeningSummaryDto Summary,
    IReadOnlyList<PaymentProjectionDto> Projection);

public record FinancialAdjustmentApiResponse(
    TataRekeningSummaryDto Summary,
    bool RequiresReopen,
    FinancialAdjustmentTypeEnum AdjustmentType,
    string? TrsBillingId);

public static class TataRekeningApiMapper
{
    public static OpenTataRekeningApiResponse ToApiResponse(OpenTataRekeningResponse response) =>
        new(response.Summary, response.Bills, response.Projection, response.PendingMergeRequests);

    public static TataRekeningMutationApiResponse ToApiResponse(CloseBillResponse response) =>
        new(response.Summary);

    public static MergeBillingApiResponse ToApiResponse(MergeBillingResponse response) =>
        new(response.MergeRequest, response.SourceSummary, response.TargetSummary);

    public static TataRekeningMutationApiResponse ToApiResponse(FinancialVerificationResponse response) =>
        new(response.Summary);

    public static FinancialAdjustmentApiResponse ToApiResponse(FinancialAdjustmentResponse response) =>
        new(response.Summary, response.RequiresReopen, response.AdjustmentType, response.TrsBillingId);

    public static AllocateFinancialResponsibilityApiResponse ToApiResponse(
        AllocateFinancialResponsibilityResponse response) =>
        new(response.Summary, response.Projection);

    public static TataRekeningMutationApiResponse ToApiResponse(FinalizeFinancialResponsibilityResponse response) =>
        new(response.Summary);

    public static TataRekeningMutationApiResponse ToApiResponse(CancelFinalizationResponse response) =>
        new(response.Summary);

    public static TataRekeningMutationApiResponse ToApiResponse(ReopenBillingResponse response) =>
        new(response.Summary);

    public static TataRekeningMutationApiResponse ToApiResponse(SettlementInitiationResponse response) =>
        new(response.Summary);
}
