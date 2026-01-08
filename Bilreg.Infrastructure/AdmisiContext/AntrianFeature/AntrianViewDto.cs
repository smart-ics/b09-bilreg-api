using Bilreg.Domain.AdmisiContext.AntrianFeature;

namespace Bilreg.Infrastructure.AdmisiContext.AntrianFeature;

public record AntrianViewDto(string AntrianId, int AntrianStatus, int NoUrut, string PersonName,
    string ReffId, string ReffDesc, DateTime AntrianDate, string SequenceTag, string AntrianDescription,
    string StartTime, string EndTime)
{
    public AntrianView ToView()
    {
        var part = SequenceTag.Split("_");
        var result = new AntrianView(
            AntrianId,
            AntrianStatus,
            NoUrut,
            PersonName,
            ReffId,
            ReffDesc,
            AntrianDate,
            SequenceTag,
            part[1],
            AntrianDescription,
            TimeOnly.Parse(StartTime),
            TimeOnly.Parse(EndTime)); 
        return result;
    }
}


