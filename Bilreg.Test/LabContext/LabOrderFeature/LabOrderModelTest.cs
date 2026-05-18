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

    private static LabOrderModel OrderedOrder() =>
        LabOrderModel.CreateFromEmr(EmrSnapshot(), [TestItem()], "LAB000010", TestAudit());

    [Fact]
    public void Defer_FromOrdered_SetsDeferredState()
    {
        var order = OrderedOrder();
        var until = new DateTime(2026, 5, 20);

        order.Defer("Puasa 12 jam", until, "U2");

        order.LabOrderStatus.Should().Be(LabOrderStatusEnum.Deferred);
        order.DeferredInfo.Reason.Should().Be("Puasa 12 jam");
        order.DeferredInfo.Until.Should().Be(until);
        order.AuditTrail.Modified.UserId.Should().Be("U2");
    }

    [Fact]
    public void Defer_WhenNotOrdered_Throws()
    {
        var order = OrderedOrder();
        order.Defer("Puasa", DateTime.Now.AddDays(1), "U2");

        var act = () => order.Defer("Lain", DateTime.Now.AddDays(2), "U2");
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ActivateFromDeferred_SetsOrderedAndExecutionRegId()
    {
        var order = OrderedOrder();
        var originalRegId = order.Patient.RegId;
        order.Defer("Puasa", new DateTime(2026, 5, 20), "U2");

        order.ActivateFromDeferred("REG-EXEC-001", "U3");

        order.LabOrderStatus.Should().Be(LabOrderStatusEnum.Ordered);
        order.ExecutionRegId.Should().Be("REG-EXEC-001");
        order.DeferredInfo.IsEmpty.Should().BeTrue();
        order.Patient.RegId.Should().Be(originalRegId);
        order.AuditTrail.Modified.UserId.Should().Be("U3");
    }

    [Fact]
    public void ActivateFromDeferred_WhenNotDeferred_Throws()
    {
        var order = OrderedOrder();

        var act = () => order.ActivateFromDeferred("REG-EXEC-001", "U3");
        act.Should().Throw<InvalidOperationException>();
    }
}
