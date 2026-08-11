using System.Data.SqlClient;
using Bilreg.Application.InventoryContext.StockLedgerFeature;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Options;
using Moq;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.InventoryContext.StockLedgerFeature;

[Collection(StockLedgerSqlCollection.Name)]
public class ReconcileScopeQueryTest
{
    private readonly IOptions<DatabaseOptions> _db = ConnStringHelper.GetTestEnv();
    private readonly LegacyStockWriterPort _writer;
    private readonly LegacyStockReadPort _legacyRead;
    private readonly StockBatchDal _batchDal;
    private readonly StockBatchRepo _batchRepo;
    private readonly StockMutasiRepo _mutasiRepo;
    private readonly StockLegacyBindingRepo _bindingRepo;
    private readonly StockLegacyScopeRepo _scopeRepo;

    private const string BrgId = "BRGSTLH10001";
    private const string DoId = "DOSTLH001";
    private const string LayananId = "LYREC";
    private const string UserId = "tester";
    private static readonly DateTime TglMutasi = new(2026, 8, 1, 10, 0, 0);
    private static readonly DateTime TglEd = new(2027, 6, 15);

    public ReconcileScopeQueryTest()
    {
        _writer = new LegacyStockWriterPort(_db);
        _legacyRead = new LegacyStockReadPort(_db);
        _batchDal = new StockBatchDal(_db);
        _batchRepo = new StockBatchRepo(_batchDal, new StokLokasiDal(_db));
        _mutasiRepo = new StockMutasiRepo(new StokMutasiDal(_db));
        _bindingRepo = new StockLegacyBindingRepo(new StokLegacyBindingDal(_db));
        _scopeRepo = new StockLegacyScopeRepo(new StokLegacyScopeDal(_db));
    }

    [Fact]
    public async Task Handle_ConservedWithDepleted_IncludesDepletedAndIsConsistent()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        await SeedReceipt(qty: 10, trsReffId: "TRSSTLH01");
        await SeedSale(qty: 10, trsReffId: "TRSSTLH01S");

        var sut = CreateReconcileHandler(coexistenceEnabled: true);
        var result = await sut.Handle(new ReconcileScopeQuery(BrgId, DoId), default);

        result.IsConsistent.Should().BeTrue();
        result.BatchQtySisa.Should().Be(0);
        result.LokasiQtyTotal.Should().Be(0);
        result.MovementNet.Should().Be(0);
        result.LocationLines.Should().ContainSingle(x =>
            x.LayananId == LayananId && x.IsDepleted && x.QtySisa == 0);
        result.Differences.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_BatchQtyDesynced_FlagsMismatch()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        await SeedReceipt(qty: 10, trsReffId: "TRSSTLH02");

        var batch = _batchRepo.LoadByNaturalKey(BrgId, DoId).Value;
        var desynced = new StockBatchDto(
            batch.StokBatchId,
            batch.BrgId,
            batch.BrgMasukReffId,
            QtySisa: 99,
            batch.Hpp,
            batch.TglMasuk,
            batch.PoReffId,
            Version: batch.Version + 1,
            CrtUser: "tester",
            CrtDate: TglMutasi,
            UpdUser: "tester",
            UpdDate: TglMutasi);
        _batchDal.UpdateConditional(desynced, batch.Version).Should().Be(1);

        var sut = CreateReconcileHandler(coexistenceEnabled: false);
        var result = await sut.Handle(new ReconcileScopeQuery(BrgId, DoId), default);

        result.IsConsistent.Should().BeFalse();
        result.Differences.Should().Contain(d =>
            d.Kind == ReconcileDifferenceKindEnum.HospitalBatchVsLokasi);
        result.Differences.Should().Contain(d =>
            d.Kind == ReconcileDifferenceKindEnum.HospitalBatchVsMovement);
        result.LegacySnapshot.Should().BeNull();
    }

    [Fact]
    public async Task Handle_EmptyUnknownScope_IsDeterministicConsistent()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        var sut = CreateReconcileHandler(coexistenceEnabled: true);
        var result = await sut.Handle(
            new ReconcileScopeQuery("BRGUNKNOWN01", "DOUNKNOWN1"),
            default);

        result.IsConsistent.Should().BeTrue();
        result.BatchQtySisa.Should().Be(0);
        result.LokasiQtyTotal.Should().Be(0);
        result.MovementNet.Should().Be(0);
        result.LocationLines.Should().BeEmpty();
        result.Differences.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_IsReadOnly_DoesNotMutateRows()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        await SeedReceipt(qty: 7, trsReffId: "TRSSTLH04");

        var beforeBatch = _batchRepo.LoadByNaturalKey(BrgId, DoId).Value;
        var beforeMutasi = _mutasiRepo.ListByScope(BrgId, DoId).ToList();
        var beforeLokasiQty = beforeBatch.ListLokasi.Sum(x => x.QtySisa);
        var beforeMutasiCount = CountMutasi(BrgId, DoId);
        var beforeBatchCount = CountBatch(BrgId, DoId);

        var sut = CreateReconcileHandler(coexistenceEnabled: true);
        var result = await sut.Handle(new ReconcileScopeQuery(BrgId, DoId), default);
        result.IsConsistent.Should().BeTrue();

        var afterBatch = _batchRepo.LoadByNaturalKey(BrgId, DoId).Value;
        afterBatch.QtySisa.Should().Be(beforeBatch.QtySisa);
        afterBatch.Version.Should().Be(beforeBatch.Version);
        afterBatch.ListLokasi.Sum(x => x.QtySisa).Should().Be(beforeLokasiQty);
        _mutasiRepo.ListByScope(BrgId, DoId).Should().HaveCount(beforeMutasi.Count);
        CountMutasi(BrgId, DoId).Should().Be(beforeMutasiCount);
        CountBatch(BrgId, DoId).Should().Be(beforeBatchCount);
    }

    private ReconcileScopeHandler CreateReconcileHandler(bool coexistenceEnabled) =>
        new(
            _batchRepo,
            _mutasiRepo,
            _legacyRead,
            Options.Create(new StockLedgerCoexistenceOptions
            {
                CoexistenceEnabled = coexistenceEnabled
            }));

    private async Task SeedReceipt(decimal qty, string trsReffId)
    {
        var receipt = CreateReceiptHandler();
        var seeded = await receipt.Handle(
            new PostGoodsReceiptConsequenceCommand(
                BrgId,
                DoId,
                LayananId,
                Qty: qty,
                Hpp: 100m,
                TrsReffId: trsReffId,
                TglMutasi: TglMutasi,
                UserId: UserId,
                TglEd: TglEd,
                PoReffId: "POSTLH001"),
            default);
        seeded.Outcome.Should().Be(PostGoodsReceiptOutcomeEnum.Success);
    }

    private async Task SeedSale(decimal qty, string trsReffId)
    {
        var sale = CreateSaleHandler();
        var result = await sale.Handle(
            new PostSaleIssueConsequenceCommand(
                BrgId,
                LayananId,
                Qty: qty,
                TrsReffId: trsReffId,
                TglMutasi: TglMutasi.AddHours(1),
                UserId: UserId,
                SaleKind: SaleIssueKindEnum.Db,
                TglEd: TglEd),
            default);
        result.Outcome.Should().Be(PostSaleIssueOutcomeEnum.Success);
    }

    private PostGoodsReceiptConsequenceHandler CreateReceiptHandler()
    {
        var mediator = new Mock<IMediator>();
        mediator
            .Setup(x => x.Send(It.IsAny<EnsureFreshnessForScopesCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EnsureFreshnessForScopesResult(
                EnsureFreshnessOutcomeEnum.Success, string.Empty, string.Empty, string.Empty));

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
            gate, _batchRepo, _mutasiRepo, _bindingRepo, _scopeRepo, uow);
    }

    private PostSaleIssueConsequenceHandler CreateSaleHandler()
    {
        var mediator = new Mock<IMediator>();
        mediator
            .Setup(x => x.Send(It.IsAny<EnsureFreshnessForScopesCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EnsureFreshnessForScopesResult(
                EnsureFreshnessOutcomeEnum.Success, string.Empty, string.Empty, string.Empty));

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

        return new PostSaleIssueConsequenceHandler(
            gate, _batchRepo, _mutasiRepo, _bindingRepo, _scopeRepo, _legacyRead, uow);
    }

    private int CountMutasi(string brgId, string doId)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_db.Value));
        return conn.ExecuteScalar<int>(
            "SELECT COUNT(1) FROM BILRG_StokMutasi WHERE BrgId = @BrgId AND BrgMasukReffId = @DoId",
            new { BrgId = brgId, DoId = doId });
    }

    private int CountBatch(string brgId, string doId)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_db.Value));
        return conn.ExecuteScalar<int>(
            "SELECT COUNT(1) FROM BILRG_StokBatch WHERE BrgId = @BrgId AND BrgMasukReffId = @DoId",
            new { BrgId = brgId, DoId = doId });
    }
}
