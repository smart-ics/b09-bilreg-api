using Bilreg.Domain.AdmisiContext.AntrianFeature;

namespace Bilreg.Infrastructure.AdmisiContext.AntrianFeature;

public record AntrianEntryDto(
    string AntrianId, int NoUrut, string PersonName, string PasienTrackerId, 
    int  AntrianStatus, DateTime CreatedAt, DateTime ServedAt, DateTime DoneAt, 
    string ReffId, string ReffDesc)
{
    public static AntrianEntryDto FromModel(string antrianId, AntrianEntryModel model)
    {
        var result = new AntrianEntryDto(antrianId, model.NoUrut, model.Visitor.PersonName, 
            model.Tracker.PasienTrackerId, (int)model.AntrianStatus, model.CreatedAt, model.ServedAt, model.DoneAt,
            model.ReffId, model.ReffDesc);
        return result;
    }

    public AntrianEntryModel ToModel()
    {
        var person = new PersonType(PersonName, new DateOnly(3000, 1, 1));
        var trackerKey = PasienTrackerModel.Key(PasienTrackerId);
        var result = new AntrianEntryModel(NoUrut, person, trackerKey, (AntrianStatusEnum)AntrianStatus,
            CreatedAt, ServedAt, DoneAt, ReffId, ReffDesc);
        return result;
    }
}