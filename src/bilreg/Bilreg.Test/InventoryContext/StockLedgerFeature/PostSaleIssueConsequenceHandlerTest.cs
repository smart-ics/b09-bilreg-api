using Bilreg.Application.InventoryContext.StockLedgerFeature;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;
using Bilreg.Infrastructure.InventoryContext.StokFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Options;
using Moq;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.InventoryContext.StockLedgerFeature;

[Collection(StockLedgerSqlCollection.Name)]
public class PostSaleIssueConsequenceHandlerTest
{
    private readonly IOptions<DatabaseOptions> _db = ConnStringHelper.GetTestEnv();
    private readonly LegacyStockWriterPort _writer;
    private readonly LegacyStockReadPort _legacyRead;
    private readonly StockBatchRepo _batchRepo;
    private readonly StockMutasiRepo _mutasiRepo;
    private readonly StockLegacyBindingRepo _bindingRepo;
    private readonly StockLegacyScopeRepo _scopeRepo;

    private const string BrgId = "BRGSTLF10001";
    private const string LayananId = "LYSAL";
    private const string UserId = "tester";
    private static readonly DateTime TglMutasi = new(2026, 7, 1, 10, 0, 0);
    private static readonly DateTime TglEdEarly = new(2027, 6, 15);
    private static readonly DateTime TglEdLate = new(2028, 7, 15);

    public PostSaleIssueConsequenceHandlerTest()
    {
        _writer = new LegacyStockWriterPort(_db);
        _legacyRead = new LegacyStockReadPort(_db);
        _batchRepo = new StockBatchRepo(new StockBatchDal(_db), new StokLokasiDal(_db));
        _mutasiRepo = new StockMutasiRepo(new StokMutasiDal(_db));
        _bindingRepo = new StockLegacyBindingRepo(new StokLegacyBindingDal(_db));
        _scopeRepo = new StockLegacyScopeRepo(new StokLegacyScopeDal(_db));
    }

    [Fact]
    public async Task Handle_FefoMultiBalance_SplitsAcrossTwoDOs()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        await SeedReceipt("DOSTLF1A", qty: 6, hpp: 50m, trsReffId: "TRSSEEDF1A",
            tglEd: TglEdEarly, tglMutasi: new DateTime(2026, 6, 1, 9, 0, 0));
        await SeedReceipt("DOSTLF1B", qty: 8, hpp: 60m, trsReffId: "TRSSEEDF1B",
            tglEd: TglEdLate, tglMutasi: new DateTime(2026, 6, 2, 9, 0, 0));

        var sut = CreateSaleHandler(gateOutcome: EnsureFreshnessOutcomeEnum.Success);
        var result = await sut.Handle(
            NewSaleCommand(trsReffId: "TRSSALE001", qty: 10),
            default);

        result.Outcome.Should().Be(PostSaleIssueOutcomeEnum.Success);
        result.Lines.Should().HaveCount(2);
        result.Lines.Sum(x => x.QtyOut).Should().Be(10);

        // FEFO: earliest ED first
        result.Lines[0].BrgMasukReffId.Should().Be("DOSTLF1A");
        result.Lines[0].QtyOut.Should().Be(6);
        result.Lines[1].BrgMasukReffId.Should().Be("DOSTLF1B");
        result.Lines[1].QtyOut.Should().Be(4);

        var batchA = _batchRepo.LoadByNaturalKey(BrgId, "DOSTLF1A").Value;
        var batchB = _batchRepo.LoadByNaturalKey(BrgId, "DOSTLF1B").Value;
        batchA.QtySisa.Should().Be(0);
        batchB.QtySisa.Should().Be(4);
        batchA.ListLokasi.Should().Contain(x =>
            x.LayananId == LayananId && x.TglEd == TglEdEarly && x.QtySisa == 0);
        batchB.ListLokasi.Should().Contain(x =>
            x.LayananId == LayananId && x.TglEd == TglEdLate && x.QtySisa == 4);

        _mutasiRepo.ListByTrsReffId("TRSSALE001")
            .Where(x => x.MovementKind == MovementKindEnum.SaleIssueDb)
            .Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_ExplicitEd_ConsumesOnlyMatchingEd()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        await SeedReceipt("DOSTLF2A", qty: 100, hpp: 40m, trsReffId: "TRSSEEDF2A",
            tglEd: TglEdEarly, tglMutasi: new DateTime(2026, 6, 1, 9, 0, 0));
        await SeedReceipt("DOSTLF2B", qty: 5, hpp: 45m, trsReffId: "TRSSEEDF2B",
            tglEd: TglEdLate, tglMutasi: new DateTime(2026, 6, 2, 9, 0, 0));
        await SeedReceipt("DOSTLF2C", qty: 5, hpp: 50m, trsReffId: "TRSSEEDF2C",
            tglEd: TglEdLate, tglMutasi: new DateTime(2026, 6, 3, 9, 0, 0));

        var sut = CreateSaleHandler(gateOutcome: EnsureFreshnessOutcomeEnum.Success);
        var result = await sut.Handle(
            NewSaleCommand(trsReffId: "TRSSALE002", qty: 8, tglEd: TglEdLate),
            default);

        result.Outcome.Should().Be(PostSaleIssueOutcomeEnum.Success);
        result.Lines.Should().HaveCount(2);
        result.Lines.Should().OnlyContain(l =>
            l.BrgMasukReffId == "DOSTLF2B" || l.BrgMasukReffId == "DOSTLF2C");
        result.Lines.Sum(x => x.QtyOut).Should().Be(8);

        var early = _batchRepo.LoadByNaturalKey(BrgId, "DOSTLF2A").Value;
        early.QtySisa.Should().Be(100);
        early.ListLokasi.Should().Contain(x => x.TglEd == TglEdEarly && x.QtySisa == 100);
    }

    [Fact]
    public async Task Handle_FifoNoEd_OrdersByReceipt()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        await SeedReceipt("DOSTLF3A", qty: 5, hpp: 30m, trsReffId: "TRSSEEDF3A",
            tglEd: StockLedgerSentinel.EmptyDate, tglMutasi: new DateTime(2026, 6, 1, 9, 0, 0));
        await SeedReceipt("DOSTLF3B", qty: 5, hpp: 35m, trsReffId: "TRSSEEDF3B",
            tglEd: StockLedgerSentinel.EmptyDate, tglMutasi: new DateTime(2026, 6, 2, 9, 0, 0));
        await SeedReceipt("DOSTLF3C", qty: 5, hpp: 40m, trsReffId: "TRSSEEDF3C",
            tglEd: StockLedgerSentinel.EmptyDate, tglMutasi: new DateTime(2026, 6, 3, 9, 0, 0));

        var sut = CreateSaleHandler(gateOutcome: EnsureFreshnessOutcomeEnum.Success);
        var result = await sut.Handle(
            NewSaleCommand(trsReffId: "TRSSALE003", qty: 12),
            default);

        result.Outcome.Should().Be(PostSaleIssueOutcomeEnum.Success);
        result.Lines.Should().HaveCount(3);
        result.Lines[0].BrgMasukReffId.Should().Be("DOSTLF3A");
        result.Lines[0].QtyOut.Should().Be(5);
        result.Lines[1].BrgMasukReffId.Should().Be("DOSTLF3B");
        result.Lines[1].QtyOut.Should().Be(5);
        result.Lines[2].BrgMasukReffId.Should().Be("DOSTLF3C");
        result.Lines[2].QtyOut.Should().Be(2);

        var batchC = _batchRepo.LoadByNaturalKey(BrgId, "DOSTLF3C").Value;
        batchC.QtySisa.Should().Be(3);
        batchC.ListLokasi.Should().Contain(x =>
            x.TglEd == StockLedgerSentinel.EmptyDate && x.QtySisa == 3);
    }

    [Fact]
    public async Task Handle_InsufficientStock_RejectsWithoutPersist()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        await SeedReceipt("DOSTLF4", qty: 3, hpp: 10m, trsReffId: "TRSSEEDF4");

        var uowMock = new Mock<IStockConsequenceUnitOfWork>();
        var sut = CreateSaleHandler(
            gateOutcome: EnsureFreshnessOutcomeEnum.Success,
            uow: uowMock.Object);

        var result = await sut.Handle(
            NewSaleCommand(trsReffId: "TRSSALE004", qty: 10),
            default);

        result.Outcome.Should().Be(PostSaleIssueOutcomeEnum.InsufficientStock);
        result.ShortfallQty.Should().Be(7);
        result.Lines.Should().BeEmpty();
        uowMock.Verify(x => x.Commit(It.IsAny<StockConsequenceDraft>()), Times.Never);

        var batch = _batchRepo.LoadByNaturalKey(BrgId, "DOSTLF4").Value;
        batch.QtySisa.Should().Be(3);
        batch.ListLokasi.Should().ContainSingle(x =>
            x.LayananId == LayananId && x.QtySisa == 3);
        _mutasiRepo.ListByTrsReffId("TRSSALE004").Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_Success_DualWriteLegacyDb()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        await SeedReceipt("DOSTLF5", qty: 10, hpp: 100m, trsReffId: "TRSSEEDF5");

        var sut = CreateSaleHandler(gateOutcome: EnsureFreshnessOutcomeEnum.Success);
        var result = await sut.Handle(
            NewSaleCommand(trsReffId: "TRSSALE005", qty: 4, saleKind: SaleIssueKindEnum.Db),
            default);

        result.Outcome.Should().Be(PostSaleIssueOutcomeEnum.Success);
        result.Lines.Should().ContainSingle();
        var line = result.Lines[0];
        line.LegacyBukuId.Should().HaveLength(10).And.StartWith("BK");
        line.LegacyStokId.Should().HaveLength(10).And.StartWith("ST");
        line.QtyOut.Should().Be(4);

        var batch = _batchRepo.LoadByNaturalKey(BrgId, "DOSTLF5").Value;
        batch.QtySisa.Should().Be(6);
        batch.ListLokasi.Should().Contain(x =>
            x.LayananId == LayananId && x.TglEd == TglEdLate && x.QtySisa == 6);

        var mutasi = _mutasiRepo.ListByTrsReffId("TRSSALE005").ToList();
        mutasi.Should().ContainSingle(x =>
            x.MovementKind == MovementKindEnum.SaleIssueDb && x.QtyOut == 4);

        _legacyRead.ListJournals(BrgId, "DOSTLF5")
            .Should().Contain(x => x.LegacyBukuId == line.LegacyBukuId
                && x.MovementKindString == "DB" && x.QtyOut == 4);
        _legacyRead.ListBalances(BrgId, "DOSTLF5")
            .Should().Contain(x => x.LegacyStokId == line.LegacyStokId
                && x.LayananId == LayananId && x.QtySisa == 6);

        var binding = _bindingRepo.FindByStokMutasiId(line.StokMutasiId);
        binding.HasValue.Should().BeTrue();
        binding.Value.LegacyBukuId.Should().Be(line.LegacyBukuId);
        binding.Value.LegacyStokId.Should().Be(line.LegacyStokId);

        var scope = _scopeRepo.LoadEntity(StockLegacyScopeModel.Key(BrgId, "DOSTLF5"));
        scope.HasValue.Should().BeTrue();
        scope.Value.LastLegacyBukuId.Should().Be(line.LegacyBukuId);
        scope.Value.TglMutasiLast.Should().Be(TglMutasi);
    }

    [Theory]
    [InlineData(SaleIssueKindEnum.Du, MovementKindEnum.SaleIssueDu, "DU")]
    [InlineData(SaleIssueKindEnum.Dt, MovementKindEnum.SaleIssueDt, "DT")]
    public async Task Handle_Success_DualWriteLegacyDuDt(
        SaleIssueKindEnum saleKind,
        MovementKindEnum expectedKind,
        string legacyString)
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        var doId = saleKind == SaleIssueKindEnum.Du ? "DOSTLFDU" : "DOSTLFDT";
        var trsSeed = saleKind == SaleIssueKindEnum.Du ? "TRSSEEDDU" : "TRSSEEDDT";
        var trsSale = saleKind == SaleIssueKindEnum.Du ? "TRSSALEDU" : "TRSSALEDT";

        await SeedReceipt(doId, qty: 10, hpp: 100m, trsReffId: trsSeed);

        var sut = CreateSaleHandler(gateOutcome: EnsureFreshnessOutcomeEnum.Success);
        var result = await sut.Handle(
            NewSaleCommand(trsReffId: trsSale, qty: 2, saleKind: saleKind),
            default);

        result.Outcome.Should().Be(PostSaleIssueOutcomeEnum.Success);
        _mutasiRepo.ListByTrsReffId(trsSale)
            .Should().ContainSingle(x => x.MovementKind == expectedKind && x.QtyOut == 2);
        _legacyRead.ListJournals(BrgId, doId)
            .Should().Contain(x => x.LegacyBukuId == result.Lines[0].LegacyBukuId
                && x.MovementKindString == legacyString && x.QtyOut == 2);
    }

    [Fact]
    public async Task Handle_IdempotentRetry_DoesNotDoubleQty()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        await SeedReceipt("DOSTLF6", qty: 10, hpp: 100m, trsReffId: "TRSSEEDF6");

        var sut = CreateSaleHandler(gateOutcome: EnsureFreshnessOutcomeEnum.Success);
        var cmd = NewSaleCommand(trsReffId: "TRSSALE006", qty: 5);

        var first = await sut.Handle(cmd, default);
        first.Outcome.Should().Be(PostSaleIssueOutcomeEnum.Success);

        var second = await sut.Handle(cmd, default);
        second.Outcome.Should().Be(PostSaleIssueOutcomeEnum.Idempotent);
        second.Lines.Should().HaveCount(1);
        second.Lines[0].StokMutasiId.Should().Be(first.Lines[0].StokMutasiId);
        second.Lines[0].LegacyBukuId.Should().Be(first.Lines[0].LegacyBukuId);

        var batch = _batchRepo.LoadByNaturalKey(BrgId, "DOSTLF6").Value;
        batch.QtySisa.Should().Be(5);
        batch.ListLokasi.Should().Contain(x =>
            x.LayananId == LayananId && x.QtySisa == 5);

        _mutasiRepo.ListByTrsReffId("TRSSALE006")
            .Where(x => x.MovementKind == MovementKindEnum.SaleIssueDb)
            .Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_GateAbort_DoesNotPersist()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        await SeedReceipt("DOSTLF7", qty: 10, hpp: 100m, trsReffId: "TRSSEEDF7");

        var uowMock = new Mock<IStockConsequenceUnitOfWork>();
        var sut = CreateSaleHandler(
            gateOutcome: EnsureFreshnessOutcomeEnum.AbortedInconsistent,
            inconsistencyReason: "scope broken",
            failedBrgId: BrgId,
            failedDoId: "DOSTLF7",
            uow: uowMock.Object);

        var result = await sut.Handle(
            NewSaleCommand(trsReffId: "TRSSALE007", qty: 4),
            default);

        result.Outcome.Should().Be(PostSaleIssueOutcomeEnum.AbortedInconsistent);
        result.InconsistencyReason.Should().Be("scope broken");
        result.FailedBrgId.Should().Be(BrgId);
        result.FailedBrgMasukReffId.Should().Be("DOSTLF7");

        uowMock.Verify(x => x.Commit(It.IsAny<StockConsequenceDraft>()), Times.Never);
        _mutasiRepo.ListByTrsReffId("TRSSALE007").Should().BeEmpty();
        var batch = _batchRepo.LoadByNaturalKey(BrgId, "DOSTLF7").Value;
        batch.QtySisa.Should().Be(10);
    }

    [Fact]
    public async Task Handle_ScopeInLegacyNotYetInV2_DiscoveredPassedToGateAndAllocatedAfterHydrate()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        const string doId = "DOLEG00001";
        var stokDal = new tb_stok_dal(_db);
        stokDal.Insert(new tb_stok_dto(
            fs_kd_trs: "STLEG00001",
            fs_kd_barang: BrgId,
            fs_kd_layanan: LayananId,
            fs_kd_po: "PO01",
            fs_kd_do: doId,
            fd_tgl_ed: "2028-07-15",
            fs_no_batch: "NB-LEG",
            fn_qty_in: 10,
            fn_qty: 10,
            fn_hpp: 50m,
            fd_tgl_do: "2026-06-01",
            fs_jam_do: "09:00:00",
            fs_kd_mutasi: "DMLEG01",
            fd_tgl_mutasi: "2026-06-01",
            fs_jam_mutasi: "09:00:00",
            fs_kd_satuan: "PCS",
            fs_nm_barang: "BARANG",
            fs_nm_layanan: "LAYANAN"));

        // At this point, v2 has 0 candidates for BrgId, LayananId.
        _batchRepo.ListAllocationCandidates(BrgId, LayananId).Should().BeEmpty();

        List<ScopeKeyDto>? capturedScopes = null;
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(x => x.Send(It.IsAny<EnsureFreshnessForScopesCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<EnsureFreshnessForScopesResult>, CancellationToken>((cmd, _) =>
            {
                var ensureCmd = (EnsureFreshnessForScopesCommand)cmd;
                capturedScopes = ensureCmd.Scopes.ToList();
                // Simulate hydration populating v2 batch & location
                var batch = StockBatchModel.Create(BrgId, doId, 50m, new DateTime(2026, 6, 1, 9, 0, 0), "PO01");
                batch.IncreaseLokasi(LayananId, TglEdLate, 10, "NB-LEG");
                _batchRepo.SaveChanges(batch, UserId);
            })
            .ReturnsAsync(new EnsureFreshnessForScopesResult(EnsureFreshnessOutcomeEnum.Success, "", "", ""));

        var gate = new LegacyFreshnessGate(
            mediatorMock.Object,
            Options.Create(new StockLedgerCoexistenceOptions { CoexistenceEnabled = true }));

        var uow = new StockConsequenceUnitOfWork(
            _writer,
            _batchRepo,
            _mutasiRepo,
            _bindingRepo,
            _scopeRepo,
            Options.Create(new StockLedgerCoexistenceOptions { CoexistenceEnabled = true }));

        var sut = new PostSaleIssueConsequenceHandler(
            gate,
            _batchRepo,
            _mutasiRepo,
            _bindingRepo,
            _scopeRepo,
            _legacyRead,
            uow);

        var result = await sut.Handle(NewSaleCommand("TRSSALELEG", qty: 4, tglEd: TglEdLate), default);

        result.Outcome.Should().Be(PostSaleIssueOutcomeEnum.Success);
        capturedScopes.Should().NotBeNull();
        capturedScopes.Should().Contain(s => s.BrgId == BrgId && s.BrgMasukReffId == doId);
        result.Lines.Should().ContainSingle();
        result.Lines[0].QtyOut.Should().Be(4);
        result.Lines[0].BrgMasukReffId.Should().Be(doId);
        result.Lines[0].LegacyStokId.Should().Be("STLEG00001");
    }

    [Fact]
    public async Task Handle_MultiRowTbStokLayer_DepletesCleanlyWithOneToOneBinding_ForQty1AndQty3()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        const string doId = "DOMULTI001";
        const string brgId = "BRGMULTI001";
        var tglEd = new DateTime(2028, 5, 20);

        // Seed 4 legacy tb_stok rows for the same layer: qty 2, 1, 1, 1 (total 5)
        var stokDal = new tb_stok_dal(_db);
        stokDal.Insert(new tb_stok_dto("STSL000001", brgId, LayananId, "", doId, "2028-05-20", "B1", 2, 2, 100m, "2026-06-01", "08:00:00", "DM01", "2026-06-01", "08:00:00", "PCS", "", ""));
        stokDal.Insert(new tb_stok_dto("STSL000002", brgId, LayananId, "", doId, "2028-05-20", "B1", 1, 1, 100m, "2026-06-01", "08:00:00", "DM01", "2026-06-01", "08:00:00", "PCS", "", ""));
        stokDal.Insert(new tb_stok_dto("STSL000003", brgId, LayananId, "", doId, "2028-05-20", "B1", 1, 1, 100m, "2026-06-01", "08:00:00", "DM01", "2026-06-01", "08:00:00", "PCS", "", ""));
        stokDal.Insert(new tb_stok_dto("STSL000004", brgId, LayananId, "", doId, "2028-05-20", "B1", 1, 1, 100m, "2026-06-01", "08:00:00", "DM01", "2026-06-01", "08:00:00", "PCS", "", ""));

        // Seed v2 batch with matching total qty = 5
        var batch = StockBatchModel.Create(brgId, doId, 100m, new DateTime(2026, 6, 1, 8, 0, 0), "PO01");
        batch.IncreaseLokasi(LayananId, tglEd, 5, "B1");
        _batchRepo.SaveChanges(batch, UserId);

        var sut = CreateSaleHandler(gateOutcome: EnsureFreshnessOutcomeEnum.Success);

        // Case 1: Deplete qty = 1
        // Expect: takes 1 from STSL000001 (which has 2). 1 slice emitted.
        var cmd1 = new PostSaleIssueConsequenceCommand(brgId, LayananId, 1m, "TRSSALE_M1", TglMutasi, UserId, SaleIssueKindEnum.Db, tglEd);
        var res1 = await sut.Handle(cmd1, default);

        res1.Outcome.Should().Be(PostSaleIssueOutcomeEnum.Success);
        res1.Lines.Should().HaveCount(1);
        res1.Lines[0].QtyOut.Should().Be(1);
        res1.Lines[0].LegacyStokId.Should().Be("STSL000001");

        var binding1 = _bindingRepo.FindByStokMutasiId(res1.Lines[0].StokMutasiId);
        binding1.HasValue.Should().BeTrue();
        binding1.Value.LegacyStokId.Should().Be("STSL000001");
        binding1.Value.LegacyBukuId.Should().Be(res1.Lines[0].LegacyBukuId);

        // STSL000001 now has 1 remaining in tb_stok. Total remaining across 4 rows is 4.
        var balancesAfter1 = _legacyRead.ListBalances(brgId, doId).OrderBy(x => x.LegacyStokId, StringComparer.Ordinal).ToList();
        balancesAfter1.First(x => x.LegacyStokId == "STSL000001").QtySisa.Should().Be(1);

        // Case 2: Deplete qty = 3
        // Remaining tb_stok rows are: STSL000001 (1), STSL000002 (1), STSL000003 (1), STSL000004 (1).
        // Depleting 3 should slice:
        // - slice 1: STSL000001 (qty 1)
        // - slice 2: STSL000002 (qty 1)
        // - slice 3: STSL000003 (qty 1)
        var cmd2 = new PostSaleIssueConsequenceCommand(brgId, LayananId, 3m, "TRSSALE_M2", TglMutasi, UserId, SaleIssueKindEnum.Db, tglEd);
        var res2 = await sut.Handle(cmd2, default);

        res2.Outcome.Should().Be(PostSaleIssueOutcomeEnum.Success);
        res2.Lines.Should().HaveCount(3);
        res2.Lines.Sum(x => x.QtyOut).Should().Be(3);
        res2.Lines[0].LegacyStokId.Should().Be("STSL000001");
        res2.Lines[0].QtyOut.Should().Be(1);
        res2.Lines[1].LegacyStokId.Should().Be("STSL000002");
        res2.Lines[1].QtyOut.Should().Be(1);
        res2.Lines[2].LegacyStokId.Should().Be("STSL000003");
        res2.Lines[2].QtyOut.Should().Be(1);

        // Verify 1:1 binding per slice
        foreach (var line in res2.Lines)
        {
            var b = _bindingRepo.FindByStokMutasiId(line.StokMutasiId);
            b.HasValue.Should().BeTrue();
            b.Value.LegacyStokId.Should().Be(line.LegacyStokId);
            b.Value.LegacyBukuId.Should().Be(line.LegacyBukuId);
        }

        var balancesAfter2 = _legacyRead.ListBalances(brgId, doId).OrderBy(x => x.LegacyStokId, StringComparer.Ordinal).ToList();
        balancesAfter2.Should().ContainSingle();
        balancesAfter2[0].LegacyStokId.Should().Be("STSL000004");
        balancesAfter2[0].QtySisa.Should().Be(1);
    }

    private async Task SeedReceipt(
        string doId,
        decimal qty,
        decimal hpp,
        string trsReffId,
        DateTime? tglEd = null,
        DateTime? tglMutasi = null)
    {
        Thread.Sleep(20);
        var receipt = CreateReceiptHandler(gateOutcome: EnsureFreshnessOutcomeEnum.Success);
        var seeded = await receipt.Handle(
            new PostGoodsReceiptConsequenceCommand(
                BrgId,
                doId,
                LayananId,
                Qty: qty,
                Hpp: hpp,
                TrsReffId: trsReffId,
                TglMutasi: tglMutasi ?? TglMutasi,
                UserId: UserId,
                TglEd: tglEd ?? TglEdLate,
                PoReffId: "POSTLF001",
                NoBatch: "NB-F1"),
            default);
        seeded.Outcome.Should().Be(PostGoodsReceiptOutcomeEnum.Success);
    }

    private PostSaleIssueConsequenceHandler CreateSaleHandler(
        EnsureFreshnessOutcomeEnum gateOutcome,
        string inconsistencyReason = "",
        string failedBrgId = "",
        string failedDoId = "",
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

        var resolvedUow = uow ?? new StockConsequenceUnitOfWork(
            _writer,
            _batchRepo,
            _mutasiRepo,
            _bindingRepo,
            _scopeRepo,
            Options.Create(new StockLedgerCoexistenceOptions { CoexistenceEnabled = true }));

        return new PostSaleIssueConsequenceHandler(
            gate,
            _batchRepo,
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

    private static PostSaleIssueConsequenceCommand NewSaleCommand(
        string trsReffId,
        decimal qty,
        SaleIssueKindEnum saleKind = SaleIssueKindEnum.Db,
        DateTime? tglEd = null) =>
        new(
            BrgId,
            LayananId,
            Qty: qty,
            TrsReffId: trsReffId,
            TglMutasi: TglMutasi,
            UserId: UserId,
            SaleKind: saleKind,
            TglEd: tglEd);
}
