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
public class PostSaleVoidConsequenceHandlerTest
{
    private readonly IOptions<DatabaseOptions> _db = ConnStringHelper.GetTestEnv();
    private readonly LegacyStockWriterPort _writer;
    private readonly LegacyStockReadPort _legacyRead;
    private readonly StockBatchRepo _batchRepo;
    private readonly StockMutasiRepo _mutasiRepo;
    private readonly StockLegacyBindingRepo _bindingRepo;
    private readonly StockLegacyScopeRepo _scopeRepo;

    private const string BrgId = "BRGSTLG10001";
    private const string LayananId = "LYSAL";
    private const string UserId = "tester";
    private static readonly DateTime TglMutasi = new(2026, 7, 1, 10, 0, 0);
    private static readonly DateTime TglVoid = new(2026, 7, 2, 11, 0, 0);
    private static readonly DateTime TglEdEarly = new(2027, 6, 15);
    private static readonly DateTime TglEdLate = new(2028, 7, 15);

    public PostSaleVoidConsequenceHandlerTest()
    {
        _writer = new LegacyStockWriterPort(_db);
        _legacyRead = new LegacyStockReadPort(_db);
        _batchRepo = new StockBatchRepo(new StockBatchDal(_db), new StokLokasiDal(_db));
        _mutasiRepo = new StockMutasiRepo(new StokMutasiDal(_db));
        _bindingRepo = new StockLegacyBindingRepo(new StokLegacyBindingDal(_db));
        _scopeRepo = new StockLegacyScopeRepo(new StokLegacyScopeDal(_db));
    }

    [Fact]
    public async Task Handle_HappyPath_RestoresBalance_RetainsOriginals()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        await SeedReceipt("DOG1A", qty: 10, hpp: 100m, trsReffId: "TRSG1SEEDA");
        var sale = await PostSale("TRSG1SAL01", qty: 4);

        var jurnalBefore = _legacyRead.ListJournals(BrgId, "DOG1A").Count;
        var originalBukuIds = sale.Lines.Select(x => x.LegacyBukuId).ToHashSet();

        var sut = CreateVoidHandler(gateOutcome: EnsureFreshnessOutcomeEnum.Success);
        var result = await sut.Handle(
            NewVoidCommand("TRSG1SAL01", "TRSG1VOD01"),
            default);

        result.Outcome.Should().Be(PostSaleVoidOutcomeEnum.Success);
        result.Lines.Should().ContainSingle();
        result.Lines[0].OriginalStokMutasiId.Should().Be(sale.Lines[0].StokMutasiId);
        result.Lines[0].QtyRestored.Should().Be(4);

        var batch = _batchRepo.LoadByNaturalKey(BrgId, "DOG1A").Value;
        batch.QtySisa.Should().Be(10);
        batch.ListLokasi.Should().Contain(x =>
            x.LayananId == LayananId && x.QtySisa == 10);

        var originalMutasi = _mutasiRepo.ListByTrsReffId("TRSG1SAL01")
            .Single(x => x.MovementKind == MovementKindEnum.SaleIssueDb);
        originalMutasi.QtyOut.Should().Be(4);
        originalMutasi.ReversesMutasiId.Should().BeEmpty();

        var voidMutasi = _mutasiRepo.ListByTrsReffId("TRSG1VOD01")
            .Single(x => x.MovementKind == MovementKindEnum.SaleVoidDb);
        voidMutasi.QtyIn.Should().Be(4);
        voidMutasi.QtyOut.Should().Be(0);
        voidMutasi.ReversesMutasiId.Should().Be(originalMutasi.StokMutasiId);

        var journals = _legacyRead.ListJournals(BrgId, "DOG1A");
        journals.Count.Should().Be(jurnalBefore + 1);
        journals.Should().Contain(x => originalBukuIds.Contains(x.LegacyBukuId));
        journals.Should().Contain(x =>
            x.LegacyBukuId == result.Lines[0].LegacyBukuId
            && x.MovementKindString == "DB_V"
            && x.QtyIn == 4);
    }

    [Fact]
    public async Task Handle_DepletedLokasi_RestoresSameLokasiRow()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        await SeedReceipt("DOG1B", qty: 5, hpp: 80m, trsReffId: "TRSG1SEEDB");
        var sale = await PostSale("TRSG1SAL02", qty: 5);
        var lokasiId = sale.Lines[0].StokLokasiId;
        var originalSaleBukuId = sale.Lines[0].LegacyBukuId;
        var jurnalBeforeVoid = _legacyRead.ListJournals(BrgId, "DOG1B").Count;

        var afterSale = _batchRepo.LoadByNaturalKey(BrgId, "DOG1B").Value;
        afterSale.ListLokasi.Should().Contain(x =>
            x.StokLokasiId == lokasiId && x.QtySisa == 0);

        var sut = CreateVoidHandler(gateOutcome: EnsureFreshnessOutcomeEnum.Success);
        var result = await sut.Handle(
            NewVoidCommand("TRSG1SAL02", "TRSG1VOD02"),
            default);

        result.Outcome.Should().Be(PostSaleVoidOutcomeEnum.Success);

        var afterVoid = _batchRepo.LoadByNaturalKey(BrgId, "DOG1B").Value;
        afterVoid.QtySisa.Should().Be(5);
        afterVoid.ListLokasi.Should().Contain(x =>
            x.StokLokasiId == lokasiId && x.QtySisa == 5);

        var voidBinding = _bindingRepo.FindByStokMutasiId(result.Lines[0].VoidStokMutasiId);
        voidBinding.HasValue.Should().BeTrue();
        voidBinding.Value.LegacyStokId.Should().Be(result.Lines[0].LegacyStokId);
        voidBinding.Value.LegacyStokId.Should().NotBeNullOrWhiteSpace();

        _legacyRead.ListBalances(BrgId, "DOG1B")
            .Should().Contain(x =>
                x.LegacyStokId == voidBinding.Value.LegacyStokId && x.QtySisa == 5);

        var journals = _legacyRead.ListJournals(BrgId, "DOG1B");
        journals.Count.Should().Be(jurnalBeforeVoid + 1);
        journals.Should().Contain(x => x.LegacyBukuId == originalSaleBukuId);
    }

    [Fact]
    public async Task Handle_MultiBalanceSale_VoidsBothLines()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        await SeedReceipt("DOG1C", qty: 6, hpp: 50m, trsReffId: "TRSG1SEEDC",
            tglEd: TglEdEarly, tglMutasi: new DateTime(2026, 6, 1, 9, 0, 0));
        await SeedReceipt("DOG1D", qty: 8, hpp: 60m, trsReffId: "TRSG1SEEDD",
            tglEd: TglEdLate, tglMutasi: new DateTime(2026, 6, 2, 9, 0, 0));

        var sale = await PostSale("TRSG1SAL03", qty: 10);
        sale.Lines.Should().HaveCount(2);

        var sut = CreateVoidHandler(gateOutcome: EnsureFreshnessOutcomeEnum.Success);
        var result = await sut.Handle(
            NewVoidCommand("TRSG1SAL03", "TRSG1VOD03"),
            default);

        result.Outcome.Should().Be(PostSaleVoidOutcomeEnum.Success);
        result.Lines.Should().HaveCount(2);
        result.Lines.Sum(x => x.QtyRestored).Should().Be(10);

        _batchRepo.LoadByNaturalKey(BrgId, "DOG1C").Value.QtySisa.Should().Be(6);
        _batchRepo.LoadByNaturalKey(BrgId, "DOG1D").Value.QtySisa.Should().Be(8);

        _mutasiRepo.ListByTrsReffId("TRSG1VOD03")
            .Where(x => x.MovementKind == MovementKindEnum.SaleVoidDb)
            .Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_Unbound_RejectsWithoutCommit()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        await SeedReceipt("DOG1E", qty: 10, hpp: 100m, trsReffId: "TRSG1SEEDE");
        var batch = _batchRepo.LoadByNaturalKey(BrgId, "DOG1E").Value;
        var lokasi = batch.ListLokasi.First(x => x.LayananId == LayananId);

        var unboundMutasi = StockMovementModel.CreateOutbound(
            lokasi.StokLokasiId,
            batch.StokBatchId,
            BrgId,
            "DOG1E",
            LayananId,
            lokasi.TglEd,
            "TRSG1UNB01",
            MovementKindEnum.SaleIssueDb,
            qtyOut: 2,
            hpp: 100m,
            tglMutasi: TglMutasi);
        _mutasiRepo.Insert(unboundMutasi, UserId);

        var uowMock = new Mock<IStockConsequenceUnitOfWork>();
        var sut = CreateVoidHandler(
            gateOutcome: EnsureFreshnessOutcomeEnum.Success,
            uow: uowMock.Object);

        var result = await sut.Handle(
            NewVoidCommand("TRSG1UNB01", "TRSG1VODUB"),
            default);

        result.Outcome.Should().Be(PostSaleVoidOutcomeEnum.Unbound);
        result.UnboundStokMutasiId.Should().Be(unboundMutasi.StokMutasiId);
        uowMock.Verify(x => x.Commit(It.IsAny<StockConsequenceDraft>()), Times.Never);
        _mutasiRepo.ListByTrsReffId("TRSG1VODUB").Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_AlreadyReversed_RejectsSecondVoidTrsReffId()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        await SeedReceipt("DOG1F", qty: 10, hpp: 100m, trsReffId: "TRSG1SEEDF");
        await PostSale("TRSG1SAL04", qty: 3);

        var sut = CreateVoidHandler(gateOutcome: EnsureFreshnessOutcomeEnum.Success);
        var first = await sut.Handle(
            NewVoidCommand("TRSG1SAL04", "TRSG1VODA4"),
            default);
        first.Outcome.Should().Be(PostSaleVoidOutcomeEnum.Success);

        var second = await sut.Handle(
            NewVoidCommand("TRSG1SAL04", "TRSG1VODB4"),
            default);

        second.Outcome.Should().Be(PostSaleVoidOutcomeEnum.AlreadyReversed);
        _mutasiRepo.ListByTrsReffId("TRSG1VODB4").Should().BeEmpty();

        var batch = _batchRepo.LoadByNaturalKey(BrgId, "DOG1F").Value;
        batch.QtySisa.Should().Be(10);
    }

    [Fact]
    public async Task Handle_IdempotentRetry_DoesNotDoubleQty()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        await SeedReceipt("DOG1G", qty: 10, hpp: 100m, trsReffId: "TRSG1SEEDG");
        await PostSale("TRSG1SAL05", qty: 4);

        var sut = CreateVoidHandler(gateOutcome: EnsureFreshnessOutcomeEnum.Success);
        var cmd = NewVoidCommand("TRSG1SAL05", "TRSG1VOD05");

        var first = await sut.Handle(cmd, default);
        first.Outcome.Should().Be(PostSaleVoidOutcomeEnum.Success);

        var second = await sut.Handle(cmd, default);
        second.Outcome.Should().Be(PostSaleVoidOutcomeEnum.Idempotent);
        second.Lines.Should().HaveCount(1);
        second.Lines[0].VoidStokMutasiId.Should().Be(first.Lines[0].VoidStokMutasiId);

        var batch = _batchRepo.LoadByNaturalKey(BrgId, "DOG1G").Value;
        batch.QtySisa.Should().Be(10);

        _mutasiRepo.ListByTrsReffId("TRSG1VOD05")
            .Where(x => x.MovementKind == MovementKindEnum.SaleVoidDb)
            .Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_IdempotentReplay_MismatchedOriginalSale_Throws()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        await SeedReceipt("DOG1I", qty: 10, hpp: 100m, trsReffId: "TRSG1SEEDI");
        await SeedReceipt("DOG1J", qty: 10, hpp: 100m, trsReffId: "TRSG1SEEDJ");
        await PostSale("TRSG1SAL07", qty: 3);
        await PostSale("TRSG1SAL08", qty: 2);

        var sut = CreateVoidHandler(gateOutcome: EnsureFreshnessOutcomeEnum.Success);
        var first = await sut.Handle(
            NewVoidCommand("TRSG1SAL07", "TRSG1VOD07"),
            default);
        first.Outcome.Should().Be(PostSaleVoidOutcomeEnum.Success);

        var act = () => sut.Handle(
            NewVoidCommand("TRSG1SAL08", "TRSG1VOD07"),
            default);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*TRSG1VOD07*TRSG1SAL08*");

        _mutasiRepo.ListByTrsReffId("TRSG1VOD07")
            .Where(x => x.MovementKind == MovementKindEnum.SaleVoidDb)
            .Should().ContainSingle();
        // Sale08 still depletes 2 from FEFO (DOG1I); void of sale07 restored its 3 only.
        _batchRepo.LoadByNaturalKey(BrgId, "DOG1I").Value.QtySisa.Should().Be(8);
        _batchRepo.LoadByNaturalKey(BrgId, "DOG1J").Value.QtySisa.Should().Be(10);
    }

    [Fact]
    public async Task Handle_GateAbort_DoesNotPersist()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        await SeedReceipt("DOG1H", qty: 10, hpp: 100m, trsReffId: "TRSG1SEEDH");
        await PostSale("TRSG1SAL06", qty: 4);

        var uowMock = new Mock<IStockConsequenceUnitOfWork>();
        var sut = CreateVoidHandler(
            gateOutcome: EnsureFreshnessOutcomeEnum.AbortedInconsistent,
            inconsistencyReason: "scope broken",
            failedBrgId: BrgId,
            failedDoId: "DOG1H",
            uow: uowMock.Object);

        var result = await sut.Handle(
            NewVoidCommand("TRSG1SAL06", "TRSG1VOD06"),
            default);

        result.Outcome.Should().Be(PostSaleVoidOutcomeEnum.AbortedInconsistent);
        result.InconsistencyReason.Should().Be("scope broken");
        result.FailedBrgId.Should().Be(BrgId);
        result.FailedBrgMasukReffId.Should().Be("DOG1H");

        uowMock.Verify(x => x.Commit(It.IsAny<StockConsequenceDraft>()), Times.Never);
        _mutasiRepo.ListByTrsReffId("TRSG1VOD06").Should().BeEmpty();

        var batch = _batchRepo.LoadByNaturalKey(BrgId, "DOG1H").Value;
        batch.QtySisa.Should().Be(6);
    }

    [Fact]
    public async Task Handle_OriginalNotFound_ReturnsOutcome()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        var sut = CreateVoidHandler(gateOutcome: EnsureFreshnessOutcomeEnum.Success);
        var result = await sut.Handle(
            NewVoidCommand("TRSG1MISS9", "TRSG1VODM9"),
            default);

        result.Outcome.Should().Be(PostSaleVoidOutcomeEnum.OriginalNotFound);
        result.Lines.Should().BeEmpty();
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
                PoReffId: "POSTLG001",
                NoBatch: "NB-G1"),
            default);
        seeded.Outcome.Should().Be(PostGoodsReceiptOutcomeEnum.Success);
    }

    private async Task<PostSaleIssueConsequenceResult> PostSale(string trsReffId, decimal qty)
    {
        var saleHandler = CreateSaleHandler(gateOutcome: EnsureFreshnessOutcomeEnum.Success);
        var sale = await saleHandler.Handle(
            new PostSaleIssueConsequenceCommand(
                BrgId,
                LayananId,
                Qty: qty,
                TrsReffId: trsReffId,
                TglMutasi: TglMutasi,
                UserId: UserId,
                SaleKind: SaleIssueKindEnum.Db),
            default);
        sale.Outcome.Should().Be(PostSaleIssueOutcomeEnum.Success);
        return sale;
    }

    private PostSaleVoidConsequenceHandler CreateVoidHandler(
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

        return new PostSaleVoidConsequenceHandler(
            gate,
            _batchRepo,
            _mutasiRepo,
            _bindingRepo,
            _scopeRepo,
            _legacyRead,
            resolvedUow);
    }

    private PostSaleIssueConsequenceHandler CreateSaleHandler(
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

        return new PostSaleIssueConsequenceHandler(
            gate,
            _batchRepo,
            _mutasiRepo,
            _bindingRepo,
            _scopeRepo,
            _legacyRead,
            uow);
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

    private static PostSaleVoidConsequenceCommand NewVoidCommand(
        string originalSaleTrsReffId,
        string voidTrsReffId) =>
        new(originalSaleTrsReffId, voidTrsReffId, TglVoid, UserId);
}
