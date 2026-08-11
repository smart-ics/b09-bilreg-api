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
public class PostGoodsReceiptConsequenceHandlerTest
{
    private readonly IOptions<DatabaseOptions> _db = ConnStringHelper.GetTestEnv();
    private readonly LegacyStockWriterPort _writer;
    private readonly LegacyStockReadPort _legacyRead;
    private readonly StockBatchRepo _batchRepo;
    private readonly StockMutasiRepo _mutasiRepo;
    private readonly StockLegacyBindingRepo _bindingRepo;
    private readonly StockLegacyScopeRepo _scopeRepo;

    private const string BrgId = "BRGSTLD20001";
    private const string DoId = "DOSTLD201";
    private const string LayananId = "LY001";
    private const string UserId = "tester";
    private static readonly DateTime TglMutasi = new(2026, 6, 1, 9, 0, 0);
    private static readonly DateTime TglEd = new(2028, 6, 15);

    public PostGoodsReceiptConsequenceHandlerTest()
    {
        _writer = new LegacyStockWriterPort(_db);
        _legacyRead = new LegacyStockReadPort(_db);
        _batchRepo = new StockBatchRepo(new StockBatchDal(_db), new StokLokasiDal(_db));
        _mutasiRepo = new StockMutasiRepo(new StokMutasiDal(_db));
        _bindingRepo = new StockLegacyBindingRepo(new StokLegacyBindingDal(_db));
        _scopeRepo = new StockLegacyScopeRepo(new StokLegacyScopeDal(_db));
    }

    [Fact]
    public async Task Handle_Success_CreatesBatchLokasiMutasiBindingAndLegacy()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        var sut = CreateHandler(gateOutcome: EnsureFreshnessOutcomeEnum.Success);
        var cmd = NewCommand(trsReffId: "TRSD20001", qty: 10, hpp: 100m);

        var result = await sut.Handle(cmd, default);

        result.Outcome.Should().Be(PostGoodsReceiptOutcomeEnum.Success);
        result.StokMutasiId.Should().NotBeNullOrWhiteSpace();
        result.StokLokasiId.Should().NotBeNullOrWhiteSpace();
        result.StokBatchId.Should().NotBeNullOrWhiteSpace();
        result.LegacyBukuId.Should().HaveLength(10).And.StartWith("BK");
        result.LegacyStokId.Should().HaveLength(10).And.StartWith("ST");

        var batch = _batchRepo.LoadByNaturalKey(BrgId, DoId);
        batch.HasValue.Should().BeTrue();
        batch.Value.QtySisa.Should().Be(10);
        batch.Value.Hpp.Should().Be(100m);
        batch.Value.ListLokasi.Should().ContainSingle(x =>
            x.LayananId == LayananId && x.TglEd == TglEd && x.QtySisa == 10);

        _mutasiRepo.Exists("TRSD20001", MovementKindEnum.GoodsReceipt, result.StokLokasiId)
            .Should().BeTrue();

        var binding = _bindingRepo.FindByLegacyBukuId(result.LegacyBukuId);
        binding.HasValue.Should().BeTrue();
        binding.Value.LegacyStokId.Should().Be(result.LegacyStokId);
        binding.Value.StokMutasiId.Should().Be(result.StokMutasiId);

        var scope = _scopeRepo.LoadEntity(StockLegacyScopeModel.Key(BrgId, DoId));
        scope.HasValue.Should().BeTrue();
        scope.Value.AlignmentStatus.Should().Be(AlignmentStatusEnum.Aligned);
        scope.Value.LastLegacyBukuId.Should().Be(result.LegacyBukuId);

        _legacyRead.ListJournals(BrgId, DoId)
            .Should().Contain(x => x.LegacyBukuId == result.LegacyBukuId && x.QtyIn == 10);
        _legacyRead.ListBalances(BrgId, DoId)
            .Should().Contain(x => x.LegacyStokId == result.LegacyStokId && x.QtySisa == 10);
    }

    [Fact]
    public async Task Handle_IdempotentRetry_DoesNotDoubleQty()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        var sut = CreateHandler(gateOutcome: EnsureFreshnessOutcomeEnum.Success);
        var cmd = NewCommand(trsReffId: "TRSD20002", qty: 7, hpp: 50m);

        var first = await sut.Handle(cmd, default);
        first.Outcome.Should().Be(PostGoodsReceiptOutcomeEnum.Success);

        var second = await sut.Handle(cmd, default);
        second.Outcome.Should().Be(PostGoodsReceiptOutcomeEnum.Idempotent);
        second.StokMutasiId.Should().Be(first.StokMutasiId);
        second.StokLokasiId.Should().Be(first.StokLokasiId);
        second.LegacyBukuId.Should().Be(first.LegacyBukuId);

        var batch = _batchRepo.LoadByNaturalKey(BrgId, DoId);
        batch.Value.QtySisa.Should().Be(7);

        _mutasiRepo.ListByTrsReffId("TRSD20002")
            .Where(x => x.MovementKind == MovementKindEnum.GoodsReceipt)
            .Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_GateAbort_DoesNotPersist()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        var uowMock = new Mock<IStockConsequenceUnitOfWork>();
        var sut = CreateHandler(
            gateOutcome: EnsureFreshnessOutcomeEnum.AbortedInconsistent,
            inconsistencyReason: "scope broken",
            uow: uowMock.Object);

        var result = await sut.Handle(NewCommand(trsReffId: "TRSD20003", qty: 5, hpp: 10m), default);

        result.Outcome.Should().Be(PostGoodsReceiptOutcomeEnum.AbortedInconsistent);
        result.InconsistencyReason.Should().Be("scope broken");
        result.FailedBrgId.Should().Be(BrgId);
        result.FailedBrgMasukReffId.Should().Be(DoId);

        uowMock.Verify(x => x.Commit(It.IsAny<StockConsequenceDraft>()), Times.Never);
        _batchRepo.LoadByNaturalKey(BrgId, DoId).HasValue.Should().BeFalse();
        _legacyRead.ListJournals(BrgId, DoId).Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_OccConflict_SurfacesConcurrencyException()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        var seed = StockBatchModel.Create(BrgId, DoId, hpp: 100m, TglMutasi);
        seed.IncreaseLokasi(LayananId, TglEd, qty: 10);
        _batchRepo.SaveChanges(seed);

        var stale = _batchRepo.LoadByNaturalKey(BrgId, DoId).Value;
        var concurrent = _batchRepo.LoadByNaturalKey(BrgId, DoId).Value;
        concurrent.IncreaseLokasi(LayananId, TglEd, qty: 1);
        _batchRepo.SaveChanges(concurrent);

        var batchRepoMock = new Mock<IStockBatchRepo>();
        batchRepoMock
            .Setup(x => x.LoadByNaturalKey(BrgId, DoId))
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

        var sut = CreateHandler(
            gateOutcome: EnsureFreshnessOutcomeEnum.Success,
            batchRepo: batchRepoMock.Object,
            uow: uow);

        var act = async () => await sut.Handle(
            NewCommand(trsReffId: "TRSD20004", qty: 2, hpp: 100m),
            default);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*concurrency*");
    }

    [Fact]
    public async Task Handle_SecondReceiptSameDo_IncreasesBalance()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        var sut = CreateHandler(gateOutcome: EnsureFreshnessOutcomeEnum.Success);

        var first = await sut.Handle(NewCommand(trsReffId: "TRSD20005A", qty: 10, hpp: 100m), default);
        first.Outcome.Should().Be(PostGoodsReceiptOutcomeEnum.Success);

        var second = await sut.Handle(NewCommand(trsReffId: "TRSD20005B", qty: 5, hpp: 100m), default);
        second.Outcome.Should().Be(PostGoodsReceiptOutcomeEnum.Success);

        second.StokLokasiId.Should().Be(first.StokLokasiId);
        second.StokBatchId.Should().Be(first.StokBatchId);
        second.StokMutasiId.Should().NotBe(first.StokMutasiId);
        second.LegacyBukuId.Should().NotBe(first.LegacyBukuId);
        second.LegacyStokId.Should().NotBe(first.LegacyStokId);

        var batch = _batchRepo.LoadByNaturalKey(BrgId, DoId);
        batch.Value.QtySisa.Should().Be(15);
        batch.Value.ListLokasi.Should().ContainSingle(x =>
            x.StokLokasiId == first.StokLokasiId && x.QtySisa == 15);

        _mutasiRepo.Exists("TRSD20005A", MovementKindEnum.GoodsReceipt, first.StokLokasiId).Should().BeTrue();
        _mutasiRepo.Exists("TRSD20005B", MovementKindEnum.GoodsReceipt, second.StokLokasiId).Should().BeTrue();

        _legacyRead.ListJournals(BrgId, DoId).Should().HaveCount(2)
            .And.Contain(x => x.LegacyBukuId == first.LegacyBukuId && x.QtyIn == 10)
            .And.Contain(x => x.LegacyBukuId == second.LegacyBukuId && x.QtyIn == 5);
        _legacyRead.ListBalances(BrgId, DoId).Should().HaveCount(2)
            .And.Contain(x => x.LegacyStokId == first.LegacyStokId && x.QtySisa == 10)
            .And.Contain(x => x.LegacyStokId == second.LegacyStokId && x.QtySisa == 5);
    }

    [Fact]
    public async Task Handle_BackdatedReceipt_DoesNotRegressWatermark()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        var sut = CreateHandler(gateOutcome: EnsureFreshnessOutcomeEnum.Success);
        var day12 = new DateTime(2026, 6, 12, 9, 0, 0);
        var day11 = new DateTime(2026, 6, 11, 9, 0, 0);

        var first = await sut.Handle(
            NewCommand(trsReffId: "TRSD20006A", qty: 10, hpp: 100m, tglMutasi: day12),
            default);
        first.Outcome.Should().Be(PostGoodsReceiptOutcomeEnum.Success);

        var second = await sut.Handle(
            NewCommand(trsReffId: "TRSD20006B", qty: 5, hpp: 100m, tglMutasi: day11),
            default);
        second.Outcome.Should().Be(PostGoodsReceiptOutcomeEnum.Success);

        var batch = _batchRepo.LoadByNaturalKey(BrgId, DoId);
        batch.Value.QtySisa.Should().Be(15);
        second.StokLokasiId.Should().Be(first.StokLokasiId);

        _legacyRead.ListJournals(BrgId, DoId).Should().HaveCount(2)
            .And.Contain(x => x.LegacyBukuId == first.LegacyBukuId && x.TglMutasi == day12)
            .And.Contain(x => x.LegacyBukuId == second.LegacyBukuId && x.TglMutasi == day11);

        var scope = _scopeRepo.LoadEntity(StockLegacyScopeModel.Key(BrgId, DoId));
        scope.HasValue.Should().BeTrue();
        scope.Value.TglMutasiLast.Should().Be(day12);
        scope.Value.LastLegacyBukuId.Should().Be(first.LegacyBukuId);
        scope.Value.AlignmentStatus.Should().Be(AlignmentStatusEnum.Aligned);
    }

    private PostGoodsReceiptConsequenceHandler CreateHandler(
        EnsureFreshnessOutcomeEnum gateOutcome,
        string inconsistencyReason = "",
        IStockBatchRepo? batchRepo = null,
        IStockConsequenceUnitOfWork? uow = null)
    {
        var mediator = new Mock<IMediator>();
        mediator
            .Setup(x => x.Send(It.IsAny<EnsureFreshnessForScopesCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EnsureFreshnessForScopesResult(
                gateOutcome,
                inconsistencyReason,
                gateOutcome == EnsureFreshnessOutcomeEnum.AbortedInconsistent ? BrgId : string.Empty,
                gateOutcome == EnsureFreshnessOutcomeEnum.AbortedInconsistent ? DoId : string.Empty));

        var gate = new LegacyFreshnessGate(
            mediator.Object,
            Options.Create(new StockLedgerCoexistenceOptions { CoexistenceEnabled = true }));

        var resolvedUow = uow ?? new StockConsequenceUnitOfWork(
            _writer,
            batchRepo ?? _batchRepo,
            _mutasiRepo,
            _bindingRepo,
            _scopeRepo,
            Options.Create(new StockLedgerCoexistenceOptions { CoexistenceEnabled = true }));

        return new PostGoodsReceiptConsequenceHandler(
            gate,
            batchRepo ?? _batchRepo,
            _mutasiRepo,
            _bindingRepo,
            _scopeRepo,
            resolvedUow);
    }

    private static PostGoodsReceiptConsequenceCommand NewCommand(
        string trsReffId,
        decimal qty,
        decimal hpp,
        DateTime? tglMutasi = null) =>
        new(
            BrgId,
            DoId,
            LayananId,
            Qty: qty,
            Hpp: hpp,
            TrsReffId: trsReffId,
            TglMutasi: tglMutasi ?? TglMutasi,
            UserId: UserId,
            TglEd: TglEd,
            PoReffId: "POSTLD201",
            NoBatch: "NB-D2");
}
