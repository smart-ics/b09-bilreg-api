using Bilreg.Domain.AdmisiContext.LayananFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.LayananFeature.LayananAgg;

public interface ILayananRepo :
    ISaveChange<LayananType>,
    ILoadEntity<LayananType, ILayananKey>,
    IDelete<ILayananKey>,
    IListData<LayananType>
{
}