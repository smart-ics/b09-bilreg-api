using Bilreg.Application.InventoryContext.StockLedgerFeature;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace Bilreg.Test.InventoryContext.StockLedgerFeature;

/// <summary>
/// Live atomicity: forced mid-flight failure after legacy write must leave neither side committed.
/// Runs without an outer TransHelper scope so UoW owns the only transaction.
/// </summary>
[Collection(StockLedgerSqlCollection.Name)]
public class StockConsequenceUnitOfWorkLiveAtomicityTest
{
    private readonly IOptions<DatabaseOptions> _db = ConnStringHelper.GetTestEnv();

    private const string LayananId = "LY001";
    private static readonly DateTime TglMasuk = new(2026, 6, 1, 8, 0, 0);
    private static readonly DateTime TglMutasi = new(2026, 6, 1, 9, 0, 0);
    private static readonly DateTime TglEd = new(2028, 6, 1);

    [Fact]
    public void Commit_ForcedFailureAfterLegacy_LeavesNeitherSideCommitted()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();

        // Unique keys per run so a leaked prior row cannot collide with PK inserts.
        var suffix = DateTime.UtcNow.ToString("HHmmssfff");
        var brgId = $"BRGATM{suffix}"[..13];
        var doId = $"DOATM{suffix}"[..10];
        var legacyBukuId = $"BKATM{suffix}"[..10];
        var legacyStokId = $"STATM{suffix}"[..10];
        var trsReffId = $"TRATM{suffix}"[..10];

        var writer = new LegacyStockWriterPort(_db);
        var legacyRead = new LegacyStockReadPort(_db);
        var batchRepo = new StockBatchRepo(new StockBatchDal(_db), new StokLokasiDal(_db));
        var mutasiRepo = new StockMutasiRepo(new StokMutasiDal(_db));
        var bindingRepo = new StockLegacyBindingRepo(new StokLegacyBindingDal(_db));
        var scopeRepo = new StockLegacyScopeRepo(new StokLegacyScopeDal(_db));

        var uow = new StockConsequenceUnitOfWork(
            writer,
            batchRepo,
            mutasiRepo,
            bindingRepo,
            scopeRepo,
            Options.Create(new StockLedgerCoexistenceOptions { CoexistenceEnabled = true }))
        {
            FailAfterLegacyWritesForTest = () =>
                throw new InvalidOperationException("forced-mid-flight-failure")
        };

        var draft = BuildDraft(brgId, doId, trsReffId, legacyBukuId, legacyStokId);

        var act = () => uow.Commit(draft);
        act.Should().Throw<InvalidOperationException>().WithMessage("forced-mid-flight-failure");

        legacyRead.ListJournals(brgId, doId)
            .Should().NotContain(x => x.LegacyBukuId == legacyBukuId);
        legacyRead.ListBalances(brgId, doId)
            .Should().NotContain(x => x.LegacyStokId == legacyStokId);
        batchRepo.LoadByNaturalKey(brgId, doId).HasValue.Should().BeFalse();
        mutasiRepo.Exists(trsReffId, MovementKindEnum.GoodsReceipt, draft.MutasiInserts[0].StokLokasiId)
            .Should().BeFalse();
        bindingRepo.FindByLegacyBukuId(legacyBukuId).HasValue.Should().BeFalse();
    }

    private static StockConsequenceDraft BuildDraft(
        string brgId,
        string doId,
        string trsReffId,
        string legacyBukuId,
        string legacyStokId)
    {
        var batch = StockBatchModel.Create(brgId, doId, hpp: 100m, TglMasuk);
        var lokasi = batch.IncreaseLokasi(LayananId, TglEd, qty: 7);
        var mutasi = StockMovementModel.CreateInbound(
            lokasi.StokLokasiId, batch.StokBatchId, brgId, doId, LayananId, TglEd,
            trsReffId, MovementKindEnum.GoodsReceipt, qtyIn: 7, hpp: 100m, TglMutasi);

        return new StockConsequenceDraft(
            BatchUpserts: [batch],
            MutasiInserts: [mutasi],
            BindingInserts: [
                StockLegacyBindingModel.CreateMutasiBuku(
                    mutasi.StokMutasiId, legacyBukuId, trsReffId,
                    lokasi.StokLokasiId, legacyStokId)
            ],
            ScopeUpdates: [],
            LegacyOperations: [
                new LegacyInboundWriteOperation(new LegacyInboundWriteRequest(
                    brgId, doId, LayananId,
                    Qty: 7, Hpp: 100m, TglEd, TglMasuk, TglMutasi,
                    TrsReffId: trsReffId,
                    MovementKindString: "DO",
                    LegacyBukuId: legacyBukuId,
                    LegacyStokId: legacyStokId))
            ],
            UserId: "tester");
    }
}
