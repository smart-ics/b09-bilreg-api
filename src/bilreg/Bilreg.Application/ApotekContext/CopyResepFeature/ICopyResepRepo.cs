using Bilreg.Domain.ApotekContext.CopyResepFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.ApotekContext.CopyResepFeature;

public interface ICopyResepRepo :
    ISaveChange<CopyResepModel>,
    ILoadEntity<CopyResepModel, ICopyResepKey>
{
}
