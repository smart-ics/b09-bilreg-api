using System.ComponentModel.DataAnnotations;
using Bilreg.Application.PaymentContext.TataRekeningFeature.Dtos;
using Bilreg.Application.PaymentContext.TataRekeningFeature.UseCases;

namespace Bilreg.Api.Controllers.PaymentContext.TataRekeningFeature.Contracts;

public record MergeBillingRequest(
    [Required, MinLength(1)] string MergeRequestId, [Required, MinLength(1)] string UserId);

public record FinancialVerificationRequest(
    [Required] FinancialVerificationAction Action,
    DateTime? VerifiedAt = null, [Required, MinLength(1)]  string UserId = null);

public record FinancialAdjustmentRequest(
    [Required] FinancialAdjustmentInputDto Adjustment,
    DateTime? AppliedAt = null, [Required, MinLength(1)] string UserId = null);

public record AllocateFinancialResponsibilityRequest(
    [Required, MinLength(1)] IReadOnlyList<PaymentAllocationInputDto> Payments);

public record FinalizeFinancialResponsibilityRequest(
    DateTime? FinalizationDate = null, [Required, MinLength(1)] string UserId = null);

public record CancelFinalizationRequest(
    [Required, MinLength(1)] string Reason, [Required, MinLength(1)] string UserId);

public record ReopenBillingRequest(
    [Required, MinLength(1)] string Reason, [Required, MinLength(1)] string UserId);

public record SettlementInitiationRequest(
    DateTime? InitiatedAt = null, [Required, MinLength(1)]  string UserId = null);
