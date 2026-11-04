namespace Bilreg.Domain.LabContext.LabExternalFeature;

public record LabExternalCancelInfoType(
    string CancelReason,
    string CancelledUserId,
    DateTime CancelledDate)
{
    public static LabExternalCancelInfoType Default => new(
        string.Empty,
        string.Empty,
        new DateTime(3000, 1, 1));
}
