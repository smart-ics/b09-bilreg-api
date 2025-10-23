using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.Helpers;

namespace Bilreg.Infrastructure.AdmisiContext.AntrianFeature;

public record AntrianDto(string AntrianId, DateTime AntrianDate,
    string StartTime, string EndTime, string SequenceTag, string AntrianDescription) : IAntrianKey
{
    public static AntrianDto FromModel(AntrianModel model)
    {
        var result = new AntrianDto(
            model.AntrianId,
            model.AntrianDate.ToDateTime(TimeOnly.MinValue),
            model.StartTime.ToString("HH:mm"),
            model.EndTime.ToString("HH:mm"),
            model.SequenceTag,
            model.AntrianDescription);
        return result;
    }

    public AntrianModel ToModel(IEnumerable<AntrianEntryModel> listEntry, ISequencer sequencer)
    {
        var result = new AntrianModel(
            AntrianId,
            DateOnly.FromDateTime(AntrianDate),
            TimeOnly.Parse(StartTime),
            TimeOnly.Parse(EndTime),
            SequenceTag,
            AntrianDescription,
            listEntry, 
            sequencer);
        return result;
    }
}