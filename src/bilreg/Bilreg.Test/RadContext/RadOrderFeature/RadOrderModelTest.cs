using Bilreg.Domain.RadContext.RadOrderFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.RadContext.RadOrderFeature;

public class RadOrderModelTest
{
    private static AuditInfoType TestAudit(string userId = "U1")
        => new(userId, new DateTime(2026, 11, 23, 10, 0, 0));

    private static PatientSnapshotType EmrSnapshot(
        string regId = "REG001",
        string patientId = "MR0001",
        string patientName = "Pasien Uji Coba")
        => new(
            regId,
            patientId,
            patientName,
            new DateTime(1990, 1, 15),
            "L",
            36);

    private static PatientSnapshotType ExternalSnapshot(
        string patientName = "Pasien Rujukan Luar")
        => new(
            string.Empty,
            string.Empty,
            patientName,
            new DateTime(1985, 6, 20),
            "P",
            41);

    private static RadOrderItemModel CreateItem(
        string tarifId = "TRF01",
        string tarifName = "Thorax AP",
        RadModalityEnum modality = RadModalityEnum.CR,
        RadUrgencyEnum urgency = RadUrgencyEnum.Rutin)
        => RadOrderItemModel.Create(tarifId, tarifName, modality, urgency);

    [Fact]
    public void Default_HasExpectedValues()
    {
        var model = RadOrderModel.Default;

        model.OrderId.Should().Be("-");
        model.EmrOrderId.Should().BeEmpty();
        model.OrderNo.Should().BeEmpty();
        model.OrderSource.Should().Be(RadOrderSourceEnum.Internal);
        model.Modality.Should().Be(RadModalityEnum.CR);
        model.RadOrderStatus.Should().Be(RadOrderStatusEnum.Requested);
        model.Urgency.Should().Be(RadUrgencyEnum.Rutin);
        model.Patient.Should().Be(PatientSnapshotType.Default);
        model.CancelledReason.Should().BeEmpty();
        model.CancelledDate.Should().Be(RadOrderModel.EmptyDate);
        model.CancelledUserId.Should().BeEmpty();
        model.Items.Should().BeEmpty();
        model.IsCancelled.Should().BeFalse();
        model.IsActive.Should().BeTrue();
        model.IsCito.Should().BeFalse();
    }

    [Fact]
    public void CreateFromEmr_SetsExpectedPropertiesAndDefaults()
    {
        var item1 = CreateItem("TRF01", "Thorax AP", RadModalityEnum.CR, RadUrgencyEnum.Rutin);
        var item2 = CreateItem("TRF02", "Thorax Lateral", RadModalityEnum.CR, RadUrgencyEnum.Rutin);

        var order = RadOrderModel.CreateFromEmr(
            emrOrderId: "EMR-RAD-001",
            patient: EmrSnapshot(),
            modality: RadModalityEnum.CR,
            items: [item1, item2],
            orderNo: "RAD26110001",
            audit: TestAudit(),
            dokterPengorderId: "DR01",
            dokterPengorderName: "dr. Sp.Rad",
            layananPengorderId: "LY01",
            layananPengorderName: "Poli Paru",
            clinicalNote: "Batuk kronis 2 bulan",
            urgency: RadUrgencyEnum.Rutin);

        order.OrderId.Should().StartWith(RadOrderModel.IdPrefix);
        order.EmrOrderId.Should().Be("EMR-RAD-001");
        order.OrderNo.Should().Be("RAD26110001");
        order.OrderSource.Should().Be(RadOrderSourceEnum.Internal);
        order.Modality.Should().Be(RadModalityEnum.CR);
        order.RadOrderStatus.Should().Be(RadOrderStatusEnum.Requested);
        order.Urgency.Should().Be(RadUrgencyEnum.Rutin);
        order.Patient.RegId.Should().Be("REG001");
        order.Patient.PatientId.Should().Be("MR0001");
        order.Patient.PatientName.Should().Be("Pasien Uji Coba");
        order.ExecutionRegId.Should().Be("REG001");
        order.DokterPengorderId.Should().Be("DR01");
        order.DokterPengorderName.Should().Be("dr. Sp.Rad");
        order.LayananPengorderId.Should().Be("LY01");
        order.LayananPengorderName.Should().Be("Poli Paru");
        order.ClinicalNote.Should().Be("Batuk kronis 2 bulan");
        order.CancelledReason.Should().BeEmpty();
        order.CancelledDate.Should().Be(RadOrderModel.EmptyDate);
        order.CancelledUserId.Should().BeEmpty();
        order.AuditTrail.Created.UserId.Should().Be("U1");

        order.Items.Should().HaveCount(2);
        order.Items[0].OrderId.Should().Be(order.OrderId);
        order.Items[0].ItemNo.Should().Be(1);
        order.Items[0].TarifId.Should().Be("TRF01");
        order.Items[1].OrderId.Should().Be(order.OrderId);
        order.Items[1].ItemNo.Should().Be(2);
        order.Items[1].TarifId.Should().Be("TRF02");

        order.IsCancelled.Should().BeFalse();
        order.IsActive.Should().BeTrue();
        order.IsCito.Should().BeFalse();
    }

    [Theory]
    [InlineData("", "REG001", "MR0001", "Pasien", "RAD01", "U1")]
    [InlineData("EMR01", "", "MR0001", "Pasien", "RAD01", "U1")]
    [InlineData("EMR01", "REG001", "", "Pasien", "RAD01", "U1")]
    [InlineData("EMR01", "REG001", "MR0001", "", "RAD01", "U1")]
    [InlineData("EMR01", "REG001", "MR0001", "Pasien", "", "U1")]
    [InlineData("EMR01", "REG001", "MR0001", "Pasien", "RAD01", "")]
    public void CreateFromEmr_InvalidRequiredArgs_ThrowsArgumentException(
        string emrOrderId, string regId, string patientId, string patientName, string orderNo, string userId)
    {
        var snapshot = new PatientSnapshotType(regId, patientId, patientName, new DateTime(1990, 1, 1), "L", 30);
        var audit = new AuditInfoType(userId, DateTime.Now);

        var act = () => RadOrderModel.CreateFromEmr(
            emrOrderId,
            snapshot,
            RadModalityEnum.CR,
            [],
            orderNo,
            audit);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateExternal_AllowsEmptyRegAndPatientId()
    {
        var item = CreateItem("TRF03", "USG Abdomen", RadModalityEnum.US);

        var order = RadOrderModel.CreateExternal(
            emrOrderId: null,
            patient: ExternalSnapshot("Ny. Rahayu"),
            modality: RadModalityEnum.US,
            items: [item],
            orderNo: "RAD26110002",
            audit: TestAudit());

        order.OrderId.Should().StartWith(RadOrderModel.IdPrefix);
        order.EmrOrderId.Should().BeEmpty();
        order.OrderSource.Should().Be(RadOrderSourceEnum.External);
        order.Modality.Should().Be(RadModalityEnum.US);
        order.Patient.RegId.Should().BeEmpty();
        order.Patient.PatientId.Should().BeEmpty();
        order.Patient.PatientName.Should().Be("Ny. Rahayu");
        order.ExecutionRegId.Should().BeEmpty();
        order.RadOrderStatus.Should().Be(RadOrderStatusEnum.Requested);
        order.Items.Should().HaveCount(1);
        order.Items[0].Modality.Should().Be(RadModalityEnum.US);
    }

    [Fact]
    public void SingleModalityGuard_CreateWithMismatchedItem_ThrowsArgumentException()
    {
        var crItem = CreateItem("TRF01", "Thorax CR", RadModalityEnum.CR);
        var ctItem = CreateItem("TRF04", "CT Brain", RadModalityEnum.CT);

        var act = () => RadOrderModel.CreateFromEmr(
            emrOrderId: "EMR-RAD-002",
            patient: EmrSnapshot(),
            modality: RadModalityEnum.CR,
            items: [crItem, ctItem],
            orderNo: "RAD26110003",
            audit: TestAudit());

        act.Should().Throw<ArgumentException>()
            .WithMessage("*modalitas CT yang berbeda dari modalitas order CR*");
    }

    [Fact]
    public void SingleModalityGuard_AddItemWithMismatchedModality_ThrowsArgumentException()
    {
        var order = RadOrderModel.CreateFromEmr(
            emrOrderId: "EMR-RAD-003",
            patient: EmrSnapshot(),
            modality: RadModalityEnum.DX,
            items: [CreateItem("TRF01", "DX Thorax", RadModalityEnum.DX)],
            orderNo: "RAD26110004",
            audit: TestAudit());

        var mismatchedItem = CreateItem("TRF05", "MRI Brain", RadModalityEnum.MR);

        var act = () => order.AddItem(mismatchedItem);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*modalitas MR yang berbeda dari modalitas order DX*");
        order.Items.Should().HaveCount(1);
    }

    [Fact]
    public void SingleModalityGuard_AddItemWithMatchingModality_Succeeds()
    {
        var order = RadOrderModel.CreateFromEmr(
            emrOrderId: "EMR-RAD-004",
            patient: EmrSnapshot(),
            modality: RadModalityEnum.CT,
            items: [CreateItem("TRF01", "CT Thorax", RadModalityEnum.CT)],
            orderNo: "RAD26110005",
            audit: TestAudit());

        order.AddItem(CreateItem("TRF02", "CT Abdomen", RadModalityEnum.CT));

        order.Items.Should().HaveCount(2);
        order.Items[0].ItemNo.Should().Be(1);
        order.Items[0].TarifId.Should().Be("TRF01");
        order.Items[1].ItemNo.Should().Be(2);
        order.Items[1].TarifId.Should().Be("TRF02");
        order.Items[1].OrderId.Should().Be(order.OrderId);
    }

    [Fact]
    public void Cancel_FromRequestedStatus_CancelsSuccessfully()
    {
        var order = RadOrderModel.CreateFromEmr(
            emrOrderId: "EMR-RAD-005",
            patient: EmrSnapshot(),
            modality: RadModalityEnum.CR,
            items: [CreateItem("TRF01", "Thorax CR", RadModalityEnum.CR)],
            orderNo: "RAD26110006",
            audit: TestAudit());

        var cancelDate = new DateTime(2026, 11, 23, 14, 30, 0);
        order.Cancel("Pasien menolak pemeriksaan", "DR_USER", cancelDate);

        order.RadOrderStatus.Should().Be(RadOrderStatusEnum.Cancelled);
        order.CancelledReason.Should().Be("Pasien menolak pemeriksaan");
        order.CancelledUserId.Should().Be("DR_USER");
        order.CancelledDate.Should().Be(cancelDate);
        order.IsCancelled.Should().BeTrue();
        order.IsActive.Should().BeFalse();
        order.AuditTrail.Modified.UserId.Should().Be("DR_USER");
        order.AuditTrail.Modified.Timestamp.Should().Be(cancelDate);
    }

    [Fact]
    public void Cancel_FromScheduledStatus_CancelsSuccessfully()
    {
        var order = RadOrderModel.CreateFromEmr(
            emrOrderId: "EMR-RAD-006",
            patient: EmrSnapshot(),
            modality: RadModalityEnum.CR,
            items: [CreateItem("TRF01", "Thorax CR", RadModalityEnum.CR)],
            orderNo: "RAD26110007",
            audit: TestAudit());

        order.Schedule("RAD_TECH");
        order.RadOrderStatus.Should().Be(RadOrderStatusEnum.Scheduled);

        order.Cancel("Kondisi pasien tidak memungkinkan", "DR_USER");

        order.RadOrderStatus.Should().Be(RadOrderStatusEnum.Cancelled);
        order.CancelledReason.Should().Be("Kondisi pasien tidak memungkinkan");
        order.CancelledUserId.Should().Be("DR_USER");
        order.IsCancelled.Should().BeTrue();
        order.IsActive.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("a")]
    [InlineData("ab")]
    public void Cancel_ShortOrEmptyReason_ThrowsArgumentException(string shortReason)
    {
        var order = RadOrderModel.CreateFromEmr(
            emrOrderId: "EMR-RAD-007",
            patient: EmrSnapshot(),
            modality: RadModalityEnum.CR,
            items: [CreateItem("TRF01", "Thorax CR", RadModalityEnum.CR)],
            orderNo: "RAD26110008",
            audit: TestAudit());

        var act = () => order.Cancel(shortReason, "USER1");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Alasan pembatalan minimal 3 karakter*");
    }

    [Fact]
    public void Cancel_WhenAlreadyCancelled_ThrowsInvalidOperationException()
    {
        var order = RadOrderModel.CreateFromEmr(
            emrOrderId: "EMR-RAD-008",
            patient: EmrSnapshot(),
            modality: RadModalityEnum.CR,
            items: [CreateItem("TRF01", "Thorax CR", RadModalityEnum.CR)],
            orderNo: "RAD26110009",
            audit: TestAudit());

        order.Cancel("Alasan valid pertama", "USER1");

        var act = () => order.Cancel("Alasan kedua", "USER2");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*pembatalan hanya diizinkan untuk status Requested, Scheduled, atau Charged*");
    }

    [Fact]
    public void Cancel_WhenVoided_ThrowsInvalidOperationException()
    {
        var order = RadOrderModel.CreateFromEmr(
            emrOrderId: "EMR-RAD-009",
            patient: EmrSnapshot(),
            modality: RadModalityEnum.CR,
            items: [CreateItem("TRF01", "Thorax CR", RadModalityEnum.CR)],
            orderNo: "RAD26110010",
            audit: TestAudit());

        order.AuditTrail.Batal("ADMIN", new DateTime(2026, 11, 23, 12, 0, 0));
        order.AuditTrail.IsVoided.Should().BeTrue();

        var act = () => order.Cancel("Alasan pembatalan", "USER1");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*sudah void*");
    }

    [Fact]
    public void IsCito_WhenHeaderUrgencyIsCito_ReturnsTrue()
    {
        var order = RadOrderModel.CreateFromEmr(
            emrOrderId: "EMR-RAD-010",
            patient: EmrSnapshot(),
            modality: RadModalityEnum.CR,
            items: [CreateItem("TRF01", "Thorax", RadModalityEnum.CR, RadUrgencyEnum.Rutin)],
            orderNo: "RAD26110011",
            audit: TestAudit(),
            urgency: RadUrgencyEnum.Cito);

        order.IsCito.Should().BeTrue();
    }

    [Fact]
    public void IsCito_WhenAnyItemIsCito_ReturnsTrue()
    {
        var itemRutin = CreateItem("TRF01", "Thorax AP", RadModalityEnum.CR, RadUrgencyEnum.Rutin);
        var itemCito = CreateItem("TRF02", "Thorax Lateral Cito", RadModalityEnum.CR, RadUrgencyEnum.Cito);

        var order = RadOrderModel.CreateFromEmr(
            emrOrderId: "EMR-RAD-011",
            patient: EmrSnapshot(),
            modality: RadModalityEnum.CR,
            items: [itemRutin, itemCito],
            orderNo: "RAD26110012",
            audit: TestAudit(),
            urgency: RadUrgencyEnum.Rutin);

        order.Urgency.Should().Be(RadUrgencyEnum.Rutin);
        order.IsCito.Should().BeTrue();
    }

    [Fact]
    public void IsCito_WhenAllRutin_ReturnsFalse()
    {
        var item1 = CreateItem("TRF01", "Thorax AP", RadModalityEnum.CR, RadUrgencyEnum.Rutin);
        var item2 = CreateItem("TRF02", "Thorax Lateral", RadModalityEnum.CR, RadUrgencyEnum.Rutin);

        var order = RadOrderModel.CreateFromEmr(
            emrOrderId: "EMR-RAD-012",
            patient: EmrSnapshot(),
            modality: RadModalityEnum.CR,
            items: [item1, item2],
            orderNo: "RAD26110013",
            audit: TestAudit(),
            urgency: RadUrgencyEnum.Rutin);

        order.IsCito.Should().BeFalse();
    }

    [Fact]
    public void Charge_FromRequested_SucceedsWithoutException()
    {
        var order = RadOrderModel.CreateFromEmr(
            emrOrderId: "EMR-RAD-013",
            patient: EmrSnapshot(),
            modality: RadModalityEnum.CR,
            items: [CreateItem("TRF01", "Thorax AP", RadModalityEnum.CR)],
            orderNo: "RAD26110014",
            audit: TestAudit());

        var act = () => order.Charge("USER1");

        act.Should().NotThrow();
        order.RadOrderStatus.Should().Be(RadOrderStatusEnum.Requested);
    }

    [Fact]
    public void Charge_FromScheduled_SucceedsWithoutException()
    {
        var order = RadOrderModel.CreateFromEmr(
            emrOrderId: "EMR-RAD-014",
            patient: EmrSnapshot(),
            modality: RadModalityEnum.CR,
            items: [CreateItem("TRF01", "Thorax AP", RadModalityEnum.CR)],
            orderNo: "RAD26110015",
            audit: TestAudit());

        order.Schedule("TECH1");
        order.RadOrderStatus.Should().Be(RadOrderStatusEnum.Scheduled);

        var act = () => order.Charge("USER1");

        act.Should().NotThrow();
    }

    [Fact]
    public void Charge_WhenVoided_ThrowsInvalidOperationException()
    {
        var order = RadOrderModel.CreateFromEmr(
            emrOrderId: "EMR-RAD-015",
            patient: EmrSnapshot(),
            modality: RadModalityEnum.CR,
            items: [CreateItem("TRF01", "Thorax AP", RadModalityEnum.CR)],
            orderNo: "RAD26110016",
            audit: TestAudit());

        order.AuditTrail.Batal("ADMIN", new DateTime(2026, 11, 24, 10, 0, 0));

        var act = () => order.Charge("USER1");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*sudah void; charge tidak diizinkan*");
    }

    [Fact]
    public void Charge_WhenStatusInvalid_ThrowsInvalidOperationException()
    {
        var order = RadOrderModel.CreateFromEmr(
            emrOrderId: "EMR-RAD-016",
            patient: EmrSnapshot(),
            modality: RadModalityEnum.CR,
            items: [CreateItem("TRF01", "Thorax AP", RadModalityEnum.CR)],
            orderNo: "RAD26110017",
            audit: TestAudit());

        order.Cancel("Batal", "USER1");

        var act = () => order.Charge("USER2");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*charge hanya diizinkan untuk status Requested atau Scheduled*");
    }

    [Fact]
    public void Charge_WhenAlreadyHasBillingTindakanId_ThrowsInvalidOperationException()
    {
        var order = new RadOrderModel(
            orderId: "RDO0000000001",
            emrOrderId: "EMR-RAD-001",
            orderNo: "RAD26110001",
            orderSource: RadOrderSourceEnum.Internal,
            modality: RadModalityEnum.CR,
            radOrderStatus: RadOrderStatusEnum.Requested,
            urgency: RadUrgencyEnum.Rutin,
            patient: EmrSnapshot(),
            executionRegId: "REG001",
            dokterPengorderId: "DR01",
            dokterPengorderName: "dr. Sp.Rad",
            layananPengorderId: "LY01",
            layananPengorderName: "Poli Paru",
            clinicalNote: "Catatan klinis",
            cancelledReason: string.Empty,
            cancelledDate: RadOrderModel.EmptyDate,
            cancelledUserId: string.Empty,
            auditTrail: AuditTrailType.Create("DR01", DateTime.Now),
            items: [CreateItem("TRF01", "Thorax AP", RadModalityEnum.CR)],
            billingTindakanId: "TIN001");

        var act = () => order.Charge("USER2");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*sudah memiliki BillingTindakanId 'TIN001'*");
    }

    [Fact]
    public void MarkCharged_SetsChargedStateAndClearsError()
    {
        var order = RadOrderModel.CreateFromEmr(
            emrOrderId: "EMR-RAD-018",
            patient: EmrSnapshot(),
            modality: RadModalityEnum.CR,
            items: [CreateItem("TRF01", "Thorax AP", RadModalityEnum.CR)],
            orderNo: "RAD26110019",
            audit: TestAudit());

        order.RecordBillingError("Ada kesalahan tarif sebelumnya", "USER1");
        order.BillingLastError.Should().Be("Ada kesalahan tarif sebelumnya");

        var chargedAt = new DateTime(2026, 11, 24, 11, 0, 0);
        order.MarkCharged("TIN12345", "USER2", chargedAt);

        order.RadOrderStatus.Should().Be(RadOrderStatusEnum.Charged);
        order.IsCharged.Should().BeTrue();
        order.BillingTindakanId.Should().Be("TIN12345");
        order.BillingLastError.Should().BeEmpty();
        order.AuditTrail.Modified.UserId.Should().Be("USER2");
        order.AuditTrail.Modified.Timestamp.Should().Be(chargedAt);
    }

    [Fact]
    public void RecordBillingError_KeepsStatusAndSetsError()
    {
        var order = RadOrderModel.CreateFromEmr(
            emrOrderId: "EMR-RAD-019",
            patient: EmrSnapshot(),
            modality: RadModalityEnum.CR,
            items: [CreateItem("TRF01", "Thorax AP", RadModalityEnum.CR)],
            orderNo: "RAD26110020",
            audit: TestAudit());

        var recordedAt = new DateTime(2026, 11, 24, 11, 15, 0);
        order.RecordBillingError("Tarif tidak ditemukan", "BILL_USER", recordedAt);

        order.RadOrderStatus.Should().Be(RadOrderStatusEnum.Requested);
        order.BillingLastError.Should().Be("Tarif tidak ditemukan");
        order.BillingTindakanId.Should().BeEmpty();
        order.AuditTrail.Modified.UserId.Should().Be("BILL_USER");
        order.AuditTrail.Modified.Timestamp.Should().Be(recordedAt);
    }

    [Fact]
    public void RecordBillingError_TruncatesLongMessage()
    {
        var order = RadOrderModel.CreateFromEmr(
            emrOrderId: "EMR-RAD-020",
            patient: EmrSnapshot(),
            modality: RadModalityEnum.CR,
            items: [CreateItem("TRF01", "Thorax AP", RadModalityEnum.CR)],
            orderNo: "RAD26110021",
            audit: TestAudit());

        var longError = new string('Z', 250);
        order.RecordBillingError(longError, "USER1");

        order.BillingLastError.Should().HaveLength(200);
        order.BillingLastError.Should().Be(new string('Z', 200));
    }

    [Fact]
    public void Cancel_FromChargedStatus_CancelsSuccessfully()
    {
        var order = RadOrderModel.CreateFromEmr(
            emrOrderId: "EMR-RAD-021",
            patient: EmrSnapshot(),
            modality: RadModalityEnum.CR,
            items: [CreateItem("TRF01", "Thorax AP", RadModalityEnum.CR)],
            orderNo: "RAD26110022",
            audit: TestAudit());

        order.MarkCharged("TIN-001", "USER1");
        order.RadOrderStatus.Should().Be(RadOrderStatusEnum.Charged);

        var cancelDate = new DateTime(2026, 11, 24, 12, 0, 0);
        order.Cancel("Pasien batal tindakan radiologi", "DOKTER1", cancelDate);

        order.RadOrderStatus.Should().Be(RadOrderStatusEnum.Cancelled);
        order.CancelledReason.Should().Be("Pasien batal tindakan radiologi");
        order.CancelledUserId.Should().Be("DOKTER1");
        order.CancelledDate.Should().Be(cancelDate);
        order.IsCancelled.Should().BeTrue();
        order.IsActive.Should().BeFalse();
        order.AuditTrail.Modified.UserId.Should().Be("DOKTER1");
        order.AuditTrail.Modified.Timestamp.Should().Be(cancelDate);
    }

    [Fact]
    public void Unschedule_FromScheduledStatus_RevertsToRequestedAndUpdatesAudit()
    {
        var order = RadOrderModel.CreateFromEmr(
            emrOrderId: "EMR-RAD-022",
            patient: EmrSnapshot(),
            modality: RadModalityEnum.CT,
            items: [CreateItem("TRF01", "CT Brain", RadModalityEnum.CT)],
            orderNo: "RAD26110023",
            audit: TestAudit());

        var scheduledAt = new DateTime(2026, 11, 25, 9, 0, 0);
        order.Schedule("RAD_ADMIN", scheduledAt);
        order.RadOrderStatus.Should().Be(RadOrderStatusEnum.Scheduled);

        var unscheduledAt = new DateTime(2026, 11, 25, 10, 30, 0);
        order.Unschedule("RAD_ADMIN_2", unscheduledAt);

        order.RadOrderStatus.Should().Be(RadOrderStatusEnum.Requested);
        order.IsActive.Should().BeTrue();
        order.IsCancelled.Should().BeFalse();
        order.AuditTrail.Modified.UserId.Should().Be("RAD_ADMIN_2");
        order.AuditTrail.Modified.Timestamp.Should().Be(unscheduledAt);
    }

    [Fact]
    public void Unschedule_WithDefaultTimestamp_SetsModifiedTimestampAndRevertsToRequested()
    {
        var order = RadOrderModel.CreateFromEmr(
            emrOrderId: "EMR-RAD-023",
            patient: EmrSnapshot(),
            modality: RadModalityEnum.CT,
            items: [CreateItem("TRF01", "CT Brain", RadModalityEnum.CT)],
            orderNo: "RAD26110024",
            audit: TestAudit());

        order.Schedule("RAD_ADMIN");
        var beforeUnschedule = DateTime.Now.AddSeconds(-1);

        order.Unschedule("RAD_ADMIN_2");

        order.RadOrderStatus.Should().Be(RadOrderStatusEnum.Requested);
        order.AuditTrail.Modified.UserId.Should().Be("RAD_ADMIN_2");
        order.AuditTrail.Modified.Timestamp.Should().BeOnOrAfter(beforeUnschedule);
    }

    [Fact]
    public void Unschedule_WhenStatusRequested_ThrowsInvalidOperationException()
    {
        var order = RadOrderModel.CreateFromEmr(
            emrOrderId: "EMR-RAD-024",
            patient: EmrSnapshot(),
            modality: RadModalityEnum.CT,
            items: [CreateItem("TRF01", "CT Brain", RadModalityEnum.CT)],
            orderNo: "RAD26110025",
            audit: TestAudit());

        var act = () => order.Unschedule("RAD_ADMIN");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*hanya diizinkan untuk status Scheduled*");
    }

    [Fact]
    public void Unschedule_WhenStatusChargedOrCancelled_ThrowsInvalidOperationException()
    {
        var chargedOrder = RadOrderModel.CreateFromEmr(
            emrOrderId: "EMR-RAD-025",
            patient: EmrSnapshot(),
            modality: RadModalityEnum.CT,
            items: [CreateItem("TRF01", "CT Brain", RadModalityEnum.CT)],
            orderNo: "RAD26110026",
            audit: TestAudit());
        chargedOrder.MarkCharged("TIN-002", "USER1");

        var cancelledOrder = RadOrderModel.CreateFromEmr(
            emrOrderId: "EMR-RAD-026",
            patient: EmrSnapshot(),
            modality: RadModalityEnum.CT,
            items: [CreateItem("TRF01", "CT Brain", RadModalityEnum.CT)],
            orderNo: "RAD26110027",
            audit: TestAudit());
        cancelledOrder.Schedule("RAD_ADMIN");
        cancelledOrder.Cancel("Batal pemeriksaan", "USER1");

        var actCharged = () => chargedOrder.Unschedule("RAD_ADMIN");
        var actCancelled = () => cancelledOrder.Unschedule("RAD_ADMIN");

        actCharged.Should().Throw<InvalidOperationException>()
            .WithMessage("*hanya diizinkan untuk status Scheduled*");
        actCancelled.Should().Throw<InvalidOperationException>()
            .WithMessage("*hanya diizinkan untuk status Scheduled*");
    }

    [Fact]
    public void Unschedule_WhenVoided_ThrowsInvalidOperationException()
    {
        var order = RadOrderModel.CreateFromEmr(
            emrOrderId: "EMR-RAD-027",
            patient: EmrSnapshot(),
            modality: RadModalityEnum.CT,
            items: [CreateItem("TRF01", "CT Brain", RadModalityEnum.CT)],
            orderNo: "RAD26110028",
            audit: TestAudit());

        order.Schedule("RAD_ADMIN");
        order.AuditTrail.Batal("ADMIN", new DateTime(2026, 11, 25, 11, 0, 0));

        var act = () => order.Unschedule("RAD_ADMIN");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*sudah void*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Unschedule_WhenUserIdNullOrWhiteSpace_ThrowsArgumentException(string? invalidUserId)
    {
        var order = RadOrderModel.CreateFromEmr(
            emrOrderId: "EMR-RAD-028",
            patient: EmrSnapshot(),
            modality: RadModalityEnum.CT,
            items: [CreateItem("TRF01", "CT Brain", RadModalityEnum.CT)],
            orderNo: "RAD26110029",
            audit: TestAudit());

        order.Schedule("RAD_ADMIN");

        var act = () => order.Unschedule(invalidUserId!);

        act.Should().Throw<ArgumentException>();
    }
}
