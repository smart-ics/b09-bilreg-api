using Bilreg.Application.ApotekContext.QueueFeature;
using Bilreg.Application.ApotekContext.QueueFeature.UseCases;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.ApotekContext.QueueFeature;
using Bilreg.Domain.ApotekContext.Shared;
using Bilreg.Domain.Shared.AuditLogFeature;
using Bilreg.Test.ApotekContext.Support;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.ApotekContext.QueueFeature;

public class PharmacyQueueLoket1CommandsTest
{
    private readonly Mock<IPharmacyQueueOperationRepo> _operations = new();
    private readonly Mock<IApotekQueueEntryRepo> _queueEntries = new();
    private readonly Mock<IPharmacyQueuePublisher> _publisher = new();
    private readonly Mock<IAuditRepo> _auditRepo = new();
    private readonly Mock<ITglJamProvider> _clock = new();
    private readonly AllowAllAuth _auth = new();

    private readonly DateTime _now = new(2026, 10, 18, 10, 0, 0);
    private readonly byte[] _tokenV1 = [1, 0, 0, 0, 0, 0, 0, 1];
    private readonly byte[] _tokenV2 = [2, 0, 0, 0, 0, 0, 0, 2];

    public PharmacyQueueLoket1CommandsTest()
    {
        _clock.Setup(x => x.Now).Returns(_now);
    }

    private PharmacyQueueCallPenerimaanCmdHandler CreateCallHandler()
        => new(_operations.Object, _queueEntries.Object, _publisher.Object, _auditRepo.Object, _clock.Object, _auth);

    private PharmacyQueueRecallPenerimaanCmdHandler CreateRecallHandler()
        => new(_operations.Object, _queueEntries.Object, _publisher.Object, _auditRepo.Object, _clock.Object, _auth);

    private PharmacyQueueSkipPenerimaanCmdHandler CreateSkipHandler()
        => new(_operations.Object, _queueEntries.Object, _publisher.Object, _auditRepo.Object, _clock.Object, _auth);

    private PharmacyQueueStartPenerimaanCmdHandler CreateStartHandler()
        => new(_operations.Object, _queueEntries.Object, _publisher.Object, _auditRepo.Object, _clock.Object, _auth);

    private PharmacyQueueReleasePenerimaanCmdHandler CreateReleaseHandler()
        => new(_operations.Object, _queueEntries.Object, _publisher.Object, _auditRepo.Object, _clock.Object, _auth);

    private static PharmacyLoketSession CreateIdleLoket(string loketKey = "APT-RCV-01", string depoId = "DEP-RJ")
        => new(
            LoketKey: loketKey,
            LoketType: LoketTypeEnum.PenerimaanResep,
            DisplayName: "Loket Penerimaan 1",
            DepoFarmasiId: depoId,
            AntrianId: "",
            NoUrut: 0,
            ClaimState: LoketClaimStateEnum.Idle,
            IsActive: false,
            AnnouncementVersion: 0,
            CalledAt: ApotekDate.Empty,
            ServiceStartedAt: ApotekDate.Empty,
            ReleasedAt: ApotekDate.Empty,
            RowVersion: [1, 0, 0, 0, 0, 0, 0, 1]);

    private static PharmacyLoketSession CreateCalledLoket(
        string loketKey = "APT-RCV-01",
        string antrianId = "ANT-001",
        int noUrut = 5,
        string depoId = "DEP-RJ",
        long announcementVersion = 1)
        => new(
            LoketKey: loketKey,
            LoketType: LoketTypeEnum.PenerimaanResep,
            DisplayName: "Loket Penerimaan 1",
            DepoFarmasiId: depoId,
            AntrianId: antrianId,
            NoUrut: noUrut,
            ClaimState: LoketClaimStateEnum.Called,
            IsActive: true,
            AnnouncementVersion: announcementVersion,
            CalledAt: new DateTime(2026, 10, 18, 9, 55, 0),
            ServiceStartedAt: ApotekDate.Empty,
            ReleasedAt: ApotekDate.Empty,
            RowVersion: [1, 0, 0, 0, 0, 0, 0, 1]);

    private static ApotekQueueEntryModel CreateQueueEntry(
        string antrianId = "ANT-001",
        int noUrut = 5,
        string prefix = "A",
        string spId = "SP-01",
        ApotekPhaseEnum phase = ApotekPhaseEnum.TAKEN)
    {
        var entry = ApotekQueueEntryModel.Create(antrianId, noUrut, spId, prefix, new DateTime(2026, 10, 18, 9, 30, 0));
        if (phase == ApotekPhaseEnum.PENERIMAAN)
            entry.StartServicePenerimaan(new DateTime(2026, 10, 18, 9, 35, 0));
        else if (phase == ApotekPhaseEnum.DILEWATI)
            entry.Skip(LoketTypeEnum.PenerimaanResep, new DateTime(2026, 10, 18, 9, 40, 0));
        else if (phase == ApotekPhaseEnum.SELESAI)
        {
            entry.TransitionToPenyiapan();
            entry.ConsolidateToSiapSerah(new DateTime(2026, 10, 18, 9, 45, 0));
            entry.CompleteHandover(new DateTime(2026, 10, 18, 9, 50, 0), false, "", "");
        }
        else if (phase == ApotekPhaseEnum.BATAL)
        {
            entry.Withdraw("Cancelled", "officer-1", new DateTime(2026, 10, 18, 9, 50, 0));
        }

        return entry;
    }

    #region Call Penerimaan Tests

    [Fact]
    public async Task CallPenerimaan_Throws_When_LoketKey_NullOrEmpty()
    {
        var sut = CreateCallHandler();

        var act = () => sut.Handle(new PharmacyQueueCallPenerimaanCmd(""), CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CallPenerimaan_Throws_When_Loket_NotFound()
    {
        _operations.Setup(x => x.GetLoket("APT-RCV-99")).Returns(MayBe<PharmacyLoketSession>.None);

        var sut = CreateCallHandler();

        var act = () => sut.Handle(new PharmacyQueueCallPenerimaanCmd("APT-RCV-99"), CancellationToken.None);
        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("*APT-RCV-99*");
    }

    [Fact]
    public async Task CallPenerimaan_Throws_When_Loket_IsNotLoketPenerimaan()
    {
        var serahLoket = new PharmacyLoketSession(
            "APT-SRH-01", LoketTypeEnum.SerahObat, "Loket Serah 1", "DEP-RJ",
            "", 0, LoketClaimStateEnum.Idle, false, 0,
            ApotekDate.Empty, ApotekDate.Empty, ApotekDate.Empty, _tokenV1);

        _operations.Setup(x => x.GetLoket("APT-SRH-01")).Returns(MayBe.From(serahLoket));

        var sut = CreateCallHandler();

        var act = () => sut.Handle(new PharmacyQueueCallPenerimaanCmd("APT-SRH-01"), CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Loket Penerimaan*");
    }

    [Fact]
    public async Task CallPenerimaan_Throws_When_Loket_IsCurrentlyBusy()
    {
        var busyLoket = CreateCalledLoket("APT-RCV-01", "ANT-001", 3);
        _operations.Setup(x => x.GetLoket("APT-RCV-01")).Returns(MayBe.From(busyLoket));

        var sut = CreateCallHandler();

        var act = () => sut.Handle(new PharmacyQueueCallPenerimaanCmd("APT-RCV-01"), CancellationToken.None);
        await act.Should().ThrowAsync<ApotekConcurrencyException>().WithMessage("*busy*");
    }

    [Fact]
    public async Task CallPenerimaan_NextTicket_CallsNextWaitingTicket_PublishesAndAudits()
    {
        // Arrange
        var idleLoket = CreateIdleLoket("APT-RCV-01", "DEP-RJ");
        _operations.Setup(x => x.GetLoket("APT-RCV-01")).Returns(MayBe.From(idleLoket));

        var ticket2 = CreateQueueEntry("ANT-001", 2, "A", "SP-01", ApotekPhaseEnum.TAKEN);
        var ticket5 = CreateQueueEntry("ANT-001", 5, "A", "SP-01", ApotekPhaseEnum.TAKEN);
        _queueEntries.Setup(x => x.ListByPhase("DEP-RJ", ApotekPhaseEnum.TAKEN))
            .Returns([ticket5, ticket2]); // unsorted, handler should order by NoUrut

        _operations.Setup(x => x.TryCallLoket("APT-RCV-01", "ANT-001", 2, _tokenV1, "officer-1", _now))
            .Returns(true);

        var updatedLoket = new PharmacyLoketSession(
            "APT-RCV-01", LoketTypeEnum.PenerimaanResep, "Loket Penerimaan 1", "DEP-RJ",
            "ANT-001", 2, LoketClaimStateEnum.Called, true, 1,
            _now, ApotekDate.Empty, ApotekDate.Empty, _tokenV2);
        _operations.SetupSequence(x => x.GetLoket("APT-RCV-01"))
            .Returns(MayBe.From(idleLoket))
            .Returns(MayBe.From(updatedLoket));

        var sut = CreateCallHandler();

        // Act
        var result = await sut.Handle(
            new PharmacyQueueCallPenerimaanCmd("APT-RCV-01", null, null, null, "officer-1"),
            CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.AntrianId.Should().Be("ANT-001");
        result.NoUrut.Should().Be(2);
        result.QueueLabel.Should().Be("A002");
        result.AnnouncementVersion.Should().Be(1);
        result.ClaimState.Should().Be(LoketClaimStateEnum.Called);
        result.RowVersion.Should().BeEquivalentTo(_tokenV2);

        _auditRepo.Verify(x => x.SaveChanges(It.Is<AuditLog>(a =>
            a.ActionType == "CALL_PENERIMAAN" &&
            a.EntityName == "PharmacyQueueLoketClaim" &&
            a.EntityId == "APT-RCV-01:ANT-001:2" &&
            a.UserId == "officer-1")), Times.Once);

        _publisher.Verify(x => x.PublishDisplayRefresh("APT-RCV-01", 1, It.IsAny<CancellationToken>()), Times.Once);
        _publisher.Verify(x => x.PublishWorklistRefresh("DEP-RJ", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CallPenerimaan_NextTicket_Throws_When_NoWaitingTickets()
    {
        var idleLoket = CreateIdleLoket("APT-RCV-01", "DEP-RJ");
        _operations.Setup(x => x.GetLoket("APT-RCV-01")).Returns(MayBe.From(idleLoket));
        _queueEntries.Setup(x => x.ListByPhase("DEP-RJ", ApotekPhaseEnum.TAKEN)).Returns([]);

        var sut = CreateCallHandler();

        var act = () => sut.Handle(
            new PharmacyQueueCallPenerimaanCmd("APT-RCV-01", null, null, null, "officer-1"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No tickets with phase TAKEN*");
    }

    [Fact]
    public async Task CallPenerimaan_SpecificTicket_CallsSpecificTicketSuccessfully()
    {
        var idleLoket = CreateIdleLoket("APT-RCV-01", "DEP-RJ");
        _operations.Setup(x => x.GetLoket("APT-RCV-01")).Returns(MayBe.From(idleLoket));

        var specificTicket = CreateQueueEntry("ANT-001", 7, "B", "SP-02", ApotekPhaseEnum.DILEWATI);
        _queueEntries.Setup(x => x.Get("ANT-001", 7)).Returns(MayBe.From(specificTicket));

        _operations.Setup(x => x.TryCallLoket("APT-RCV-01", "ANT-001", 7, _tokenV1, "officer-1", _now))
            .Returns(true);

        var updatedLoket = new PharmacyLoketSession(
            "APT-RCV-01", LoketTypeEnum.PenerimaanResep, "Loket Penerimaan 1", "DEP-RJ",
            "ANT-001", 7, LoketClaimStateEnum.Called, true, 5,
            _now, ApotekDate.Empty, ApotekDate.Empty, _tokenV2);
        _operations.SetupSequence(x => x.GetLoket("APT-RCV-01"))
            .Returns(MayBe.From(idleLoket))
            .Returns(MayBe.From(updatedLoket));

        var sut = CreateCallHandler();

        var result = await sut.Handle(
            new PharmacyQueueCallPenerimaanCmd("APT-RCV-01", "ANT-001", 7, _tokenV1, "officer-1"),
            CancellationToken.None);

        result.Success.Should().BeTrue();
        result.AntrianId.Should().Be("ANT-001");
        result.NoUrut.Should().Be(7);
        result.QueueLabel.Should().Be("B007");
    }

    [Fact]
    public async Task CallPenerimaan_SpecificTicket_Throws_When_TicketPhase_NotCallable()
    {
        var idleLoket = CreateIdleLoket("APT-RCV-01", "DEP-RJ");
        _operations.Setup(x => x.GetLoket("APT-RCV-01")).Returns(MayBe.From(idleLoket));

        var doneTicket = CreateQueueEntry("ANT-001", 3, "A", "SP-01", ApotekPhaseEnum.SELESAI);
        _queueEntries.Setup(x => x.Get("ANT-001", 3)).Returns(MayBe.From(doneTicket));

        var sut = CreateCallHandler();

        var act = () => sut.Handle(
            new PharmacyQueueCallPenerimaanCmd("APT-RCV-01", "ANT-001", 3, _tokenV1, "officer-1"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*cannot be called*");
    }

    [Fact]
    public async Task CallPenerimaan_Throws_ApotekConcurrencyException_When_TryCallLoket_Fails()
    {
        var idleLoket = CreateIdleLoket("APT-RCV-01", "DEP-RJ");
        _operations.Setup(x => x.GetLoket("APT-RCV-01")).Returns(MayBe.From(idleLoket));

        var ticket = CreateQueueEntry("ANT-001", 2, "A", "SP-01", ApotekPhaseEnum.TAKEN);
        _queueEntries.Setup(x => x.ListByPhase("DEP-RJ", ApotekPhaseEnum.TAKEN)).Returns([ticket]);

        _operations.Setup(x => x.TryCallLoket("APT-RCV-01", "ANT-001", 2, _tokenV1, "officer-1", _now))
            .Returns(false);

        var sut = CreateCallHandler();

        var act = () => sut.Handle(
            new PharmacyQueueCallPenerimaanCmd("APT-RCV-01", null, null, null, "officer-1"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ApotekConcurrencyException>()
            .WithMessage("*Failed to claim call*");

        _publisher.Verify(x => x.PublishDisplayRefresh(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
        _publisher.Verify(x => x.PublishWorklistRefresh(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Recall Penerimaan Tests

    [Fact]
    public async Task RecallPenerimaan_Throws_When_RowVersion_Empty()
    {
        var sut = CreateRecallHandler();

        var act = () => sut.Handle(new PharmacyQueueRecallPenerimaanCmd("APT-RCV-01", []), CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task RecallPenerimaan_Throws_When_Loket_NotActiveCalled()
    {
        var idleLoket = CreateIdleLoket("APT-RCV-01", "DEP-RJ");
        _operations.Setup(x => x.GetLoket("APT-RCV-01")).Returns(MayBe.From(idleLoket));

        var sut = CreateRecallHandler();

        var act = () => sut.Handle(new PharmacyQueueRecallPenerimaanCmd("APT-RCV-01", _tokenV1), CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*does not have an active called ticket*");
    }

    [Fact]
    public async Task RecallPenerimaan_IncrementsCounter_PublishesAndAudits()
    {
        var calledLoket = CreateCalledLoket("APT-RCV-01", "ANT-001", 5, "DEP-RJ", 1);
        _operations.SetupSequence(x => x.GetLoket("APT-RCV-01"))
            .Returns(MayBe.From(calledLoket))
            .Returns(MayBe.From(calledLoket with { AnnouncementVersion = 2, RowVersion = _tokenV2 }));

        var ticket = CreateQueueEntry("ANT-001", 5, "A");
        _queueEntries.Setup(x => x.Get("ANT-001", 5)).Returns(MayBe.From(ticket));

        _operations.Setup(x => x.TryRecallLoket("APT-RCV-01", _tokenV1, "officer-1", _now))
            .Returns(true);

        var sut = CreateRecallHandler();

        var result = await sut.Handle(
            new PharmacyQueueRecallPenerimaanCmd("APT-RCV-01", _tokenV1, "officer-1"),
            CancellationToken.None);

        result.Success.Should().BeTrue();
        result.AnnouncementVersion.Should().Be(2);
        result.QueueLabel.Should().Be("A005");

        _auditRepo.Verify(x => x.SaveChanges(It.Is<AuditLog>(a =>
            a.ActionType == "RECALL_PENERIMAAN" &&
            a.EntityName == "PharmacyQueueLoketClaim" &&
            a.EntityId == "APT-RCV-01:ANT-001:5")), Times.Once);

        _publisher.Verify(x => x.PublishDisplayRefresh("APT-RCV-01", 2, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RecallPenerimaan_Throws_ApotekConcurrencyException_When_TryRecall_Fails()
    {
        var calledLoket = CreateCalledLoket("APT-RCV-01", "ANT-001", 5);
        _operations.Setup(x => x.GetLoket("APT-RCV-01")).Returns(MayBe.From(calledLoket));

        _operations.Setup(x => x.TryRecallLoket("APT-RCV-01", _tokenV1, "officer-1", _now))
            .Returns(false);

        var sut = CreateRecallHandler();

        var act = () => sut.Handle(
            new PharmacyQueueRecallPenerimaanCmd("APT-RCV-01", _tokenV1, "officer-1"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ApotekConcurrencyException>()
            .WithMessage("*Failed to recall ticket*");
    }

    #endregion

    #region Skip Penerimaan Tests

    [Fact]
    public async Task SkipPenerimaan_Throws_When_Reason_Empty()
    {
        var sut = CreateSkipHandler();

        var act = () => sut.Handle(new PharmacyQueueSkipPenerimaanCmd("APT-RCV-01", "", _tokenV1), CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task SkipPenerimaan_Throws_When_Loket_NotActive()
    {
        var idleLoket = CreateIdleLoket("APT-RCV-01", "DEP-RJ");
        _operations.Setup(x => x.GetLoket("APT-RCV-01")).Returns(MayBe.From(idleLoket));

        var sut = CreateSkipHandler();

        var act = () => sut.Handle(new PharmacyQueueSkipPenerimaanCmd("APT-RCV-01", "NoResponse", _tokenV1), CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*no active ticket to skip*");
    }

    [Fact]
    public async Task SkipPenerimaan_TransitionsTicketToSkipped_ReleasesLoket_PublishesAndAudits()
    {
        var calledLoket = CreateCalledLoket("APT-RCV-01", "ANT-001", 5, "DEP-RJ");
        var releasedLoket = CreateIdleLoket("APT-RCV-01", "DEP-RJ") with { RowVersion = _tokenV2 };

        _operations.SetupSequence(x => x.GetLoket("APT-RCV-01"))
            .Returns(MayBe.From(calledLoket))
            .Returns(MayBe.From(releasedLoket));

        var ticket = CreateQueueEntry("ANT-001", 5, "A");
        _queueEntries.Setup(x => x.Get("ANT-001", 5)).Returns(MayBe.From(ticket));

        _operations.Setup(x => x.TrySkipLoket("APT-RCV-01", "Tidak Merespons", _tokenV1, "officer-1", _now))
            .Returns(true);

        var sut = CreateSkipHandler();

        var result = await sut.Handle(
            new PharmacyQueueSkipPenerimaanCmd("APT-RCV-01", "Tidak Merespons", _tokenV1, "officer-1"),
            CancellationToken.None);

        result.Success.Should().BeTrue();
        result.ClaimState.Should().Be(LoketClaimStateEnum.Idle);
        result.QueueLabel.Should().Be("A005");

        _auditRepo.Verify(x => x.SaveChanges(It.Is<AuditLog>(a =>
            a.ActionType == "SKIP_PENERIMAAN" &&
            a.EntityName == "PharmacyQueueLoketClaim" &&
            a.EntityId == "APT-RCV-01:ANT-001:5" &&
            a.Reason == "Tidak Merespons")), Times.Once);

        _publisher.Verify(x => x.PublishWorklistRefresh("DEP-RJ", It.IsAny<CancellationToken>()), Times.Once);
        _publisher.Verify(x => x.PublishDisplayRefresh("APT-RCV-01", 0, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SkipPenerimaan_Throws_ApotekConcurrencyException_When_TrySkip_Fails()
    {
        var calledLoket = CreateCalledLoket("APT-RCV-01", "ANT-001", 5);
        _operations.Setup(x => x.GetLoket("APT-RCV-01")).Returns(MayBe.From(calledLoket));

        _operations.Setup(x => x.TrySkipLoket("APT-RCV-01", "Pasien Pergi", _tokenV1, "officer-1", _now))
            .Returns(false);

        var sut = CreateSkipHandler();

        var act = () => sut.Handle(
            new PharmacyQueueSkipPenerimaanCmd("APT-RCV-01", "Pasien Pergi", _tokenV1, "officer-1"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ApotekConcurrencyException>()
            .WithMessage("*Failed to skip ticket*");
    }

    #endregion

    #region Start Service Penerimaan Tests

    [Fact]
    public async Task StartPenerimaan_Throws_When_Loket_NotCalled()
    {
        var inServiceLoket = CreateCalledLoket("APT-RCV-01", "ANT-001", 5) with { ClaimState = LoketClaimStateEnum.InService };
        _operations.Setup(x => x.GetLoket("APT-RCV-01")).Returns(MayBe.From(inServiceLoket));

        var sut = CreateStartHandler();

        var act = () => sut.Handle(new PharmacyQueueStartPenerimaanCmd("APT-RCV-01", _tokenV1), CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*must be in Called state*");
    }

    [Fact]
    public async Task StartPenerimaan_TransitionsLoketToInService_PublishesAndAudits()
    {
        var calledLoket = CreateCalledLoket("APT-RCV-01", "ANT-001", 5, "DEP-RJ");
        var inServiceLoket = calledLoket with { ClaimState = LoketClaimStateEnum.InService, RowVersion = _tokenV2 };

        _operations.SetupSequence(x => x.GetLoket("APT-RCV-01"))
            .Returns(MayBe.From(calledLoket))
            .Returns(MayBe.From(inServiceLoket));

        var ticket = CreateQueueEntry("ANT-001", 5, "A");
        _queueEntries.Setup(x => x.Get("ANT-001", 5)).Returns(MayBe.From(ticket));

        _operations.Setup(x => x.TryStartServiceLoket("APT-RCV-01", _tokenV1, "officer-1", _now))
            .Returns(true);

        var sut = CreateStartHandler();

        var result = await sut.Handle(
            new PharmacyQueueStartPenerimaanCmd("APT-RCV-01", _tokenV1, "officer-1"),
            CancellationToken.None);

        result.Success.Should().BeTrue();
        result.ClaimState.Should().Be(LoketClaimStateEnum.InService);
        result.QueueLabel.Should().Be("A005");

        _auditRepo.Verify(x => x.SaveChanges(It.Is<AuditLog>(a =>
            a.ActionType == "START_PENERIMAAN" &&
            a.EntityName == "PharmacyQueueLoketClaim" &&
            a.EntityId == "APT-RCV-01:ANT-001:5")), Times.Once);

        _publisher.Verify(x => x.PublishWorklistRefresh("DEP-RJ", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StartPenerimaan_Throws_ApotekConcurrencyException_When_TryStartService_Fails()
    {
        var calledLoket = CreateCalledLoket("APT-RCV-01", "ANT-001", 5);
        _operations.Setup(x => x.GetLoket("APT-RCV-01")).Returns(MayBe.From(calledLoket));

        _operations.Setup(x => x.TryStartServiceLoket("APT-RCV-01", _tokenV1, "officer-1", _now))
            .Returns(false);

        var sut = CreateStartHandler();

        var act = () => sut.Handle(
            new PharmacyQueueStartPenerimaanCmd("APT-RCV-01", _tokenV1, "officer-1"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ApotekConcurrencyException>()
            .WithMessage("*Failed to start service*");
    }

    #endregion

    #region Release Penerimaan Tests

    [Fact]
    public async Task ReleasePenerimaan_Throws_When_Loket_AlreadyIdle()
    {
        var idleLoket = CreateIdleLoket("APT-RCV-01", "DEP-RJ");
        _operations.Setup(x => x.GetLoket("APT-RCV-01")).Returns(MayBe.From(idleLoket));

        var sut = CreateReleaseHandler();

        var act = () => sut.Handle(new PharmacyQueueReleasePenerimaanCmd("APT-RCV-01", _tokenV1), CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already idle*");
    }

    [Fact]
    public async Task ReleasePenerimaan_ReleasesClaim_PublishesAndAudits()
    {
        var activeLoket = CreateCalledLoket("APT-RCV-01", "ANT-001", 5, "DEP-RJ") with { ClaimState = LoketClaimStateEnum.InService };
        var idleLoket = CreateIdleLoket("APT-RCV-01", "DEP-RJ") with { RowVersion = _tokenV2 };

        _operations.SetupSequence(x => x.GetLoket("APT-RCV-01"))
            .Returns(MayBe.From(activeLoket))
            .Returns(MayBe.From(idleLoket));

        var ticket = CreateQueueEntry("ANT-001", 5, "A");
        _queueEntries.Setup(x => x.Get("ANT-001", 5)).Returns(MayBe.From(ticket));

        _operations.Setup(x => x.TryReleaseLoket("APT-RCV-01", _tokenV1, "officer-1", _now))
            .Returns(true);

        var sut = CreateReleaseHandler();

        var result = await sut.Handle(
            new PharmacyQueueReleasePenerimaanCmd("APT-RCV-01", _tokenV1, "officer-1"),
            CancellationToken.None);

        result.Success.Should().BeTrue();
        result.ClaimState.Should().Be(LoketClaimStateEnum.Idle);
        result.QueueLabel.Should().Be("A005");

        _auditRepo.Verify(x => x.SaveChanges(It.Is<AuditLog>(a =>
            a.ActionType == "RELEASE_PENERIMAAN" &&
            a.EntityName == "PharmacyQueueLoketClaim" &&
            a.EntityId == "APT-RCV-01:ANT-001:5")), Times.Once);

        _publisher.Verify(x => x.PublishWorklistRefresh("DEP-RJ", It.IsAny<CancellationToken>()), Times.Once);
        _publisher.Verify(x => x.PublishDisplayRefresh("APT-RCV-01", 0, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReleasePenerimaan_Throws_ApotekConcurrencyException_When_TryRelease_Fails()
    {
        var activeLoket = CreateCalledLoket("APT-RCV-01", "ANT-001", 5);
        _operations.Setup(x => x.GetLoket("APT-RCV-01")).Returns(MayBe.From(activeLoket));

        _operations.Setup(x => x.TryReleaseLoket("APT-RCV-01", _tokenV1, "officer-1", _now))
            .Returns(false);

        var sut = CreateReleaseHandler();

        var act = () => sut.Handle(
            new PharmacyQueueReleasePenerimaanCmd("APT-RCV-01", _tokenV1, "officer-1"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ApotekConcurrencyException>()
            .WithMessage("*Failed to release loket*");
    }

    #endregion
}
