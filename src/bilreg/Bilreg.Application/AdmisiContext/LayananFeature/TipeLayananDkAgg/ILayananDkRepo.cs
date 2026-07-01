using Bilreg.Domain.AdmisiContext.LayananFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.LayananFeature.TipeLayananDkAgg;

public interface ILayananDkRepo :
    ILoadEntity<LayananDkType, ILayananDkKey>,
    IListData<LayananDkType, IInstalasiDkKey>,
    IListData<LayananDkType>
{
}
