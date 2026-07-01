using Farinv.Domain.SalesContext.AntrianFeature;
using System.Globalization;

namespace Farinv.Infrastructure.SalesContext.AntrianFeature;

public record AntrianDto(string AntrianId, DateTime AntrianDate,
    int ServicePoint, string AntrianDescription) : IAntrianKey
{
    public static AntrianDto FromModel(AntrianModel model) {
        var result = new AntrianDto(
            model.AntrianId,
            model.AntrianDate.ToDateTime(TimeOnly.MinValue),
            model.ServicePoint,
            model.AntrianDescription);
        return result;
    }

    public AntrianModel ToModel(IEnumerable<AntrianEntryModel> listEntry)
    {
        var result = new AntrianModel(
            AntrianId,
            DateOnly.FromDateTime(AntrianDate),
            ServicePoint,
            AntrianDescription,
            listEntry);
        return result;
    }
}
