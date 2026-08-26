using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.SalesContext.ReturJualFeature;

namespace Bilreg.Infrastructure.SalesContext.ReturJualFeature;

public record ReturJualItemDto(
    string ReturJualId,
    string ReturJualItemId,
    int NoUrut,
    bool IsVoided,
    string BrgId,
    string BrgName,
    decimal QtyJual,
    decimal QtyRetur,
    string SatuanId,

    string SatuanName)
{

    public static IEnumerable<ReturJualItemDto> FlattenFromModel(ReturJualModel model)
    {
        var list = new List<ReturJualItemDto>();

        return list;
    }

}
