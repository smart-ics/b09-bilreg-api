using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.Shared.Helpers;
using System.Globalization;

namespace Bilreg.Infrastructure.AdmisiContext.AntrianFeature;

public record AntrianDto(string AntrianId, DateTime AntrianDate,
    string StartTime, string EndTime, string SequenceTag, string AntrianDescription,
    string ServicePointCode) : IAntrianKey
{
    public static AntrianDto FromModel(AntrianModel model)
    {
        var result = new AntrianDto(
            model.AntrianId,
            model.AntrianDate.ToDateTime(TimeOnly.MinValue),
            model.StartTime.ToString("HH:mm", CultureInfo.InvariantCulture),
            model.EndTime.ToString("HH:mm", CultureInfo.InvariantCulture),
            model.SequenceTag,
            model.AntrianDescription,
            model.ServicePoint.ServicePointCode);
        return result;
    }

    public AntrianModel ToModel(IEnumerable<AntrianEntryModel> listEntry, ISequencer sequencer)
    {
        var servicePointCode = string.IsNullOrWhiteSpace(ServicePointCode)
            ? AntrianModel.ServicePointCodeFromSequenceTag(SequenceTag)
            : ServicePointCode;
        var servicePoint = new ServicePointType(servicePointCode, AntrianDescription);

        var result = new AntrianModel(
            AntrianId,
            DateOnly.FromDateTime(AntrianDate),
            TimeOnly.ParseExact(StartTime, "HH:mm", CultureInfo.InvariantCulture),
            TimeOnly.ParseExact(EndTime, "HH:mm", CultureInfo.InvariantCulture),
            SequenceTag,
            AntrianDescription,
            servicePoint,
            listEntry, 
            sequencer);
        return result;
    }
}
