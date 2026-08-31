using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Domain.PurchaseContext.PurchaseOrderFeature;

public record PartnerType: IPartnerKey
{
    public PartnerType(string id, string name)
    {
        PartnerId = id;
        PartnerName = name;
    }
    
    public string PartnerId { get; init; }
    public string PartnerName { get; init; }

    public static PartnerType Default => new(AppConst.DASH, AppConst.DASH);
    public PartnerReff ToReff() => new(PartnerId, PartnerName);
}

public record PartnerReff(string PartnerId, string PartnerName);