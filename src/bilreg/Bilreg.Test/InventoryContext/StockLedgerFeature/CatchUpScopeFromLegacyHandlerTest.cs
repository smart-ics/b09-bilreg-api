using Bilreg.Application.InventoryContext.StockLedgerFeature;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.InventoryContext.StockLedgerFeature;

public class CatchUpScopeFromLegacyHandlerTest
{
    private readonly Mock<ILegacyStockReadPort> _legacyRead = new();
    private readonly Mock<IStockBatchRepo> _batchRepo = new();
    private readonly Mock<IStockMutasiRepo> _mutasiRepo = new();
    private readonly Mock<IStockLegacyScopeRepo> _scopeRepo = new();
    private readonly Mock<IStockLegacyBindingRepo> _bindingRepo = new();
    private readonly CatchUpScopeFromLegacyHandler _sut;

    private const string BrgId = "BRG0000000001";
    private const string DoId = "DO00000001";
    private const string UserId = "U1";
    private static readonly DateTime Watermark = new(2026, 3, 1, 10, 0, 0);

    public CatchUpScopeFromLegacyHandlerTest()
    {
        var replayer = new LegacyScopeJournalReplayer(
            _batchRepo.Object,
            _mutasiRepo.Object,
            _bindingRepo.Object,
            _scopeRepo.Object);

        _sut = new CatchUpScopeFromLegacyHandler(
            _legacyRead.Object,
            _batchRepo.Object,
            _scopeRepo.Object,
            replayer);

        _bindingRepo
            .Setup(x => x.FindByLegacyBukuId(It.IsAny<string>()))
            .Returns(MayBe<StockLegacyBindingModel>.None);
    }

    private StockLegacyScopeModel AlignedScope(string lastBukuId = "BK00000001")
    {
        var scope = StockLegacyScopeModel.CreateNotAligned(BrgId, DoId);
        scope.MarkAligned(Watermark, lastBukuId, DateTime.Now);
        return scope;
    }

    private StockLegacyScopeModel EmptyAlignedScope()
    {
        var scope = StockLegacyScopeModel.CreateNotAligned(BrgId, DoId);
        scope.MarkAligned(StockLedgerSentinel.EmptyDate, string.Empty, DateTime.Now);
        return scope;
    }

    private StockBatchModel ExistingBatch(decimal qty = 10)
    {
        var batch = StockBatchModel.Create(BrgId, DoId, hpp: 1000, tglMasuk: Watermark);
        batch.IncreaseLokasi("LY001", StockLedgerSentinel.EmptyDate, qty);
        return batch;
    }

    [Fact]
    public async Task Handle_AppliesOnlyPostWatermarkRows()
    {
        var scope = AlignedScope("BK00000001");
        _scopeRepo.Setup(x => x.LoadEntity(It.IsAny<IStockLegacyScopeKey>()))
            .Returns(MayBe.From(scope));

        var batch = ExistingBatch(10);
        _batchRepo.Setup(x => x.LoadByNaturalKey(BrgId, DoId))
            .Returns(MayBe.From(batch));

        var before = Watermark.AddHours(-1);
        var after = Watermark.AddHours(2);
        _legacyRead.Setup(x => x.ListJournals(BrgId, DoId)).Returns(
        [
            new LegacyStockJournalReadModel(
                "BK00000001", BrgId, DoId, "LY001", "", "",
                QtyIn: 10, QtyOut: 0, Hpp: 1000, MovementKindString: "DO",
                TrsReffId: "DM1", TglMutasi: Watermark, TglEd: StockLedgerSentinel.EmptyDate),
            new LegacyStockJournalReadModel(
                "BK00000000", BrgId, DoId, "LY001", "", "",
                QtyIn: 0, QtyOut: 1, Hpp: 1000, MovementKindString: "PK",
                TrsReffId: "PK0", TglMutasi: before, TglEd: StockLedgerSentinel.EmptyDate),
            new LegacyStockJournalReadModel(
                "BK00000002", BrgId, DoId, "LY001", "", "",
                QtyIn: 0, QtyOut: 3, Hpp: 1000, MovementKindString: "PK",
                TrsReffId: "PK2", TglMutasi: after, TglEd: StockLedgerSentinel.EmptyDate),
        ]);

        StockLegacyScopeModel? saved = null;
        _scopeRepo.Setup(x => x.SaveChanges(It.IsAny<StockLegacyScopeModel>()))
            .Callback<StockLegacyScopeModel>(s => saved = s);

        var result = await _sut.Handle(new CatchUpScopeFromLegacyCommand(BrgId, DoId, UserId), default);

        result.Outcome.Should().Be(CatchUpScopeOutcomeEnum.Success);
        result.AppliedJournalCount.Should().Be(1);
        batch.QtySisa.Should().Be(7);
        _mutasiRepo.Verify(x => x.Insert(It.IsAny<StockMovementModel>()), Times.Once);
        saved.Should().NotBeNull();
        saved!.AlignmentStatus.Should().Be(AlignmentStatusEnum.Aligned);
        saved.LastLegacyBukuId.Should().Be("BK00000002");
        saved.TglMutasiLast.Should().Be(after);
    }

    [Fact]
    public async Task Handle_SameDatetime_TieBreakByBukuId()
    {
        var scope = AlignedScope("BK00000001");
        _scopeRepo.Setup(x => x.LoadEntity(It.IsAny<IStockLegacyScopeKey>()))
            .Returns(MayBe.From(scope));

        var batch = ExistingBatch(10);
        _batchRepo.Setup(x => x.LoadByNaturalKey(BrgId, DoId))
            .Returns(MayBe.From(batch));

        _legacyRead.Setup(x => x.ListJournals(BrgId, DoId)).Returns(
        [
            new LegacyStockJournalReadModel(
                "BK00000001", BrgId, DoId, "LY001", "", "",
                QtyIn: 10, QtyOut: 0, Hpp: 1000, MovementKindString: "DO",
                TrsReffId: "DM1", TglMutasi: Watermark, TglEd: StockLedgerSentinel.EmptyDate),
            new LegacyStockJournalReadModel(
                "BK00000002", BrgId, DoId, "LY001", "", "",
                QtyIn: 0, QtyOut: 2, Hpp: 1000, MovementKindString: "PK",
                TrsReffId: "PK2", TglMutasi: Watermark, TglEd: StockLedgerSentinel.EmptyDate),
        ]);

        StockLegacyScopeModel? saved = null;
        _scopeRepo.Setup(x => x.SaveChanges(It.IsAny<StockLegacyScopeModel>()))
            .Callback<StockLegacyScopeModel>(s => saved = s);

        var result = await _sut.Handle(new CatchUpScopeFromLegacyCommand(BrgId, DoId, UserId), default);

        result.Outcome.Should().Be(CatchUpScopeOutcomeEnum.Success);
        result.AppliedJournalCount.Should().Be(1);
        batch.QtySisa.Should().Be(8);
        saved!.LastLegacyBukuId.Should().Be("BK00000002");
        saved.TglMutasiLast.Should().Be(Watermark);
    }

    [Fact]
    public async Task Handle_NoPending_ReturnsIdempotent()
    {
        var scope = AlignedScope("BK00000001");
        _scopeRepo.Setup(x => x.LoadEntity(It.IsAny<IStockLegacyScopeKey>()))
            .Returns(MayBe.From(scope));

        _legacyRead.Setup(x => x.ListJournals(BrgId, DoId)).Returns(
        [
            new LegacyStockJournalReadModel(
                "BK00000001", BrgId, DoId, "LY001", "", "",
                QtyIn: 10, QtyOut: 0, Hpp: 1000, MovementKindString: "DO",
                TrsReffId: "DM1", TglMutasi: Watermark, TglEd: StockLedgerSentinel.EmptyDate),
        ]);

        var result = await _sut.Handle(new CatchUpScopeFromLegacyCommand(BrgId, DoId, UserId), default);

        result.Outcome.Should().Be(CatchUpScopeOutcomeEnum.Idempotent);
        result.AppliedJournalCount.Should().Be(0);
        _mutasiRepo.Verify(x => x.Insert(It.IsAny<StockMovementModel>()), Times.Never);
        _batchRepo.Verify(x => x.SaveChanges(It.IsAny<StockBatchModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_BindingSkip_DoesNotDoubleQty()
    {
        var scope = AlignedScope("BK00000001");
        _scopeRepo.Setup(x => x.LoadEntity(It.IsAny<IStockLegacyScopeKey>()))
            .Returns(MayBe.From(scope));

        var batch = ExistingBatch(7);
        _batchRepo.Setup(x => x.LoadByNaturalKey(BrgId, DoId))
            .Returns(MayBe.From(batch));

        var after = Watermark.AddHours(1);
        _legacyRead.Setup(x => x.ListJournals(BrgId, DoId)).Returns(
        [
            new LegacyStockJournalReadModel(
                "BK00000002", BrgId, DoId, "LY001", "", "",
                QtyIn: 0, QtyOut: 3, Hpp: 1000, MovementKindString: "PK",
                TrsReffId: "PK2", TglMutasi: after, TglEd: StockLedgerSentinel.EmptyDate),
        ]);
        _bindingRepo.Setup(x => x.FindByLegacyBukuId("BK00000002"))
            .Returns(MayBe.From(StockLegacyBindingModel.CreateMutasiBuku(
                "STM000000002", "BK00000002", "PK2")));

        StockLegacyScopeModel? saved = null;
        _scopeRepo.Setup(x => x.SaveChanges(It.IsAny<StockLegacyScopeModel>()))
            .Callback<StockLegacyScopeModel>(s => saved = s);

        var result = await _sut.Handle(new CatchUpScopeFromLegacyCommand(BrgId, DoId, UserId), default);

        result.Outcome.Should().Be(CatchUpScopeOutcomeEnum.Success);
        result.AppliedJournalCount.Should().Be(0);
        batch.QtySisa.Should().Be(7);
        _mutasiRepo.Verify(x => x.Insert(It.IsAny<StockMovementModel>()), Times.Never);
        saved!.LastLegacyBukuId.Should().Be("BK00000002");
    }

    [Fact]
    public async Task Handle_Stale_ClearsToAligned_WhenNoPending()
    {
        var scope = AlignedScope("BK00000001");
        scope.MarkStale();
        _scopeRepo.Setup(x => x.LoadEntity(It.IsAny<IStockLegacyScopeKey>()))
            .Returns(MayBe.From(scope));

        _legacyRead.Setup(x => x.ListJournals(BrgId, DoId)).Returns(
        [
            new LegacyStockJournalReadModel(
                "BK00000001", BrgId, DoId, "LY001", "", "",
                QtyIn: 10, QtyOut: 0, Hpp: 1000, MovementKindString: "DO",
                TrsReffId: "DM1", TglMutasi: Watermark, TglEd: StockLedgerSentinel.EmptyDate),
        ]);

        StockLegacyScopeModel? saved = null;
        _scopeRepo.Setup(x => x.SaveChanges(It.IsAny<StockLegacyScopeModel>()))
            .Callback<StockLegacyScopeModel>(s => saved = s);

        var result = await _sut.Handle(new CatchUpScopeFromLegacyCommand(BrgId, DoId, UserId), default);

        result.Outcome.Should().Be(CatchUpScopeOutcomeEnum.Idempotent);
        result.AlignmentStatus.Should().Be(AlignmentStatusEnum.Aligned);
        saved.Should().NotBeNull();
        saved!.AlignmentStatus.Should().Be(AlignmentStatusEnum.Aligned);
    }

    [Fact]
    public async Task Handle_Stale_AppliesPending_ThenAligned()
    {
        var scope = AlignedScope("BK00000001");
        scope.MarkStale();
        _scopeRepo.Setup(x => x.LoadEntity(It.IsAny<IStockLegacyScopeKey>()))
            .Returns(MayBe.From(scope));

        var batch = ExistingBatch(10);
        _batchRepo.Setup(x => x.LoadByNaturalKey(BrgId, DoId))
            .Returns(MayBe.From(batch));

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

        var result = await _sut.Handle(new CatchUpScopeFromLegacyCommand(BrgId, DoId, UserId), default);

        result.Outcome.Should().Be(CatchUpScopeOutcomeEnum.Success);
        result.AlignmentStatus.Should().Be(AlignmentStatusEnum.Aligned);
        saved!.AlignmentStatus.Should().Be(AlignmentStatusEnum.Aligned);
        saved.LastLegacyBukuId.Should().Be("BK00000002");
    }

    [Fact]
    public async Task Handle_UnknownKind_MarksInconsistent()
    {
        var scope = AlignedScope("BK00000001");
        _scopeRepo.Setup(x => x.LoadEntity(It.IsAny<IStockLegacyScopeKey>()))
            .Returns(MayBe.From(scope));

        var batch = ExistingBatch(10);
        _batchRepo.Setup(x => x.LoadByNaturalKey(BrgId, DoId))
            .Returns(MayBe.From(batch));

        var after = Watermark.AddHours(1);
        _legacyRead.Setup(x => x.ListJournals(BrgId, DoId)).Returns(
        [
            new LegacyStockJournalReadModel(
                "BK00000002", BrgId, DoId, "LY001", "", "",
                QtyIn: 0, QtyOut: 1, Hpp: 1000, MovementKindString: "XX",
                TrsReffId: "XX2", TglMutasi: after, TglEd: StockLedgerSentinel.EmptyDate),
        ]);

        StockLegacyScopeModel? saved = null;
        _scopeRepo.Setup(x => x.SaveChanges(It.IsAny<StockLegacyScopeModel>()))
            .Callback<StockLegacyScopeModel>(s => saved = s);

        var result = await _sut.Handle(new CatchUpScopeFromLegacyCommand(BrgId, DoId, UserId), default);

        result.Outcome.Should().Be(CatchUpScopeOutcomeEnum.Inconsistent);
        result.InconsistencyReason.Should().Contain("XX");
        saved!.AlignmentStatus.Should().Be(AlignmentStatusEnum.Inconsistent);
        _mutasiRepo.Verify(x => x.Insert(It.IsAny<StockMovementModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_EmptyAligned_CreatesFirstBatchFromInbound()
    {
        var scope = EmptyAlignedScope();
        _scopeRepo.Setup(x => x.LoadEntity(It.IsAny<IStockLegacyScopeKey>()))
            .Returns(MayBe.From(scope));
        _batchRepo.Setup(x => x.LoadByNaturalKey(BrgId, DoId))
            .Returns(MayBe<StockBatchModel>.None);

        var tgl = new DateTime(2026, 4, 1, 12, 0, 0);
        _legacyRead.Setup(x => x.ListJournals(BrgId, DoId)).Returns(
        [
            new LegacyStockJournalReadModel(
                "BK00000001", BrgId, DoId, "LY001", "", "",
                QtyIn: 10, QtyOut: 0, Hpp: 1500, MovementKindString: "DO",
                TrsReffId: "DM1", TglMutasi: tgl, TglEd: StockLedgerSentinel.EmptyDate),
        ]);

        StockBatchModel? savedBatch = null;
        _batchRepo.Setup(x => x.SaveChanges(It.IsAny<StockBatchModel>()))
            .Callback<StockBatchModel>(b => savedBatch = b);

        StockLegacyScopeModel? savedScope = null;
        _scopeRepo.Setup(x => x.SaveChanges(It.IsAny<StockLegacyScopeModel>()))
            .Callback<StockLegacyScopeModel>(s => savedScope = s);

        var result = await _sut.Handle(new CatchUpScopeFromLegacyCommand(BrgId, DoId, UserId), default);

        result.Outcome.Should().Be(CatchUpScopeOutcomeEnum.Success);
        result.AppliedJournalCount.Should().Be(1);
        savedBatch.Should().NotBeNull();
        savedBatch!.QtySisa.Should().Be(10);
        savedBatch.Hpp.Should().Be(1500);
        _mutasiRepo.Verify(x => x.Insert(It.IsAny<StockMovementModel>()), Times.Once);
        _bindingRepo.Verify(x => x.Insert(It.IsAny<StockLegacyBindingModel>()), Times.Once);
        savedScope.Should().NotBeNull();
        savedScope!.AlignmentStatus.Should().Be(AlignmentStatusEnum.Aligned);
        savedScope.LastLegacyBukuId.Should().Be("BK00000001");
        savedScope.TglMutasiLast.Should().Be(tgl);
    }

    [Fact]
    public async Task Handle_BindingWithoutBatch_MarksInconsistent()
    {
        var scope = AlignedScope("BK00000001");
        _scopeRepo.Setup(x => x.LoadEntity(It.IsAny<IStockLegacyScopeKey>()))
            .Returns(MayBe.From(scope));
        _batchRepo.Setup(x => x.LoadByNaturalKey(BrgId, DoId))
            .Returns(MayBe<StockBatchModel>.None);

        var after = Watermark.AddHours(1);
        _legacyRead.Setup(x => x.ListJournals(BrgId, DoId)).Returns(
        [
            new LegacyStockJournalReadModel(
                "BK00000002", BrgId, DoId, "LY001", "", "",
                QtyIn: 0, QtyOut: 1, Hpp: 1000, MovementKindString: "PK",
                TrsReffId: "PK2", TglMutasi: after, TglEd: StockLedgerSentinel.EmptyDate),
        ]);
        _bindingRepo.Setup(x => x.FindByLegacyBukuId("BK00000002"))
            .Returns(MayBe.From(StockLegacyBindingModel.CreateMutasiBuku(
                "STM000000002", "BK00000002", "PK2")));

        StockLegacyScopeModel? saved = null;
        _scopeRepo.Setup(x => x.SaveChanges(It.IsAny<StockLegacyScopeModel>()))
            .Callback<StockLegacyScopeModel>(s => saved = s);

        var result = await _sut.Handle(new CatchUpScopeFromLegacyCommand(BrgId, DoId, UserId), default);

        result.Outcome.Should().Be(CatchUpScopeOutcomeEnum.Inconsistent);
        result.InconsistencyReason.Should().Contain("bindings without batch");
        saved!.AlignmentStatus.Should().Be(AlignmentStatusEnum.Inconsistent);
        _mutasiRepo.Verify(x => x.Insert(It.IsAny<StockMovementModel>()), Times.Never);
        _batchRepo.Verify(x => x.SaveChanges(It.IsAny<StockBatchModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_OutboundOnlyWithoutBatch_MarksInconsistent()
    {
        var scope = EmptyAlignedScope();
        _scopeRepo.Setup(x => x.LoadEntity(It.IsAny<IStockLegacyScopeKey>()))
            .Returns(MayBe.From(scope));
        _batchRepo.Setup(x => x.LoadByNaturalKey(BrgId, DoId))
            .Returns(MayBe<StockBatchModel>.None);

        var tgl = new DateTime(2026, 4, 1, 12, 0, 0);
        _legacyRead.Setup(x => x.ListJournals(BrgId, DoId)).Returns(
        [
            new LegacyStockJournalReadModel(
                "BK00000001", BrgId, DoId, "LY001", "", "",
                QtyIn: 0, QtyOut: 1, Hpp: 1000, MovementKindString: "PK",
                TrsReffId: "PK1", TglMutasi: tgl, TglEd: StockLedgerSentinel.EmptyDate),
        ]);

        StockLegacyScopeModel? saved = null;
        _scopeRepo.Setup(x => x.SaveChanges(It.IsAny<StockLegacyScopeModel>()))
            .Callback<StockLegacyScopeModel>(s => saved = s);

        var result = await _sut.Handle(new CatchUpScopeFromLegacyCommand(BrgId, DoId, UserId), default);

        result.Outcome.Should().Be(CatchUpScopeOutcomeEnum.Inconsistent);
        result.InconsistencyReason.Should().Contain("Failed to apply");
        saved!.AlignmentStatus.Should().Be(AlignmentStatusEnum.Inconsistent);
        _mutasiRepo.Verify(x => x.Insert(It.IsAny<StockMovementModel>()), Times.Never);
        _batchRepo.Verify(x => x.SaveChanges(It.IsAny<StockBatchModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NotAligned_ReturnsWithoutLegacyReads()
    {
        var scope = StockLegacyScopeModel.CreateNotAligned(BrgId, DoId);
        _scopeRepo.Setup(x => x.LoadEntity(It.IsAny<IStockLegacyScopeKey>()))
            .Returns(MayBe.From(scope));

        var result = await _sut.Handle(new CatchUpScopeFromLegacyCommand(BrgId, DoId, UserId), default);

        result.Outcome.Should().Be(CatchUpScopeOutcomeEnum.NotAligned);
        _legacyRead.Verify(x => x.ListJournals(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_PreExistingInconsistent_ReturnsWithoutLegacyReads()
    {
        var scope = StockLegacyScopeModel.CreateNotAligned(BrgId, DoId);
        scope.MarkInconsistent("prior");
        _scopeRepo.Setup(x => x.LoadEntity(It.IsAny<IStockLegacyScopeKey>()))
            .Returns(MayBe.From(scope));

        var result = await _sut.Handle(new CatchUpScopeFromLegacyCommand(BrgId, DoId, UserId), default);

        result.Outcome.Should().Be(CatchUpScopeOutcomeEnum.Inconsistent);
        result.InconsistencyReason.Should().Be("prior");
        _legacyRead.Verify(x => x.ListJournals(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}
