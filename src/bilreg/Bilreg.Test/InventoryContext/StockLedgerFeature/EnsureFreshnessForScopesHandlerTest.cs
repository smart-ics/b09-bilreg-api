using Bilreg.Application.InventoryContext.StockLedgerFeature;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using FluentAssertions;
using MediatR;
using Moq;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.InventoryContext.StockLedgerFeature;

public class EnsureFreshnessForScopesHandlerTest
{
    private readonly Mock<ILegacyStockReadPort> _legacyRead = new();
    private readonly Mock<IStockLegacyScopeRepo> _scopeRepo = new();
    private readonly Mock<IMediator> _mediator = new();
    private readonly EnsureFreshnessForScopesHandler _sut;

    private const string BrgId = "BRG0000000001";
    private const string DoId = "DO00000001";
    private const string BrgId2 = "BRG0000000002";
    private const string DoId2 = "DO00000002";
    private const string UserId = "U1";
    private static readonly DateTime Watermark = new(2026, 3, 1, 10, 0, 0);

    public EnsureFreshnessForScopesHandlerTest()
    {
        _sut = new EnsureFreshnessForScopesHandler(
            _legacyRead.Object,
            _scopeRepo.Object,
            _mediator.Object);
    }

    [Fact]
    public async Task Handle_NotAligned_HydratesThenCatchUp()
    {
        var scope = StockLegacyScopeModel.CreateNotAligned(BrgId, DoId);
        _scopeRepo.Setup(x => x.LoadEntity(It.IsAny<IStockLegacyScopeKey>()))
            .Returns(MayBe.From(scope));

        _mediator
            .Setup(x => x.Send(It.IsAny<HydrateScopeFromLegacyCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HydrateScopeFromLegacyResult(
                HydrateScopeOutcomeEnum.Success,
                AlignmentStatusEnum.Aligned,
                string.Empty,
                AppliedJournalCount: 1));

        _mediator
            .Setup(x => x.Send(It.IsAny<CatchUpScopeFromLegacyCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CatchUpScopeFromLegacyResult(
                CatchUpScopeOutcomeEnum.Idempotent,
                AlignmentStatusEnum.Aligned,
                string.Empty,
                AppliedJournalCount: 0));

        var result = await _sut.Handle(
            new EnsureFreshnessForScopesCommand([new ScopeKeyDto(BrgId, DoId)], UserId),
            default);

        result.Outcome.Should().Be(EnsureFreshnessOutcomeEnum.Success);
        _mediator.Verify(
            x => x.Send(It.IsAny<HydrateScopeFromLegacyCommand>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _mediator.Verify(
            x => x.Send(It.IsAny<CatchUpScopeFromLegacyCommand>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_AlignedWithNewLegacy_MarksStaleThenCatchUp()
    {
        var scope = StockLegacyScopeModel.CreateNotAligned(BrgId, DoId);
        scope.MarkAligned(Watermark, "BK00000001", DateTime.Now);
        _scopeRepo.Setup(x => x.LoadEntity(It.IsAny<IStockLegacyScopeKey>()))
            .Returns(MayBe.From(scope));

        var after = Watermark.AddHours(1);
        _legacyRead.Setup(x => x.ListJournals(BrgId, DoId)).Returns(
        [
            new LegacyStockJournalReadModel(
                "BK00000002", BrgId, DoId, "LY001", "", "",
                QtyIn: 0, QtyOut: 1, Hpp: 1000, MovementKindString: "PK",
                TrsReffId: "PK2", TglMutasi: after, TglEd: StockLedgerSentinel.EmptyDate),
        ]);

        StockLegacyScopeModel? saved = null;
        _scopeRepo.Setup(x => x.SaveChanges(It.IsAny<StockLegacyScopeModel>()))
            .Callback<StockLegacyScopeModel>(s => saved = s);

        _mediator
            .Setup(x => x.Send(It.IsAny<CatchUpScopeFromLegacyCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CatchUpScopeFromLegacyResult(
                CatchUpScopeOutcomeEnum.Success,
                AlignmentStatusEnum.Aligned,
                string.Empty,
                AppliedJournalCount: 1));

        var result = await _sut.Handle(
            new EnsureFreshnessForScopesCommand([new ScopeKeyDto(BrgId, DoId)], UserId),
            default);

        result.Outcome.Should().Be(EnsureFreshnessOutcomeEnum.Success);
        saved.Should().NotBeNull();
        saved!.AlignmentStatus.Should().Be(AlignmentStatusEnum.Stale);
        _mediator.Verify(
            x => x.Send(It.IsAny<HydrateScopeFromLegacyCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _mediator.Verify(
            x => x.Send(It.IsAny<CatchUpScopeFromLegacyCommand>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_EmptyAlignedWithLaterLegacy_MarksStaleThenCatchUp()
    {
        var scope = StockLegacyScopeModel.CreateNotAligned(BrgId, DoId);
        scope.MarkAligned(StockLedgerSentinel.EmptyDate, string.Empty, DateTime.Now);
        _scopeRepo.Setup(x => x.LoadEntity(It.IsAny<IStockLegacyScopeKey>()))
            .Returns(MayBe.From(scope));

        var tgl = new DateTime(2026, 4, 1, 12, 0, 0);
        _legacyRead.Setup(x => x.ListJournals(BrgId, DoId)).Returns(
        [
            new LegacyStockJournalReadModel(
                "BK00000001", BrgId, DoId, "LY001", "", "",
                QtyIn: 10, QtyOut: 0, Hpp: 1500, MovementKindString: "DO",
                TrsReffId: "DM1", TglMutasi: tgl, TglEd: StockLedgerSentinel.EmptyDate),
        ]);

        StockLegacyScopeModel? saved = null;
        _scopeRepo.Setup(x => x.SaveChanges(It.IsAny<StockLegacyScopeModel>()))
            .Callback<StockLegacyScopeModel>(s => saved = s);

        _mediator
            .Setup(x => x.Send(It.IsAny<CatchUpScopeFromLegacyCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CatchUpScopeFromLegacyResult(
                CatchUpScopeOutcomeEnum.Success,
                AlignmentStatusEnum.Aligned,
                string.Empty,
                AppliedJournalCount: 1));

        var result = await _sut.Handle(
            new EnsureFreshnessForScopesCommand([new ScopeKeyDto(BrgId, DoId)], UserId),
            default);

        result.Outcome.Should().Be(EnsureFreshnessOutcomeEnum.Success);
        saved.Should().NotBeNull();
        saved!.AlignmentStatus.Should().Be(AlignmentStatusEnum.Stale);
        _mediator.Verify(
            x => x.Send(It.IsAny<HydrateScopeFromLegacyCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _mediator.Verify(
            x => x.Send(It.IsAny<CatchUpScopeFromLegacyCommand>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Stale_CatchUpWithoutHydrate()
    {
        var scope = StockLegacyScopeModel.CreateNotAligned(BrgId, DoId);
        scope.MarkAligned(Watermark, "BK00000001", DateTime.Now);
        scope.MarkStale();
        _scopeRepo.Setup(x => x.LoadEntity(It.IsAny<IStockLegacyScopeKey>()))
            .Returns(MayBe.From(scope));

        _mediator
            .Setup(x => x.Send(It.IsAny<CatchUpScopeFromLegacyCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CatchUpScopeFromLegacyResult(
                CatchUpScopeOutcomeEnum.Success,
                AlignmentStatusEnum.Aligned,
                string.Empty,
                AppliedJournalCount: 1));

        var result = await _sut.Handle(
            new EnsureFreshnessForScopesCommand([new ScopeKeyDto(BrgId, DoId)], UserId),
            default);

        result.Outcome.Should().Be(EnsureFreshnessOutcomeEnum.Success);
        _mediator.Verify(
            x => x.Send(It.IsAny<HydrateScopeFromLegacyCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _legacyRead.Verify(x => x.ListJournals(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _mediator.Verify(
            x => x.Send(It.IsAny<CatchUpScopeFromLegacyCommand>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Inconsistent_AbortsWithoutHydrateOrCatchUp()
    {
        var scope = StockLegacyScopeModel.CreateNotAligned(BrgId, DoId);
        scope.MarkInconsistent("broken");
        _scopeRepo.Setup(x => x.LoadEntity(It.IsAny<IStockLegacyScopeKey>()))
            .Returns(MayBe.From(scope));

        var result = await _sut.Handle(
            new EnsureFreshnessForScopesCommand([new ScopeKeyDto(BrgId, DoId)], UserId),
            default);

        result.Outcome.Should().Be(EnsureFreshnessOutcomeEnum.AbortedInconsistent);
        result.InconsistencyReason.Should().Be("broken");
        result.FailedBrgId.Should().Be(BrgId);
        result.FailedBrgMasukReffId.Should().Be(DoId);
        _mediator.Verify(
            x => x.Send(It.IsAny<HydrateScopeFromLegacyCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _mediator.Verify(
            x => x.Send(It.IsAny<CatchUpScopeFromLegacyCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_HydrateInconsistent_Aborts()
    {
        var scope = StockLegacyScopeModel.CreateNotAligned(BrgId, DoId);
        _scopeRepo.Setup(x => x.LoadEntity(It.IsAny<IStockLegacyScopeKey>()))
            .Returns(MayBe.From(scope));

        _mediator
            .Setup(x => x.Send(It.IsAny<HydrateScopeFromLegacyCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HydrateScopeFromLegacyResult(
                HydrateScopeOutcomeEnum.Inconsistent,
                AlignmentStatusEnum.Inconsistent,
                "bad kind",
                AppliedJournalCount: 0));

        var result = await _sut.Handle(
            new EnsureFreshnessForScopesCommand([new ScopeKeyDto(BrgId, DoId)], UserId),
            default);

        result.Outcome.Should().Be(EnsureFreshnessOutcomeEnum.AbortedInconsistent);
        result.InconsistencyReason.Should().Be("bad kind");
        _mediator.Verify(
            x => x.Send(It.IsAny<CatchUpScopeFromLegacyCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_MultiScope_Success()
    {
        _scopeRepo
            .Setup(x => x.LoadEntity(It.Is<IStockLegacyScopeKey>(k => k.BrgId == BrgId)))
            .Returns(MayBe.From(StockLegacyScopeModel.CreateNotAligned(BrgId, DoId)));

        var aligned2 = StockLegacyScopeModel.CreateNotAligned(BrgId2, DoId2);
        aligned2.MarkAligned(Watermark, "BK2", DateTime.Now);
        _scopeRepo
            .Setup(x => x.LoadEntity(It.Is<IStockLegacyScopeKey>(k => k.BrgId == BrgId2)))
            .Returns(MayBe.From(aligned2));

        _legacyRead.Setup(x => x.ListJournals(BrgId2, DoId2)).Returns([]);

        _mediator
            .Setup(x => x.Send(It.IsAny<HydrateScopeFromLegacyCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HydrateScopeFromLegacyResult(
                HydrateScopeOutcomeEnum.Success,
                AlignmentStatusEnum.Aligned,
                string.Empty,
                0));
        _mediator
            .Setup(x => x.Send(It.IsAny<CatchUpScopeFromLegacyCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CatchUpScopeFromLegacyResult(
                CatchUpScopeOutcomeEnum.Idempotent,
                AlignmentStatusEnum.Aligned,
                string.Empty,
                0));

        var result = await _sut.Handle(
            new EnsureFreshnessForScopesCommand(
            [
                new ScopeKeyDto(BrgId, DoId),
                new ScopeKeyDto(BrgId2, DoId2)
            ],
            UserId),
            default);

        result.Outcome.Should().Be(EnsureFreshnessOutcomeEnum.Success);
        _mediator.Verify(
            x => x.Send(It.IsAny<HydrateScopeFromLegacyCommand>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _mediator.Verify(
            x => x.Send(It.IsAny<CatchUpScopeFromLegacyCommand>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }
}
