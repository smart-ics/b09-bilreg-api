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
public class PostInternalConsumptionConsequenceHandlerTest
{
    private readonly IOptions<DatabaseOptions> _db = ConnStringHelper.GetTestEnv();
    private readonly LegacyStockWriterPort _writer;
    private readonly LegacyStockReadPort _legacyRead;
    private readonly StockBatchRepo _batchRepo;
    private readonly StockMutasiRepo _mutasiRepo;
    private readonly StockLegacyBindingRepo _bindingRepo;
    private readonly StockLegacyScopeRepo _scopeRepo;

    private const string BrgId = "BRGSTLF20001";
    private const string LayananId = "LYPK";
    private const string UserId = "tester";
    private static readonly DateTime TglMutasi = new(2026, 7, 1, 10, 0, 0);
    private static readonly DateTime TglEdEarly = new(2027, 6, 15);
    private static readonly DateTime TglEdLate = new(2028, 7, 15);

    public PostInternalConsumptionConsequenceHandlerTest()
    {
        _writer = new LegacyStockWriterPort(_db);
        _legacyRead = new LegacyStockReadPort(_db);
        _batchRepo = new StockBatchRepo(new StockBatchDal(_db), new StokLokasiDal(_db));
        _mutasiRepo = new StockMutasiRepo(new StokMutasiDal(_db));
        _bindingRepo = new StockLegacyBindingRepo(new StokLegacyBindingDal(_db));
        _scopeRepo = new StockLegacyScopeRepo(new StokLegacyScopeDal(_db));
    }

    [Fact]
    public async Task Handle_HappyPath_DualWriteLegacyPk()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        await SeedReceipt("DOSTLF2PK", qty: 10, hpp: 100m, trsReffId: "TRSSEEDF2P");

        var sut = CreateConsumptionHandler(gateOutcome: EnsureFreshnessOutcomeEnum.Success);
        var result = await sut.Handle(
            NewConsumptionCommand(trsReffId: "TRSPK0001", qty: 4),
            default);

        result.Outcome.Should().Be(PostInternalConsumptionOutcomeEnum.Success);
        result.Lines.Should().ContainSingle();
        var line = result.Lines[0];
        line.LegacyBukuId.Should().HaveLength(10).And.StartWith("BK");
        line.LegacyStokId.Should().HaveLength(10).And.StartWith("ST");
        line.QtyOut.Should().Be(4);

        var batch = _batchRepo.LoadByNaturalKey(BrgId, "DOSTLF2PK").Value;
        batch.QtySisa.Should().Be(6);
        batch.ListLokasi.Should().Contain(x =>
            x.LayananId == LayananId && x.TglEd == TglEdLate && x.QtySisa == 6);

        var mutasi = _mutasiRepo.ListByTrsReffId("TRSPK0001").ToList();
        mutasi.Should().ContainSingle(x =>
            x.MovementKind == MovementKindEnum.InternalConsumption && x.QtyOut == 4);

        _legacyRead.ListJournals(BrgId, "DOSTLF2PK")
            .Should().Contain(x => x.LegacyBukuId == line.LegacyBukuId
                && x.MovementKindString == "PK" && x.QtyOut == 4);
        _legacyRead.ListBalances(BrgId, "DOSTLF2PK")
            .Should().Contain(x => x.LegacyStokId == line.LegacyStokId
                && x.LayananId == LayananId && x.QtySisa == 6);

        var binding = _bindingRepo.FindByStokMutasiId(line.StokMutasiId);
        binding.HasValue.Should().BeTrue();
        binding.Value.LegacyBukuId.Should().Be(line.LegacyBukuId);
        binding.Value.LegacyStokId.Should().Be(line.LegacyStokId);

        var scope = _scopeRepo.LoadEntity(StockLegacyScopeModel.Key(BrgId, "DOSTLF2PK"));
        scope.HasValue.Should().BeTrue();
        scope.Value.LastLegacyBukuId.Should().Be(line.LegacyBukuId);
        scope.Value.TglMutasiLast.Should().Be(TglMutasi);
    }

    [Fact]
    public async Task Handle_FefoMultiBalance_SplitsAcrossTwoDOs()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        await SeedReceipt("DOSTLF2A", qty: 6, hpp: 50m, trsReffId: "TRSSEEDF2A",
            tglEd: TglEdEarly, tglMutasi: new DateTime(2026, 6, 1, 9, 0, 0));
        await SeedReceipt("DOSTLF2B", qty: 8, hpp: 60m, trsReffId: "TRSSEEDF2B",
            tglEd: TglEdLate, tglMutasi: new DateTime(2026, 6, 2, 9, 0, 0));

        var sut = CreateConsumptionHandler(gateOutcome: EnsureFreshnessOutcomeEnum.Success);
        var result = await sut.Handle(
            NewConsumptionCommand(trsReffId: "TRSPK0002", qty: 10),
            default);

        result.Outcome.Should().Be(PostInternalConsumptionOutcomeEnum.Success);
        result.Lines.Should().HaveCount(2);
        result.Lines.Sum(x => x.QtyOut).Should().Be(10);

        // FEFO: earliest ED first
        result.Lines[0].BrgMasukReffId.Should().Be("DOSTLF2A");
        result.Lines[0].QtyOut.Should().Be(6);
        result.Lines[1].BrgMasukReffId.Should().Be("DOSTLF2B");
        result.Lines[1].QtyOut.Should().Be(4);

        var batchA = _batchRepo.LoadByNaturalKey(BrgId, "DOSTLF2A").Value;
        var batchB = _batchRepo.LoadByNaturalKey(BrgId, "DOSTLF2B").Value;
        batchA.QtySisa.Should().Be(0);
        batchB.QtySisa.Should().Be(4);
        batchA.ListLokasi.Should().Contain(x =>
            x.LayananId == LayananId && x.TglEd == TglEdEarly && x.QtySisa == 0);
        batchB.ListLokasi.Should().Contain(x =>
            x.LayananId == LayananId && x.TglEd == TglEdLate && x.QtySisa == 4);

        _mutasiRepo.ListByTrsReffId("TRSPK0002")
            .Where(x => x.MovementKind == MovementKindEnum.InternalConsumption)
            .Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_InsufficientStock_RejectsWithoutPersist()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        await SeedReceipt("DOSTLF2IN", qty: 3, hpp: 10m, trsReffId: "TRSSEEDF2N");

        var uowMock = new Mock<IStockConsequenceUnitOfWork>();
        var sut = CreateConsumptionHandler(
            gateOutcome: EnsureFreshnessOutcomeEnum.Success,
            uow: uowMock.Object);

        var result = await sut.Handle(
            NewConsumptionCommand(trsReffId: "TRSPK0003", qty: 10),
            default);

        result.Outcome.Should().Be(PostInternalConsumptionOutcomeEnum.InsufficientStock);
        result.ShortfallQty.Should().Be(7);
        result.Lines.Should().BeEmpty();
        uowMock.Verify(x => x.Commit(It.IsAny<StockConsequenceDraft>()), Times.Never);

        var batch = _batchRepo.LoadByNaturalKey(BrgId, "DOSTLF2IN").Value;
        batch.QtySisa.Should().Be(3);
        batch.ListLokasi.Should().ContainSingle(x =>
            x.LayananId == LayananId && x.QtySisa == 3);
        _mutasiRepo.ListByTrsReffId("TRSPK0003").Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_IdempotentRetry_DoesNotDoubleQty()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        await SeedReceipt("DOSTLF2ID", qty: 10, hpp: 100m, trsReffId: "TRSSEEDF2D");

        var sut = CreateConsumptionHandler(gateOutcome: EnsureFreshnessOutcomeEnum.Success);
        var cmd = NewConsumptionCommand(trsReffId: "TRSPK0004", qty: 5);

        var first = await sut.Handle(cmd, default);
        first.Outcome.Should().Be(PostInternalConsumptionOutcomeEnum.Success);

        var second = await sut.Handle(cmd, default);
        second.Outcome.Should().Be(PostInternalConsumptionOutcomeEnum.Idempotent);
        second.Lines.Should().HaveCount(1);
        second.Lines[0].StokMutasiId.Should().Be(first.Lines[0].StokMutasiId);
        second.Lines[0].LegacyBukuId.Should().Be(first.Lines[0].LegacyBukuId);

        var batch = _batchRepo.LoadByNaturalKey(BrgId, "DOSTLF2ID").Value;
        batch.QtySisa.Should().Be(5);
        batch.ListLokasi.Should().Contain(x =>
            x.LayananId == LayananId && x.QtySisa == 5);

        _mutasiRepo.ListByTrsReffId("TRSPK0004")
            .Where(x => x.MovementKind == MovementKindEnum.InternalConsumption)
            .Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_ScopeInLegacyNotYetInV2_DiscoveredPassedToGateAndAllocatedAfterHydrate()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        const string doId = "DOLEG00002";
        var stokDal = new tb_stok_dal(_db);
        stokDal.Insert(new tb_stok_dto(
            fs_kd_trs: "STLEG00002",
            fs_kd_barang: BrgId,
            fs_kd_layanan: LayananId,
            fs_kd_po: "PO02",
            fs_kd_do: doId,
            fd_tgl_ed: "2028-07-15",
            fs_no_batch: "NB-LEG2",
            fn_qty_in: 10,
            fn_qty: 10,
            fn_hpp: 50m,
            fd_tgl_do: "2026-06-01",
            fs_jam_do: "09:00:00",
            fs_kd_mutasi: "DMLEG02",
            fd_tgl_mutasi: "2026-06-01",
            fs_jam_mutasi: "09:00:00",
            fs_kd_satuan: "PCS",
            fs_nm_barang: "BARANG",
            fs_nm_layanan: "LAYANAN"));

        _batchRepo.ListAllocationCandidates(BrgId, LayananId).Should().BeEmpty();

        List<ScopeKeyDto>? capturedScopes = null;
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(x => x.Send(It.IsAny<EnsureFreshnessForScopesCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<EnsureFreshnessForScopesResult>, CancellationToken>((cmd, _) =>
            {
                var ensureCmd = (EnsureFreshnessForScopesCommand)cmd;
                capturedScopes = ensureCmd.Scopes.ToList();
                var batch = StockBatchModel.Create(BrgId, doId, 50m, new DateTime(2026, 6, 1, 9, 0, 0), "PO02");
                batch.IncreaseLokasi(LayananId, TglEdLate, 10, "NB-LEG2");
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

        var sut = new PostInternalConsumptionConsequenceHandler(
            gate,
            _batchRepo,
            _mutasiRepo,
            _bindingRepo,
            _scopeRepo,
            _legacyRead,
            uow);

        var result = await sut.Handle(NewConsumptionCommand("TRSPK_LEG1", qty: 3, tglEd: TglEdLate), default);

        result.Outcome.Should().Be(PostInternalConsumptionOutcomeEnum.Success);
        capturedScopes.Should().NotBeNull();
        capturedScopes.Should().Contain(s => s.BrgId == BrgId && s.BrgMasukReffId == doId);
        result.Lines.Should().ContainSingle();
        result.Lines[0].QtyOut.Should().Be(3);
        result.Lines[0].BrgMasukReffId.Should().Be(doId);
        result.Lines[0].LegacyStokId.Should().Be("STLEG00002");
    }

    [Fact]
    public async Task Handle_MultiRowTbStokLayer_DepletesCleanlyWithOneToOneBinding_ForQty1AndQty3()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        const string doId = "DOMULTIPK1";
        const string brgId = "BRGPKMULTI01";
        var tglEd = new DateTime(2028, 5, 20);

        // Seed 4 legacy tb_stok rows for the same layer: qty 2, 1, 1, 1 (total 5)
        var stokDal = new tb_stok_dal(_db);
        stokDal.Insert(new tb_stok_dto("STPK000001", brgId, LayananId, "", doId, "2028-05-20", "B1", 2, 2, 100m, "2026-06-01", "08:00:00", "DM01", "2026-06-01", "08:00:00", "PCS", "", ""));
        stokDal.Insert(new tb_stok_dto("STPK000002", brgId, LayananId, "", doId, "2028-05-20", "B1", 1, 1, 100m, "2026-06-01", "08:00:00", "DM01", "2026-06-01", "08:00:00", "PCS", "", ""));
        stokDal.Insert(new tb_stok_dto("STPK000003", brgId, LayananId, "", doId, "2028-05-20", "B1", 1, 1, 100m, "2026-06-01", "08:00:00", "DM01", "2026-06-01", "08:00:00", "PCS", "", ""));
        stokDal.Insert(new tb_stok_dto("STPK000004", brgId, LayananId, "", doId, "2028-05-20", "B1", 1, 1, 100m, "2026-06-01", "08:00:00", "DM01", "2026-06-01", "08:00:00", "PCS", "", ""));

        // Seed v2 batch with matching total qty = 5
        var batch = StockBatchModel.Create(brgId, doId, 100m, new DateTime(2026, 6, 1, 8, 0, 0), "PO01");
        batch.IncreaseLokasi(LayananId, tglEd, 5, "B1");
        _batchRepo.SaveChanges(batch, UserId);

        var sut = CreateConsumptionHandler(gateOutcome: EnsureFreshnessOutcomeEnum.Success);

        // Case 1: Deplete qty = 1
        var cmd1 = new PostInternalConsumptionConsequenceCommand(brgId, LayananId, 1m, "TRSPK_M1", TglMutasi, UserId, tglEd);
        var res1 = await sut.Handle(cmd1, default);

        res1.Outcome.Should().Be(PostInternalConsumptionOutcomeEnum.Success);
        res1.Lines.Should().HaveCount(1);
        res1.Lines[0].QtyOut.Should().Be(1);
        res1.Lines[0].LegacyStokId.Should().Be("STPK000001");

        var binding1 = _bindingRepo.FindByStokMutasiId(res1.Lines[0].StokMutasiId);
        binding1.HasValue.Should().BeTrue();
        binding1.Value.LegacyStokId.Should().Be("STPK000001");
        binding1.Value.LegacyBukuId.Should().Be(res1.Lines[0].LegacyBukuId);

        var balancesAfter1 = _legacyRead.ListBalances(brgId, doId).OrderBy(x => x.LegacyStokId, StringComparer.Ordinal).ToList();
        balancesAfter1.First(x => x.LegacyStokId == "STPK000001").QtySisa.Should().Be(1);

        // Case 2: Deplete qty = 3
        var cmd2 = new PostInternalConsumptionConsequenceCommand(brgId, LayananId, 3m, "TRSPK_M2", TglMutasi, UserId, tglEd);
        var res2 = await sut.Handle(cmd2, default);

        res2.Outcome.Should().Be(PostInternalConsumptionOutcomeEnum.Success);
        res2.Lines.Should().HaveCount(3);
        res2.Lines.Sum(x => x.QtyOut).Should().Be(3);
        res2.Lines[0].LegacyStokId.Should().Be("STPK000001");
        res2.Lines[0].QtyOut.Should().Be(1);
        res2.Lines[1].LegacyStokId.Should().Be("STPK000002");
        res2.Lines[1].QtyOut.Should().Be(1);
        res2.Lines[2].LegacyStokId.Should().Be("STPK000003");
        res2.Lines[2].QtyOut.Should().Be(1);

        foreach (var line in res2.Lines)
        {
            var b = _bindingRepo.FindByStokMutasiId(line.StokMutasiId);
            b.HasValue.Should().BeTrue();
            b.Value.LegacyStokId.Should().Be(line.LegacyStokId);
            b.Value.LegacyBukuId.Should().Be(line.LegacyBukuId);
        }

        var balancesAfter2 = _legacyRead.ListBalances(brgId, doId).OrderBy(x => x.LegacyStokId, StringComparer.Ordinal).ToList();
        balancesAfter2.Should().ContainSingle();
        balancesAfter2[0].LegacyStokId.Should().Be("STPK000004");
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
                PoReffId: "POSTLF201",
                NoBatch: "NB-F2"),
            default);
        seeded.Outcome.Should().Be(PostGoodsReceiptOutcomeEnum.Success);
    }

    private PostInternalConsumptionConsequenceHandler CreateConsumptionHandler(
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

        return new PostInternalConsumptionConsequenceHandler(
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

    private static PostInternalConsumptionConsequenceCommand NewConsumptionCommand(
        string trsReffId,
        decimal qty,
        DateTime? tglEd = null) =>
        new(
            BrgId,
            LayananId,
            Qty: qty,
            TrsReffId: trsReffId,
            TglMutasi: TglMutasi,
            UserId: UserId,
            TglEd: tglEd);
}
