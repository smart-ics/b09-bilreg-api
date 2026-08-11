using Bilreg.Application.InventoryContext.StockLedgerFeature;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Options;
using Moq;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.InventoryContext.StockLedgerFeature;

[Collection(StockLedgerSqlCollection.Name)]
public class PostStockTransferConsequenceHandlerTest
{
    private readonly IOptions<DatabaseOptions> _db = ConnStringHelper.GetTestEnv();
    private readonly LegacyStockWriterPort _writer;
    private readonly LegacyStockReadPort _legacyRead;
    private readonly StockBatchRepo _batchRepo;
    private readonly StockMutasiRepo _mutasiRepo;
    private readonly StockLegacyBindingRepo _bindingRepo;
    private readonly StockLegacyScopeRepo _scopeRepo;

    private const string BrgId = "BRGSTLE00001";
    private const string SourceLayananId = "LYSRC";
    private const string DestLayananId = "LYDST";
    private const string UserId = "tester";
    private static readonly DateTime TglMutasi = new(2026, 7, 1, 10, 0, 0);
    private static readonly DateTime TglEd = new(2028, 7, 15);

    public PostStockTransferConsequenceHandlerTest()
    {
        _writer = new LegacyStockWriterPort(_db);
        _legacyRead = new LegacyStockReadPort(_db);
        _batchRepo = new StockBatchRepo(new StockBatchDal(_db), new StokLokasiDal(_db));
        _mutasiRepo = new StockMutasiRepo(new StokMutasiDal(_db));
        _bindingRepo = new StockLegacyBindingRepo(new StokLegacyBindingDal(_db));
        _scopeRepo = new StockLegacyScopeRepo(new StokLegacyScopeDal(_db));
    }

    [Fact]
    public async Task Handle_Success_TransfersWithEdPreserved()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        await SeedReceipt("DOSTLE001", qty: 10, hpp: 100m, trsReffId: "TRSSEED001");

        var sut = CreateTransferHandler(gateOutcome: EnsureFreshnessOutcomeEnum.Success);
        var result = await sut.Handle(
            NewTransferCommand(trsReffId: "TRSXFER001", qty: 4),
            default);

        result.Outcome.Should().Be(PostStockTransferOutcomeEnum.Success);
        result.Lines.Should().ContainSingle();
        var line = result.Lines[0];
        line.BrgMasukReffId.Should().Be("DOSTLE001");
        line.LegacyBukuOutId.Should().HaveLength(10).And.StartWith("BK");
        line.LegacyBukuInId.Should().HaveLength(10).And.StartWith("BK");
        line.LegacyStokInId.Should().HaveLength(10).And.StartWith("ST");

        var batch = _batchRepo.LoadByNaturalKey(BrgId, "DOSTLE001");
        batch.HasValue.Should().BeTrue();
        batch.Value.QtySisa.Should().Be(10);
        batch.Value.ListLokasi.Should().Contain(x =>
            x.LayananId == SourceLayananId && x.TglEd == TglEd && x.QtySisa == 6);
        batch.Value.ListLokasi.Should().Contain(x =>
            x.LayananId == DestLayananId && x.TglEd == TglEd && x.QtySisa == 4);

        var mutasi = _mutasiRepo.ListByTrsReffId("TRSXFER001").ToList();
        mutasi.Should().HaveCount(2);
        mutasi.Should().ContainSingle(x =>
            x.MovementKind == MovementKindEnum.TransferOut && x.QtyOut == 4 && x.TglEd == TglEd);
        mutasi.Should().ContainSingle(x =>
            x.MovementKind == MovementKindEnum.TransferIn && x.QtyIn == 4 && x.TglEd == TglEd);

        _legacyRead.ListJournals(BrgId, "DOSTLE001")
            .Should().Contain(x => x.LegacyBukuId == line.LegacyBukuOutId
                && x.MovementKindString == "MT_OUT" && x.QtyOut == 4)
            .And.Contain(x => x.LegacyBukuId == line.LegacyBukuInId
                && x.MovementKindString == "MT_IN" && x.QtyIn == 4);
        _legacyRead.ListBalances(BrgId, "DOSTLE001")
            .Should().Contain(x => x.LegacyStokId == line.LegacyStokInId
                && x.LayananId == DestLayananId && x.QtySisa == 4 && x.TglEd == TglEd);

        var expectedWatermarkBukuId = LegacyWatermarkHelper.IsAfterWatermark(
                TglMutasi, line.LegacyBukuInId, TglMutasi, line.LegacyBukuOutId)
            ? line.LegacyBukuInId
            : line.LegacyBukuOutId;
        var scope = _scopeRepo.LoadEntity(StockLegacyScopeModel.Key(BrgId, "DOSTLE001"));
        scope.HasValue.Should().BeTrue();
        scope.Value.LastLegacyBukuId.Should().Be(expectedWatermarkBukuId);
        scope.Value.TglMutasiLast.Should().Be(TglMutasi);
        LegacyWatermarkHelper.IsAfterWatermark(
                TglMutasi, line.LegacyBukuOutId, scope.Value.TglMutasiLast, scope.Value.LastLegacyBukuId)
            .Should().BeFalse();
        LegacyWatermarkHelper.IsAfterWatermark(
                TglMutasi, line.LegacyBukuInId, scope.Value.TglMutasiLast, scope.Value.LastLegacyBukuId)
            .Should().BeFalse();
    }

    [Fact]
    public async Task Handle_MultiBalance_SplitsAcrossTwoDOs()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        // Earlier TglMasuk via earlier receipt TglMutasi so FIFO/FEFO prefers first DO.
        await SeedReceipt("DOSTLE002A", qty: 6, hpp: 50m, trsReffId: "TRSSEED02A",
            tglMutasi: new DateTime(2026, 6, 1, 9, 0, 0));
        await SeedReceipt("DOSTLE002B", qty: 8, hpp: 60m, trsReffId: "TRSSEED02B",
            tglMutasi: new DateTime(2026, 6, 2, 9, 0, 0));

        var sut = CreateTransferHandler(gateOutcome: EnsureFreshnessOutcomeEnum.Success);
        var result = await sut.Handle(
            NewTransferCommand(trsReffId: "TRSXFER002", qty: 10),
            default);

        result.Outcome.Should().Be(PostStockTransferOutcomeEnum.Success);
        result.Lines.Should().HaveCount(2);
        result.Lines.Sum(x =>
        {
            var outMutasi = _mutasiRepo.ListByTrsReffId("TRSXFER002")
                .Single(m => m.StokMutasiId == x.StokMutasiOutId);
            return outMutasi.QtyOut;
        }).Should().Be(10);

        var batchA = _batchRepo.LoadByNaturalKey(BrgId, "DOSTLE002A").Value;
        var batchB = _batchRepo.LoadByNaturalKey(BrgId, "DOSTLE002B").Value;
        batchA.QtySisa.Should().Be(6);
        batchB.QtySisa.Should().Be(8);
        (batchA.ListLokasi.Where(x => x.LayananId == DestLayananId).Sum(x => x.QtySisa)
            + batchB.ListLokasi.Where(x => x.LayananId == DestLayananId).Sum(x => x.QtySisa))
            .Should().Be(10);

        _mutasiRepo.ListByTrsReffId("TRSXFER002")
            .Count(x => x.MovementKind == MovementKindEnum.TransferOut).Should().Be(2);
        _mutasiRepo.ListByTrsReffId("TRSXFER002")
            .Count(x => x.MovementKind == MovementKindEnum.TransferIn).Should().Be(2);
    }

    [Fact]
    public async Task Handle_InsufficientStock_RejectsWithoutPersist()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        await SeedReceipt("DOSTLE003", qty: 3, hpp: 10m, trsReffId: "TRSSEED003");

        var uowMock = new Mock<IStockConsequenceUnitOfWork>();
        var sut = CreateTransferHandler(
            gateOutcome: EnsureFreshnessOutcomeEnum.Success,
            uow: uowMock.Object);

        var result = await sut.Handle(
            NewTransferCommand(trsReffId: "TRSXFER003", qty: 10),
            default);

        result.Outcome.Should().Be(PostStockTransferOutcomeEnum.InsufficientStock);
        result.ShortfallQty.Should().Be(7);
        result.Lines.Should().BeEmpty();
        uowMock.Verify(x => x.Commit(It.IsAny<StockConsequenceDraft>()), Times.Never);

        var batch = _batchRepo.LoadByNaturalKey(BrgId, "DOSTLE003").Value;
        batch.ListLokasi.Should().ContainSingle(x =>
            x.LayananId == SourceLayananId && x.QtySisa == 3);
        batch.ListLokasi.Should().NotContain(x => x.LayananId == DestLayananId);
        _mutasiRepo.ListByTrsReffId("TRSXFER003").Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_IdempotentRetry_DoesNotDoubleQty()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        await SeedReceipt("DOSTLE004", qty: 10, hpp: 100m, trsReffId: "TRSSEED004");

        var sut = CreateTransferHandler(gateOutcome: EnsureFreshnessOutcomeEnum.Success);
        var cmd = NewTransferCommand(trsReffId: "TRSXFER004", qty: 5);

        var first = await sut.Handle(cmd, default);
        first.Outcome.Should().Be(PostStockTransferOutcomeEnum.Success);

        var second = await sut.Handle(cmd, default);
        second.Outcome.Should().Be(PostStockTransferOutcomeEnum.Idempotent);
        second.Lines.Should().HaveCount(1);
        second.Lines[0].StokMutasiOutId.Should().Be(first.Lines[0].StokMutasiOutId);
        second.Lines[0].StokMutasiInId.Should().Be(first.Lines[0].StokMutasiInId);
        second.Lines[0].LegacyBukuOutId.Should().Be(first.Lines[0].LegacyBukuOutId);

        var batch = _batchRepo.LoadByNaturalKey(BrgId, "DOSTLE004").Value;
        batch.QtySisa.Should().Be(10);
        batch.ListLokasi.Should().Contain(x =>
            x.LayananId == SourceLayananId && x.QtySisa == 5);
        batch.ListLokasi.Should().Contain(x =>
            x.LayananId == DestLayananId && x.QtySisa == 5);

        _mutasiRepo.ListByTrsReffId("TRSXFER004")
            .Where(x => x.MovementKind == MovementKindEnum.TransferOut)
            .Should().ContainSingle();
        _mutasiRepo.ListByTrsReffId("TRSXFER004")
            .Where(x => x.MovementKind == MovementKindEnum.TransferIn)
            .Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_GateAbort_DoesNotPersist()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        await SeedReceipt("DOSTLE005", qty: 10, hpp: 100m, trsReffId: "TRSSEED005");

        var uowMock = new Mock<IStockConsequenceUnitOfWork>();
        var sut = CreateTransferHandler(
            gateOutcome: EnsureFreshnessOutcomeEnum.AbortedInconsistent,
            inconsistencyReason: "scope broken",
            failedBrgId: BrgId,
            failedDoId: "DOSTLE005",
            uow: uowMock.Object);

        var result = await sut.Handle(
            NewTransferCommand(trsReffId: "TRSXFER005", qty: 4),
            default);

        result.Outcome.Should().Be(PostStockTransferOutcomeEnum.AbortedInconsistent);
        result.InconsistencyReason.Should().Be("scope broken");
        result.FailedBrgId.Should().Be(BrgId);
        result.FailedBrgMasukReffId.Should().Be("DOSTLE005");

        uowMock.Verify(x => x.Commit(It.IsAny<StockConsequenceDraft>()), Times.Never);
        _mutasiRepo.ListByTrsReffId("TRSXFER005").Should().BeEmpty();
        var batch = _batchRepo.LoadByNaturalKey(BrgId, "DOSTLE005").Value;
        batch.ListLokasi.Should().NotContain(x => x.LayananId == DestLayananId);
    }

    [Fact]
    public async Task Handle_OccConflict_Throws()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        await SeedReceipt("DOSTLE006", qty: 10, hpp: 100m, trsReffId: "TRSSEED006");

        var stale = _batchRepo.LoadByNaturalKey(BrgId, "DOSTLE006").Value;
        var concurrent = _batchRepo.LoadByNaturalKey(BrgId, "DOSTLE006").Value;
        concurrent.IncreaseLokasi(SourceLayananId, TglEd, qty: 1);
        _batchRepo.SaveChanges(concurrent);

        var batchRepoMock = new Mock<IStockBatchRepo>();
        batchRepoMock
            .Setup(x => x.ListAllocationCandidates(BrgId, SourceLayananId))
            .Returns(() => _batchRepo.ListAllocationCandidates(BrgId, SourceLayananId));
        batchRepoMock
            .Setup(x => x.LoadByNaturalKey(BrgId, "DOSTLE006"))
            .Returns(MayBe.From(stale));
        batchRepoMock
            .Setup(x => x.SaveChanges(It.IsAny<StockBatchModel>(), It.IsAny<string>()))
            .Callback<StockBatchModel, string>((m, u) => _batchRepo.SaveChanges(m, u));
        batchRepoMock
            .Setup(x => x.SaveChanges(It.IsAny<StockBatchModel>()))
            .Callback<StockBatchModel>(m => _batchRepo.SaveChanges(m));

        var uow = new StockConsequenceUnitOfWork(
            _writer,
            batchRepoMock.Object,
            _mutasiRepo,
            _bindingRepo,
            _scopeRepo,
            Options.Create(new StockLedgerCoexistenceOptions { CoexistenceEnabled = true }));

        var sut = CreateTransferHandler(
            gateOutcome: EnsureFreshnessOutcomeEnum.Success,
            batchRepo: batchRepoMock.Object,
            uow: uow);

        var act = async () => await sut.Handle(
            NewTransferCommand(trsReffId: "TRSXFER006", qty: 2),
            default);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*concurrency*");
    }

    [Fact]
    public async Task Handle_DepletedSource_RetainsZeroBalance()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        await SeedReceipt("DOSTLE007", qty: 7, hpp: 40m, trsReffId: "TRSSEED007");

        var sut = CreateTransferHandler(gateOutcome: EnsureFreshnessOutcomeEnum.Success);
        var result = await sut.Handle(
            NewTransferCommand(trsReffId: "TRSXFER007", qty: 7),
            default);

        result.Outcome.Should().Be(PostStockTransferOutcomeEnum.Success);

        var batch = _batchRepo.LoadByNaturalKey(BrgId, "DOSTLE007").Value;
        batch.QtySisa.Should().Be(7);
        batch.ListLokasi.Should().Contain(x =>
            x.LayananId == SourceLayananId
            && x.TglEd == TglEd
            && x.QtySisa == 0
            && x.StokLokasiId == result.Lines[0].SourceStokLokasiId);
        batch.ListLokasi.Should().Contain(x =>
            x.LayananId == DestLayananId && x.TglEd == TglEd && x.QtySisa == 7);
    }

    [Fact]
    public async Task Handle_IdempotentRetry_AfterFullDeplete_ReturnsPriorSuccess()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        await SeedReceipt("DOSTLE008", qty: 7, hpp: 40m, trsReffId: "TRSSEED008");

        var sut = CreateTransferHandler(gateOutcome: EnsureFreshnessOutcomeEnum.Success);
        var cmd = NewTransferCommand(trsReffId: "TRSXFER008", qty: 7);

        var first = await sut.Handle(cmd, default);
        first.Outcome.Should().Be(PostStockTransferOutcomeEnum.Success);

        var second = await sut.Handle(cmd, default);
        second.Outcome.Should().Be(PostStockTransferOutcomeEnum.Idempotent);
        second.Lines.Should().HaveCount(1);
        second.Lines[0].StokMutasiOutId.Should().Be(first.Lines[0].StokMutasiOutId);
        second.Lines[0].StokMutasiInId.Should().Be(first.Lines[0].StokMutasiInId);
        second.Lines[0].LegacyBukuOutId.Should().Be(first.Lines[0].LegacyBukuOutId);
        second.Lines[0].LegacyBukuInId.Should().Be(first.Lines[0].LegacyBukuInId);

        var batch = _batchRepo.LoadByNaturalKey(BrgId, "DOSTLE008").Value;
        batch.ListLokasi.Should().Contain(x =>
            x.LayananId == SourceLayananId && x.TglEd == TglEd && x.QtySisa == 0);
        batch.ListLokasi.Should().Contain(x =>
            x.LayananId == DestLayananId && x.TglEd == TglEd && x.QtySisa == 7);

        _mutasiRepo.ListByTrsReffId("TRSXFER008")
            .Where(x => x.MovementKind == MovementKindEnum.TransferOut)
            .Should().ContainSingle();
        _mutasiRepo.ListByTrsReffId("TRSXFER008")
            .Where(x => x.MovementKind == MovementKindEnum.TransferIn)
            .Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_IdempotentRetry_AfterSourceEmptiedAndNewStock_StillIdempotent()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        await SeedReceipt("DOSTLE009A", qty: 5, hpp: 40m, trsReffId: "TRSSEED09A");

        var sut = CreateTransferHandler(gateOutcome: EnsureFreshnessOutcomeEnum.Success);
        var cmd = NewTransferCommand(trsReffId: "TRSXFER009", qty: 5);

        var first = await sut.Handle(cmd, default);
        first.Outcome.Should().Be(PostStockTransferOutcomeEnum.Success);

        await SeedReceipt("DOSTLE009B", qty: 8, hpp: 45m, trsReffId: "TRSSEED09B",
            tglMutasi: new DateTime(2026, 7, 2, 10, 0, 0));

        var second = await sut.Handle(cmd, default);
        second.Outcome.Should().Be(PostStockTransferOutcomeEnum.Idempotent);
        second.Lines.Should().HaveCount(1);
        second.Lines[0].StokMutasiOutId.Should().Be(first.Lines[0].StokMutasiOutId);
        second.Lines[0].StokMutasiInId.Should().Be(first.Lines[0].StokMutasiInId);

        _mutasiRepo.ListByTrsReffId("TRSXFER009").Should().HaveCount(2);

        var newBatch = _batchRepo.LoadByNaturalKey(BrgId, "DOSTLE009B").Value;
        newBatch.ListLokasi.Should().ContainSingle(x =>
            x.LayananId == SourceLayananId && x.QtySisa == 8);
        newBatch.ListLokasi.Should().NotContain(x => x.LayananId == DestLayananId);
    }

    private async Task SeedReceipt(
        string doId,
        decimal qty,
        decimal hpp,
        string trsReffId,
        DateTime? tglMutasi = null)
    {
        var receipt = CreateReceiptHandler(gateOutcome: EnsureFreshnessOutcomeEnum.Success);
        var seeded = await receipt.Handle(
            new PostGoodsReceiptConsequenceCommand(
                BrgId,
                doId,
                SourceLayananId,
                Qty: qty,
                Hpp: hpp,
                TrsReffId: trsReffId,
                TglMutasi: tglMutasi ?? TglMutasi,
                UserId: UserId,
                TglEd: TglEd,
                PoReffId: "POSTLE001",
                NoBatch: "NB-E"),
            default);
        seeded.Outcome.Should().Be(PostGoodsReceiptOutcomeEnum.Success);
    }

    private PostStockTransferConsequenceHandler CreateTransferHandler(
        EnsureFreshnessOutcomeEnum gateOutcome,
        string inconsistencyReason = "",
        string failedBrgId = "",
        string failedDoId = "",
        IStockBatchRepo? batchRepo = null,
        IStockConsequenceUnitOfWork? uow = null)
    {
        var mediator = new Mock<IMediator>();
        mediator
            .Setup(x => x.Send(It.IsAny<EnsureFreshnessForScopesCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EnsureFreshnessForScopesResult(
                gateOutcome,
                inconsistencyReason,
                failedBrgId,
                failedDoId));

        var gate = new LegacyFreshnessGate(
            mediator.Object,
            Options.Create(new StockLedgerCoexistenceOptions { CoexistenceEnabled = true }));

        var resolvedBatchRepo = batchRepo ?? _batchRepo;
        var resolvedUow = uow ?? new StockConsequenceUnitOfWork(
            _writer,
            resolvedBatchRepo,
            _mutasiRepo,
            _bindingRepo,
            _scopeRepo,
            Options.Create(new StockLedgerCoexistenceOptions { CoexistenceEnabled = true }));

        return new PostStockTransferConsequenceHandler(
            gate,
            resolvedBatchRepo,
            _mutasiRepo,
            _bindingRepo,
            _scopeRepo,
            _legacyRead,
            resolvedUow);
    }

    private PostGoodsReceiptConsequenceHandler CreateReceiptHandler(
        EnsureFreshnessOutcomeEnum gateOutcome)
    {
        var mediator = new Mock<IMediator>();
        mediator
            .Setup(x => x.Send(It.IsAny<EnsureFreshnessForScopesCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EnsureFreshnessForScopesResult(
                gateOutcome,
                string.Empty,
                string.Empty,
                string.Empty));

        var gate = new LegacyFreshnessGate(
            mediator.Object,
            Options.Create(new StockLedgerCoexistenceOptions { CoexistenceEnabled = true }));

        var uow = new StockConsequenceUnitOfWork(
            _writer,
            _batchRepo,
            _mutasiRepo,
            _bindingRepo,
            _scopeRepo,
            Options.Create(new StockLedgerCoexistenceOptions { CoexistenceEnabled = true }));

        return new PostGoodsReceiptConsequenceHandler(
            gate,
            _batchRepo,
            _mutasiRepo,
            _bindingRepo,
            _scopeRepo,
            uow);
    }

    private static PostStockTransferConsequenceCommand NewTransferCommand(
        string trsReffId,
        decimal qty) =>
        new(
            BrgId,
            Qty: qty,
            SourceLayananId,
            DestLayananId,
            TrsReffId: trsReffId,
            TglMutasi: TglMutasi,
            UserId: UserId,
            TglEd: TglEd);
}
