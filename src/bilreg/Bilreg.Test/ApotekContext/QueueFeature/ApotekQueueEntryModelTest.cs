using Bilreg.Domain.ApotekContext.QueueFeature;
using Bilreg.Domain.ApotekContext.Shared;
using FluentAssertions;

namespace Bilreg.Test.ApotekContext.QueueFeature;

public class ApotekQueueEntryModelTest
{
    private static readonly DateTime BaseTime = new(2026, 10, 18, 8, 0, 0);

    private static ApotekQueueEntryModel CreateSampleEntry(string prefix = "A", int noUrut = 5)
    {
        return ApotekQueueEntryModel.Create("Q20261018-01", noUrut, "SP01", prefix, BaseTime);
    }

    [Fact]
    public void Create_initializes_queue_entry_with_taken_phase_and_formatted_label()
    {
        var entry = ApotekQueueEntryModel.Create("Q20261018-01", 5, "SP01", "a", BaseTime);

        entry.AntrianId.Should().Be("Q20261018-01");
        entry.NoUrut.Should().Be(5);
        entry.ServicePointId.Should().Be("SP01");
        entry.QueueLabel.Should().Be("A005");
        entry.ApotekPhase.Should().Be(ApotekPhaseEnum.TAKEN);
        entry.TakenAt.Should().Be(BaseTime);
        entry.CallCountPenerimaan.Should().Be(0);
        entry.LastCalledPenerimaanAt.Should().Be(ApotekDate.Empty);
        entry.CallCountSerah.Should().Be(0);
        entry.LastCalledSerahAt.Should().Be(ApotekDate.Empty);
        entry.ServedAt.Should().Be(ApotekDate.Empty);
        entry.PreparedAt.Should().Be(ApotekDate.Empty);
        entry.DoneAt.Should().Be(ApotekDate.Empty);
        entry.WithdrawnAt.Should().Be(ApotekDate.Empty);
        entry.LastSkippedAt.Should().Be(ApotekDate.Empty);
        entry.LastSkippedLoketType.Should().Be(LoketTypeEnum.None);
        entry.WithdrawalReason.Should().BeEmpty();
        entry.WithdrawalUserId.Should().BeEmpty();
        entry.IsPaymentOverridden.Should().BeFalse();
        entry.PaymentOverrideReason.Should().BeEmpty();
        entry.PaymentOverridePharmacistId.Should().BeEmpty();
        entry.IsTerminal.Should().BeFalse();
    }

    [Fact]
    public void Create_guards_against_invalid_parameters()
    {
        Action blankAntrian = () => ApotekQueueEntryModel.Create("", 1, "SP01", "A", BaseTime);
        blankAntrian.Should().Throw<ArgumentException>().WithParameterName("antrianId");

        Action zeroNoUrut = () => ApotekQueueEntryModel.Create("Q1", 0, "SP01", "A", BaseTime);
        zeroNoUrut.Should().Throw<ArgumentException>().WithParameterName("noUrut");

        Action negativeNoUrut = () => ApotekQueueEntryModel.Create("Q1", -1, "SP01", "A", BaseTime);
        negativeNoUrut.Should().Throw<ArgumentException>().WithParameterName("noUrut");

        Action blankServicePoint = () => ApotekQueueEntryModel.Create("Q1", 1, "", "A", BaseTime);
        blankServicePoint.Should().Throw<ArgumentException>().WithParameterName("servicePointId");

        Action blankPrefix = () => ApotekQueueEntryModel.Create("Q1", 1, "SP01", "", BaseTime);
        blankPrefix.Should().Throw<ArgumentException>().WithParameterName("queuePrefix");

        Action invalidTime = () => ApotekQueueEntryModel.Create("Q1", 1, "SP01", "A", ApotekDate.Empty);
        invalidTime.Should().Throw<ArgumentException>().WithParameterName("takenAt");
    }

    [Fact]
    public void RecordCallPenerimaan_increments_counter_and_updates_timestamp()
    {
        var entry = CreateSampleEntry();
        var callTime1 = BaseTime.AddMinutes(5);
        var callTime2 = BaseTime.AddMinutes(7);

        entry.RecordCallPenerimaan(callTime1);
        entry.CallCountPenerimaan.Should().Be(1);
        entry.LastCalledPenerimaanAt.Should().Be(callTime1);

        entry.RecordCallPenerimaan(callTime2);
        entry.CallCountPenerimaan.Should().Be(2);
        entry.LastCalledPenerimaanAt.Should().Be(callTime2);
    }

    [Fact]
    public void RecordCallPenerimaan_throws_when_in_invalid_phase_or_terminal()
    {
        var entry = CreateSampleEntry();
        entry.TransitionToPenyiapan();

        Action callInPreparing = () => entry.RecordCallPenerimaan(BaseTime.AddMinutes(10));
        callInPreparing.Should().Throw<ApotekDomainException>()
            .WithMessage("*Cannot call at Loket Penerimaan when phase is 'PENYIAPAN'*");

        var withdrawn = CreateSampleEntry();
        withdrawn.Withdraw("Patient left", "USER1", BaseTime.AddMinutes(2));

        Action callTerminal = () => withdrawn.RecordCallPenerimaan(BaseTime.AddMinutes(5));
        callTerminal.Should().Throw<ApotekDomainException>()
            .WithMessage("*terminal state*");
    }

    [Fact]
    public void StartServicePenerimaan_transitions_to_PENERIMAAN_and_records_ServedAt()
    {
        var entry = CreateSampleEntry();
        var servedAt = BaseTime.AddMinutes(10);

        entry.StartServicePenerimaan(servedAt);

        entry.ApotekPhase.Should().Be(ApotekPhaseEnum.PENERIMAAN);
        entry.ServedAt.Should().Be(servedAt);
    }

    [Fact]
    public void StartServicePenerimaan_throws_when_invalid_phase_or_terminal()
    {
        var entry = CreateSampleEntry();
        entry.TransitionToPenyiapan();

        Action startInPreparing = () => entry.StartServicePenerimaan(BaseTime.AddMinutes(10));
        startInPreparing.Should().Throw<ApotekDomainException>()
            .WithMessage("*Cannot start service at Loket Penerimaan when phase is 'PENYIAPAN'*");
    }

    [Fact]
    public void ConsolidateToSiapSerah_transitions_from_PENYIAPAN_to_SIAP_SERAH()
    {
        var entry = CreateSampleEntry();
        entry.StartServicePenerimaan(BaseTime.AddMinutes(5));
        entry.TransitionToPenyiapan();

        var preparedAt = BaseTime.AddMinutes(20);
        entry.ConsolidateToSiapSerah(preparedAt);

        entry.ApotekPhase.Should().Be(ApotekPhaseEnum.SIAP_SERAH);
        entry.PreparedAt.Should().Be(preparedAt);
    }

    [Fact]
    public void ConsolidateToSiapSerah_throws_when_not_in_PENYIAPAN()
    {
        var entry = CreateSampleEntry();

        Action invalidConsolidate = () => entry.ConsolidateToSiapSerah(BaseTime.AddMinutes(15));
        invalidConsolidate.Should().Throw<ApotekDomainException>()
            .WithMessage("*Only queue entries in PENYIAPAN may consolidate to SIAP_SERAH*");
    }

    [Fact]
    public void RecordCallSerah_increments_counter_and_updates_timestamp()
    {
        var entry = CreateSampleEntry();
        entry.StartServicePenerimaan(BaseTime.AddMinutes(5));
        entry.TransitionToPenyiapan();
        entry.ConsolidateToSiapSerah(BaseTime.AddMinutes(20));

        var callSerahAt = BaseTime.AddMinutes(22);
        entry.RecordCallSerah(callSerahAt);

        entry.CallCountSerah.Should().Be(1);
        entry.LastCalledSerahAt.Should().Be(callSerahAt);
    }

    [Fact]
    public void RecordCallSerah_throws_when_not_ready_for_handover()
    {
        var entry = CreateSampleEntry();

        Action callBeforeReady = () => entry.RecordCallSerah(BaseTime.AddMinutes(5));
        callBeforeReady.Should().Throw<ApotekDomainException>()
            .WithMessage("*Cannot call at Loket Serah when phase is 'TAKEN'*");
    }

    [Fact]
    public void StartServiceSerah_transitions_to_SERAH_OBAT()
    {
        var entry = CreateSampleEntry();
        entry.StartServicePenerimaan(BaseTime.AddMinutes(5));
        entry.TransitionToPenyiapan();
        entry.ConsolidateToSiapSerah(BaseTime.AddMinutes(20));

        entry.StartServiceSerah(BaseTime.AddMinutes(23));

        entry.ApotekPhase.Should().Be(ApotekPhaseEnum.SERAH_OBAT);
    }

    [Fact]
    public void CompleteHandover_without_override_transitions_to_SELESAI()
    {
        var entry = CreateSampleEntry();
        entry.StartServicePenerimaan(BaseTime.AddMinutes(5));
        entry.TransitionToPenyiapan();
        entry.ConsolidateToSiapSerah(BaseTime.AddMinutes(20));
        entry.StartServiceSerah(BaseTime.AddMinutes(23));

        var doneAt = BaseTime.AddMinutes(25);
        entry.CompleteHandover(doneAt, isPaymentOverridden: false);

        entry.ApotekPhase.Should().Be(ApotekPhaseEnum.SELESAI);
        entry.DoneAt.Should().Be(doneAt);
        entry.IsPaymentOverridden.Should().BeFalse();
        entry.PaymentOverrideReason.Should().BeEmpty();
        entry.PaymentOverridePharmacistId.Should().BeEmpty();
        entry.IsTerminal.Should().BeTrue();
    }

    [Fact]
    public void CompleteHandover_with_valid_override_records_override_metadata()
    {
        var entry = CreateSampleEntry();
        entry.StartServicePenerimaan(BaseTime.AddMinutes(5));
        entry.TransitionToPenyiapan();
        entry.ConsolidateToSiapSerah(BaseTime.AddMinutes(20));
        entry.StartServiceSerah(BaseTime.AddMinutes(23));

        var doneAt = BaseTime.AddMinutes(25);
        entry.CompleteHandover(doneAt, isPaymentOverridden: true, reason: "Cito emergency life support", pharmacistId: "APT-001");

        entry.ApotekPhase.Should().Be(ApotekPhaseEnum.SELESAI);
        entry.DoneAt.Should().Be(doneAt);
        entry.IsPaymentOverridden.Should().BeTrue();
        entry.PaymentOverrideReason.Should().Be("Cito emergency life support");
        entry.PaymentOverridePharmacistId.Should().Be("APT-001");
        entry.IsTerminal.Should().BeTrue();
    }

    [Fact]
    public void CompleteHandover_with_override_throws_if_reason_or_pharmacistId_is_empty()
    {
        var entry = CreateSampleEntry();
        entry.StartServicePenerimaan(BaseTime.AddMinutes(5));
        entry.TransitionToPenyiapan();
        entry.ConsolidateToSiapSerah(BaseTime.AddMinutes(20));
        entry.StartServiceSerah(BaseTime.AddMinutes(23));

        Action blankReason = () => entry.CompleteHandover(BaseTime.AddMinutes(25), true, "", "APT-001");
        blankReason.Should().Throw<ApotekDomainException>()
            .WithMessage("*Payment override reason is required*");

        Action blankPharmacist = () => entry.CompleteHandover(BaseTime.AddMinutes(25), true, "Emergency", "");
        blankPharmacist.Should().Throw<ApotekDomainException>()
            .WithMessage("*Pharmacist ID is required*");
    }

    [Fact]
    public void CompleteHandover_throws_when_in_ineligible_phase()
    {
        var entry = CreateSampleEntry();

        Action completePrematurely = () => entry.CompleteHandover(BaseTime.AddMinutes(10));
        completePrematurely.Should().Throw<ApotekDomainException>()
            .WithMessage("*Cannot complete handover when phase is 'TAKEN'*");
    }

    [Fact]
    public void Skip_transitions_to_DILEWATI_and_records_skip_metadata()
    {
        var entry = CreateSampleEntry();
        var skipTime = BaseTime.AddMinutes(12);

        entry.Skip(LoketTypeEnum.PenerimaanResep, skipTime);

        entry.ApotekPhase.Should().Be(ApotekPhaseEnum.DILEWATI);
        entry.LastSkippedAt.Should().Be(skipTime);
        entry.LastSkippedLoketType.Should().Be(LoketTypeEnum.PenerimaanResep);
    }

    [Fact]
    public void Skip_throws_on_invalid_loket_type()
    {
        var entry = CreateSampleEntry();

        Action invalidLoket = () => entry.Skip(LoketTypeEnum.None, BaseTime.AddMinutes(5));
        invalidLoket.Should().Throw<ArgumentException>()
            .WithMessage("*A valid loket type is required to skip.*");
    }

    [Fact]
    public void Withdraw_transitions_to_BATAL_and_records_withdrawal_metadata()
    {
        var entry = CreateSampleEntry();
        var withdrawAt = BaseTime.AddMinutes(15);

        entry.Withdraw("Pasien pulang paksa", "STAFF-01", withdrawAt);

        entry.ApotekPhase.Should().Be(ApotekPhaseEnum.BATAL);
        entry.WithdrawnAt.Should().Be(withdrawAt);
        entry.WithdrawalReason.Should().Be("Pasien pulang paksa");
        entry.WithdrawalUserId.Should().Be("STAFF-01");
        entry.IsTerminal.Should().BeTrue();
    }

    [Fact]
    public void Withdraw_throws_when_reason_or_userId_is_missing()
    {
        var entry = CreateSampleEntry();

        Action blankReason = () => entry.Withdraw("", "STAFF-01", BaseTime.AddMinutes(5));
        blankReason.Should().Throw<ArgumentException>().WithParameterName("reason");

        Action blankUser = () => entry.Withdraw("Cancelled", "", BaseTime.AddMinutes(5));
        blankUser.Should().Throw<ArgumentException>().WithParameterName("userId");
    }

    [Fact]
    public void MarkNoShow_transitions_to_NO_SHOW()
    {
        var entry = CreateSampleEntry();
        var noShowAt = BaseTime.AddMinutes(30);

        entry.MarkNoShow(noShowAt);

        entry.ApotekPhase.Should().Be(ApotekPhaseEnum.NO_SHOW);
        entry.WithdrawnAt.Should().Be(noShowAt);
        entry.IsTerminal.Should().BeTrue();
    }

    [Theory]
    [InlineData(ApotekPhaseEnum.SELESAI)]
    [InlineData(ApotekPhaseEnum.BATAL)]
    [InlineData(ApotekPhaseEnum.NO_SHOW)]
    public void Terminal_states_are_immutable_and_block_all_transitions(ApotekPhaseEnum terminalPhase)
    {
        var entry = ApotekQueueEntryModel.Rehydrate(
            "Q01", 1, "SP01", "A001", terminalPhase,
            1, BaseTime, 0, ApotekDate.Empty,
            BaseTime, BaseTime, BaseTime, BaseTime, BaseTime, ApotekDate.Empty,
            LoketTypeEnum.None, "Reason", "USER1", false, "", "");

        entry.IsTerminal.Should().BeTrue();

        var testTime = BaseTime.AddMinutes(40);

        Action callPenerimaan = () => entry.RecordCallPenerimaan(testTime);
        callPenerimaan.Should().Throw<ApotekDomainException>().WithMessage("*terminal state*");

        Action startPenerimaan = () => entry.StartServicePenerimaan(testTime);
        startPenerimaan.Should().Throw<ApotekDomainException>().WithMessage("*terminal state*");

        Action transitionPenyiapan = () => entry.TransitionToPenyiapan();
        transitionPenyiapan.Should().Throw<ApotekDomainException>().WithMessage("*terminal state*");

        Action consolidate = () => entry.ConsolidateToSiapSerah(testTime);
        consolidate.Should().Throw<ApotekDomainException>().WithMessage("*terminal state*");

        Action callSerah = () => entry.RecordCallSerah(testTime);
        callSerah.Should().Throw<ApotekDomainException>().WithMessage("*terminal state*");

        Action startSerah = () => entry.StartServiceSerah(testTime);
        startSerah.Should().Throw<ApotekDomainException>().WithMessage("*terminal state*");

        Action handover = () => entry.CompleteHandover(testTime);
        handover.Should().Throw<ApotekDomainException>().WithMessage("*terminal state*");

        Action skip = () => entry.Skip(LoketTypeEnum.PenerimaanResep, testTime);
        skip.Should().Throw<ApotekDomainException>().WithMessage("*terminal state*");

        Action withdraw = () => entry.Withdraw("Cancelled", "USER1", testTime);
        withdraw.Should().Throw<ApotekDomainException>().WithMessage("*terminal state*");

        Action noShow = () => entry.MarkNoShow(testTime);
        noShow.Should().Throw<ApotekDomainException>().WithMessage("*terminal state*");
    }

    [Fact]
    public void Rehydrate_restores_all_21_properties_accurately()
    {
        var now = new DateTime(2026, 10, 18, 9, 30, 0);
        var entry = ApotekQueueEntryModel.Rehydrate(
            antrianId: "Q01",
            noUrut: 12,
            servicePointId: "SP02",
            queueLabel: "B012",
            apotekPhase: ApotekPhaseEnum.SERAH_OBAT,
            callCountPenerimaan: 2,
            lastCalledPenerimaanAt: now.AddMinutes(-20),
            callCountSerah: 1,
            lastCalledSerahAt: now.AddMinutes(-5),
            takenAt: now.AddMinutes(-30),
            servedAt: now.AddMinutes(-18),
            preparedAt: now.AddMinutes(-10),
            doneAt: ApotekDate.Empty,
            withdrawnAt: ApotekDate.Empty,
            lastSkippedAt: now.AddMinutes(-25),
            lastSkippedLoketType: LoketTypeEnum.PenerimaanResep,
            withdrawalReason: "",
            withdrawalUserId: "",
            isPaymentOverridden: true,
            paymentOverrideReason: "Override by Doctor",
            paymentOverridePharmacistId: "PHARM-99");

        entry.AntrianId.Should().Be("Q01");
        entry.NoUrut.Should().Be(12);
        entry.ServicePointId.Should().Be("SP02");
        entry.QueueLabel.Should().Be("B012");
        entry.ApotekPhase.Should().Be(ApotekPhaseEnum.SERAH_OBAT);
        entry.CallCountPenerimaan.Should().Be(2);
        entry.LastCalledPenerimaanAt.Should().Be(now.AddMinutes(-20));
        entry.CallCountSerah.Should().Be(1);
        entry.LastCalledSerahAt.Should().Be(now.AddMinutes(-5));
        entry.TakenAt.Should().Be(now.AddMinutes(-30));
        entry.ServedAt.Should().Be(now.AddMinutes(-18));
        entry.PreparedAt.Should().Be(now.AddMinutes(-10));
        entry.DoneAt.Should().Be(ApotekDate.Empty);
        entry.WithdrawnAt.Should().Be(ApotekDate.Empty);
        entry.LastSkippedAt.Should().Be(now.AddMinutes(-25));
        entry.LastSkippedLoketType.Should().Be(LoketTypeEnum.PenerimaanResep);
        entry.WithdrawalReason.Should().BeEmpty();
        entry.WithdrawalUserId.Should().BeEmpty();
        entry.IsPaymentOverridden.Should().BeTrue();
        entry.PaymentOverrideReason.Should().Be("Override by Doctor");
        entry.PaymentOverridePharmacistId.Should().Be("PHARM-99");
    }

    [Fact]
    public void Key_matches_interface_identity()
    {
        var key = ApotekQueueEntryModel.Key("Q01", 5);
        key.AntrianId.Should().Be("Q01");
        key.NoUrut.Should().Be(5);
    }
}
