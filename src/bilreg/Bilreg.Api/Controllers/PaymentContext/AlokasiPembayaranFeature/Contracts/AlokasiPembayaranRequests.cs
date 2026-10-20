using System.ComponentModel.DataAnnotations;
using Bilreg.Application.PaymentContext.TataRekeningFeature.Dtos;

namespace Bilreg.Api.Controllers.PaymentContext.AlokasiPembayaranFeature.Contracts;

public record AllocateFinancialResponsibilityRequest(
    [Required, MinLength(1)] IReadOnlyList<PaymentAllocationInputDto> Payments);

public record FinalizeFinancialResponsibilityRequest(
    DateTime? FinalizationDate = null,
    string? UserId = null);

public record CancelFinalizationRequest(
    [Required, MinLength(1)] string Reason,
    string? UserId = null);

public record SettlementInitiationRequest(
    DateTime? InitiatedAt = null,
    string? UserId = null);
