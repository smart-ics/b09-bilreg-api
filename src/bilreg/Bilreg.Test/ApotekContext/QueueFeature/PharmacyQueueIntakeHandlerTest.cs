using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.ApotekContext.QueueFeature;
using Bilreg.Application.ApotekContext.QueueFeature.UseCases;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
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

public class PharmacyQueueIntakeHandlerTest
{
    private readonly Mock<IPharmacyServicePointRepo> _servicePoints = new();
    private readonly Mock<IApotekQueueEntryRepo> _queueEntries = new();
    private readonly Mock<IAntrianRepo> _antrianRepo = new();
    private readonly Mock<IAntrianFactory> _antrianFactory = new();
    private readonly Mock<ISequencer> _sequencer = new();
    private readonly Mock<IPharmacyQueuePublisher> _publisher = new();
    private readonly Mock<ITglJamProvider> _clock = new();
    private readonly AllowAllAuth _auth = new();

    private readonly DateTime _now = new(2026, 10, 18, 9, 30, 0);

    public PharmacyQueueIntakeHandlerTest()
    {
        _clock.Setup(x => x.Now).Returns(_now);
    }

    private PharmacyQueueIntakeHandler CreateSut()
        => new(
            _servicePoints.Object,
            _queueEntries.Object,
            _antrianRepo.Object,
            _antrianFactory.Object,
            _sequencer.Object,
            _publisher.Object,
            _clock.Object,
            _auth);

    [Fact]
    public async Task Handle_Throws_When_ServicePointId_NullOrWhiteSpace()
    {
        var sut = CreateSut();
        var act = () => sut.Handle(new PharmacyQueueIntakeCmd(""), CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Handle_Throws_When_ServicePoint_NotFound()
    {
        _servicePoints
            .Setup(x => x.Get("SP-999"))
            .Returns(MayBe<PharmacyServicePointModel>.None);

        var sut = CreateSut();
        var act = () => sut.Handle(new PharmacyQueueIntakeCmd("SP-999"), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*SP-999*");
    }

    [Fact]
    public async Task Handle_Throws_When_ServicePoint_NotActive()
    {
        var servicePoint = PharmacyServicePointModel.Create("SP-01", "DEPO-RJ", "Resep Non-Racik", "A");
        servicePoint.Deactivate();

        _servicePoints
            .Setup(x => x.Get("SP-01"))
            .Returns(MayBe.From(servicePoint));

        var sut = CreateSut();
        var act = () => sut.Handle(new PharmacyQueueIntakeCmd("SP-01"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not active*");
    }

    [Fact]
    public async Task Handle_Allocates_Sequence_OnDemand_And_Creates_Generic_And_Apotek_Entries()
    {
        // Arrange
        var servicePoint = PharmacyServicePointModel.Create("SP-01", "DEPO-RJ", "Resep Non-Racikan", "A");
        _servicePoints
            .Setup(x => x.Get("SP-01"))
            .Returns(MayBe.From(servicePoint));

        var businessDate = DateOnly.FromDateTime(_now);
        _antrianRepo.Setup(x => x.ListData(businessDate)).Returns([]);

        var queue = new AntrianModel(
            "ANT-001", businessDate, TimeOnly.MinValue, TimeOnly.MaxValue,
            "AN2610180000_SP-01", "Resep Non-Racikan",
            new ServicePointType("SP-01", "Resep Non-Racikan"),
            [], _sequencer.Object);

        _antrianFactory
            .Setup(x => x.Create(It.IsAny<ServicePointType>(), businessDate))
            .Returns(queue);

        _sequencer
            .Setup(x => x.GetNextNoUrut("AN2610180000_SP-01"))
            .Returns(5);

        _queueEntries
            .Setup(x => x.ListByPhase("DEPO-RJ", ApotekPhaseEnum.TAKEN))
            .Returns([]);

        AntrianEntryModel? savedGeneric = null;
        _antrianRepo
            .Setup(x => x.SaveNewEntry(It.IsAny<AntrianModel>(), It.IsAny<AntrianEntryModel>()))
            .Callback<AntrianModel, AntrianEntryModel>((q, e) => savedGeneric = e);

        ApotekQueueEntryModel? savedApotek = null;
        _queueEntries
            .Setup(x => x.Save(It.IsAny<ApotekQueueEntryModel>()))
            .Callback<ApotekQueueEntryModel>(e => savedApotek = e);

        _publisher
            .Setup(x => x.PublishWorklistRefresh("DEPO-RJ", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(new PharmacyQueueIntakeCmd("SP-01"), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.AntrianId.Should().Be("ANT-001");
        result.NoUrut.Should().Be(5);
        result.QueueLabel.Should().Be("A005");
        result.ServicePointId.Should().Be("SP-01");
        result.ServicePointName.Should().Be("Resep Non-Racikan");
        result.QueuePrefix.Should().Be("A");
        result.ApotekPhase.Should().Be(ApotekPhaseEnum.TAKEN);
        result.TakenAt.Should().Be(_now);
        result.WaitingAheadCount.Should().Be(0);

        // Verify Generic Entry (Waiting = 0)
        savedGeneric.Should().NotBeNull();
        savedGeneric!.NoUrut.Should().Be(5);
        savedGeneric.AntrianStatus.Should().Be(AntrianStatusEnum.Waiting);
        savedGeneric.CreatedAt.Should().Be(_now);

        // Verify Apotek Entry (TAKEN, A005)
        savedApotek.Should().NotBeNull();
        savedApotek!.AntrianId.Should().Be("ANT-001");
        savedApotek.NoUrut.Should().Be(5);
        savedApotek.QueueLabel.Should().Be("A005");
        savedApotek.ServicePointId.Should().Be("SP-01");
        savedApotek.ApotekPhase.Should().Be(ApotekPhaseEnum.TAKEN);
        savedApotek.TakenAt.Should().Be(_now);

        // Verify SignalR broadcast
        _publisher.Verify(x => x.PublishWorklistRefresh("DEPO-RJ", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Calculates_WaitingAheadCount_Correctly()
    {
        var servicePoint = PharmacyServicePointModel.Create("SP-02", "DEPO-RJ", "Resep Racikan", "B");
        _servicePoints.Setup(x => x.Get("SP-02")).Returns(MayBe.From(servicePoint));

        var businessDate = DateOnly.FromDateTime(_now);
        var queue = new AntrianModel(
            "ANT-002", businessDate, TimeOnly.MinValue, TimeOnly.MaxValue,
            "AN2610180000_SP-02", "Resep Racikan",
            new ServicePointType("SP-02", "Resep Racikan"),
            [], _sequencer.Object);

        _antrianFactory.Setup(x => x.Create(It.IsAny<ServicePointType>(), businessDate)).Returns(queue);
        _sequencer.Setup(x => x.GetNextNoUrut(It.IsAny<string>())).Returns(12);

        // 2 earlier tickets in TAKEN for this service point
        var ticket1 = ApotekQueueEntryModel.Create("ANT-002", 3, "SP-02", "B", _now.AddMinutes(-10));
        var ticket2 = ApotekQueueEntryModel.Create("ANT-002", 7, "SP-02", "B", _now.AddMinutes(-5));
        var otherSpTicket = ApotekQueueEntryModel.Create("ANT-001", 1, "SP-OTHER", "A", _now.AddMinutes(-8));

        _queueEntries
            .Setup(x => x.ListByPhase("DEPO-RJ", ApotekPhaseEnum.TAKEN))
            .Returns([ticket1, ticket2, otherSpTicket]);

        _publisher.Setup(x => x.PublishWorklistRefresh("DEPO-RJ", It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.Handle(new PharmacyQueueIntakeCmd("SP-02"), CancellationToken.None);

        result.QueueLabel.Should().Be("B012");
        result.WaitingAheadCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_Recovers_From_AdmissionQueueSessionRaceException()
    {
        var servicePoint = PharmacyServicePointModel.Create("SP-01", "DEPO-RJ", "Resep Non-Racikan", "A");
        _servicePoints.Setup(x => x.Get("SP-01")).Returns(MayBe.From(servicePoint));

        var businessDate = DateOnly.FromDateTime(_now);
        _antrianRepo.SetupSequence(x => x.ListData(businessDate))
            .Returns([]) // First check: not found
            .Returns([new AntrianHeaderView("ANT-WINNER", "Resep Non-Racikan", businessDate, TimeOnly.MinValue, "AN2610180000_SP-01")]); // Reload check

        var initialQueue = new AntrianModel("ANT-LOSER", businessDate, TimeOnly.MinValue, TimeOnly.MaxValue,
            "AN2610180000_SP-01", "Resep Non-Racikan", new ServicePointType("SP-01", "Resep Non-Racikan"), [], _sequencer.Object);
        _antrianFactory.Setup(x => x.Create(It.IsAny<ServicePointType>(), businessDate)).Returns(initialQueue);

        var winnerQueue = new AntrianModel("ANT-WINNER", businessDate, TimeOnly.MinValue, TimeOnly.MaxValue,
            "AN2610180000_SP-01", "Resep Non-Racikan", new ServicePointType("SP-01", "Resep Non-Racikan"), [], _sequencer.Object);
        _antrianRepo.Setup(x => x.LoadEntity(It.Is<IAntrianKey>(k => k.AntrianId == "ANT-WINNER")))
            .Returns(MayBe.From(winnerQueue));

        _sequencer.Setup(x => x.GetNextNoUrut("AN2610180000_SP-01")).Returns(3);

        var callCount = 0;
        _antrianRepo
            .Setup(x => x.SaveNewEntry(It.IsAny<AntrianModel>(), It.IsAny<AntrianEntryModel>()))
            .Callback<AntrianModel, AntrianEntryModel>((q, e) =>
            {
                callCount++;
                if (callCount == 1)
                    throw new AdmissionQueueSessionRaceException("AN2610180000_SP-01");
            });

        _queueEntries.Setup(x => x.ListByPhase(It.IsAny<string>(), ApotekPhaseEnum.TAKEN)).Returns([]);
        _publisher.Setup(x => x.PublishWorklistRefresh(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.Handle(new PharmacyQueueIntakeCmd("SP-01"), CancellationToken.None);

        result.AntrianId.Should().Be("ANT-WINNER");
        result.NoUrut.Should().Be(3);
        result.QueueLabel.Should().Be("A003");
        callCount.Should().Be(2);
    }
}
