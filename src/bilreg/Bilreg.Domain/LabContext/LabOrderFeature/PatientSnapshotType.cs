namespace Bilreg.Domain.LabContext.LabOrderFeature;

public record PatientSnapshotType(
    string RegId,
    string PatientId,
    string PatientName,
    DateTime BirthDate,
    string Gender,
    int AgeAtOrder)
{
    public static PatientSnapshotType Default => new(
        RegId: "",
        PatientId: "",
        PatientName: "",
        BirthDate: new DateTime(3000, 1, 1),
        Gender: "",
        AgeAtOrder: 0);
}
