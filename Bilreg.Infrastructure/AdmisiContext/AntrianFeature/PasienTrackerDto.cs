using Bilreg.Domain.AdmisiContext.AntrianFeature;

namespace Bilreg.Infrastructure.AdmisiContext.AntrianFeature;

public record PasienTrackerDto(
    string PasienTrackerId,
    string PersonName,
    DateTime TglLahir,
    DateTime VisitDate)
{
    public static PasienTrackerDto FromModel(PasienTrackerModel model)
    {
        var result = new PasienTrackerDto(
            model.PasienTrackerId,
            model.Person.PersonName,
            model.Person.TglLahir.ToDateTime(TimeOnly.MinValue),
            model.VisitDate.ToDateTime(TimeOnly.MinValue));
        return result;
    }
    
    public PasienTrackerModel ToModel()
    {
        var person = new PersonType(PersonName, DateOnly.FromDateTime(TglLahir));
        var visitDate = DateOnly.FromDateTime(VisitDate);
        var result = new PasienTrackerModel(PasienTrackerId, person, visitDate, []); 
        return result;
    }   
}