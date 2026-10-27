using Ardalis.GuardClauses;
using MediatR;

namespace Bilreg.Application.PurchaseContext.DeliveryFeature.UseCases;

public record DeliveryValidateSuratJalanQuery(
    string SupplierId,
    string SuratJalanNo,
    string? ExcludeDoId = null
) : IRequest<DeliveryValidateSuratJalanResponse>;

public record DeliveryValidateSuratJalanResponse(
    bool IsUnique,
    string ExistingDeliveryOrderId,
    string ExistingDate,
    string ExistingPetugasName
);

public class DeliveryValidateSuratJalanHandler : IRequestHandler<DeliveryValidateSuratJalanQuery, DeliveryValidateSuratJalanResponse>
{
    private readonly IDeliveryValidationDal _deliveryValidationDal;

    public DeliveryValidateSuratJalanHandler(IDeliveryValidationDal deliveryValidationDal)
    {
        _deliveryValidationDal = deliveryValidationDal;
    }

    public Task<DeliveryValidateSuratJalanResponse> Handle(DeliveryValidateSuratJalanQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.SupplierId, nameof(request.SupplierId));
        Guard.Against.NullOrWhiteSpace(request.SuratJalanNo, nameof(request.SuratJalanNo));

        var duplicate = _deliveryValidationDal.CheckDuplicateSuratJalan(
            request.SupplierId,
            request.SuratJalanNo,
            request.ExcludeDoId
        );

        if (duplicate is null)
        {
            return Task.FromResult(new DeliveryValidateSuratJalanResponse(
                IsUnique: true,
                ExistingDeliveryOrderId: string.Empty,
                ExistingDate: string.Empty,
                ExistingPetugasName: string.Empty
            ));
        }

        return Task.FromResult(new DeliveryValidateSuratJalanResponse(
            IsUnique: false,
            ExistingDeliveryOrderId: duplicate.ExistingDoId,
            ExistingDate: duplicate.ExistingDate,
            ExistingPetugasName: duplicate.ExistingPetugasName
        ));
    }
}
