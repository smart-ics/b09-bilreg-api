using Bilreg.Domain.AdmisiContext.AntrianFeature;

namespace Bilreg.Infrastructure.AdmisiContext.AntrianFeature;

public record AntrianEntryDto(
    string AntrianId, int NoUrut, string PersonName, int  AntrianStatus,
    DateTime CreatedAt, DateTime ServedAt, DateTime DoneAt)
{
    public static AntrianEntryDto FromModel(string antrianId, AntrianEntryModel model)
    {
        var result = new AntrianEntryDto(antrianId, model.NoUrut, model.Visitor.PersonName, 
            (int)model.AntrianStatus, model.CreatedAt, model.ServedAt, model.DoneAt);
        return result;
    }

    public AntrianEntryModel ToModel()
    {
        var person = new PersonType(PersonName, new DateOnly(3000, 1, 1));
        var result = new AntrianEntryModel(NoUrut, person, (AntrianStatusEnum)AntrianStatus,
            CreatedAt, ServedAt, DoneAt);
        return result;
    }
}