using Bilreg.Application.InventoryContext.StockLedgerFeature;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.InventoryContext.StockLedgerFeature;

[Collection(StockLedgerSqlCollection.Name)]
public class StockConsequenceUnitOfWorkTest
{
    private readonly IOptions<DatabaseOptions> _db = ConnStringHelper.GetTestEnv();
    private readonly LegacyStockWriterPort _writer;
    private readonly LegacyStockReadPort _legacyRead;
    private readonly StockBatchRepo _batchRepo;
    private readonly StockMutasiRepo _mutasiRepo;
    private readonly StockLegacyBindingRepo _bindingRepo;
    private readonly StockLegacyScopeRepo _scopeRepo;

    private const string BrgId = "BRGSTLUOW0001";
    private const string DoId = "DOSTLUOW01";
    private const string LayananId = "LY001";
    private static readonly DateTime TglMasuk = new(2026, 5, 1, 8, 0, 0);
    private static readonly DateTime TglMutasi = new(2026, 5, 1, 9, 0, 0);
    private static readonly DateTime TglEd = new(2028, 1, 15);

    public StockConsequenceUnitOfWorkTest()
    {
        _writer = new LegacyStockWriterPort(_db);
        _legacyRead = new LegacyStockReadPort(_db);
        _batchRepo = new StockBatchRepo(new StockBatchDal(_db), new StokLokasiDal(_db));
        _mutasiRepo = new StockMutasiRepo(new StokMutasiDal(_db));
        _bindingRepo = new StockLegacyBindingRepo(new StokLegacyBindingDal(_db));
        _scopeRepo = new StockLegacyScopeRepo(new StokLegacyScopeDal(_db));
    }

    [Fact]
    public void Commit_CoexistenceOn_PersistsLegacyLedgerBindingAndScope()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        var uow = CreateUow(coexistenceEnabled: true);
        var draft = BuildInboundDraft(BrgId, DoId, "TRSUOW0001", "BKUOW00001", "STUOW00001");

        uow.Commit(draft);

        var batch = _batchRepo.LoadByNaturalKey(BrgId, DoId);
        batch.HasValue.Should().BeTrue();
        batch.Value.QtySisa.Should().Be(10);

        _mutasiRepo.Exists("TRSUOW0001", MovementKindEnum.GoodsReceipt, draft.MutasiInserts[0].StokLokasiId)
            .Should().BeTrue();

        var binding = _bindingRepo.FindByLegacyBukuId("BKUOW00001");
        binding.HasValue.Should().BeTrue();
        binding.Value.LegacyStokId.Should().Be("STUOW00001");

        var scope = _scopeRepo.LoadEntity(StockLegacyScopeModel.Key(BrgId, DoId));
        scope.HasValue.Should().BeTrue();
        scope.Value.AlignmentStatus.Should().Be(AlignmentStatusEnum.Aligned);
        scope.Value.LastLegacyBukuId.Should().Be("BKUOW00001");

        _legacyRead.ListJournals(BrgId, DoId)
            .Should().Contain(x => x.LegacyBukuId == "BKUOW00001" && x.QtyIn == 10);
        _legacyRead.ListBalances(BrgId, DoId)
            .Should().Contain(x => x.LegacyStokId == "STUOW00001" && x.QtySisa == 10);
    }

    [Fact]
    public void Commit_OccConflict_Throws()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        var seed = StockBatchModel.Create(BrgId, "DOSTLUOW02", hpp: 100m, TglMasuk);
        seed.IncreaseLokasi(LayananId, TglEd, qty: 10);
        _batchRepo.SaveChanges(seed);

        var workerA = _batchRepo.LoadByNaturalKey(BrgId, "DOSTLUOW02").Value;
        var workerB = _batchRepo.LoadByNaturalKey(BrgId, "DOSTLUOW02").Value;
        workerA.IncreaseLokasi(LayananId, TglEd, qty: 1);
        _batchRepo.SaveChanges(workerA);

        workerB.IncreaseLokasi(LayananId, TglEd, qty: 1);
        var lokasi = workerB.ListLokasi.Single();
        var mutasi = StockMovementModel.CreateInbound(
            lokasi.StokLokasiId, workerB.StokBatchId, BrgId, "DOSTLUOW02", LayananId, TglEd,
            "TRSUOWOCC1", MovementKindEnum.GoodsReceipt, qtyIn: 1, hpp: 100m, TglMutasi);

        var draft = new StockConsequenceDraft(
            BatchUpserts: [workerB],
            MutasiInserts: [mutasi],
            BindingInserts: [
                StockLegacyBindingModel.CreateMutasiBuku(
                    mutasi.StokMutasiId, "BKOCC00001", "TRSUOWOCC1",
                    lokasi.StokLokasiId, "STOCC00001")
            ],
            ScopeUpdates: [],
            LegacyOperations: [
                new LegacyInboundWriteOperation(new LegacyInboundWriteRequest(
                    BrgId, "DOSTLUOW02", LayananId,
                    Qty: 1, Hpp: 100m, TglEd, TglMasuk, TglMutasi,
                    TrsReffId: "TRSUOWOCC1",
                    MovementKindString: "DO",
                    LegacyBukuId: "BKOCC00001",
                    LegacyStokId: "STOCC00001"))
            ],
            UserId: "tester");

        var uow = CreateUow(coexistenceEnabled: true);
        var act = () => uow.Commit(draft);

        act.Should().Throw<InvalidOperationException>().WithMessage("*concurrency*");
    }

    [Fact]
    public void Commit_CoexistenceOff_SkipsLegacyWrites()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        var uow = CreateUow(coexistenceEnabled: false);
        var draft = BuildInboundDraft(BrgId, "DOSTLUOW03", "TRSUOW0003", "BKUOW00003", "STUOW00003");

        uow.Commit(draft);

        _batchRepo.LoadByNaturalKey(BrgId, "DOSTLUOW03").HasValue.Should().BeTrue();
        _mutasiRepo.Exists("TRSUOW0003", MovementKindEnum.GoodsReceipt, draft.MutasiInserts[0].StokLokasiId)
            .Should().BeTrue();
        _bindingRepo.FindByLegacyBukuId("BKUOW00003").HasValue.Should().BeTrue();

        _legacyRead.ListJournals(BrgId, "DOSTLUOW03").Should().BeEmpty();
        _legacyRead.ListBalances(BrgId, "DOSTLUOW03").Should().BeEmpty();
    }

    private StockConsequenceUnitOfWork CreateUow(bool coexistenceEnabled) =>
        new(
            _writer,
            _batchRepo,
            _mutasiRepo,
            _bindingRepo,
            _scopeRepo,
            Options.Create(new StockLedgerCoexistenceOptions { CoexistenceEnabled = coexistenceEnabled }));

    private static StockConsequenceDraft BuildInboundDraft(
        string brgId,
        string doId,
        string trsReffId,
        string legacyBukuId,
        string legacyStokId)
    {
        var batch = StockBatchModel.Create(brgId, doId, hpp: 100m, TglMasuk, poReffId: "POUOW0001");
        var lokasi = batch.IncreaseLokasi(LayananId, TglEd, qty: 10, noBatch: "NB-UOW");
        var mutasi = StockMovementModel.CreateInbound(
            lokasi.StokLokasiId, batch.StokBatchId, brgId, doId, LayananId, TglEd,
            trsReffId, MovementKindEnum.GoodsReceipt, qtyIn: 10, hpp: 100m, TglMutasi,
            poReffId: "POUOW0001");

        var scope = StockLegacyScopeModel.CreateNotAligned(brgId, doId);
        scope.MarkAligned(TglMutasi, legacyBukuId, DateTime.Now);

        return new StockConsequenceDraft(
            BatchUpserts: [batch],
            MutasiInserts: [mutasi],
            BindingInserts: [
                StockLegacyBindingModel.CreateMutasiBuku(
                    mutasi.StokMutasiId, legacyBukuId, trsReffId,
                    lokasi.StokLokasiId, legacyStokId)
            ],
            ScopeUpdates: [scope],
            LegacyOperations: [
                new LegacyInboundWriteOperation(new LegacyInboundWriteRequest(
                    brgId, doId, LayananId,
                    Qty: 10, Hpp: 100m, TglEd, TglMasuk, TglMutasi,
                    TrsReffId: trsReffId,
                    MovementKindString: "DO",
                    PoReffId: "POUOW0001",
                    NoBatch: "NB-UOW",
                    LegacyBukuId: legacyBukuId,
                    LegacyStokId: legacyStokId))
            ],
            UserId: "tester");
    }
}
