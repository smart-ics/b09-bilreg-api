using Bilreg.Domain.AdmisiContext.AntrianFeature;

namespace Bilreg.Infrastructure.AdmisiContext.AntrianFeature;

public record PasienTrackerDto(
    string PasienTrackerId,
    string PersonName,
    DateTime TglLahir,
    DateTime VisitDate,
    DateTime StartPeriod,
    DateTime LastPeriod)
{
    public static PasienTrackerDto FromModel(PasienTrackerModel model)
    {
        var result = new PasienTrackerDto(
            model.PasienTrackerId,
            model.Person.PersonName,
            model.Person.TglLahir.ToDateTime(TimeOnly.MinValue),
            model.VisitDate.ToDateTime(TimeOnly.MinValue),
            model.StartPeriod.ToDateTime(TimeOnly.MinValue),
            model.LastPeriod.ToDateTime(TimeOnly.MinValue));
        return result;
    }
    
    public PasienTrackerModel ToModel(IEnumerable<PasienTrackerEventType> listEvent)
    {
        var person = new PersonType(PersonName, DateOnly.FromDateTime(TglLahir));
        var visitDate = DateOnly.FromDateTime(VisitDate);
        var startPeriod = DateOnly.FromDateTime(StartPeriod);
        var lastPeriod = DateOnly.FromDateTime(LastPeriod);
        var result = new PasienTrackerModel(
            PasienTrackerId, person, visitDate, startPeriod, lastPeriod, listEvent);
        return result;
    }   
}
