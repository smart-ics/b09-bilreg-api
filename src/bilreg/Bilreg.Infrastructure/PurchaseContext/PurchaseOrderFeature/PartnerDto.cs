using Bilreg.Domain.PurchaseContext.PurchaseOrderFeature;

namespace Bilreg.Infrastructure.PurchaseContext.PurchaseOrderFeature;

public record PartnerDto(
    string PartnerId,
    string PartnerName
)
{
    public static PartnerDto FromModel(PartnerType model)
    {
        var dto = new PartnerDto(model.PartnerId, model.PartnerName);
        return dto;
    }

    public PartnerType ToModel()
    {
        var model = new PartnerType(PartnerId, PartnerName);
        return model;
    }
}