namespace Bilreg.Application.RadContext.RadJadwalFeature;

public interface IRadTrackerIntegration
{
    Task PublishMilestoneAsync(
        string regId,
        string jadwalRadId,
        string milestone,
        string note,
        CancellationToken ct = default);
}
