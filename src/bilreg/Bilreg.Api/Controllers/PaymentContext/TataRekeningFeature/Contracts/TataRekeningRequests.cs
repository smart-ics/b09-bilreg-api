using System.ComponentModel.DataAnnotations;
using Bilreg.Application.PaymentContext.TataRekeningFeature.Dtos;
using Bilreg.Application.PaymentContext.TataRekeningFeature.UseCases;

namespace Bilreg.Api.Controllers.PaymentContext.TataRekeningFeature.Contracts;

public record MergeBillingRequest(
    [Required, MinLength(1)] string MergeRequestId,
    string? UserId = null);

public record FinancialVerificationRequest(
    [Required] FinancialVerificationAction Action,
    DateTime? VerifiedAt = null,
    string? UserId = null);

public record FinancialAdjustmentRequest(
    [Required] FinancialAdjustmentInputDto Adjustment,
    DateTime? AppliedAt = null,
    string? UserId = null);

public record ReopenBillingRequest(
    [Required, MinLength(1)] string Reason,
    string? UserId = null);
