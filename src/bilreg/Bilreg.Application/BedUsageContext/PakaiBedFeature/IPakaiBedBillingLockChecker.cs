namespace Bilreg.Application.BedUsageContext.PakaiBedFeature;

public record BillingLockCheckResult(bool IsLocked, string Reason = "")
{
    public static BillingLockCheckResult Unlocked() => new(false, string.Empty);
    public static BillingLockCheckResult Locked(string reason) => new(true, reason);
}

public interface IPakaiBedBillingLockChecker
{
    Task<BillingLockCheckResult> CheckLockStatusAsync(
        string pakaiBedId,
        string regId,
        CancellationToken cancellationToken = default);
}
