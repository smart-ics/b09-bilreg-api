using Bilreg.Domain.ApotekContext.DispensingFeature;
using Bilreg.Domain.ApotekContext.Shared;

namespace Bilreg.Application.ApotekContext.WorklistFeature;

/// <summary>
/// Projection-only Serah Obat worklist categories. Not Dispensing aggregate states.
/// </summary>
public static class SerahWorklistProjection
{
    public const string ReadyForPickup = "ReadyForPickup";
    public const string PickupExpired = "PickupExpired";
    public const string ReadyForReview = "ReadyForReview";
    public const string ReadyForHandover = "ReadyForHandover";
    public const string Completed = "Completed";
    public const string AccountablyResolved = "AccountablyResolved";

    public static string ComputeCategory(
        DispensingStatusEnum status,
        DateTime preparedAt,
        DateTime pickupCalledAt,
        DateTime educationAt,
        DateTime handoverAt,
        DateTime overrideAt,
        DateTime asOf,
        int collectionWindowDays)
    {
        if (status == DispensingStatusEnum.Completed || !ApotekDate.IsEmpty(handoverAt))
            return Completed;

        if (status is DispensingStatusEnum.Expired or DispensingStatusEnum.Cancelled or DispensingStatusEnum.Unfulfilled)
            return AccountablyResolved;

        if (!ApotekDate.IsEmpty(educationAt) && !ApotekDate.IsEmpty(pickupCalledAt))
            return ReadyForHandover;

        if (!ApotekDate.IsEmpty(pickupCalledAt))
            return ReadyForReview;

        if (IsPickupExpiredProjection(preparedAt, handoverAt, overrideAt, asOf, collectionWindowDays))
            return PickupExpired;

        return ReadyForPickup;
    }

    private static bool IsPickupExpiredProjection(
        DateTime preparedAt,
        DateTime handoverAt,
        DateTime overrideAt,
        DateTime asOf,
        int collectionWindowDays)
        => ApotekDate.IsEmpty(handoverAt)
           && ApotekDate.IsEmpty(overrideAt)
           && !ApotekDate.IsEmpty(preparedAt)
           && asOf > preparedAt.AddDays(collectionWindowDays);
}
