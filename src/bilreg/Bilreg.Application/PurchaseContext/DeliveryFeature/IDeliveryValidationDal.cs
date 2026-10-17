namespace Bilreg.Application.PurchaseContext.DeliveryFeature;

public interface IDeliveryValidationDal
{
    bool IsSuratJalanUnique(string supplierId, string suratJalanNo, string? excludeDoId = null);
}
