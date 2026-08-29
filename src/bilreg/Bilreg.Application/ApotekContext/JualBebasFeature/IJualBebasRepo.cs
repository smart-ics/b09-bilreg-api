using Bilreg.Domain.ApotekContext.JualBebasFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.ApotekContext.JualBebasFeature;

public interface IJualBebasRepo :
    ISaveChange<JualBebasModel>,
    ILoadEntity<JualBebasModel, IJualBebasKey>
{
}
