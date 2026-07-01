using System.ComponentModel.DataAnnotations;
using Bilreg.Application.PaymentContext.TataRekeningFeature.Dtos;
using Bilreg.Application.PaymentContext.TataRekeningFeature.UseCases;

namespace Bilreg.Api.Controllers.PaymentContext.TataRekeningFeature.Contracts;

public record MergeBillingRequest(
    [Required, MinLength(1)] string MergeRequestId);

public record FinancialVerificationRequest(
    [Required] FinancialVerificationAction Action,
    DateTime? VerifiedAt = null);

public record FinancialAdjustmentRequest(
    [Required] FinancialAdjustmentInputDto Adjustment,
    DateTime? AppliedAt = null);

public record AllocateFinancialResponsibilityRequest(
    [Required, MinLength(1)] IReadOnlyList<PaymentAllocationInputDto> Payments);

public record FinalizeFinancialResponsibilityRequest(
    DateTime? FinalizationDate = null);

public record CancelFinalizationRequest(
    [Required, MinLength(1)] string Reason);

public record ReopenBillingRequest(
    [Required, MinLength(1)] string Reason);

public record SettlementInitiationRequest(
    DateTime? InitiatedAt = null);
