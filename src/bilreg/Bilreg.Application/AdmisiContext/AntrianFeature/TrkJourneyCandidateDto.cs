namespace Bilreg.Application.AdmisiContext.AntrianFeature;

public record TrkJourneyCandidateDto(
    string PasienTrackerId,
    string PersonName,
    string TglLahir,
    string VisitDate,
    string StartPeriod,
    string LastPeriod,
    IEnumerable<TrkJourneyCandidateEventDto> Events);

public record TrkJourneyCandidateEventDto(
    int NoUrut,
    string EventName,
    string EventDate,
    string ReffId);
