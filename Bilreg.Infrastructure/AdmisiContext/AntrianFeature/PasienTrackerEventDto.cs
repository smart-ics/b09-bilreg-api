using Bilreg.Domain.AdmisiContext.AntrianFeature;

public record PasienTrackerEventDto(string PasienTrackerId,
    int NoUrut, string EventName, DateTime EventDate, string ReffId)
{
    public static PasienTrackerEventDto FromModel(string pasienTrackerId, PasienTrackerEventType model)
    => new PasienTrackerEventDto(pasienTrackerId, model.NoUrut, model.EventName,
        model.EventDate, model.ReffId);
    
    public PasienTrackerEventType ToModel()
        =>  new PasienTrackerEventType(NoUrut, EventName, EventDate, ReffId);
}