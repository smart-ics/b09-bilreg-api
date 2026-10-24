namespace Bilreg.Domain.PurchaseContext.DeliveryFeature;

public interface IDeliveryOrderKey
{
    string DeliveryOrderId { get; }
}

public record DeliveryOrderKey(string DeliveryOrderId) : IDeliveryOrderKey;
