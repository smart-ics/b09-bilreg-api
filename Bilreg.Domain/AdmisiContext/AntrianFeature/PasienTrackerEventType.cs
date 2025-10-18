namespace Bilreg.Domain.AdmisiContext.AntrianFeature;

public record PasienTrackerEventType(int NoUrut, string EventName, DateTime EventDate, string ReffId)
{
    public static PasienTrackerEventType Default => new(0, "-", new DateTime(3000, 1, 1), "-");
}