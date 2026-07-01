using Farinv.Domain.BrgContext.BrgFeature;
using Nuna.Lib.DataAccessHelper;

namespace Farinv.Application.BrgContext.BrgFeature;

public interface IBrgRepo :
    ISaveChange<IBrg>,
    ILoadEntity<IBrg, IBrgKey>,
    IDeleteEntity<IBrgKey>,
    IListData<BrgView, string>
{
}

public record BrgView(string BrgId, string BrgName, string KetBarang, string GroupRekDkId)
{
    public static BrgView FromModel(IBrg model)
    {
        return new BrgView(
            model.BrgId,
            model.BrgName,
            model.KetBarang,
            model.GroupRekDk.GroupRekDkId);
    }
};
