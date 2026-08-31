using Bilreg.Application.PurchaseContext.PurchaseOrderFeature;
using Bilreg.Domain.PurchaseContext.PurchaseOrderFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.PurchaseContext.PurchaseOrderFeature;

public class PartnerRepo: IPartnerRepo
{
    private readonly IPartnerDal _partnerDal;

    public PartnerRepo(IPartnerDal partnerDal)
    {
        _partnerDal = partnerDal;
    }

    public MayBe<PartnerType> LoadEntity(IPartnerKey key)
    {
        var dto = _partnerDal.GetData(key);
        if (dto is null)
            return MayBe<PartnerType>.None;

        var model = dto.ToModel();
        return MayBe.From(model);
    }
}