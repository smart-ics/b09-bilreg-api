using System.Reflection;
using Bilreg.Application.ApotekContext.QueueFeature;
using Bilreg.Application.ApotekContext.QueueFeature.UseCases;
using Bilreg.Application.ApotekContext.Shared;
using Bilreg.Domain.ApotekContext.QueueFeature;
using Bilreg.Domain.ApotekContext.Shared;
using Bilreg.Domain.Shared.Helpers;
using Bilreg.Test.ApotekContext.Support;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.ApotekContext.QueueFeature;

public class PharmacyQueueSerahCommandsTest
{
    private readonly Mock<IPharmacyQueueOperationRepo> _operationRepo = new();
    private readonly Mock<IApotekQueueEntryRepo> _queueEntryRepo = new();
    private readonly Mock<IPharmacyQueuePublisher> _publisher = new();
    private readonly Mock<ITglJamProvider> _clock = new();
    private readonly AllowAllAuth _auth = new();

    private readonly DateTime _now = new(2026, 10, 18, 10, 30, 0);
    private static readonly byte[] ValidRowVersion = [1, 2, 3, 4, 5, 6, 7, 8];

    public PharmacyQueueSerahCommandsTest()
    {
        _clock.Setup(x => x.Now).Returns(_now);
    }

    private static PharmacyLoketSession CreateLoketSession(
        string loketKey,
        LoketTypeEnum loketType = LoketTypeEnum.SerahObat,
        string depoId = "DEP-01",
        LoketClaimStateEnum claimState = LoketClaimStateEnum.Idle,
        string antrianId = "",
        int noUrut = 0,
        long announcementVersion = 1)
    {
        return new PharmacyLoketSession(
            LoketKey: loketKey,
            LoketType: loketType,
            DisplayName: "Loket Serah Obat 1",
            DepoFarmasiId: depoId,
            AntrianId: antrianId,
            NoUrut: noUrut,
            ClaimState: claimState,
            IsActive: claimState != LoketClaimStateEnum.Idle,
            AnnouncementVersion: announcementVersion,
            CalledAt: claimState != LoketClaimStateEnum.Idle ? DateTime.Now : ApotekDate.Empty,
            ServiceStartedAt: claimState == LoketClaimStateEnum.InService ? DateTime.Now : ApotekDate.Empty,
            ReleasedAt: ApotekDate.Empty,
            RowVersion: ValidRowVersion);
    }

    #region CallSerah Tests

    [Fact]
    public async Task CallSerah_WhenNextWaiting_CallsFirstSiapSerahTicket()
    {
        const string loketKey = "APT-SRH-01";
        var loket = CreateLoketSession(loketKey);
        _operationRepo.Setup(x => x.GetLoket(loketKey)).Returns(MayBe.From(loket));

        var entry1 = ApotekQueueEntryModel.Create("Q-01", 1, "SP-01", "A");
        entry1.TransitionToPenyiapan();
        entry1.ConsolidateToSiapSerah(_now.AddMinutes(-10));

        var entry2 = ApotekQueueEntryModel.Create("Q-01", 2, "SP-01", "A");
        entry2.TransitionToPenyiapan();
        entry2.ConsolidateToSiapSerah(_now.AddMinutes(-5));

        _queueEntryRepo.Setup(x => x.ListByPhase("DEP-01", ApotekPhaseEnum.SIAP_SERAH))
            .Returns([entry1, entry2]);

        _operationRepo.Setup(x => x.TryCallLoket(loketKey, "Q-01", 1, ValidRowVersion, "user1", _now))
            .Returns(true);

        var updatedLoket = CreateLoketSession(loketKey, claimState: LoketClaimStateEnum.Called, antrianId: "Q-01", noUrut: 1, announcementVersion: 2);
        _operationRepo.SetupSequence(x => x.GetLoket(loketKey))
            .Returns(MayBe.From(loket))
            .Returns(MayBe.From(updatedLoket));

        _queueEntryRepo.Setup(x => x.Get("Q-01", 1)).Returns(MayBe.From(entry1));

        var sut = new PharmacyQueueCallSerahHandler(_operationRepo.Object, _queueEntryRepo.Object, _publisher.Object, _clock.Object, _auth);

        var res = await sut.Handle(new PharmacyQueueCallSerahCmd(loketKey, UserId: "user1"), CancellationToken.None);

        res.Success.Should().BeTrue();
        res.AntrianId.Should().Be("Q-01");
        res.NoUrut.Should().Be(1);
        res.QueueLabel.Should().Be("A001");
        res.ClaimState.Should().Be(LoketClaimStateEnum.Called);
        _publisher.Verify(x => x.PublishDisplayRefresh(loketKey, 2, It.IsAny<CancellationToken>()), Times.Once);
        _publisher.Verify(x => x.PublishWorklistRefresh("DEP-01", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CallSerah_WhenSpecificTicketSupplied_CallsSpecifiedTicket()
    {
        const string loketKey = "APT-SRH-01";
        var loket = CreateLoketSession(loketKey);
        var updatedLoket = CreateLoketSession(loketKey, claimState: LoketClaimStateEnum.Called, antrianId: "Q-01", noUrut: 7, announcementVersion: 2);
        _operationRepo.SetupSequence(x => x.GetLoket(loketKey))
            .Returns(MayBe.From(loket))
            .Returns(MayBe.From(updatedLoket));

        var entry = ApotekQueueEntryModel.Create("Q-01", 7, "SP-01", "B");
        entry.TransitionToPenyiapan();
        entry.ConsolidateToSiapSerah(_now);

        _queueEntryRepo.Setup(x => x.Get("Q-01", 7)).Returns(MayBe.From(entry));

        _operationRepo.Setup(x => x.TryCallLoket(loketKey, "Q-01", 7, ValidRowVersion, "user1", _now))
            .Returns(true);

        var sut = new PharmacyQueueCallSerahHandler(_operationRepo.Object, _queueEntryRepo.Object, _publisher.Object, _clock.Object, _auth);

        var res = await sut.Handle(new PharmacyQueueCallSerahCmd(loketKey, AntrianId: "Q-01", NoUrut: 7, UserId: "user1"), CancellationToken.None);

        res.Success.Should().BeTrue();
        res.QueueLabel.Should().Be("B007");
    }

    [Fact]
    public async Task CallSerah_Throws_WhenLoketNotSerahObat()
    {
        const string loketKey = "APT-RCV-01";
        var loket = CreateLoketSession(loketKey, loketType: LoketTypeEnum.PenerimaanResep);
        _operationRepo.Setup(x => x.GetLoket(loketKey)).Returns(MayBe.From(loket));

        var sut = new PharmacyQueueCallSerahHandler(_operationRepo.Object, _queueEntryRepo.Object, _publisher.Object, _clock.Object, _auth);
        var act = () => sut.Handle(new PharmacyQueueCallSerahCmd(loketKey), CancellationToken.None);

        await act.Should().ThrowAsync<ApotekDomainException>()
            .WithMessage("*not configured for Serah Obat*");
    }

    [Fact]
    public async Task CallSerah_Throws_WhenTicketPhaseNotAllowed()
    {
        const string loketKey = "APT-SRH-01";
        var loket = CreateLoketSession(loketKey);
        _operationRepo.Setup(x => x.GetLoket(loketKey)).Returns(MayBe.From(loket));

        // Ticket still in PENYIAPAN (not yet SIAP_SERAH)
        var entry = ApotekQueueEntryModel.Create("Q-01", 3, "SP-01", "A");
        entry.TransitionToPenyiapan();
        _queueEntryRepo.Setup(x => x.Get("Q-01", 3)).Returns(MayBe.From(entry));

        var sut = new PharmacyQueueCallSerahHandler(_operationRepo.Object, _queueEntryRepo.Object, _publisher.Object, _clock.Object, _auth);
        var act = () => sut.Handle(new PharmacyQueueCallSerahCmd(loketKey, AntrianId: "Q-01", NoUrut: 3), CancellationToken.None);

        await act.Should().ThrowAsync<ApotekDomainException>()
            .WithMessage("*Cannot call at Loket Serah when phase is 'PENYIAPAN'*");
    }

    [Fact]
    public async Task CallSerah_Throws_WhenNoTicketsWaitingInSiapSerah()
    {
        const string loketKey = "APT-SRH-01";
        var loket = CreateLoketSession(loketKey);
        _operationRepo.Setup(x => x.GetLoket(loketKey)).Returns(MayBe.From(loket));

        _queueEntryRepo.Setup(x => x.ListByPhase("DEP-01", ApotekPhaseEnum.SIAP_SERAH))
            .Returns([]);

        var sut = new PharmacyQueueCallSerahHandler(_operationRepo.Object, _queueEntryRepo.Object, _publisher.Object, _clock.Object, _auth);
        var act = () => sut.Handle(new PharmacyQueueCallSerahCmd(loketKey), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Tidak ada antrean berstatus SIAP_SERAH*");
    }

    [Fact]
    public async Task CallSerah_ThrowsConcurrency_WhenTryCallFails()
    {
        const string loketKey = "APT-SRH-01";
        var loket = CreateLoketSession(loketKey);
        _operationRepo.Setup(x => x.GetLoket(loketKey)).Returns(MayBe.From(loket));

        var entry = ApotekQueueEntryModel.Create("Q-01", 1, "SP-01", "A");
        entry.TransitionToPenyiapan();
        entry.ConsolidateToSiapSerah(_now);
        _queueEntryRepo.Setup(x => x.ListByPhase("DEP-01", ApotekPhaseEnum.SIAP_SERAH)).Returns([entry]);

        // Race condition: another officer claimed it first
        _operationRepo.Setup(x => x.TryCallLoket(loketKey, "Q-01", 1, ValidRowVersion, "user1", _now))
            .Returns(false);

        var sut = new PharmacyQueueCallSerahHandler(_operationRepo.Object, _queueEntryRepo.Object, _publisher.Object, _clock.Object, _auth);
        var act = () => sut.Handle(new PharmacyQueueCallSerahCmd(loketKey, UserId: "user1"), CancellationToken.None);

        await act.Should().ThrowAsync<ApotekConcurrencyException>();
    }

    #endregion

    #region RecallSerah Tests

    [Fact]
    public async Task RecallSerah_Succeeds_IncrementsAnnouncementVersion()
    {
        const string loketKey = "APT-SRH-01";
        var loket = CreateLoketSession(loketKey, claimState: LoketClaimStateEnum.Called, antrianId: "Q-01", noUrut: 5, announcementVersion: 1);
        var updatedLoket = CreateLoketSession(loketKey, claimState: LoketClaimStateEnum.Called, antrianId: "Q-01", noUrut: 5, announcementVersion: 2);

        _operationRepo.SetupSequence(x => x.GetLoket(loketKey))
            .Returns(MayBe.From(loket))
            .Returns(MayBe.From(updatedLoket));

        var entry = ApotekQueueEntryModel.Create("Q-01", 5, "SP-01", "A");
        _queueEntryRepo.Setup(x => x.Get("Q-01", 5)).Returns(MayBe.From(entry));

        _operationRepo.Setup(x => x.TryRecallLoket(loketKey, ValidRowVersion, "user1", _now))
            .Returns(true);

        var sut = new PharmacyQueueRecallSerahHandler(_operationRepo.Object, _queueEntryRepo.Object, _publisher.Object, _clock.Object, _auth);
        var res = await sut.Handle(new PharmacyQueueRecallSerahCmd(loketKey, ValidRowVersion, "user1"), CancellationToken.None);

        res.Success.Should().BeTrue();
        res.AnnouncementVersion.Should().Be(2);
        _publisher.Verify(x => x.PublishDisplayRefresh(loketKey, 2, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RecallSerah_Throws_WhenLoketIdle()
    {
        const string loketKey = "APT-SRH-01";
        var loket = CreateLoketSession(loketKey, claimState: LoketClaimStateEnum.Idle);
        _operationRepo.Setup(x => x.GetLoket(loketKey)).Returns(MayBe.From(loket));

        var sut = new PharmacyQueueRecallSerahHandler(_operationRepo.Object, _queueEntryRepo.Object, _publisher.Object, _clock.Object, _auth);
        var act = () => sut.Handle(new PharmacyQueueRecallSerahCmd(loketKey, ValidRowVersion, "user1"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*does not have an active ticket to recall*");
    }

    #endregion

    #region SkipSerah Tests

    [Fact]
    public async Task SkipSerah_Succeeds_ReleasesLoketClaim()
    {
        const string loketKey = "APT-SRH-01";
        var loket = CreateLoketSession(loketKey, claimState: LoketClaimStateEnum.Called, antrianId: "Q-01", noUrut: 5);
        var updatedLoket = CreateLoketSession(loketKey, claimState: LoketClaimStateEnum.Idle);

        _operationRepo.SetupSequence(x => x.GetLoket(loketKey))
            .Returns(MayBe.From(loket))
            .Returns(MayBe.From(updatedLoket));

        var entry = ApotekQueueEntryModel.Create("Q-01", 5, "SP-01", "A");
        _queueEntryRepo.Setup(x => x.Get("Q-01", 5)).Returns(MayBe.From(entry));

        _operationRepo.Setup(x => x.TrySkipLoket(loketKey, "Pasien tidak menyahut", ValidRowVersion, "user1", _now))
            .Returns(true);

        var sut = new PharmacyQueueSkipSerahHandler(_operationRepo.Object, _queueEntryRepo.Object, _publisher.Object, _clock.Object, _auth);
        var res = await sut.Handle(new PharmacyQueueSkipSerahCmd(loketKey, "Pasien tidak menyahut", ValidRowVersion, "user1"), CancellationToken.None);

        res.Success.Should().BeTrue();
        res.ClaimState.Should().Be(LoketClaimStateEnum.Idle);
        _publisher.Verify(x => x.PublishWorklistRefresh("DEP-01", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SkipSerah_Throws_WhenLoketIdle()
    {
        const string loketKey = "APT-SRH-01";
        var loket = CreateLoketSession(loketKey, claimState: LoketClaimStateEnum.Idle);
        _operationRepo.Setup(x => x.GetLoket(loketKey)).Returns(MayBe.From(loket));

        var sut = new PharmacyQueueSkipSerahHandler(_operationRepo.Object, _queueEntryRepo.Object, _publisher.Object, _clock.Object, _auth);
        var act = () => sut.Handle(new PharmacyQueueSkipSerahCmd(loketKey, "Reason", ValidRowVersion, "user1"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*does not have an active ticket to skip*");
    }

    #endregion

    #region StartSerah Tests

    [Fact]
    public async Task StartSerah_Succeeds_TransitionsToSerahObat()
    {
        const string loketKey = "APT-SRH-01";
        var loket = CreateLoketSession(loketKey, claimState: LoketClaimStateEnum.Called, antrianId: "Q-01", noUrut: 5);
        var updatedLoket = CreateLoketSession(loketKey, claimState: LoketClaimStateEnum.InService, antrianId: "Q-01", noUrut: 5);

        _operationRepo.SetupSequence(x => x.GetLoket(loketKey))
            .Returns(MayBe.From(loket))
            .Returns(MayBe.From(updatedLoket));

        var entry = ApotekQueueEntryModel.Create("Q-01", 5, "SP-01", "A");
        _queueEntryRepo.Setup(x => x.Get("Q-01", 5)).Returns(MayBe.From(entry));

        _operationRepo.Setup(x => x.TryStartServiceLoket(loketKey, ValidRowVersion, "user1", _now))
            .Returns(true);

        var sut = new PharmacyQueueStartSerahHandler(_operationRepo.Object, _queueEntryRepo.Object, _publisher.Object, _clock.Object, _auth);
        var res = await sut.Handle(new PharmacyQueueStartSerahCmd(loketKey, ValidRowVersion, "user1"), CancellationToken.None);

        res.Success.Should().BeTrue();
        res.ClaimState.Should().Be(LoketClaimStateEnum.InService);
        _publisher.Verify(x => x.PublishWorklistRefresh("DEP-01", It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Authorization Policy Tests

    [Fact]
    public async Task CallSerah_ThrowsUnauthorized_WhenAuthPolicyRejects()
    {
        var authMock = new Mock<IAptAuthorizationPolicy>();
        authMock.Setup(x => x.AssertCommandAllowed(nameof(PharmacyQueueCallSerahCmd), "bad_user"))
            .Throws(new UnauthorizedAccessException("Forbidden"));

        var sut = new PharmacyQueueCallSerahHandler(_operationRepo.Object, _queueEntryRepo.Object, _publisher.Object, _clock.Object, authMock.Object);
        var act = () => sut.Handle(new PharmacyQueueCallSerahCmd("APT-SRH-01", UserId: "bad_user"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("Forbidden");
    }

    [Fact]
    public async Task RecallSerah_ThrowsUnauthorized_WhenAuthPolicyRejects()
    {
        var authMock = new Mock<IAptAuthorizationPolicy>();
        authMock.Setup(x => x.AssertCommandAllowed(nameof(PharmacyQueueRecallSerahCmd), "bad_user"))
            .Throws(new UnauthorizedAccessException("Forbidden"));

        var sut = new PharmacyQueueRecallSerahHandler(_operationRepo.Object, _queueEntryRepo.Object, _publisher.Object, _clock.Object, authMock.Object);
        var act = () => sut.Handle(new PharmacyQueueRecallSerahCmd("APT-SRH-01", ValidRowVersion, UserId: "bad_user"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("Forbidden");
    }

    [Fact]
    public async Task SkipSerah_ThrowsUnauthorized_WhenAuthPolicyRejects()
    {
        var authMock = new Mock<IAptAuthorizationPolicy>();
        authMock.Setup(x => x.AssertCommandAllowed(nameof(PharmacyQueueSkipSerahCmd), "bad_user"))
            .Throws(new UnauthorizedAccessException("Forbidden"));

        var sut = new PharmacyQueueSkipSerahHandler(_operationRepo.Object, _queueEntryRepo.Object, _publisher.Object, _clock.Object, authMock.Object);
        var act = () => sut.Handle(new PharmacyQueueSkipSerahCmd("APT-SRH-01", "Alasan", ValidRowVersion, UserId: "bad_user"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("Forbidden");
    }

    [Fact]
    public async Task StartSerah_ThrowsUnauthorized_WhenAuthPolicyRejects()
    {
        var authMock = new Mock<IAptAuthorizationPolicy>();
        authMock.Setup(x => x.AssertCommandAllowed(nameof(PharmacyQueueStartSerahCmd), "bad_user"))
            .Throws(new UnauthorizedAccessException("Forbidden"));

        var sut = new PharmacyQueueStartSerahHandler(_operationRepo.Object, _queueEntryRepo.Object, _publisher.Object, _clock.Object, authMock.Object);
        var act = () => sut.Handle(new PharmacyQueueStartSerahCmd("APT-SRH-01", ValidRowVersion, UserId: "bad_user"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("Forbidden");
    }

    [Fact]
    public void P5_S10_MutationHandlers_InvokeAuthorizationPolicyPerCommand()
    {
        var p5S10Handlers = new[]
        {
            typeof(PharmacyQueueCompleteHandoverHandler),
            typeof(PharmacyQueueCallSerahHandler),
            typeof(PharmacyQueueRecallSerahHandler),
            typeof(PharmacyQueueSkipSerahHandler),
            typeof(PharmacyQueueStartSerahHandler)
        };

        foreach (var handler in p5S10Handlers)
        {
            handler.GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                .Concat(handler.GetFields(BindingFlags.Instance | BindingFlags.Public))
                .Any(f => f.FieldType == typeof(IAptAuthorizationPolicy))
                .Should().BeTrue($"handler {handler.Name} must depend on {nameof(IAptAuthorizationPolicy)}");
        }
    }

    #endregion
}
