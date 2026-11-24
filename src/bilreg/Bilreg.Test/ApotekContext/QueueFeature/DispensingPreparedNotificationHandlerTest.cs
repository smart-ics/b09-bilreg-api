using Bilreg.Application.ApotekContext.DispensingFeature;
using Bilreg.Application.ApotekContext.DispensingFeature.UseCases;
using Bilreg.Application.ApotekContext.QueueFeature;
using Bilreg.Application.ApotekContext.QueueFeature.UseCases;
using Bilreg.Application.ApotekContext.SalesOrderFeature;
using Bilreg.Application.ApotekContext.Shared;
using Bilreg.Domain.ApotekContext.DispensingFeature;
using Bilreg.Domain.ApotekContext.QueueFeature;
using Bilreg.Domain.ApotekContext.SalesOrderFeature;
using Bilreg.Domain.ApotekContext.Shared;
using FluentAssertions;
using MediatR;
using Moq;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.ApotekContext.QueueFeature;

public class DispensingPreparedNotificationHandlerTest
{
    private readonly Mock<ISalesOrderRepo> _salesOrderRepo = new();
    private readonly Mock<IDispensingRepo> _dispensingRepo = new();
    private readonly Mock<IPharmacyQueueMappingRepo> _mappingRepo = new();
    private readonly Mock<IApotekQueueEntryRepo> _queueEntryRepo = new();
    private readonly Mock<IPharmacyServicePointRepo> _servicePointRepo = new();
    private readonly Mock<IPharmacyQueuePublisher> _publisher = new();
    private readonly Mock<ITglJamProvider> _clock = new();

    private readonly DateTime _now = new(2026, 10, 18, 10, 15, 0);

    public DispensingPreparedNotificationHandlerTest()
    {
        _clock.Setup(x => x.Now).Returns(_now);
    }

    private DispensingPreparedNotificationHandler CreateSut()
        => new(
            _salesOrderRepo.Object,
            _dispensingRepo.Object,
            _mappingRepo.Object,
            _queueEntryRepo.Object,
            _servicePointRepo.Object,
            _publisher.Object,
            _clock.Object);

    private static SalesOrderModel CreateSalesOrder(string soId, string sourceId, SalesOrderSourceKindEnum sourceKind = SalesOrderSourceKindEnum.ResepKerja)
    {
        return SalesOrderModel.Establish(
            sourceKind, sourceId, "TEL-01", "REG-01", "POLI-01", "Pasien A",
            PayerPathEnum.Bpjs, PartialReasonEnum.None,
            [SalesOrderItemModel.Establish(1, 1, "BRG1", "Obat A", "TAB", 10, FornasCoverageEnum.Covered, "", false)],
            []);
    }

    private static DispensingModel CreateDispensing(
        string dispensingId,
        string salesOrderId,
        DispensingStatusEnum status,
        DateTime preparedAt = default)
    {
        return DispensingModel.Rehydrate(
            dispensingId,
            salesOrderId,
            0,
            status,
            "L-FAR",
            "L-DTU",
            DateTime.Now,
            DateTime.Now,
            preparedAt == default ? (status == DispensingStatusEnum.Prepared ? DateTime.Now : ApotekDate.Empty) : preparedAt,
            ApotekDate.Empty, "", "", ApotekDate.Empty, "", "", ApotekDate.Empty,
            "", "", ApotekDate.Empty, ApotekDate.Empty, "", ApotekDate.Empty, 1,
            [new DispensingItemModel(1, 1, "BRG1", 10, "", "", "", DispensingItemOutcomeEnum.Open)],
            []);
    }

    [Fact]
    public async Task Handle_WhenSingleDispensingPrepared_TransitionsQueueToSiapSerah_AndPublishesRefresh()
    {
        // Arrange
        const string soId = "SO-01";
        const string dispId = "ADP-01";
        const string demandId = "RK-01";
        const string antrianId = "Q-01";
        const int noUrut = 5;

        var so = CreateSalesOrder(soId, demandId);
        _salesOrderRepo.Setup(x => x.LoadEntity(It.Is<ISalesOrderKey>(k => k.SalesOrderId == soId)))
            .Returns(MayBe.From(so));

        var mapping = QueueMappingModel.Create(QueueDemandKindEnum.ResepKerja, demandId, antrianId, noUrut, "TRK-01", QueueMappingMethodEnum.Manual, "user", DateTime.Now);
        _mappingRepo.Setup(x => x.GetByDemand(QueueDemandKindEnum.ResepKerja, demandId))
            .Returns(MayBe.From(mapping));
        _mappingRepo.Setup(x => x.ListByQueue(antrianId, noUrut))
            .Returns([mapping]);

        var queueEntry = ApotekQueueEntryModel.Create(antrianId, noUrut, "SP-01", "A");
        queueEntry.TransitionToPenyiapan();
        _queueEntryRepo.Setup(x => x.Get(antrianId, noUrut))
            .Returns(MayBe.From(queueEntry));

        var dispensing = CreateDispensing(dispId, soId, DispensingStatusEnum.Prepared, _now);
        _salesOrderRepo.Setup(x => x.ListBySource(SalesOrderSourceKindEnum.ResepKerja, demandId))
            .Returns([so]);
        _dispensingRepo.Setup(x => x.ListBySalesOrder(so.SalesOrderId))
            .Returns([dispensing]);

        var sp = PharmacyServicePointModel.Create("SP-01", "DEP-01", "Depo Rajal", "A");
        _servicePointRepo.Setup(x => x.Get("SP-01"))
            .Returns(MayBe.From(sp));

        var sut = CreateSut();

        // Act
        await sut.Handle(new DispensingPreparedNotification(dispId, soId, _now), CancellationToken.None);

        // Assert
        queueEntry.ApotekPhase.Should().Be(ApotekPhaseEnum.SIAP_SERAH);
        queueEntry.PreparedAt.Should().Be(_now);
        _queueEntryRepo.Verify(x => x.Save(queueEntry), Times.Once);
        _publisher.Verify(x => x.PublishWorklistRefresh("DEP-01", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenMultiDispensingPartiallyPrepared_RemainsInPenyiapan()
    {
        // Arrange: 2 dispensings (non-racik & racik), only 1 prepared
        const string soId1 = "SO-01";
        const string soId2 = "SO-02";
        const string demandId = "RK-01";
        const string antrianId = "Q-01";
        const int noUrut = 5;

        var so1 = CreateSalesOrder(soId1, demandId);
        var so2 = CreateSalesOrder(soId2, demandId);
        _salesOrderRepo.Setup(x => x.LoadEntity(It.Is<ISalesOrderKey>(k => k.SalesOrderId == soId1)))
            .Returns(MayBe.From(so1));

        var mapping = QueueMappingModel.Create(QueueDemandKindEnum.ResepKerja, demandId, antrianId, noUrut, "TRK-01", QueueMappingMethodEnum.Manual, "user", DateTime.Now);
        _mappingRepo.Setup(x => x.GetByDemand(QueueDemandKindEnum.ResepKerja, demandId))
            .Returns(MayBe.From(mapping));
        _mappingRepo.Setup(x => x.ListByQueue(antrianId, noUrut))
            .Returns([mapping]);

        var queueEntry = ApotekQueueEntryModel.Create(antrianId, noUrut, "SP-01", "A");
        queueEntry.TransitionToPenyiapan();
        _queueEntryRepo.Setup(x => x.Get(antrianId, noUrut))
            .Returns(MayBe.From(queueEntry));

        var disp1 = CreateDispensing("ADP-01", soId1, DispensingStatusEnum.Prepared, _now);
        var disp2 = CreateDispensing("ADP-02", soId2, DispensingStatusEnum.Preparing); // Still preparing!

        _salesOrderRepo.Setup(x => x.ListBySource(SalesOrderSourceKindEnum.ResepKerja, demandId))
            .Returns([so1, so2]);
        _dispensingRepo.Setup(x => x.ListBySalesOrder(so1.SalesOrderId)).Returns([disp1]);
        _dispensingRepo.Setup(x => x.ListBySalesOrder(so2.SalesOrderId)).Returns([disp2]);

        var sut = CreateSut();

        // Act
        await sut.Handle(new DispensingPreparedNotification("ADP-01", soId1, _now), CancellationToken.None);

        // Assert: Multi-dispensing consolidation blocked until all are prepared
        queueEntry.ApotekPhase.Should().Be(ApotekPhaseEnum.PENYIAPAN);
        _queueEntryRepo.Verify(x => x.Save(It.IsAny<ApotekQueueEntryModel>()), Times.Never);
        _publisher.Verify(x => x.PublishWorklistRefresh(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenMultiDispensingAllPrepared_ConsolidatesToSiapSerah()
    {
        // Arrange: 2 dispensings, second one is now prepared
        const string soId1 = "SO-01";
        const string soId2 = "SO-02";
        const string demandId = "RK-01";
        const string antrianId = "Q-01";
        const int noUrut = 5;

        var so1 = CreateSalesOrder(soId1, demandId);
        var so2 = CreateSalesOrder(soId2, demandId);
        _salesOrderRepo.Setup(x => x.LoadEntity(It.Is<ISalesOrderKey>(k => k.SalesOrderId == soId2)))
            .Returns(MayBe.From(so2));

        var mapping = QueueMappingModel.Create(QueueDemandKindEnum.ResepKerja, demandId, antrianId, noUrut, "TRK-01", QueueMappingMethodEnum.Manual, "user", DateTime.Now);
        _mappingRepo.Setup(x => x.GetByDemand(QueueDemandKindEnum.ResepKerja, demandId))
            .Returns(MayBe.From(mapping));
        _mappingRepo.Setup(x => x.ListByQueue(antrianId, noUrut))
            .Returns([mapping]);

        var queueEntry = ApotekQueueEntryModel.Create(antrianId, noUrut, "SP-01", "A");
        queueEntry.TransitionToPenyiapan();
        _queueEntryRepo.Setup(x => x.Get(antrianId, noUrut))
            .Returns(MayBe.From(queueEntry));

        var disp1 = CreateDispensing("ADP-01", soId1, DispensingStatusEnum.Prepared, _now.AddMinutes(-5));
        var disp2 = CreateDispensing("ADP-02", soId2, DispensingStatusEnum.Prepared, _now); // Both prepared!

        _salesOrderRepo.Setup(x => x.ListBySource(SalesOrderSourceKindEnum.ResepKerja, demandId))
            .Returns([so1, so2]);
        _dispensingRepo.Setup(x => x.ListBySalesOrder(so1.SalesOrderId)).Returns([disp1]);
        _dispensingRepo.Setup(x => x.ListBySalesOrder(so2.SalesOrderId)).Returns([disp2]);

        var sp = PharmacyServicePointModel.Create("SP-01", "DEP-01", "Depo Rajal", "A");
        _servicePointRepo.Setup(x => x.Get("SP-01"))
            .Returns(MayBe.From(sp));

        var sut = CreateSut();

        // Act
        await sut.Handle(new DispensingPreparedNotification("ADP-02", soId2, _now), CancellationToken.None);

        // Assert
        queueEntry.ApotekPhase.Should().Be(ApotekPhaseEnum.SIAP_SERAH);
        queueEntry.PreparedAt.Should().Be(_now);
        _queueEntryRepo.Verify(x => x.Save(queueEntry), Times.Once);
        _publisher.Verify(x => x.PublishWorklistRefresh("DEP-01", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenQueueNotInPenyiapan_DoesNotConsolidate()
    {
        const string soId = "SO-01";
        const string demandId = "RK-01";
        const string antrianId = "Q-01";
        const int noUrut = 5;

        var so = CreateSalesOrder(soId, demandId);
        _salesOrderRepo.Setup(x => x.LoadEntity(It.Is<ISalesOrderKey>(k => k.SalesOrderId == soId)))
            .Returns(MayBe.From(so));

        var mapping = QueueMappingModel.Create(QueueDemandKindEnum.ResepKerja, demandId, antrianId, noUrut, "TRK-01", QueueMappingMethodEnum.Manual, "user", DateTime.Now);
        _mappingRepo.Setup(x => x.GetByDemand(QueueDemandKindEnum.ResepKerja, demandId))
            .Returns(MayBe.From(mapping));

        // Queue is still in TAKEN phase (not PENYIAPAN)
        var queueEntry = ApotekQueueEntryModel.Create(antrianId, noUrut, "SP-01", "A");
        _queueEntryRepo.Setup(x => x.Get(antrianId, noUrut))
            .Returns(MayBe.From(queueEntry));

        var sut = CreateSut();

        // Act
        await sut.Handle(new DispensingPreparedNotification("ADP-01", soId, _now), CancellationToken.None);

        // Assert
        queueEntry.ApotekPhase.Should().Be(ApotekPhaseEnum.TAKEN);
        _queueEntryRepo.Verify(x => x.Save(It.IsAny<ApotekQueueEntryModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenSalesOrderNotFound_ReturnsGracefully()
    {
        _salesOrderRepo.Setup(x => x.LoadEntity(It.IsAny<ISalesOrderKey>()))
            .Returns(MayBe<SalesOrderModel>.None);

        var sut = CreateSut();
        var act = () => sut.Handle(new DispensingPreparedNotification("ADP-01", "SO-NONEXISTENT", _now), CancellationToken.None);

        await act.Should().NotThrowAsync();
        _queueEntryRepo.Verify(x => x.Save(It.IsAny<ApotekQueueEntryModel>()), Times.Never);
    }

    [Fact]
    public async Task DispensingPrepareHandler_PublishesNotification_WhenMediatorProvided()
    {
        var dispMock = new Mock<IDispensingRepo>();
        var authMock = new Mock<IAptAuthorizationPolicy>();
        var mediatorMock = new Mock<IMediator>();

        var dispensing = CreateDispensing("ADP-01", "SO-01", DispensingStatusEnum.Preparing);
        dispMock.Setup(x => x.LoadEntity(It.IsAny<IDispensingKey>()))
            .Returns(MayBe.From(dispensing));

        var handler = new DispensingPrepareHandler(dispMock.Object, authMock.Object, mediatorMock.Object);

        var res = await handler.Handle(new DispensingPrepareCmd("user1", "ADP-01", dispensing.Version), CancellationToken.None);

        res.Status.Should().Be(DispensingStatusEnum.Prepared);
        mediatorMock.Verify(x => x.Publish(
            It.Is<DispensingPreparedNotification>(n => n.DispensingId == "ADP-01" && n.SalesOrderId == "SO-01"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
