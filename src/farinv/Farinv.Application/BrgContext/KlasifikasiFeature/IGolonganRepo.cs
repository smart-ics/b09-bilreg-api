using Farinv.Domain.BrgContext.KlasifikasiFeature;
using Nuna.Lib.DataAccessHelper;

namespace Farinv.Application.BrgContext.KlasifikasiFeature;

public interface IGolonganRepo :
    ISaveChange<GolonganType>,
    ILoadEntity<GolonganType, IGolonganKey>,
    IDeleteEntity<IGolonganKey>,
    IListData<GolonganType>
{
}