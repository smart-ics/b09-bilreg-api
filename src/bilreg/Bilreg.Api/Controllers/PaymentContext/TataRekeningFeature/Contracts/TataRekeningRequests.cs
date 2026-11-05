using System.ComponentModel.DataAnnotations;
using Bilreg.Application.PaymentContext.TataRekeningFeature.Dtos;
using Bilreg.Application.PaymentContext.TataRekeningFeature.UseCases;

namespace Bilreg.Api.Controllers.PaymentContext.TataRekeningFeature.Contracts;

public record MergeBillingRequest(
    [Required, MinLength(1)] string MergeRequestId, string? UserId = null);

public record FinancialVerificationRequest(
    [Required] FinancialVerificationAction Action,
    DateTime? VerifiedAt = null, string? UserId = null);

public record FinancialAdjustmentRequest(
    [Required] FinancialAdjustmentInputDto Adjustment,
    DateTime? AppliedAt = null, string? UserId = null);

public record AllocateFinancialResponsibilityRequest(
    [Required, MinLength(1)] IReadOnlyList<PaymentAllocationInputDto> Payments);

public record FinalizeFinancialResponsibilityRequest(
    DateTime? FinalizationDate = null, string? UserId = null);

public record CancelFinalizationRequest(
    [Required, MinLength(10)] string Reason,
    string? UserId = null,
    [Required, MinLength(1)] string SupervisorUserId = "");

public record ReopenBillingRequest(
    [Required, MinLength(1)] string Reason, string? UserId = null);

public record SettlementInitiationRequest(
    DateTime? InitiatedAt = null, string? UserId = null);
