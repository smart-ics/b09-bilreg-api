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
    }

    [Fact]
    public void Commit_PersistsLegacyBukuAndStok()
    {
        using var trans = TransHelper.NewScope();

        var uow = new StockConsequenceUnitOfWork(_writer);
        var draft = BuildInboundDraft(BrgId, DoId, "TRSUOW0001", "BKUOW00001", "STUOW00001");

        uow.Commit(draft);

        _legacyRead.ListJournals(BrgId, DoId)
            .Should().Contain(x => x.LegacyBukuId == "BKUOW00001" && x.QtyIn == 10);
        _legacyRead.ListBalances(BrgId, DoId)
            .Should().Contain(x => x.LegacyStokId == "STUOW00001" && x.QtySisa == 10);
    }

    [Fact]
    public void Commit_MultipleOperations_CommitsAtomicallyToLegacyTables()
    {
        using var trans = TransHelper.NewScope();

        var uow = new StockConsequenceUnitOfWork(_writer);
        var draft = new StockConsequenceDraft(
            BatchUpserts: [],
            MutasiInserts: [],
            BindingInserts: [],
            ScopeUpdates: [],
            LegacyOperations: [
                new LegacyInboundWriteOperation(new LegacyInboundWriteRequest(
                    BrgId, "DOSTLUOW02", LayananId,
                    Qty: 10, Hpp: 100m, TglEd, TglMasuk, TglMutasi,
                    TrsReffId: "TRSUOW0002",
                    MovementKindString: "DO",
                    LegacyBukuId: "BKUOW00002",
                    LegacyStokId: "STUOW00002")),
                new LegacyOutboundBukuWriteOperation(new LegacyOutboundBukuWriteRequest(
                    BrgId, "DOSTLUOW02", LayananId,
                    QtyOut: 4, Hpp: 100m, TglEd, TglMutasi.AddMinutes(5),
                    TrsReffId: "TRSUOWOUT2",
                    MovementKindString: "PK",
                    LegacyBukuId: "BKUOWOUT02")),
                new LegacyStokDepleteWriteOperation(new LegacyStokDepleteRequest(
                    LegacyStokId: "STUOW00002",
                    QtyOut: 4))
            ],
            UserId: "tester");

        uow.Commit(draft);

        var journals = _legacyRead.ListJournals(BrgId, "DOSTLUOW02");
        journals.Should().Contain(x => x.LegacyBukuId == "BKUOW00002" && x.QtyIn == 10);
        journals.Should().Contain(x => x.LegacyBukuId == "BKUOWOUT02" && x.QtyOut == 4);

        _legacyRead.ListBalances(BrgId, "DOSTLUOW02")
            .Should().ContainSingle(x => x.LegacyStokId == "STUOW00002" && x.QtySisa == 6);
    }

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
