using Bilreg.Application.Shared.Param.ParamSistemAgg;
using Bilreg.Domain.ApotekContext.Shared;

namespace Bilreg.Application.ApotekContext.Shared;

public interface ICollectionWindowDaysProvider
{
    int GetDays();
}

public class CollectionWindowDaysProvider : ICollectionWindowDaysProvider
{
    private readonly IParamSistemDal _paramDal;

    public CollectionWindowDaysProvider(IParamSistemDal paramDal)
    {
        _paramDal = paramDal;
    }

    public int GetDays()
    {
        try
        {
            var row = _paramDal.GetData(ApotekSystemParameters.CollectionWindowDaysKey);
            if (row is not null
                && int.TryParse(row.Value, out var days)
                && days > 0)
                return days;
        }
        catch
        {
            // Missing parameter falls back to the documented default.
        }

        return ApotekSystemParameters.DefaultCollectionWindowDays;
    }
}
