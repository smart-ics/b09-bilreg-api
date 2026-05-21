using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Test.LabContext.LabOrderFeature;

internal static class LabOrderTestSupport
{
    public static LabOrderItemModel SampleItem(
        string testDefinitionId = "LTD0001",
        string labTestCode = "HB",
        string labTestName = "Hemoglobin",
        string tarifId = "TR1",
        string tarifCode = "T-HB",
        string tarifName = "Tarif HB",
        VacutainerTypeEnum tubeType = VacutainerTypeEnum.Edta,
        string specimenType = "Blood",
        int requiredTubeCount = 1)
        => LabOrderItemModel.Create(
            testDefinitionId,
            labTestCode,
            labTestName,
            tarifId,
            tarifCode,
            tarifName,
            tubeType,
            specimenType,
            requiredTubeCount);

    public static LabOrderModel.ResolvedOrderLine ResolvedLine(
        LabOrderItemModel? item = null,
        IReadOnlyList<LabOrderItemComponentModel>? components = null)
        => new(item ?? SampleItem(), components ?? []);

    public static PatientSnapshotType EmrSnapshot(
        string regId = "REG001",
        string patientId = "MR0001",
        string patientName = "Pasien Tes")
        => new(regId, patientId, patientName, new DateTime(1990, 1, 15), "L", 36);

    public static LabOrderModel CreateEmrOrder(
        string orderNo = "LAB00000001",
        string emrOrderId = "EMR-ORDER-001",
        IEnumerable<LabOrderModel.ResolvedOrderLine>? lines = null,
        PatientSnapshotType? snapshot = null,
        AuditInfoType? audit = null)
        => LabOrderModel.CreateFromEmr(
            emrOrderId,
            snapshot ?? EmrSnapshot(),
            lines ?? [ResolvedLine()],
            orderNo,
            audit ?? new AuditInfoType("U1", new DateTime(2026, 5, 18, 10, 0, 0)));
}
