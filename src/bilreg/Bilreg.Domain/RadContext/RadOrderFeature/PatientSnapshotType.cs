namespace Bilreg.Domain.RadContext.RadOrderFeature;

public record PatientSnapshotType(
    string RegId,
    string PatientId,
    string PatientName,
    DateTime BirthDate,
    string Gender,
    int AgeAtOrder)
{
    public static PatientSnapshotType Default => new(
        RegId: string.Empty,
        PatientId: string.Empty,
        PatientName: string.Empty,
        BirthDate: new DateTime(3000, 1, 1),
        Gender: string.Empty,
        AgeAtOrder: 0);
}
