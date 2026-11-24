using Bilreg.Domain.RadContext.RadOrderFeature;

namespace Bilreg.Application.RadContext.RadOrderFeature;

public interface IRadOrderItemDal
{
    IEnumerable<RadOrderItemDto> ListData(IRadOrderKey key);
    void Insert(IEnumerable<RadOrderItemDto> items);
    void Delete(IRadOrderKey key);
}
