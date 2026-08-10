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
public class GetAvailabilityAtLocationQueryTest
{
    private readonly IOptions<DatabaseOptions> _db = ConnStringHelper.GetTestEnv();
    private readonly LegacyStockWriterPort _writer;
    private readonly LegacyStockReadPort _legacyRead;
    private readonly StockBatchRepo _batchRepo;
    private readonly StockMutasiRepo _mutasiRepo;
    private readonly StockLegacyBindingRepo _bindingRepo;
    private readonly StockLegacyScopeRepo _scopeRepo;

    private const string BrgId = "BRGSTLH20001";
    private const string LayananId = "LYAVL";
    private const string UserId = "tester";
    private static readonly DateTime TglMutasi = new(2026, 8, 2, 10, 0, 0);
    private static readonly DateTime TglEdEarly = new(2027, 6, 15);
    private static readonly DateTime TglEdLate = new(2028, 7, 15);

    public GetAvailabilityAtLocationQueryTest()
    {
        _writer = new LegacyStockWriterPort(_db);
        _legacyRead = new LegacyStockReadPort(_db);
        _batchRepo = new StockBatchRepo(new StockBatchDal(_db), new StokLokasiDal(_db));
        _mutasiRepo = new StockMutasiRepo(new StokMutasiDal(_db));
        _bindingRepo = new StockLegacyBindingRepo(new StokLegacyBindingDal(_db));
        _scopeRepo = new StockLegacyScopeRepo(new StokLegacyScopeDal(_db));
    }

    [Fact]
    public async Task Handle_ReturnsPositiveBalancesOnly_ExcludesDepleted()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        await SeedReceipt("DOSTLH2A", qty: 5, hpp: 50m, trsReffId: "TRSAVL01A",
            tglEd: TglEdEarly, tglMutasi: new DateTime(2026, 7, 1, 9, 0, 0));
        await SeedReceipt("DOSTLH2B", qty: 8, hpp: 60m, trsReffId: "TRSAVL01B",
            tglEd: TglEdLate, tglMutasi: new DateTime(2026, 7, 2, 9, 0, 0));

        // Deplete early DO fully.
        await SeedSale(qty: 5, trsReffId: "TRSAVL01S", tglEd: TglEdEarly);

        var sut = new GetAvailabilityAtLocationHandler(_batchRepo);
        var result = await sut.Handle(
            new GetAvailabilityAtLocationQuery(BrgId, LayananId),
            default);

        result.Candidates.Should().ContainSingle();
        result.Candidates[0].BrgMasukReffId.Should().Be("DOSTLH2B");
        result.Candidates[0].QtySisa.Should().Be(8);
        result.Candidates.Should().OnlyContain(c => c.QtySisa > 0);
    }

    [Fact]
    public async Task Handle_OptionalTglEd_FiltersToMatchingExpiry()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        await SeedReceipt("DOSTLH2C", qty: 10, hpp: 40m, trsReffId: "TRSAVL02A",
            tglEd: TglEdEarly, tglMutasi: new DateTime(2026, 7, 1, 9, 0, 0));
        await SeedReceipt("DOSTLH2D", qty: 6, hpp: 45m, trsReffId: "TRSAVL02B",
            tglEd: TglEdLate, tglMutasi: new DateTime(2026, 7, 2, 9, 0, 0));

        var sut = new GetAvailabilityAtLocationHandler(_batchRepo);
        var result = await sut.Handle(
            new GetAvailabilityAtLocationQuery(BrgId, LayananId, TglEd: TglEdLate),
            default);

        result.TglEdFilter.Should().Be(TglEdLate);
        result.Candidates.Should().ContainSingle();
        result.Candidates[0].BrgMasukReffId.Should().Be("DOSTLH2D");
        result.Candidates[0].TglEd.Should().Be(TglEdLate);
    }

    [Fact]
    public async Task Handle_FefoPreviewOrder_EarliestEdFirst()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        await SeedReceipt("DOSTLH2E", qty: 6, hpp: 50m, trsReffId: "TRSAVL03A",
            tglEd: TglEdEarly, tglMutasi: new DateTime(2026, 7, 1, 9, 0, 0));
        await SeedReceipt("DOSTLH2F", qty: 8, hpp: 60m, trsReffId: "TRSAVL03B",
            tglEd: TglEdLate, tglMutasi: new DateTime(2026, 7, 2, 9, 0, 0));

        var sut = new GetAvailabilityAtLocationHandler(_batchRepo);
        var result = await sut.Handle(
            new GetAvailabilityAtLocationQuery(BrgId, LayananId),
            default);

        result.Candidates.Should().HaveCount(2);
        result.Candidates[0].PreviewSequence.Should().Be(1);
        result.Candidates[0].BrgMasukReffId.Should().Be("DOSTLH2E");
        result.Candidates[0].TglEd.Should().Be(TglEdEarly);
        result.Candidates[1].PreviewSequence.Should().Be(2);
        result.Candidates[1].BrgMasukReffId.Should().Be("DOSTLH2F");
        result.Candidates[1].TglEd.Should().Be(TglEdLate);
    }

    private async Task SeedReceipt(
        string doId,
        decimal qty,
        decimal hpp,
        string trsReffId,
        DateTime tglEd,
        DateTime tglMutasi)
    {
        var receipt = CreateReceiptHandler();
        var seeded = await receipt.Handle(
            new PostGoodsReceiptConsequenceCommand(
                BrgId,
                doId,
                LayananId,
                Qty: qty,
                Hpp: hpp,
                TrsReffId: trsReffId,
                TglMutasi: tglMutasi,
                UserId: UserId,
                TglEd: tglEd,
                PoReffId: "POSTLH002"),
            default);
        seeded.Outcome.Should().Be(PostGoodsReceiptOutcomeEnum.Success);
    }

    private async Task SeedSale(decimal qty, string trsReffId, DateTime tglEd)
    {
        var sale = CreateSaleHandler();
        var result = await sale.Handle(
            new PostSaleIssueConsequenceCommand(
                BrgId,
                LayananId,
                Qty: qty,
                TrsReffId: trsReffId,
                TglMutasi: TglMutasi,
                UserId: UserId,
                SaleKind: SaleIssueKindEnum.Db,
                TglEd: tglEd),
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
}
