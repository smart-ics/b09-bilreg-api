using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.SalesContext.ReturJualFeature;

namespace Bilreg.Infrastructure.SalesContext.ReturJualFeature;

public record ReturJualItemDto
{

    public static IEnumerable<ReturJualItemDto> FlattenFromModel(ReturJualModel model)
    {
        var list = new List<ReturJualItemDto>();

        return list;
    }

}
