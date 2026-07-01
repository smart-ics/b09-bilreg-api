namespace Bilreg.Application.LabContext.LabOwareFeature;

public record LabOwareOutboundPayload(
    string OrderNo,
    LabOwarePatientPayload Patient,
    IReadOnlyList<LabOwareTestItemPayload> TestItems,
    LabOwareSpecimenPayload? Specimen);

public record LabOwarePatientPayload(
    string RegId,
    string PatientId,
    string PatientName,
    DateTime BirthDate,
    string Gender,
    int AgeAtOrder);

public record LabOwareTestItemPayload(
    int ItemNo,
    string TestCode,
    string TestName,
    int TubeType,
    string SpecimenType,
    int RequiredTubeCount);

public record LabOwareSpecimenPayload(
    DateTime CollectedDate,
    string CollectedUserId,
    string CollectionNote);
