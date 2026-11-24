using Bilreg.Application.ChargeContext.PricingBarangFeature;
using Bilreg.Domain.ChargeContext.PricingBarangFeature;

namespace Bilreg.Infrastructure.ChargeContext.PricingBarangFeature;

public class HnaBatchRepo : IHnaBatchRepo
{
    private readonly IHnaBatchDal _batchDal;
    private readonly IHnaBatchItemDal _itemDal;

    public HnaBatchRepo(IHnaBatchDal batchDal, IHnaBatchItemDal itemDal)
    {
        _batchDal = batchDal;
        _itemDal = itemDal;
    }

    public void SaveChanges(HnaBatchModel model)
    {
        _batchDal.SaveBatch(model);
    }

    public decimal? GetActiveHna(string barangId, DateTime tgl)
    {
        return _itemDal.GetActiveHna(barangId, tgl);
    }
}
