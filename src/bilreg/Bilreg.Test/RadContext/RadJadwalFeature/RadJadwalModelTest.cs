using Bilreg.Application.RadContext.RadJadwalFeature;
using Bilreg.Domain.RadContext.RadJadwalFeature;
using Bilreg.Domain.RadContext.RadOrderFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.RadContext.RadJadwalFeature;

public class RadJadwalModelTest
{
    private static AuditInfoType TestAudit(string userId = "RAD_ADMIN")
        => new(userId, new DateTime(2026, 11, 26, 8, 0, 0));

    private static PatientSnapshotType SamplePatient(
        string regId = "REG001",
        string patientId = "MR0001",
        string patientName = "Pasien Uji Jadwal")
        => new(
            regId,
            patientId,
            patientName,
            new DateTime(1990, 5, 15),
            "L",
            36);

    private static RadJadwalModel CreateSampleJadwal(
        RadModalityEnum modality = RadModalityEnum.CT,
        TimeOnly? startTime = null,
        int durationMinutes = 20,
        int? turnaroundBufferMinutes = null,
        string jadwalRadId = "")
        => RadJadwalModel.Create(
            orderId: "RDO0000000001",
            orderNo: "RAD26110001",
            patient: SamplePatient(),
            modality: modality,
            scheduleNo: "SCH26110001",
            equipmentId: "EQ-CT01",
            equipmentName: "CT Scan 64 Slice",
            roomId: "RM-CT01",
            roomName: "Ruang CT Scan 1",
            scheduleDate: new DateOnly(2026, 11, 27),
            startTime: startTime ?? new TimeOnly(9, 0),
            durationMinutes: durationMinutes,
            audit: TestAudit(),
            turnaroundBufferMinutes: turnaroundBufferMinutes,
            doctorRadiologyId: "DR-RAD01",
            doctorRadiologyName: "dr. Radiologi, Sp.Rad",
            preparationNotes: "Puasa 4 jam sebelum tindakan",
            labRequirements: "Ureum & Kreatinin",
            safetyChecklist: new RadSafetyChecklistType(false, false, false, "Aman"),
            jadwalRadId: jadwalRadId);

    [Fact]
    public void Default_HasExpectedValues()
    {
        var model = RadJadwalModel.Default;

        model.JadwalRadId.Should().Be("-");
        model.ScheduleNo.Should().BeEmpty();
        model.OrderId.Should().BeEmpty();
        model.OrderNo.Should().BeEmpty();
        model.RegId.Should().BeEmpty();
        model.Patient.Should().Be(PatientSnapshotType.Default);
        model.Modality.Should().Be(RadModalityEnum.CT);
        model.Status.Should().Be(RadJadwalStatusEnum.Scheduled);
        model.IsScheduled.Should().BeTrue();
        model.IsCompleted.Should().BeFalse();
        model.IsCancelled.Should().BeFalse();
        model.IsActive.Should().BeTrue();
        model.IsArrived.Should().BeFalse();
        model.ArrivedAt.Should().BeNull();
        model.IsCleared.Should().BeFalse();
        model.ClearedAt.Should().BeNull();
        model.RescheduleCount.Should().Be(0);
        model.CancellationCategory.Should().BeNull();
        model.CancellationReason.Should().BeEmpty();
        model.SafetyChecklist.Should().Be(RadSafetyChecklistType.Default);
    }

    [Theory]
    [InlineData(RadModalityEnum.MR, 15)]
    [InlineData(RadModalityEnum.CT, 10)]
    [InlineData(RadModalityEnum.US, 10)]
    [InlineData(RadModalityEnum.CR, 10)]
    [InlineData(RadModalityEnum.DX, 10)]
    public void GetDefaultTurnaroundBufferMinutes_ReturnsExpectedDefaultByModality(
        RadModalityEnum modality,
        int expectedBufferMinutes)
    {
        var buffer = RadJadwalModel.GetDefaultTurnaroundBufferMinutes(modality);

        buffer.Should().Be(expectedBufferMinutes);
    }

    [Fact]
    public void Create_WithMrModality_Uses15MinuteTurnaroundBufferDefaultAndComputesEndTime()
    {
        var start = new TimeOnly(10, 0);
        var jadwal = CreateSampleJadwal(
            modality: RadModalityEnum.MR,
            startTime: start,
            durationMinutes: 30);

        jadwal.JadwalRadId.Should().StartWith(RadJadwalModel.IdPrefix);
        jadwal.ScheduleNo.Should().Be("SCH26110001");
        jadwal.OrderId.Should().Be("RDO0000000001");
        jadwal.OrderNo.Should().Be("RAD26110001");
        jadwal.RegId.Should().Be("REG001");
        jadwal.Modality.Should().Be(RadModalityEnum.MR);
        jadwal.DurationMinutes.Should().Be(30);
        jadwal.TurnaroundBufferMinutes.Should().Be(15);
        jadwal.StartTime.Should().Be(new TimeOnly(10, 0));
        jadwal.EndTime.Should().Be(new TimeOnly(10, 45));
        jadwal.Status.Should().Be(RadJadwalStatusEnum.Scheduled);
        jadwal.IsScheduled.Should().BeTrue();
        jadwal.IsActive.Should().BeTrue();
        jadwal.RescheduleCount.Should().Be(0);
        jadwal.IsArrived.Should().BeFalse();
        jadwal.IsCleared.Should().BeFalse();
        jadwal.PreparationNotes.Should().Be("Puasa 4 jam sebelum tindakan");
        jadwal.LabRequirements.Should().Be("Ureum & Kreatinin");
        jadwal.SafetyChecklist.Notes.Should().Be("Aman");
        jadwal.AuditTrail.Created.UserId.Should().Be("RAD_ADMIN");
    }

    [Theory]
    [InlineData(RadModalityEnum.CT, 20, 10, 30)]
    [InlineData(RadModalityEnum.US, 20, 10, 30)]
    public void Create_WithCtOrUsModality_Uses10MinuteTurnaroundBufferDefault(
        RadModalityEnum modality,
        int durationMinutes,
        int expectedBuffer,
        int expectedTotalMinutes)
    {
        var start = new TimeOnly(8, 30);
        var jadwal = CreateSampleJadwal(
            modality: modality,
            startTime: start,
            durationMinutes: durationMinutes);

        jadwal.TurnaroundBufferMinutes.Should().Be(expectedBuffer);
        jadwal.EndTime.Should().Be(start.AddMinutes(expectedTotalMinutes));
    }

    [Fact]
    public void Create_WithExplicitBufferAndId_RespectsExplicitValues()
    {
        var jadwal = CreateSampleJadwal(
            modality: RadModalityEnum.MR,
            startTime: new TimeOnly(14, 0),
            durationMinutes: 45,
            turnaroundBufferMinutes: 20,
            jadwalRadId: "JAD-CUSTOM-01");

        jadwal.JadwalRadId.Should().Be("JAD-CUSTOM-01");
        jadwal.TurnaroundBufferMinutes.Should().Be(20);
        jadwal.EndTime.Should().Be(new TimeOnly(15, 5));
    }

    [Theory]
    [InlineData("", "RAD01", "Pasien", "SCH01", "EQ01", "U1")]
    [InlineData("RDO01", "", "Pasien", "SCH01", "EQ01", "U1")]
    [InlineData("RDO01", "RAD01", "", "SCH01", "EQ01", "U1")]
    [InlineData("RDO01", "RAD01", "Pasien", "", "EQ01", "U1")]
    [InlineData("RDO01", "RAD01", "Pasien", "SCH01", "", "U1")]
    [InlineData("RDO01", "RAD01", "Pasien", "SCH01", "EQ01", "")]
    public void Create_WithMissingRequiredStringArgs_ThrowsArgumentException(
        string orderId,
        string orderNo,
        string patientName,
        string scheduleNo,
        string equipmentId,
        string userId)
    {
        var patient = SamplePatient(patientName: patientName);
        var audit = new AuditInfoType(userId, new DateTime(2026, 11, 26, 8, 0, 0));

        var act = () => RadJadwalModel.Create(
            orderId,
            orderNo,
            patient,
            RadModalityEnum.CT,
            scheduleNo,
            equipmentId,
            "CT Scan",
            "RM01",
            "Ruang CT",
            new DateOnly(2026, 11, 27),
            new TimeOnly(9, 0),
            20,
            audit);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Create_WithNonPositiveDuration_ThrowsArgumentException(int durationMinutes)
    {
        var act = () => CreateSampleJadwal(durationMinutes: durationMinutes);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Durasi pemeriksaan*");
    }

    [Fact]
    public void Create_WithNegativeTurnaroundBuffer_ThrowsArgumentException()
    {
        var act = () => CreateSampleJadwal(turnaroundBufferMinutes: -1);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Turnaround buffer*");
    }

    [Fact]
    public void Create_WhenTimeSpanCrossesMidnight_ThrowsArgumentException()
    {
        var act = () => CreateSampleJadwal(
            modality: RadModalityEnum.MR,
            startTime: new TimeOnly(23, 30),
            durationMinutes: 20);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*melewati batas hari*");
    }

    [Fact]
    public void Reschedule_InPlace_UpdatesSlotEquipmentIncrementsCountAndUpdatesAudit()
    {
        var jadwal = CreateSampleJadwal(modality: RadModalityEnum.CT, startTime: new TimeOnly(9, 0), durationMinutes: 20);
        var originalId = jadwal.JadwalRadId;
        var originalScheduleNo = jadwal.ScheduleNo;
        var rescheduledAt = new DateTime(2026, 11, 26, 11, 15, 0);

        jadwal.Reschedule(
            newScheduleDate: new DateOnly(2026, 11, 28),
            newStartTime: new TimeOnly(13, 0),
            newEndTime: new TimeOnly(13, 40),
            newEquipmentId: "EQ-CT02",
            newEquipmentName: "CT Scan 128 Slice",
            newRoomId: "RM-CT02",
            newRoomName: "Ruang CT Scan 2",
            reason: "Kalibrasi mesin CT 1",
            userId: "RAD_SPV",
            rescheduledAt: rescheduledAt);

        jadwal.JadwalRadId.Should().Be(originalId);
        jadwal.ScheduleNo.Should().Be(originalScheduleNo);
        jadwal.ScheduleDate.Should().Be(new DateOnly(2026, 11, 28));
        jadwal.StartTime.Should().Be(new TimeOnly(13, 0));
        jadwal.EndTime.Should().Be(new TimeOnly(13, 40));
        jadwal.DurationMinutes.Should().Be(30); // 40 total - 10 buffer
        jadwal.TurnaroundBufferMinutes.Should().Be(10);
        jadwal.EquipmentId.Should().Be("EQ-CT02");
        jadwal.EquipmentName.Should().Be("CT Scan 128 Slice");
        jadwal.RoomId.Should().Be("RM-CT02");
        jadwal.RoomName.Should().Be("Ruang CT Scan 2");
        jadwal.RescheduleCount.Should().Be(1);
        jadwal.Status.Should().Be(RadJadwalStatusEnum.Scheduled);
        jadwal.AuditTrail.Modified.UserId.Should().Be("RAD_SPV");
        jadwal.AuditTrail.Modified.Timestamp.Should().Be(rescheduledAt);
    }

    [Fact]
    public void Reschedule_ShorterThanBuffer_ResetsBufferToZeroAndKeepsDuration()
    {
        var jadwal = CreateSampleJadwal(modality: RadModalityEnum.MR, startTime: new TimeOnly(9, 0), durationMinutes: 30);

        jadwal.Reschedule(
            newScheduleDate: new DateOnly(2026, 11, 28),
            newStartTime: new TimeOnly(10, 0),
            newEndTime: new TimeOnly(10, 10),
            newEquipmentId: "EQ-MRI01",
            reason: "Slot darurat singkat",
            userId: "RAD_SPV");

        jadwal.DurationMinutes.Should().Be(10);
        jadwal.TurnaroundBufferMinutes.Should().Be(0);
        jadwal.RescheduleCount.Should().Be(1);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("ab")]
    public void Reschedule_WithInvalidReason_ThrowsArgumentException(string shortReason)
    {
        var jadwal = CreateSampleJadwal();

        var act = () => jadwal.Reschedule(
            new DateOnly(2026, 11, 28),
            new TimeOnly(10, 0),
            new TimeOnly(10, 30),
            "EQ-CT01",
            shortReason,
            "RAD_SPV");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Alasan penjadwalan ulang minimal 3 karakter*");
    }

    [Fact]
    public void Reschedule_WhenEndTimeNotAfterStartTime_ThrowsArgumentException()
    {
        var jadwal = CreateSampleJadwal();

        var act = () => jadwal.Reschedule(
            new DateOnly(2026, 11, 28),
            new TimeOnly(10, 30),
            new TimeOnly(10, 30),
            "EQ-CT01",
            "Perubahan jam",
            "RAD_SPV");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Waktu selesai harus lebih besar dari waktu mulai*");
    }

    [Fact]
    public void Reschedule_WhenNotScheduledOrVoided_ThrowsInvalidOperationException()
    {
        var cancelled = CreateSampleJadwal();
        cancelled.Cancel(RadCancellationCategoryEnum.PasienBatal, "Pasien berhalangan", "U1");

        var voided = CreateSampleJadwal();
        voided.AuditTrail.Batal("ADMIN", new DateTime(2026, 11, 26, 9, 0, 0));

        var actCancelled = () => cancelled.Reschedule(
            new DateOnly(2026, 11, 28),
            new TimeOnly(10, 0),
            new TimeOnly(10, 30),
            "EQ-CT01",
            "Reschedule jadwal batal",
            "U2");

        var actVoided = () => voided.Reschedule(
            new DateOnly(2026, 11, 28),
            new TimeOnly(10, 0),
            new TimeOnly(10, 30),
            "EQ-CT01",
            "Reschedule jadwal void",
            "U2");

        actCancelled.Should().Throw<InvalidOperationException>()
            .WithMessage("*status Scheduled*");
        actVoided.Should().Throw<InvalidOperationException>()
            .WithMessage("*sudah void*");
    }

    [Fact]
    public void Cancel_ValidScheduled_SetsCancelledStateCategoryReasonAndAudit()
    {
        var jadwal = CreateSampleJadwal();
        var cancelledAt = new DateTime(2026, 11, 26, 12, 30, 0);

        jadwal.Cancel(
            RadCancellationCategoryEnum.MesinRusak,
            "Tabung X-Ray overheating",
            "TECH_LEAD",
            cancelledAt);

        jadwal.Status.Should().Be(RadJadwalStatusEnum.Cancelled);
        jadwal.IsCancelled.Should().BeTrue();
        jadwal.IsScheduled.Should().BeFalse();
        jadwal.IsActive.Should().BeFalse();
        jadwal.CancellationCategory.Should().Be(RadCancellationCategoryEnum.MesinRusak);
        jadwal.CancellationReason.Should().Be("Tabung X-Ray overheating");
        jadwal.AuditTrail.Modified.UserId.Should().Be("TECH_LEAD");
        jadwal.AuditTrail.Modified.Timestamp.Should().Be(cancelledAt);
    }

    [Fact]
    public void Cancel_LongReason_TruncatesTo200Chars()
    {
        var jadwal = CreateSampleJadwal();
        var longReason = new string('R', 250);

        jadwal.Cancel(RadCancellationCategoryEnum.PasienBatal, longReason, "U1");

        jadwal.CancellationReason.Should().HaveLength(200);
        jadwal.CancellationReason.Should().Be(new string('R', 200));
    }

    [Fact]
    public void Cancel_InvalidCategoryOrShortReasonOrInvalidState_ThrowsExpectedException()
    {
        var jadwal = CreateSampleJadwal();

        var actInvalidCategory = () => jadwal.Cancel((RadCancellationCategoryEnum)99, "Alasan valid", "U1");
        var actShortReason = () => jadwal.Cancel(RadCancellationCategoryEnum.NoShow, "ab", "U1");

        actInvalidCategory.Should().Throw<ArgumentException>();
        actShortReason.Should().Throw<ArgumentException>();

        jadwal.Cancel(RadCancellationCategoryEnum.NoShow, "Pasien tidak datang", "U1");
        var actDoubleCancel = () => jadwal.Cancel(RadCancellationCategoryEnum.PasienBatal, "Batal lagi", "U2");

        actDoubleCancel.Should().Throw<InvalidOperationException>()
            .WithMessage("*status Scheduled*");
    }

    [Fact]
    public void RecordArrival_ValidScheduled_SetsIsArrivedAndArrivedAt()
    {
        var jadwal = CreateSampleJadwal();
        var arrivedAt = new DateTime(2026, 11, 27, 8, 45, 0);

        jadwal.RecordArrival("FO_RAD", arrivedAt);

        jadwal.IsArrived.Should().BeTrue();
        jadwal.ArrivedAt.Should().Be(arrivedAt);
        jadwal.Status.Should().Be(RadJadwalStatusEnum.Scheduled);
        jadwal.AuditTrail.Modified.UserId.Should().Be("FO_RAD");
        jadwal.AuditTrail.Modified.Timestamp.Should().Be(arrivedAt);
    }

    [Fact]
    public void RecordArrival_WhenCancelledOrVoided_ThrowsInvalidOperationException()
    {
        var cancelled = CreateSampleJadwal();
        cancelled.Cancel(RadCancellationCategoryEnum.PasienBatal, "Batal", "U1");

        var voided = CreateSampleJadwal();
        voided.AuditTrail.Batal("ADMIN", new DateTime(2026, 11, 26, 9, 0, 0));

        var actCancelled = () => cancelled.RecordArrival("FO_RAD");
        var actVoided = () => voided.RecordArrival("FO_RAD");

        actCancelled.Should().Throw<InvalidOperationException>()
            .WithMessage("*status Scheduled*");
        actVoided.Should().Throw<InvalidOperationException>()
            .WithMessage("*sudah void*");
    }

    [Fact]
    public void VerifyClearance_ValidScheduled_UpdatesClearanceChecklistAndAudit()
    {
        var jadwal = CreateSampleJadwal(modality: RadModalityEnum.MR);
        var clearedAt = new DateTime(2026, 11, 27, 9, 50, 0);
        var checklist = new RadSafetyChecklistType(
            HasMetalImplant: false,
            HasContrastAllergy: true,
            IsPregnant: false,
            Notes: "Premedikasi kortikosteroid selesai");

        jadwal.VerifyClearance(
            isCleared: true,
            clearanceNotes: "Pasien lolos skrining MRI dengan premedikasi",
            clearedByUserId: "NS_RAD01",
            updatedChecklist: checklist,
            clearedAt: clearedAt);

        jadwal.IsCleared.Should().BeTrue();
        jadwal.ClearanceNotes.Should().Be("Pasien lolos skrining MRI dengan premedikasi");
        jadwal.ClearedByUserId.Should().Be("NS_RAD01");
        jadwal.ClearedAt.Should().Be(clearedAt);
        jadwal.SafetyChecklist.Should().Be(checklist);
        jadwal.Status.Should().Be(RadJadwalStatusEnum.Scheduled);
        jadwal.AuditTrail.Modified.UserId.Should().Be("NS_RAD01");
        jadwal.AuditTrail.Modified.Timestamp.Should().Be(clearedAt);
    }

    [Fact]
    public void VerifyClearance_WhenNotCleared_SetsIsClearedFalseAndKeepsScheduled()
    {
        var jadwal = CreateSampleJadwal(modality: RadModalityEnum.MR);
        var checklist = new RadSafetyChecklistType(HasMetalImplant: true, Notes: "Pacemaker terdeteksi");

        jadwal.VerifyClearance(
            isCleared: false,
            clearanceNotes: "Tunda pemeriksaan untuk konsultasi kardiologi",
            clearedByUserId: "NS_RAD01",
            updatedChecklist: checklist);

        jadwal.IsCleared.Should().BeFalse();
        jadwal.SafetyChecklist.HasMetalImplant.Should().BeTrue();
        jadwal.Status.Should().Be(RadJadwalStatusEnum.Scheduled);
    }

    [Fact]
    public void Complete_WhenBothArrivedAndCleared_TransitionsToCompleted()
    {
        var jadwal = CreateSampleJadwal();
        var arrivedAt = new DateTime(2026, 11, 27, 8, 50, 0);
        var clearedAt = new DateTime(2026, 11, 27, 8, 55, 0);
        var completedAt = new DateTime(2026, 11, 27, 8, 56, 0);

        jadwal.RecordArrival("FO_RAD", arrivedAt);
        jadwal.VerifyClearance(true, "Aman untuk CT kontras", "NS_RAD", clearedAt: clearedAt);
        jadwal.Complete("TECH_RAD", completedAt);

        jadwal.Status.Should().Be(RadJadwalStatusEnum.Completed);
        jadwal.IsCompleted.Should().BeTrue();
        jadwal.IsScheduled.Should().BeFalse();
        jadwal.IsActive.Should().BeFalse();
        jadwal.AuditTrail.Modified.UserId.Should().Be("TECH_RAD");
        jadwal.AuditTrail.Modified.Timestamp.Should().Be(completedAt);
    }

    [Fact]
    public void Complete_ClearanceGateInvariant_WhenNotArrived_ThrowsInvalidOperationException()
    {
        var jadwal = CreateSampleJadwal();
        jadwal.VerifyClearance(true, "Lolos skrining awal", "NS_RAD");

        jadwal.IsArrived.Should().BeFalse();
        jadwal.IsCleared.Should().BeTrue();

        var act = () => jadwal.Complete("TECH_RAD");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*IsArrived = false*");
        jadwal.Status.Should().Be(RadJadwalStatusEnum.Scheduled);
    }

    [Fact]
    public void Complete_ClearanceGateInvariant_WhenArrivedButNotCleared_ThrowsInvalidOperationException()
    {
        var jadwal = CreateSampleJadwal();
        jadwal.RecordArrival("FO_RAD");

        jadwal.IsArrived.Should().BeTrue();
        jadwal.IsCleared.Should().BeFalse();

        var act = () => jadwal.Complete("TECH_RAD");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*IsCleared = false*");
        jadwal.Status.Should().Be(RadJadwalStatusEnum.Scheduled);
    }

    [Fact]
    public void Complete_WhenAlreadyCompletedOrCancelledOrVoided_ThrowsInvalidOperationException()
    {
        var completed = CreateSampleJadwal();
        completed.RecordArrival("FO_RAD");
        completed.VerifyClearance(true, "OK", "NS_RAD");
        completed.Complete("TECH_RAD");

        var voided = CreateSampleJadwal();
        voided.RecordArrival("FO_RAD");
        voided.VerifyClearance(true, "OK", "NS_RAD");
        voided.AuditTrail.Batal("ADMIN", new DateTime(2026, 11, 26, 10, 0, 0));

        var actCompleted = () => completed.Complete("TECH_RAD");
        var actVoided = () => voided.Complete("TECH_RAD");

        actCompleted.Should().Throw<InvalidOperationException>()
            .WithMessage("*status Scheduled*");
        actVoided.Should().Throw<InvalidOperationException>()
            .WithMessage("*sudah void*");
    }

    [Fact]
    public void DtoMapping_FromModelAndToModel_PreservesDatesTimesChecklistAndLifecycleState()
    {
        var model = CreateSampleJadwal(
            modality: RadModalityEnum.MR,
            startTime: new TimeOnly(10, 15),
            durationMinutes: 30,
            jadwalRadId: "JAD0000000001");
        model.RecordArrival("FO_RAD", new DateTime(2026, 11, 27, 10, 0, 0));
        model.VerifyClearance(
            true,
            "Lolos skrining MRI",
            "NS_RAD",
            new RadSafetyChecklistType(false, true, false, "Alergi ringan terkontrol"),
            new DateTime(2026, 11, 27, 10, 5, 0));
        model.Complete("TECH_RAD", new DateTime(2026, 11, 27, 10, 6, 0));

        var dto = RadJadwalDto.FromModel(model, phone: "081234567890");
        var roundTripped = dto.ToModel();

        dto.ScheduleDate.Should().Be(new DateTime(2026, 11, 27));
        dto.StartTime.Should().Be(new TimeSpan(10, 15, 0));
        dto.EndTime.Should().Be(new TimeSpan(11, 0, 0));
        dto.Phone.Should().Be("081234567890");
        dto.Modality.Should().Be("MR");

        roundTripped.JadwalRadId.Should().Be(model.JadwalRadId);
        roundTripped.ScheduleNo.Should().Be(model.ScheduleNo);
        roundTripped.OrderId.Should().Be(model.OrderId);
        roundTripped.Modality.Should().Be(RadModalityEnum.MR);
        roundTripped.ScheduleDate.Should().Be(new DateOnly(2026, 11, 27));
        roundTripped.StartTime.Should().Be(new TimeOnly(10, 15));
        roundTripped.EndTime.Should().Be(new TimeOnly(11, 0));
        roundTripped.DurationMinutes.Should().Be(30);
        roundTripped.TurnaroundBufferMinutes.Should().Be(15);
        roundTripped.IsArrived.Should().BeTrue();
        roundTripped.ArrivedAt.Should().Be(new DateTime(2026, 11, 27, 10, 0, 0));
        roundTripped.IsCleared.Should().BeTrue();
        roundTripped.ClearedByUserId.Should().Be("NS_RAD");
        roundTripped.ClearedAt.Should().Be(new DateTime(2026, 11, 27, 10, 5, 0));
        roundTripped.Status.Should().Be(RadJadwalStatusEnum.Completed);
        roundTripped.SafetyChecklist.HasContrastAllergy.Should().BeTrue();
        roundTripped.SafetyChecklist.Notes.Should().Be("Alergi ringan terkontrol");
    }
}
