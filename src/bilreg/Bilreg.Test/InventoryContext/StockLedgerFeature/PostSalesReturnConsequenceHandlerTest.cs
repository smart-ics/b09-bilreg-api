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
public class PostSalesReturnConsequenceHandlerTest
{
    private readonly IOptions<DatabaseOptions> _db = ConnStringHelper.GetTestEnv();
    private readonly LegacyStockWriterPort _writer;
    private readonly LegacyStockReadPort _legacyRead;
    private readonly StockBatchRepo _batchRepo;
    private readonly StockMutasiRepo _mutasiRepo;
    private readonly StockLegacyBindingRepo _bindingRepo;
    private readonly StockLegacyScopeRepo _scopeRepo;

    private const string BrgId = "BRGSTLG20001";
    private const string DoId = "DOSTLG201";
    private const string LayananId = "LYRET";
    private const string UserId = "tester";
    private const decimal Hpp = 100m;
    private static readonly DateTime TglMutasi = new(2026, 8, 1, 10, 0, 0);
    private static readonly DateTime TglEd = new(2028, 8, 15);

    public PostSalesReturnConsequenceHandlerTest()
    {
        _writer = new LegacyStockWriterPort(_db);
        _legacyRead = new LegacyStockReadPort(_db);
        _batchRepo = new StockBatchRepo(new StockBatchDal(_db), new StokLokasiDal(_db));
        _mutasiRepo = new StockMutasiRepo(new StokMutasiDal(_db));
        _bindingRepo = new StockLegacyBindingRepo(new StokLegacyBindingDal(_db));
        _scopeRepo = new StockLegacyScopeRepo(new StokLegacyScopeDal(_db));
    }

    [Fact]
    public async Task Handle_Success_RestoresAfterSale()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        var receipt = await SeedReceipt(DoId, qty: 10, trsReffId: "TRSG2RCPT1");
        await SeedSale(trsReffId: "TRSG2SALE1", qty: 10);

        var sut = CreateReturnHandler(gateOutcome: EnsureFreshnessOutcomeEnum.Success);
        var result = await sut.Handle(
            NewReturnCommand(trsReffId: "TRSG2RET01", qty: 3),
            default);

        result.Outcome.Should().Be(PostSalesReturnOutcomeEnum.Success);
        result.StokMutasiId.Should().NotBeNullOrWhiteSpace();
        result.StokLokasiId.Should().Be(receipt.StokLokasiId);
        result.StokBatchId.Should().Be(receipt.StokBatchId);
        result.LegacyBukuId.Should().HaveLength(10).And.StartWith("BK");
        result.LegacyStokId.Should().HaveLength(10).And.StartWith("ST");

        var batch = _batchRepo.LoadByNaturalKey(BrgId, DoId).Value;
        batch.QtySisa.Should().Be(3);
        batch.ListLokasi.Should().ContainSingle(x =>
            x.StokLokasiId == receipt.StokLokasiId && x.QtySisa == 3);

        _mutasiRepo.Exists("TRSG2RET01", MovementKindEnum.SalesReturnRj, result.StokLokasiId)
            .Should().BeTrue();

        var binding = _bindingRepo.FindByStokMutasiId(result.StokMutasiId);
        binding.HasValue.Should().BeTrue();
        binding.Value.LegacyBukuId.Should().Be(result.LegacyBukuId);
        binding.Value.LegacyStokId.Should().Be(result.LegacyStokId);

        _legacyRead.ListJournals(BrgId, DoId)
            .Should().Contain(x =>
                x.LegacyBukuId == result.LegacyBukuId
                && x.MovementKindString == "RJ"
                && x.QtyIn == 3
                && x.TrsReffId == "TRSG2RET01");
        _legacyRead.ListBalances(BrgId, DoId)
            .Should().Contain(x => x.LegacyStokId == result.LegacyStokId && x.QtySisa == 3);
    }

    [Fact]
    public async Task Handle_Success_PreservesReceiptSource()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        var receipt = await SeedReceipt(DoId, qty: 10, trsReffId: "TRSG2RCPT2");
        await SeedSale(trsReffId: "TRSG2SALE2", qty: 5);

        var sut = CreateReturnHandler(gateOutcome: EnsureFreshnessOutcomeEnum.Success);
        var result = await sut.Handle(
            NewReturnCommand(trsReffId: "TRSG2RET02", qty: 2),
            default);

        result.Outcome.Should().Be(PostSalesReturnOutcomeEnum.Success);
        result.StokBatchId.Should().Be(receipt.StokBatchId);

        var mutasi = _mutasiRepo.ListByTrsReffId("TRSG2RET02")
            .Should().ContainSingle(x => x.MovementKind == MovementKindEnum.SalesReturnRj)
            .Subject;
        mutasi.BrgMasukReffId.Should().Be(DoId);
        mutasi.BrgId.Should().Be(BrgId);

        var journal = _legacyRead.ListJournals(BrgId, DoId)
            .Should().ContainSingle(x => x.LegacyBukuId == result.LegacyBukuId)
            .Subject;
        journal.BrgMasukReffId.Should().Be(DoId);
        journal.MovementKindString.Should().Be("RJ");

        var batch = _batchRepo.LoadByNaturalKey(BrgId, DoId).Value;
        batch.BrgMasukReffId.Should().Be(DoId);
        batch.StokBatchId.Should().Be(receipt.StokBatchId);
    }

    [Fact]
    public async Task Handle_IdempotentRetry_DoesNotDoubleQty()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        await SeedReceipt(DoId, qty: 10, trsReffId: "TRSG2RCPT3");
        await SeedSale(trsReffId: "TRSG2SALE3", qty: 10);

        var sut = CreateReturnHandler(gateOutcome: EnsureFreshnessOutcomeEnum.Success);
        var cmd = NewReturnCommand(trsReffId: "TRSG2RET03", qty: 4);

        var first = await sut.Handle(cmd, default);
        first.Outcome.Should().Be(PostSalesReturnOutcomeEnum.Success);

        var second = await sut.Handle(cmd, default);
        second.Outcome.Should().Be(PostSalesReturnOutcomeEnum.Idempotent);
        second.StokMutasiId.Should().Be(first.StokMutasiId);
        second.StokLokasiId.Should().Be(first.StokLokasiId);
        second.LegacyBukuId.Should().Be(first.LegacyBukuId);
        second.LegacyStokId.Should().Be(first.LegacyStokId);

        var batch = _batchRepo.LoadByNaturalKey(BrgId, DoId).Value;
        batch.QtySisa.Should().Be(4);

        _mutasiRepo.ListByTrsReffId("TRSG2RET03")
            .Where(x => x.MovementKind == MovementKindEnum.SalesReturnRj)
            .Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_GateAbort_DoesNotPersist()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        await SeedReceipt(DoId, qty: 10, trsReffId: "TRSG2RCPT4");

        var uowMock = new Mock<IStockConsequenceUnitOfWork>();
        var sut = CreateReturnHandler(
            gateOutcome: EnsureFreshnessOutcomeEnum.AbortedInconsistent,
            inconsistencyReason: "scope broken",
            failedBrgId: BrgId,
            failedDoId: DoId,
            uow: uowMock.Object);

        var result = await sut.Handle(
            NewReturnCommand(trsReffId: "TRSG2RET04", qty: 2),
            default);

        result.Outcome.Should().Be(PostSalesReturnOutcomeEnum.AbortedInconsistent);
        result.InconsistencyReason.Should().Be("scope broken");
        result.FailedBrgId.Should().Be(BrgId);
        result.FailedBrgMasukReffId.Should().Be(DoId);

        uowMock.Verify(x => x.Commit(It.IsAny<StockConsequenceDraft>()), Times.Never);
        _mutasiRepo.ListByTrsReffId("TRSG2RET04").Should().BeEmpty();
        var batch = _batchRepo.LoadByNaturalKey(BrgId, DoId).Value;
        batch.QtySisa.Should().Be(10);
    }

    [Theory]
    [InlineData(SalesReturnKindEnum.Ru, MovementKindEnum.SalesReturnRu, "RU")]
    [InlineData(SalesReturnKindEnum.Rt, MovementKindEnum.SalesReturnRt, "RT")]
    public async Task Handle_ReturnKinds_MapLegacyStrings(
        SalesReturnKindEnum returnKind,
        MovementKindEnum expectedKind,
        string expectedLegacy)
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        var doId = returnKind == SalesReturnKindEnum.Ru ? "DOSTLG2RU" : "DOSTLG2RT";
        var trsSeed = returnKind == SalesReturnKindEnum.Ru ? "TRSG2SDR" : "TRSG2SDT";
        var trsRet = returnKind == SalesReturnKindEnum.Ru ? "TRSG2RETRU" : "TRSG2RETRT";
        var trsSale = returnKind == SalesReturnKindEnum.Ru ? "TRSG2SLRU" : "TRSG2SLRT";

        await SeedReceipt(doId, qty: 8, trsReffId: trsSeed);
        await SeedSale(trsReffId: trsSale, qty: 3, doId: doId);

        var sut = CreateReturnHandler(gateOutcome: EnsureFreshnessOutcomeEnum.Success);
        var result = await sut.Handle(
            NewReturnCommand(trsReffId: trsRet, qty: 2, returnKind: returnKind, doId: doId),
            default);

        result.Outcome.Should().Be(PostSalesReturnOutcomeEnum.Success);
        _mutasiRepo.Exists(trsRet, expectedKind, result.StokLokasiId).Should().BeTrue();
        _legacyRead.ListJournals(BrgId, doId)
            .Should().Contain(x =>
                x.LegacyBukuId == result.LegacyBukuId
                && x.MovementKindString == expectedLegacy
                && x.QtyIn == 2);
    }

    [Fact]
    public async Task Handle_BatchNotFound_Rejects()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        var uowMock = new Mock<IStockConsequenceUnitOfWork>();
        var sut = CreateReturnHandler(
            gateOutcome: EnsureFreshnessOutcomeEnum.Success,
            uow: uowMock.Object);

        var result = await sut.Handle(
            NewReturnCommand(trsReffId: "TRSG2RET05", qty: 1, doId: "DONOTFOUND"),
            default);

        result.Outcome.Should().Be(PostSalesReturnOutcomeEnum.BatchNotFound);
        result.FailedBrgId.Should().Be(BrgId);
        result.FailedBrgMasukReffId.Should().Be("DONOTFOUND");
        uowMock.Verify(x => x.Commit(It.IsAny<StockConsequenceDraft>()), Times.Never);
        _mutasiRepo.ListByTrsReffId("TRSG2RET05").Should().BeEmpty();
    }

    private async Task<PostGoodsReceiptConsequenceResult> SeedReceipt(
        string doId,
        decimal qty,
        string trsReffId)
    {
        var receipt = CreateReceiptHandler(gateOutcome: EnsureFreshnessOutcomeEnum.Success);
        var seeded = await receipt.Handle(
            new PostGoodsReceiptConsequenceCommand(
                BrgId,
                doId,
                LayananId,
                Qty: qty,
                Hpp: Hpp,
                TrsReffId: trsReffId,
                TglMutasi: TglMutasi.AddDays(-2),
                UserId: UserId,
                TglEd: TglEd,
                PoReffId: "POSTLG201",
                NoBatch: "NB-G2"),
            default);
        seeded.Outcome.Should().Be(PostGoodsReceiptOutcomeEnum.Success);
        return seeded;
    }

    private async Task SeedSale(string trsReffId, decimal qty, string? doId = null)
    {
        var sale = CreateSaleHandler(gateOutcome: EnsureFreshnessOutcomeEnum.Success);
        var result = await sale.Handle(
            new PostSaleIssueConsequenceCommand(
                BrgId,
                LayananId,
                Qty: qty,
                TrsReffId: trsReffId,
                TglMutasi: TglMutasi.AddDays(-1),
                UserId: UserId,
                SaleKind: SaleIssueKindEnum.Db,
                TglEd: TglEd),
            default);
        result.Outcome.Should().Be(PostSaleIssueOutcomeEnum.Success);
        if (doId is not null)
            result.Lines.Should().Contain(x => x.BrgMasukReffId == doId);
    }

    private PostSalesReturnConsequenceHandler CreateReturnHandler(
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

        return new PostSalesReturnConsequenceHandler(
            gate,
            _batchRepo,
            _mutasiRepo,
            _bindingRepo,
            _scopeRepo,
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

    private static PostSalesReturnConsequenceCommand NewReturnCommand(
        string trsReffId,
        decimal qty,
        SalesReturnKindEnum returnKind = SalesReturnKindEnum.Rj,
        string? doId = null) =>
        new(
            BrgId,
            doId ?? DoId,
            LayananId,
            Qty: qty,
            Hpp: Hpp,
            TrsReffId: trsReffId,
            TglMutasi: TglMutasi,
            UserId: UserId,
            ReturnKind: returnKind,
            TglEd: TglEd,
            NoBatch: "NB-G2");
}
