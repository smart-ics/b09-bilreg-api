using System.ComponentModel.DataAnnotations;
using Bilreg.Application.PaymentContext.TataRekeningFeature.Dtos;

namespace Bilreg.Api.Controllers.PaymentContext.TataRekeningFeature.Contracts;

public record MergeBillingRequest(
    [Required, MinLength(1)] string MergeRequestId,
    string? UserId = null);

public record ReopenBillingRequest(
    [Required, MinLength(1)] string Reason,
    string? UserId = null);

public record AllocateFinalizeRequest(
    [Required, MinLength(1)] IReadOnlyList<PaymentAllocationInputDto> Payments,
    DateTime? FinalizationDate = null,
    string? UserId = null);

public record CancelFinalizeRequest(
    [Required, MinLength(1)] string Reason,
    string? UserId = null);

public record SettlementInitiationRequest(
    DateTime? InitiatedAt = null,
    string? UserId = null);
