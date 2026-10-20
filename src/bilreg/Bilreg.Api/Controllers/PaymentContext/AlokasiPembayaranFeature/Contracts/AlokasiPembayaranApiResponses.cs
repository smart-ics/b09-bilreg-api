using Bilreg.Application.PaymentContext.AlokasiPembayaranFeature;
using Bilreg.Application.PaymentContext.TataRekeningFeature.Dtos;

namespace Bilreg.Api.Controllers.PaymentContext.AlokasiPembayaranFeature.Contracts;

public record AllocateFinancialResponsibilityApiResponse(
    TataRekeningSummaryDto Summary,
    IReadOnlyList<PaymentProjectionDto> Projection);

public record AlokasiPembayaranMutationApiResponse(TataRekeningSummaryDto Summary);

public static class AlokasiPembayaranApiMapper
{
    public static AllocateFinancialResponsibilityApiResponse ToApiResponse(
        AllocateFinancialResponsibilityResponse response) =>
        new(response.Summary, response.Projection);

    public static AlokasiPembayaranMutationApiResponse ToApiResponse(
        FinalizeFinancialResponsibilityResponse response) =>
        new(response.Summary);

    public static AlokasiPembayaranMutationApiResponse ToApiResponse(
        CancelFinalizationResponse response) =>
        new(response.Summary);

    public static AlokasiPembayaranMutationApiResponse ToApiResponse(
        SettlementInitiationResponse response) =>
        new(response.Summary);
}
