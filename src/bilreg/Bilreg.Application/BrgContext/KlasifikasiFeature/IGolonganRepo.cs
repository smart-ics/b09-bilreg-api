using Bilreg.Domain.BrgContext.KlasifikasiFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.BrgContext.KlasifikasiFeature;

public interface IGolonganRepo :
    ISaveChange<GolonganType>,
    ILoadEntity<GolonganType, IGolonganKey>,
    IDeleteEntity<IGolonganKey>,
    IListData<GolonganType>
{
}