using Ardalis.GuardClauses;
using Bilreg.Domain.Helpers.CommonValueObjects;

namespace Bilreg.Domain.AdmisiContext.AntrianFeature;

public record PasienTrackerEventType(string EventName, DateTime EventTime, string ReffId)
{
    public static PasienTrackerEventType Default => new("-", new DateTime(3000, 1, 1), "-");
}
