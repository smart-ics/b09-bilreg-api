using Bilreg.Domain.BedUsageContext.BedOperationalFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.BedUsageContext.BedOperationalFeature;

public interface IBedOperationalRepo :
    ISaveChange<BedOperationalModel>,
    ILoadEntity<BedOperationalModel, IBedOperationalKey>
{
}
