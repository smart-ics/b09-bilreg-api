using Bilreg.Application.InventoryContext.StockLedgerFeature;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.InventoryContext.StockLedgerFeature;

public class HydrateScopeFromLegacyHandlerTest
{
    private readonly Mock<ILegacyStockReadPort> _legacyRead = new();
    private readonly Mock<IStockBatchRepo> _batchRepo = new();
    private readonly Mock<IStockMutasiRepo> _mutasiRepo = new();
    private readonly Mock<IStockLegacyScopeRepo> _scopeRepo = new();
    private readonly Mock<IStockLegacyBindingRepo> _bindingRepo = new();
    private readonly HydrateScopeFromLegacyHandler _sut;

    private const string BrgId = "BRG0000000001";
    private const string DoId = "DO00000001";
    private const string UserId = "U1";

    public HydrateScopeFromLegacyHandlerTest()
    {
        var replayer = new LegacyScopeJournalReplayer(
            _batchRepo.Object,
            _mutasiRepo.Object,
            _bindingRepo.Object,
            _scopeRepo.Object);

        _sut = new HydrateScopeFromLegacyHandler(
            _legacyRead.Object,
            _batchRepo.Object,
            _scopeRepo.Object,
            replayer);

        _scopeRepo
            .Setup(x => x.LoadEntity(It.IsAny<IStockLegacyScopeKey>()))
            .Returns(MayBe<StockLegacyScopeModel>.None);
        _batchRepo
            .Setup(x => x.LoadByNaturalKey(BrgId, DoId))
            .Returns(MayBe<StockBatchModel>.None);
        _bindingRepo
            .Setup(x => x.FindByLegacyBukuId(It.IsAny<string>()))
            .Returns(MayBe<StockLegacyBindingModel>.None);
    }

    [Fact]
    public async Task Handle_EmptyJournals_MarksAlignedEmpty()
    {
        _legacyRead.Setup(x => x.ListJournals(BrgId, DoId)).Returns([]);

        StockLegacyScopeModel? saved = null;
        _scopeRepo.Setup(x => x.SaveChanges(It.IsAny<StockLegacyScopeModel>()))
            .Callback<StockLegacyScopeModel>(s => saved = s);

        var result = await _sut.Handle(new HydrateScopeFromLegacyCommand(BrgId, DoId, UserId), default);

        result.Outcome.Should().Be(HydrateScopeOutcomeEnum.Success);
        result.AlignmentStatus.Should().Be(AlignmentStatusEnum.Aligned);
        result.AppliedJournalCount.Should().Be(0);
        saved.Should().NotBeNull();
        saved!.AlignmentStatus.Should().Be(AlignmentStatusEnum.Aligned);
        saved.LastLegacyBukuId.Should().BeEmpty();
        _batchRepo.Verify(x => x.SaveChanges(It.IsAny<StockBatchModel>()), Times.Never);
        _mutasiRepo.Verify(x => x.Insert(It.IsAny<StockMovementModel>()), Times.Never);
        _legacyRead.Verify(x => x.ListBalances(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DoInbound_EstablishesBatchLokasiMutasiBinding()
    {
        var tgl = new DateTime(2026, 3, 1, 10, 0, 0);
        _legacyRead.Setup(x => x.ListJournals(BrgId, DoId)).Returns(
        [
            new LegacyStockJournalReadModel(
                "BK00000001", BrgId, DoId, "LY001", "PO00000001", "",
                QtyIn: 10, QtyOut: 0, Hpp: 1000, MovementKindString: "DO",
                TrsReffId: "DM00000001", TglMutasi: tgl, TglEd: StockLedgerSentinel.EmptyDate)
        ]);

        StockBatchModel? savedBatch = null;
        _batchRepo.Setup(x => x.SaveChanges(It.IsAny<StockBatchModel>()))
            .Callback<StockBatchModel>(b => savedBatch = b);
        StockMovementModel? insertedMutasi = null;
        _mutasiRepo.Setup(x => x.Insert(It.IsAny<StockMovementModel>()))
            .Callback<StockMovementModel>(m => insertedMutasi = m);
        StockLegacyBindingModel? insertedBinding = null;
        _bindingRepo.Setup(x => x.Insert(It.IsAny<StockLegacyBindingModel>()))
            .Callback<StockLegacyBindingModel>(b => insertedBinding = b);

        var result = await _sut.Handle(new HydrateScopeFromLegacyCommand(BrgId, DoId, UserId), default);

        result.Outcome.Should().Be(HydrateScopeOutcomeEnum.Success);
        result.AppliedJournalCount.Should().Be(1);
        savedBatch.Should().NotBeNull();
        savedBatch!.QtySisa.Should().Be(10);
        savedBatch.ListLokasi.Should().ContainSingle(x => x.LayananId == "LY001" && x.QtySisa == 10);
        insertedMutasi.Should().NotBeNull();
        insertedMutasi!.MovementKind.Should().Be(MovementKindEnum.GoodsReceipt);
        insertedMutasi.QtyIn.Should().Be(10);
        insertedBinding.Should().NotBeNull();
        insertedBinding!.LegacyBukuId.Should().Be("BK00000001");
        insertedBinding.BindingKind.Should().Be(BindingKindEnum.MutasiBuku);
    }

    [Fact]
    public async Task Handle_InboundThenOutbound_RetainsDepletedLokasi()
    {
        var t1 = new DateTime(2026, 3, 1, 10, 0, 0);
        var t2 = new DateTime(2026, 3, 2, 11, 0, 0);
        _legacyRead.Setup(x => x.ListJournals(BrgId, DoId)).Returns(
        [
            new LegacyStockJournalReadModel(
                "BK00000001", BrgId, DoId, "LY001", "", "",
                QtyIn: 5, QtyOut: 0, Hpp: 500, MovementKindString: "DO",
                TrsReffId: "DM1", TglMutasi: t1, TglEd: StockLedgerSentinel.EmptyDate),
            new LegacyStockJournalReadModel(
                "BK00000002", BrgId, DoId, "LY001", "", "",
                QtyIn: 0, QtyOut: 5, Hpp: 500, MovementKindString: "PK",
                TrsReffId: "PK1", TglMutasi: t2, TglEd: StockLedgerSentinel.EmptyDate)
        ]);

        StockBatchModel? savedBatch = null;
        _batchRepo.Setup(x => x.SaveChanges(It.IsAny<StockBatchModel>()))
            .Callback<StockBatchModel>(b => savedBatch = b);

        var result = await _sut.Handle(new HydrateScopeFromLegacyCommand(BrgId, DoId, UserId), default);

        result.Outcome.Should().Be(HydrateScopeOutcomeEnum.Success);
        result.AppliedJournalCount.Should().Be(2);
        savedBatch.Should().NotBeNull();
        savedBatch!.QtySisa.Should().Be(0);
        savedBatch.ListLokasi.Should().ContainSingle(x => x.QtySisa == 0);
        _mutasiRepo.Verify(x => x.Insert(It.IsAny<StockMovementModel>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_AlreadyAligned_ReturnsIdempotentWithoutReads()
    {
        var aligned = StockLegacyScopeModel.CreateNotAligned(BrgId, DoId);
        aligned.MarkAligned(new DateTime(2026, 3, 1), "BK00000001", DateTime.Now);
        _scopeRepo.Setup(x => x.LoadEntity(It.IsAny<IStockLegacyScopeKey>()))
            .Returns(MayBe.From(aligned));

        var result = await _sut.Handle(new HydrateScopeFromLegacyCommand(BrgId, DoId, UserId), default);

        result.Outcome.Should().Be(HydrateScopeOutcomeEnum.Idempotent);
        result.AppliedJournalCount.Should().Be(0);
        _legacyRead.Verify(x => x.ListJournals(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _mutasiRepo.Verify(x => x.Insert(It.IsAny<StockMovementModel>()), Times.Never);
        _batchRepo.Verify(x => x.SaveChanges(It.IsAny<StockBatchModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_BindingExists_WithBatch_SkipsJournalWithoutDoubleQty()
    {
        var tgl = new DateTime(2026, 3, 1, 10, 0, 0);
        _legacyRead.Setup(x => x.ListJournals(BrgId, DoId)).Returns(
        [
            new LegacyStockJournalReadModel(
                "BK00000001", BrgId, DoId, "LY001", "", "",
                QtyIn: 10, QtyOut: 0, Hpp: 1000, MovementKindString: "DO",
                TrsReffId: "DM1", TglMutasi: tgl, TglEd: StockLedgerSentinel.EmptyDate)
        ]);
        _bindingRepo.Setup(x => x.FindByLegacyBukuId("BK00000001"))
            .Returns(MayBe.From(StockLegacyBindingModel.CreateMutasiBuku(
                "STM000000001", "BK00000001", "DM1")));

        var existing = StockBatchModel.Create(BrgId, DoId, hpp: 1000, tglMasuk: tgl);
        existing.IncreaseLokasi("LY001", StockLedgerSentinel.EmptyDate, 10);
        _batchRepo.Setup(x => x.LoadByNaturalKey(BrgId, DoId))
            .Returns(MayBe.From(existing));

        StockLegacyScopeModel? saved = null;
        _scopeRepo.Setup(x => x.SaveChanges(It.IsAny<StockLegacyScopeModel>()))
            .Callback<StockLegacyScopeModel>(s => saved = s);

        var result = await _sut.Handle(new HydrateScopeFromLegacyCommand(BrgId, DoId, UserId), default);

        result.Outcome.Should().Be(HydrateScopeOutcomeEnum.Success);
        result.AlignmentStatus.Should().Be(AlignmentStatusEnum.Aligned);
        result.AppliedJournalCount.Should().Be(0);
        existing.QtySisa.Should().Be(10);
        _mutasiRepo.Verify(x => x.Insert(It.IsAny<StockMovementModel>()), Times.Never);
        _batchRepo.Verify(x => x.SaveChanges(It.IsAny<StockBatchModel>()), Times.Never);
        saved.Should().NotBeNull();
        saved!.AlignmentStatus.Should().Be(AlignmentStatusEnum.Aligned);
        saved.LastLegacyBukuId.Should().Be("BK00000001");
    }

    [Fact]
    public async Task Handle_OrphanBindingWithoutBatch_RecoversBatchInMemoryWithoutReinsertingBinding()
    {
        var tgl = new DateTime(2026, 3, 1, 10, 0, 0);
        _legacyRead.Setup(x => x.ListJournals(BrgId, DoId)).Returns(
        [
            new LegacyStockJournalReadModel(
                "BK00000001", BrgId, DoId, "LY001", "", "",
                QtyIn: 10, QtyOut: 0, Hpp: 1000, MovementKindString: "DO",
                TrsReffId: "DM1", TglMutasi: tgl, TglEd: StockLedgerSentinel.EmptyDate)
        ]);
        _bindingRepo.Setup(x => x.FindByLegacyBukuId("BK00000001"))
            .Returns(MayBe.From(StockLegacyBindingModel.CreateMutasiBuku(
                "STM000000001", "BK00000001", "DM1")));

        StockBatchModel? savedBatch = null;
        _batchRepo.Setup(x => x.SaveChanges(It.IsAny<StockBatchModel>()))
            .Callback<StockBatchModel>(b => savedBatch = b);
        StockLegacyScopeModel? saved = null;
        _scopeRepo.Setup(x => x.SaveChanges(It.IsAny<StockLegacyScopeModel>()))
            .Callback<StockLegacyScopeModel>(s => saved = s);

        var result = await _sut.Handle(new HydrateScopeFromLegacyCommand(BrgId, DoId, UserId), default);

        result.Outcome.Should().Be(HydrateScopeOutcomeEnum.Success);
        result.AlignmentStatus.Should().Be(AlignmentStatusEnum.Aligned);
        result.AppliedJournalCount.Should().Be(0);
        savedBatch.Should().NotBeNull();
        savedBatch!.QtySisa.Should().Be(10);
        savedBatch.ListLokasi.Should().ContainSingle(x => x.LayananId == "LY001" && x.QtySisa == 10);
        saved.Should().NotBeNull();
        saved!.AlignmentStatus.Should().Be(AlignmentStatusEnum.Aligned);
        _mutasiRepo.Verify(x => x.Insert(It.IsAny<StockMovementModel>()), Times.Never);
        _bindingRepo.Verify(x => x.Insert(It.IsAny<StockLegacyBindingModel>()), Times.Never);
        _batchRepo.Verify(x => x.SaveChanges(It.IsAny<StockBatchModel>()), Times.Once);
    }

    [Fact]
    public async Task Handle_OrphanBindingWithoutBatch_WithRemainingJournals_RecoversAndAppliesRemaining()
    {
        var t1 = new DateTime(2026, 3, 1, 10, 0, 0);
        var t2 = new DateTime(2026, 3, 2, 11, 0, 0);
        _legacyRead.Setup(x => x.ListJournals(BrgId, DoId)).Returns(
        [
            new LegacyStockJournalReadModel(
                "BK00000001", BrgId, DoId, "LY001", "", "",
                QtyIn: 10, QtyOut: 0, Hpp: 1000, MovementKindString: "DO",
                TrsReffId: "DM1", TglMutasi: t1, TglEd: StockLedgerSentinel.EmptyDate),
            new LegacyStockJournalReadModel(
                "BK00000002", BrgId, DoId, "LY001", "", "",
                QtyIn: 5, QtyOut: 0, Hpp: 1000, MovementKindString: "DO",
                TrsReffId: "DM2", TglMutasi: t2, TglEd: StockLedgerSentinel.EmptyDate)
        ]);
        _bindingRepo.Setup(x => x.FindByLegacyBukuId("BK00000001"))
            .Returns(MayBe.From(StockLegacyBindingModel.CreateMutasiBuku(
                "STM000000001", "BK00000001", "DM1")));

        StockBatchModel? savedBatch = null;
        _batchRepo.Setup(x => x.SaveChanges(It.IsAny<StockBatchModel>()))
            .Callback<StockBatchModel>(b => savedBatch = b);
        StockLegacyScopeModel? saved = null;
        _scopeRepo.Setup(x => x.SaveChanges(It.IsAny<StockLegacyScopeModel>()))
            .Callback<StockLegacyScopeModel>(s => saved = s);

        var result = await _sut.Handle(new HydrateScopeFromLegacyCommand(BrgId, DoId, UserId), default);

        result.Outcome.Should().Be(HydrateScopeOutcomeEnum.Success);
        result.AlignmentStatus.Should().Be(AlignmentStatusEnum.Aligned);
        result.AppliedJournalCount.Should().Be(1);
        savedBatch.Should().NotBeNull();
        savedBatch!.QtySisa.Should().Be(15);
        saved.Should().NotBeNull();
        saved!.AlignmentStatus.Should().Be(AlignmentStatusEnum.Aligned);
        saved.LastLegacyBukuId.Should().Be("BK00000002");
        _mutasiRepo.Verify(x => x.Insert(It.IsAny<StockMovementModel>()), Times.Once);
        _bindingRepo.Verify(x => x.Insert(It.IsAny<StockLegacyBindingModel>()), Times.Once);
        _batchRepo.Verify(x => x.SaveChanges(It.IsAny<StockBatchModel>()), Times.Once);
    }

    [Fact]
    public async Task Handle_OutboundBeforeInbound_MarksInconsistent()
    {
        var tgl = new DateTime(2026, 3, 1, 10, 0, 0);
        _legacyRead.Setup(x => x.ListJournals(BrgId, DoId)).Returns(
        [
            new LegacyStockJournalReadModel(
                "BK00000001", BrgId, DoId, "LY001", "", "",
                QtyIn: 0, QtyOut: 5, Hpp: 500, MovementKindString: "PK",
                TrsReffId: "PK1", TglMutasi: tgl, TglEd: StockLedgerSentinel.EmptyDate)
        ]);

        StockLegacyScopeModel? saved = null;
        _scopeRepo.Setup(x => x.SaveChanges(It.IsAny<StockLegacyScopeModel>()))
            .Callback<StockLegacyScopeModel>(s => saved = s);

        var result = await _sut.Handle(new HydrateScopeFromLegacyCommand(BrgId, DoId, UserId), default);

        result.Outcome.Should().Be(HydrateScopeOutcomeEnum.Inconsistent);
        result.AlignmentStatus.Should().Be(AlignmentStatusEnum.Inconsistent);
        result.InconsistencyReason.Should().Contain("BK00000001");
        result.InconsistencyReason.Should().Contain("Location Stock Balance not found");
        saved.Should().NotBeNull();
        saved!.AlignmentStatus.Should().Be(AlignmentStatusEnum.Inconsistent);
        _mutasiRepo.Verify(x => x.Insert(It.IsAny<StockMovementModel>()), Times.Never);
        _batchRepo.Verify(x => x.SaveChanges(It.IsAny<StockBatchModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UnknownKind_MarksInconsistent()
    {
        var tgl = new DateTime(2026, 3, 1, 10, 0, 0);
        _legacyRead.Setup(x => x.ListJournals(BrgId, DoId)).Returns(
        [
            new LegacyStockJournalReadModel(
                "BK00000001", BrgId, DoId, "LY001", "", "",
                QtyIn: 10, QtyOut: 0, Hpp: 1000, MovementKindString: "XX",
                TrsReffId: "DM1", TglMutasi: tgl, TglEd: StockLedgerSentinel.EmptyDate)
        ]);

        StockLegacyScopeModel? saved = null;
        _scopeRepo.Setup(x => x.SaveChanges(It.IsAny<StockLegacyScopeModel>()))
            .Callback<StockLegacyScopeModel>(s => saved = s);

        var result = await _sut.Handle(new HydrateScopeFromLegacyCommand(BrgId, DoId, UserId), default);

        result.Outcome.Should().Be(HydrateScopeOutcomeEnum.Inconsistent);
        result.AlignmentStatus.Should().Be(AlignmentStatusEnum.Inconsistent);
        result.InconsistencyReason.Should().Contain("XX");
        saved.Should().NotBeNull();
        saved!.AlignmentStatus.Should().Be(AlignmentStatusEnum.Inconsistent);
        _mutasiRepo.Verify(x => x.Insert(It.IsAny<StockMovementModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_PreExistingInconsistent_ReturnsWithoutReplay()
    {
        var scope = StockLegacyScopeModel.CreateNotAligned(BrgId, DoId);
        scope.MarkInconsistent("prior");
        _scopeRepo.Setup(x => x.LoadEntity(It.IsAny<IStockLegacyScopeKey>()))
            .Returns(MayBe.From(scope));

        var result = await _sut.Handle(new HydrateScopeFromLegacyCommand(BrgId, DoId, UserId), default);

        result.Outcome.Should().Be(HydrateScopeOutcomeEnum.Inconsistent);
        result.InconsistencyReason.Should().Be("prior");
        _legacyRead.Verify(x => x.ListJournals(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Stale_ReturnsStaleNeedsCatchUp()
    {
        var scope = StockLegacyScopeModel.CreateNotAligned(BrgId, DoId);
        scope.MarkAligned(DateTime.Now, "BK1", DateTime.Now);
        scope.MarkStale();
        _scopeRepo.Setup(x => x.LoadEntity(It.IsAny<IStockLegacyScopeKey>()))
            .Returns(MayBe.From(scope));

        var result = await _sut.Handle(new HydrateScopeFromLegacyCommand(BrgId, DoId, UserId), default);

        result.Outcome.Should().Be(HydrateScopeOutcomeEnum.StaleNeedsCatchUp);
        _legacyRead.Verify(x => x.ListJournals(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_FailureAtKthJournal_DoesNotInsertAnyMutasiOrBinding()
    {
        var t1 = new DateTime(2026, 3, 1, 10, 0, 0);
        var t2 = new DateTime(2026, 3, 2, 10, 0, 0);
        var t3 = new DateTime(2026, 3, 3, 10, 0, 0);
        _legacyRead.Setup(x => x.ListJournals(BrgId, DoId)).Returns(
        [
            new LegacyStockJournalReadModel(
                "BK00000001", BrgId, DoId, "LY001", "", "",
                QtyIn: 10, QtyOut: 0, Hpp: 1000, MovementKindString: "DO",
                TrsReffId: "DM1", TglMutasi: t1, TglEd: StockLedgerSentinel.EmptyDate),
            new LegacyStockJournalReadModel(
                "BK00000002", BrgId, DoId, "LY001", "", "",
                QtyIn: 0, QtyOut: 2, Hpp: 1000, MovementKindString: "PK",
                TrsReffId: "PK1", TglMutasi: t2, TglEd: StockLedgerSentinel.EmptyDate),
            new LegacyStockJournalReadModel(
                "BK00000003", BrgId, DoId, "LY001", "", "",
                QtyIn: 0, QtyOut: 99, Hpp: 1000, MovementKindString: "PK",
                TrsReffId: "PK2", TglMutasi: t3, TglEd: StockLedgerSentinel.EmptyDate),
        ]);

        var result = await _sut.Handle(new HydrateScopeFromLegacyCommand(BrgId, DoId, UserId), default);

        result.Outcome.Should().Be(HydrateScopeOutcomeEnum.Inconsistent);
        result.InconsistencyReason.Should().Contain("BK00000003");
        _mutasiRepo.Verify(x => x.Insert(It.IsAny<StockMovementModel>()), Times.Never);
        _bindingRepo.Verify(x => x.Insert(It.IsAny<StockLegacyBindingModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_MultiLineSameTrsReffIdAndLocation_DoMtOutMtInDu_Succeeds()
    {
        var t1 = new DateTime(2026, 3, 1, 10, 0, 0);
        var t2 = new DateTime(2026, 3, 2, 10, 0, 0);
        var t3 = new DateTime(2026, 3, 3, 10, 0, 0);
        var t4 = new DateTime(2026, 3, 4, 10, 0, 0);
        _legacyRead.Setup(x => x.ListJournals(BrgId, DoId)).Returns(
        [
            new LegacyStockJournalReadModel("BK00000001", BrgId, DoId, "LY001", "", "", 10, 0, 1000, "DO", "DM1", t1, StockLedgerSentinel.EmptyDate),
            new LegacyStockJournalReadModel("BK00000002", BrgId, DoId, "LY001", "", "", 5, 0, 1000, "DO", "DM1", t1, StockLedgerSentinel.EmptyDate),
            new LegacyStockJournalReadModel("BK00000003", BrgId, DoId, "LY001", "", "", 0, 3, 1000, "MT_OUT", "MT1", t2, StockLedgerSentinel.EmptyDate),
            new LegacyStockJournalReadModel("BK00000004", BrgId, DoId, "LY001", "", "", 0, 2, 1000, "MT_OUT", "MT1", t2, StockLedgerSentinel.EmptyDate),
            new LegacyStockJournalReadModel("BK00000005", BrgId, DoId, "LY002", "", "", 3, 0, 1000, "MT_IN", "MT1", t3, StockLedgerSentinel.EmptyDate),
            new LegacyStockJournalReadModel("BK00000006", BrgId, DoId, "LY002", "", "", 2, 0, 1000, "MT_IN", "MT1", t3, StockLedgerSentinel.EmptyDate),
            new LegacyStockJournalReadModel("BK00000007", BrgId, DoId, "LY002", "", "", 0, 1, 1000, "DU", "DU1", t4, StockLedgerSentinel.EmptyDate),
            new LegacyStockJournalReadModel("BK00000008", BrgId, DoId, "LY002", "", "", 0, 2, 1000, "DU", "DU1", t4, StockLedgerSentinel.EmptyDate),
        ]);

        var mutasis = new List<StockMovementModel>();
        _mutasiRepo.Setup(x => x.Insert(It.IsAny<StockMovementModel>())).Callback<StockMovementModel>(mutasis.Add);
        var bindings = new List<StockLegacyBindingModel>();
        _bindingRepo.Setup(x => x.Insert(It.IsAny<StockLegacyBindingModel>())).Callback<StockLegacyBindingModel>(bindings.Add);

        var result = await _sut.Handle(new HydrateScopeFromLegacyCommand(BrgId, DoId, UserId), default);

        result.Outcome.Should().Be(HydrateScopeOutcomeEnum.Success);
        result.AppliedJournalCount.Should().Be(8);
        mutasis.Select(m => m.StokMutasiId).Distinct(StringComparer.Ordinal).Should().HaveCount(8);
        bindings.Select(b => b.BindingId).Distinct(StringComparer.Ordinal).Should().HaveCount(8);
    }

    [Fact]
    public async Task Handle_ManyConsecutiveJournals_ProducesUniqueIdsWithoutCollision()
    {
        var t0 = new DateTime(2026, 3, 1, 10, 0, 0);
        var journals = new List<LegacyStockJournalReadModel>
        {
            new("BK00000001", BrgId, DoId, "LY001", "", "", 100, 0, 1000, "DO", "DM1", t0, StockLedgerSentinel.EmptyDate)
        };
        for (var i = 2; i <= 50; i++)
        {
            journals.Add(new LegacyStockJournalReadModel(
                $"BK{i:D8}", BrgId, DoId, "LY001", "", "", 0, 1, 1000, "PK", $"PK{i:D6}", t0.AddSeconds(i), StockLedgerSentinel.EmptyDate));
        }

        _legacyRead.Setup(x => x.ListJournals(BrgId, DoId)).Returns(journals);
        var mutasis = new List<StockMovementModel>();
        _mutasiRepo.Setup(x => x.Insert(It.IsAny<StockMovementModel>())).Callback<StockMovementModel>(mutasis.Add);
        var bindings = new List<StockLegacyBindingModel>();
        _bindingRepo.Setup(x => x.Insert(It.IsAny<StockLegacyBindingModel>())).Callback<StockLegacyBindingModel>(bindings.Add);

        var result = await _sut.Handle(new HydrateScopeFromLegacyCommand(BrgId, DoId, UserId), default);

        result.Outcome.Should().Be(HydrateScopeOutcomeEnum.Success);
        result.AppliedJournalCount.Should().Be(50);
        mutasis.Select(m => m.StokMutasiId).Distinct(StringComparer.Ordinal).Should().HaveCount(50);
        bindings.Select(b => b.BindingId).Distinct(StringComparer.Ordinal).Should().HaveCount(50);
    }
}
