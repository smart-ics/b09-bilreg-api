namespace Bilreg.Domain.LabContext.LabOrderFeature;

public record CollectionInfoType(DateTime CollectedDate, string CollectedUserId, string CollectionNote)
{
    private static readonly DateTime EmptyCollectedDate = new(3000, 1, 1);

    public static CollectionInfoType Default => new(EmptyCollectedDate, "", "");

    public bool IsEmpty =>
        CollectedDate == EmptyCollectedDate
        && string.IsNullOrWhiteSpace(CollectedUserId)
        && string.IsNullOrWhiteSpace(CollectionNote);
}
