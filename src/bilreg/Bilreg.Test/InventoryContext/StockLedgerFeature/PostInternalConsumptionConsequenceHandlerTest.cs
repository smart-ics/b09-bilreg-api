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

    private async Task SeedReceipt(
        string doId,
        decimal qty,
        decimal hpp,
        string trsReffId,
        DateTime? tglEd = null,
        DateTime? tglMutasi = null)
    {
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
