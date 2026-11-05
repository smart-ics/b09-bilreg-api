using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.BhpRanapFeature;
using Bilreg.Domain.BedUsageContext.PakaiBedFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.BedUsageContext.BhpRanapFeature;

public interface IBhpRanapRepo :
    ISaveChange<BhpRanapModel>,
    ILoadEntity<BhpRanapModel, IBhpRanapKey>,
    IListData<BhpRanapView, IPakaiBed>,
    IListData<BhpRanapView, IRegKey>
{
}

public record BhpRanapView(
    string BhpRanapId,
    string PakaiBedId,
    string RegId,
    string LayananId,
    string BhpItemId,
    string BhpItemName,
    int Qty,
    decimal Price,
    decimal Subtotal,
    DateTime CreatedDateTime,
    string CreatedUser,
    bool IsVoid);
