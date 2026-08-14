using Bilreg.Domain.BrgContext.KlasifikasiFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.BrgContext.KlasifikasiFeature;

public interface IJenisRepo :
    ISaveChange<JenisType>,
    ILoadEntity<JenisType, IJenisKey>,
    IDeleteEntity<IJenisKey>,
    IListData<JenisType>
{
}