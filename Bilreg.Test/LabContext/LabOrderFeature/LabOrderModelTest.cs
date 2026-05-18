using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.LabContext.LabOrderFeature;

public class LabOrderModelTest
{
    private static AuditInfoType TestAudit() => new("U1", new DateTime(2026, 5, 18, 10, 0, 0));

    private static PatientSnapshotType EmrSnapshot() => new(
        RegId: "REG001",
        PatientId: "MR0001",
        PatientName: "Pasien Tes",
        BirthDate: new DateTime(1990, 1, 15),
        Gender: "L",
        AgeAtOrder: 36);

    private static PatientSnapshotType ExternalSnapshot() => new(
        RegId: "",
        PatientId: "",
        PatientName: "Walk-in",
        BirthDate: new DateTime(1985, 6, 1),
        Gender: "P",
        AgeAtOrder: 40);

    private static LabOrderItemModel TestItem() =>
        LabOrderItemModel.Create(
            testId: "T1",
            testCode: "HB",
            testName: "Hemoglobin",
            tarifId: "TR1",
            tarifCode: "T-HB",
            tarifName: "Tarif HB",
            tubeType: VacutainerTypeEnum.Edta,
            specimenType: "Blood",
            requiredTubeCount: 1);

    [Fact]
    public void CreateFromEmr_SetsInitialWorkflowState()
    {
        var order = LabOrderModel.CreateFromEmr(EmrSnapshot(), [TestItem()], "LAB000001", TestAudit());

        order.OrderId.Should().StartWith("LBO");
        order.OrderNo.Should().Be("LAB000001");
        order.OrderSource.Should().Be(LabOrderSourceEnum.Emr);
        order.LabOrderStatus.Should().Be(LabOrderStatusEnum.Ordered);
        order.FinancialClearance.Should().Be(FinancialClearanceEnum.Pending);
        order.OwareStatus.Should().Be(OwareStatusEnum.Pending);
        order.Patient.PatientId.Should().Be("MR0001");
        order.Items.Should().HaveCount(1);
        order.Items.First().ItemNo.Should().Be(1);
        order.AuditTrail.Created.UserId.Should().Be("U1");
    }

    [Fact]
    public void CreateExternal_AllowsEmptyRegAndPatientId()
    {
        var order = LabOrderModel.CreateExternal(ExternalSnapshot(), [TestItem()], "LAB000002", TestAudit());

        order.OrderSource.Should().Be(LabOrderSourceEnum.ExternalPatient);
        order.Patient.RegId.Should().BeEmpty();
        order.Patient.PatientId.Should().BeEmpty();
        order.LabOrderStatus.Should().Be(LabOrderStatusEnum.Ordered);
    }

    [Fact]
    public void CreateFromEmr_EmptyRegId_Throws()
    {
        var bad = EmrSnapshot() with { RegId = "" };
        var act = () => LabOrderModel.CreateFromEmr(bad, [TestItem()], "LAB000003", TestAudit());
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateFromEmr_NoItems_Throws()
    {
        var act = () => LabOrderModel.CreateFromEmr(EmrSnapshot(), [], "LAB000004", TestAudit());
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateFromEmr_AssignsSequentialItemNumbers()
    {
        var item2 = TestItem() with { TestId = "T2", TestName = "Glucose" };
        var order = LabOrderModel.CreateFromEmr(EmrSnapshot(), [TestItem(), item2], "LAB000005", TestAudit());

        order.Items.Select(x => x.ItemNo).Should().Equal(1, 2);
    }
}
