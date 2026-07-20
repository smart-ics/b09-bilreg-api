using Bilreg.Domain.BedUsageContext.RnaServiceExecutionFeature;
using FluentAssertions;

namespace Bilreg.Test.BedUsageContext.RnaServiceExecutionFeature;

public class RnaServiceExecutionModelTest
{
    private static readonly DateTime PerformedAt =
        new(2026, 7, 17, 8, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime RecordedAt =
        new(2026, 7, 17, 9, 0, 0, DateTimeKind.Utc);

    private static RnaServiceExecutionModel CreateOrdered() =>
        RnaServiceExecutionModel.CreateOrdered(
            "RG001", "PS001", "CARE001", "WARD001", "ORD001", "OCC001",
            "OBL001", "CPOE", "SRC001", 1);

    [Fact]
    public void GivenOrderedWork_WhenAssigned_ThenItIsNotExecutionEvidence()
    {
        var assigned = CreateOrdered().AssignPerformer("PEG001");

        assigned.WorkStatus.Should().Be(RnaServiceWorkStatusEnum.Assigned);
        assigned.ListExecutionFact.Should().BeEmpty();
        assigned.IsExecuted.Should().BeFalse();
    }

    [Fact]
    public void GivenPendingWork_WhenBillableExecutionRecorded_ThenItContainsServiceAndActualTime()
    {
        var executed = CreateOrdered().RecordExecution(
            BillableClassificationEnum.Billable,
            TarifServiceReff.Create("TRF001", "Nebulisasi"),
            null,
            "PEG001",
            PerformedAt,
            RecordedAt,
            "PEG001",
            "Dicatat setelah tindakan selesai");

        executed.WorkStatus.Should().Be(RnaServiceWorkStatusEnum.Executed);
        executed.CurrentExecutionFact!.TarifService!.TarifServiceId.Should().Be("TRF001");
        executed.CurrentExecutionFact.PerformedAt.Should().Be(PerformedAt);
        executed.CurrentExecutionFact.RecordedAt.Should().Be(RecordedAt);
    }

    [Fact]
    public void GivenBillableExecutionWithDescription_WhenRecorded_ThenThrows()
    {
        var act = () => CreateOrdered().RecordExecution(
            BillableClassificationEnum.Billable,
            TarifServiceReff.Create("TRF001", "Nebulisasi"),
            "Tidak boleh diisi",
            "PEG001", PerformedAt, RecordedAt, "PEG001", "late entry");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GivenLateEntryWithoutReason_WhenRecorded_ThenThrows()
    {
        var act = () => CreateOrdered().RecordExecution(
            BillableClassificationEnum.NonBillable,
            null,
            "Edukasi pasien",
            "PEG001", PerformedAt, RecordedAt, "PEG001", null);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Late entry wajib memiliki alasan*");
    }

    [Fact]
    public void GivenWithdrawnWork_WhenRecordingExecution_ThenThrows()
    {
        var withdrawn = CreateOrdered().WithdrawUnexecutedWork();

        var act = () => withdrawn.RecordExecution(
            BillableClassificationEnum.NonBillable, null, "Edukasi pasien", "PEG001",
            PerformedAt, PerformedAt, "PEG001", null);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void GivenExecutedWork_WhenCorrected_ThenOriginalAndCorrectionAreRetained()
    {
        var executed = CreateOrdered().RecordExecution(
            BillableClassificationEnum.Billable,
            TarifServiceReff.Create("TRF001", "Nebulisasi"),
            null, "PEG001", PerformedAt, PerformedAt, "PEG001", null);

        var corrected = executed.CorrectExecution(
            ExecutionCorrectionKindEnum.Corrected,
            BillableClassificationEnum.Billable,
            TarifServiceReff.Create("TRF002", "Fisioterapi"),
            null, "PEG002", PerformedAt.AddMinutes(30), RecordedAt, "PEG002", "Late entry",
            "Service awal salah", "HEAD001", null, null, RecordedAt);

        corrected.ListExecutionFact.Should().HaveCount(2);
        corrected.ListCorrection.Should().ContainSingle();
        corrected.CurrentExecutionFact!.TarifService!.TarifServiceId.Should().Be("TRF002");
    }

    [Fact]
    public void GivenExecutedWork_WhenEnteredInError_ThenHistoryRemainsButNoActiveFactExists()
    {
        var executed = CreateOrdered().RecordExecution(
            BillableClassificationEnum.NonBillable, null, "Edukasi pasien", "PEG001",
            PerformedAt, PerformedAt, "PEG001", null);

        var enteredInError = executed.MarkEnteredInError(
            "Duplikasi occurrence", "HEAD001", "GOV001", "EVID001", RecordedAt, RecordedAt);

        enteredInError.WorkStatus.Should().Be(RnaServiceWorkStatusEnum.EnteredInError);
        enteredInError.ListExecutionFact.Should().ContainSingle();
        enteredInError.ListCorrection.Should().ContainSingle()
            .Which.CorrectionKind.Should().Be(ExecutionCorrectionKindEnum.EnteredInError);
        enteredInError.CurrentExecutionFact.Should().BeNull();
    }

    [Fact]
    public void GivenReplacementCorrectionWithoutIndependentReviewer_WhenCorrected_ThenThrows()
    {
        var executed = CreateOrdered().RecordExecution(
            BillableClassificationEnum.NonBillable, null, "Edukasi pasien", "PEG001",
            PerformedAt, PerformedAt, "PEG001", null);

        var act = () => executed.CorrectExecution(
            ExecutionCorrectionKindEnum.Replaced,
            BillableClassificationEnum.NonBillable, null, "Edukasi pengganti", "PEG002",
            PerformedAt, PerformedAt, "PEG002", null, "Salah pasien", "HEAD001", null, null,
            RecordedAt);

        act.Should().Throw<ArgumentException>();
    }
}
