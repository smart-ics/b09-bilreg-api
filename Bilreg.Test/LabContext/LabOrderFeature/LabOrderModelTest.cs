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

    [Fact]
    public void Charge_FromOrdered_DoesNotChangeState()
    {
        var order = OrderedOrder();

        order.Charge("U2");

        order.LabOrderStatus.Should().Be(LabOrderStatusEnum.Ordered);
        order.BillingTindakanId.Should().BeEmpty();
    }

    [Fact]
    public void Charge_FromDeferred_Throws()
    {
        var order = OrderedOrder();
        order.Defer("Puasa", new DateTime(2026, 5, 20), "U2");

        var act = () => order.Charge("U3");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Deferred*");
    }

    [Fact]
    public void Charge_WhenBillingTindakanIdAlreadySet_Throws()
    {
        var order = OrderedOrder();
        order.BillingTindakanId = "TDK001";

        var act = () => order.Charge("U3");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*BillingTindakanId*");
    }

    [Fact]
    public void Charge_WhenNotOrdered_Throws()
    {
        var order = OrderedOrder();
        order.MarkCharged("TDK001", "U2");

        var act = () => order.Charge("U3");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Ordered*");
    }

    [Fact]
    public void MarkCharged_SetsChargedStateAndClearsError()
    {
        var order = OrderedOrder();
        order.RecordBillingError("Tarif tidak aktif", "U2");

        order.MarkCharged("TDK-FAKE-0001", "U3");

        order.LabOrderStatus.Should().Be(LabOrderStatusEnum.Charged);
        order.BillingTindakanId.Should().Be("TDK-FAKE-0001");
        order.BillingLastError.Should().BeEmpty();
        order.AuditTrail.Modified.UserId.Should().Be("U3");
    }

    [Fact]
    public void RecordBillingError_KeepsStatusAndSetsError()
    {
        var order = OrderedOrder();

        order.RecordBillingError("Mapping tarif tidak ditemukan", "U2");

        order.LabOrderStatus.Should().Be(LabOrderStatusEnum.Ordered);
        order.BillingLastError.Should().Be("Mapping tarif tidak ditemukan");
        order.BillingTindakanId.Should().BeEmpty();
        order.AuditTrail.Modified.UserId.Should().Be("U2");
    }

    [Fact]
    public void RecordBillingError_TruncatesLongMessage()
    {
        var order = OrderedOrder();
        var longError = new string('X', 250);

        order.RecordBillingError(longError, "U2");

        order.BillingLastError.Should().HaveLength(200);
    }

    private static LabOrderModel ChargedOrder()
    {
        var order = OrderedOrder();
        order.Charge("U2");
        order.MarkCharged("TDK-FAKE-0001", "U2");
        return order;
    }

    private static LabOrderModel VerifiedOrder()
    {
        var order = ChargedOrder();
        order.MarkRecorded("UR");
        order.MarkVerified("PATH");
        return order;
    }

    [Fact]
    public void CollectSpecimen_FromCharged_SetsCollectedAndCollectionInfo()
    {
        var order = ChargedOrder();
        var when = new DateTime(2026, 5, 18, 14, 30, 0);
        var info = new CollectionInfoType(when, "U9", "Lancar");

        order.CollectSpecimen("U9", info);

        order.LabOrderStatus.Should().Be(LabOrderStatusEnum.Collected);
        order.CollectionInfo.CollectedDate.Should().Be(when);
        order.CollectionInfo.CollectedUserId.Should().Be("U9");
        order.CollectionInfo.CollectionNote.Should().Be("Lancar");
        order.AuditTrail.Modified.UserId.Should().Be("U9");
    }

    [Fact]
    public void CollectSpecimen_UsesUserIdForPersistedCollector()
    {
        var order = ChargedOrder();
        var when = new DateTime(2026, 5, 18, 14, 30, 0);
        var info = new CollectionInfoType(when, "ignored", "");

        order.CollectSpecimen("U9", info);

        order.CollectionInfo.CollectedUserId.Should().Be("U9");
    }

    [Fact]
    public void CollectSpecimen_WhenOrdered_Throws()
    {
        var order = OrderedOrder();
        var info = new CollectionInfoType(DateTime.Now, "U1", "");

        var act = () => order.CollectSpecimen("U1", info);
        act.Should().Throw<InvalidOperationException>().WithMessage("*Charged*");
    }

    [Fact]
    public void CollectSpecimen_WhenDeferred_Throws()
    {
        var order = OrderedOrder();
        order.Defer("Puasa", new DateTime(2026, 5, 20), "U2");
        var info = new CollectionInfoType(DateTime.Now, "U1", "");

        var act = () => order.CollectSpecimen("U1", info);
        act.Should().Throw<InvalidOperationException>().WithMessage("*Charged*");
    }

    [Fact]
    public void CollectSpecimen_WhenVoided_Throws()
    {
        var audit = AuditTrailType.Create("U1", new DateTime(2026, 5, 1, 10, 0, 0));
        audit.Batal("UV", new DateTime(2026, 5, 2, 10, 0, 0));
        var order = LabOrderModel.Load(
            "LBO000000777",
            "LAB0000777",
            LabOrderSourceEnum.Emr,
            LabOrderStatusEnum.Charged,
            FinancialClearanceEnum.Pending,
            OwareStatusEnum.Pending,
            EmrSnapshot(),
            executionRegId: "",
            DeferredInfoType.Default,
            billingTindakanId: "TDK1",
            billingLastError: "",
            CollectionInfoType.Default,
            financialClearanceDate: new DateTime(3000, 1, 1),
            financialClearanceUserId: "",
            financialClearanceReason: "",
            releasedDate: new DateTime(3000, 1, 1),
            releasedUserId: "",
            releaseNote: "",
            audit,
            [TestItem()]);
        var info = new CollectionInfoType(DateTime.Now, "U1", "");

        var act = () => order.CollectSpecimen("U1", info);
        act.Should().Throw<InvalidOperationException>().WithMessage("*void*");
    }

    [Fact]
    public void CollectSpecimen_EmptyCollectedDate_Throws()
    {
        var order = ChargedOrder();
        var info = CollectionInfoType.Default;

        var act = () => order.CollectSpecimen("U1", info);
        act.Should().Throw<ArgumentException>().WithParameterName("collectionInfo");
    }

    [Fact]
    public void CollectSpecimen_TruncatesLongNote()
    {
        var order = ChargedOrder();
        var longNote = new string('N', 250);
        var info = new CollectionInfoType(DateTime.Now, "U1", longNote);

        order.CollectSpecimen("U1", info);

        order.CollectionInfo.CollectionNote.Should().HaveLength(200);
    }

    private static LabOrderModel CollectedOrder()
    {
        var order = ChargedOrder();
        var when = new DateTime(2026, 5, 18, 12, 0, 0);
        order.CollectSpecimen("U9", new CollectionInfoType(when, "U9", ""));
        return order;
    }

    [Fact]
    public void MarkRecorded_FromCharged_SetsRecorded()
    {
        var order = ChargedOrder();

        order.MarkRecorded("UR");

        order.LabOrderStatus.Should().Be(LabOrderStatusEnum.Recorded);
        order.AuditTrail.Modified.UserId.Should().Be("UR");
    }

    [Fact]
    public void MarkRecorded_FromCollected_SetsRecorded()
    {
        var order = CollectedOrder();

        order.MarkRecorded("UR");

        order.LabOrderStatus.Should().Be(LabOrderStatusEnum.Recorded);
    }

    [Fact]
    public void MarkRecorded_FromRecorded_AllowsIdempotentUpdate()
    {
        var order = ChargedOrder();
        order.MarkRecorded("U1");
        order.MarkRecorded("U2");

        order.LabOrderStatus.Should().Be(LabOrderStatusEnum.Recorded);
        order.AuditTrail.Modified.UserId.Should().Be("U2");
    }

    [Fact]
    public void MarkRecorded_FromOrdered_Throws()
    {
        var order = OrderedOrder();

        var act = () => order.MarkRecorded("U1");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void MarkVerified_FromRecorded_SetsVerified()
    {
        var order = ChargedOrder();
        order.MarkRecorded("UR");
        order.MarkVerified("PATH");

        order.LabOrderStatus.Should().Be(LabOrderStatusEnum.Verified);
        order.AuditTrail.Modified.UserId.Should().Be("PATH");
    }

    [Fact]
    public void MarkVerified_FromCharged_Throws()
    {
        var order = ChargedOrder();

        var act = () => order.MarkVerified("PATH");

        act.Should().Throw<InvalidOperationException>().WithMessage("*Recorded*");
    }

    [Fact]
    public void ApproveFinancialClearance_FromVerifiedPending_SetsApproved()
    {
        var order = VerifiedOrder();

        order.ApproveFinancialClearance("FIN1");

        order.FinancialClearance.Should().Be(FinancialClearanceEnum.Approved);
        order.FinancialClearanceUserId.Should().Be("FIN1");
        order.FinancialClearanceReason.Should().BeEmpty();
        order.AuditTrail.Modified.UserId.Should().Be("FIN1");
    }

    [Fact]
    public void ApproveFinancialClearance_WhenRecorded_Throws()
    {
        var order = ChargedOrder();
        order.MarkRecorded("UR");

        var act = () => order.ApproveFinancialClearance("FIN1");

        act.Should().Throw<InvalidOperationException>().WithMessage("*Verified*");
    }

    [Fact]
    public void ApproveFinancialClearance_WhenAlreadyApproved_Throws()
    {
        var order = VerifiedOrder();
        order.ApproveFinancialClearance("FIN1");

        var act = () => order.ApproveFinancialClearance("FIN2");

        act.Should().Throw<InvalidOperationException>().WithMessage("*Pending*");
    }

    [Fact]
    public void RejectFinancialClearance_FromVerifiedPending_SetsRejected()
    {
        var order = VerifiedOrder();

        order.RejectFinancialClearance("Belum lunas", "FIN1");

        order.FinancialClearance.Should().Be(FinancialClearanceEnum.Rejected);
        order.FinancialClearanceReason.Should().Be("Belum lunas");
        order.AuditTrail.Modified.UserId.Should().Be("FIN1");
    }

    [Fact]
    public void RejectFinancialClearance_WhenAlreadyApproved_Throws()
    {
        var order = VerifiedOrder();
        order.ApproveFinancialClearance("FIN1");

        var act = () => order.RejectFinancialClearance("Alasan", "FIN2");

        act.Should().Throw<InvalidOperationException>().WithMessage("*Pending*");
    }

    [Fact]
    public void Release_FromVerifiedApproved_SetsReleased()
    {
        var order = VerifiedOrder();
        order.ApproveFinancialClearance("FIN1");

        order.Release("REL1", "Serahkan ke pasien");

        order.LabOrderStatus.Should().Be(LabOrderStatusEnum.Released);
        order.ReleasedUserId.Should().Be("REL1");
        order.ReleaseNote.Should().Be("Serahkan ke pasien");
    }

    [Fact]
    public void Release_WhenClearancePending_Throws()
    {
        var order = VerifiedOrder();

        var act = () => order.Release("REL1", "");

        act.Should().Throw<InvalidOperationException>().WithMessage("*Approved*");
    }

    [Fact]
    public void Release_Twice_Throws()
    {
        var order = VerifiedOrder();
        order.ApproveFinancialClearance("FIN1");
        order.Release("REL1", "");

        var act = () => order.Release("REL2", "");

        act.Should().Throw<InvalidOperationException>().WithMessage("*Released*");
    }
}
