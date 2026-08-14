using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.InventoryContext.StockLedgerFeature;

public class StockBatchRepoTest
{
    private readonly StockBatchRepo _sut = new(
        new StockBatchDal(ConnStringHelper.GetTestEnv()),
        new StokLokasiDal(ConnStringHelper.GetTestEnv()));

    private static readonly DateTime TglMasuk = new(2026, 3, 1, 10, 0, 0);
    private static readonly DateTime TglEd = new(2027, 6, 30);
    private const string BrgId = "BRG0000000001";
    private const string DoId = "DO00000001";
    private const string LayananId = "LY001";

    [Fact]
    public void SaveChanges_InsertAndReload_RoundTrip()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        var batch = StockBatchModel.Create(BrgId, DoId, hpp: 100m, TglMasuk, poReffId: "PO00000001");
        batch.IncreaseLokasi(LayananId, TglEd, qty: 10, noBatch: "NB-1");

        _sut.SaveChanges(batch);

        var loaded = _sut.LoadEntity(batch);
        loaded.HasValue.Should().BeTrue();
        var actual = loaded.Value;
        actual.StokBatchId.Should().Be(batch.StokBatchId);
        actual.QtySisa.Should().Be(10);
        actual.Version.Should().Be(batch.Version);
        actual.ListLokasi.Should().ContainSingle();
        var lokasi = actual.ListLokasi.Single();
        lokasi.QtySisa.Should().Be(10);
        lokasi.TglMasuk.Should().Be(TglMasuk);
        lokasi.NoBatch.Should().Be("NB-1");
    }

    [Fact]
    public void SaveChanges_Update_ThenReload_ReflectsQty()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        var batch = StockBatchModel.Create(BrgId, DoId, hpp: 100m, TglMasuk);
        batch.IncreaseLokasi(LayananId, TglEd, qty: 10);
        _sut.SaveChanges(batch);

        var reloaded = _sut.LoadEntity(batch).Value;
        reloaded.IncreaseLokasi(LayananId, TglEd, qty: 5);
        _sut.SaveChanges(reloaded);

        var actual = _sut.LoadEntity(batch).Value;
        actual.QtySisa.Should().Be(15);
        actual.ListLokasi.Single().QtySisa.Should().Be(15);
    }

    [Fact]
    public void SaveChanges_OccConflict_WhenVersionStale()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        var batch = StockBatchModel.Create(BrgId, DoId, hpp: 100m, TglMasuk);
        batch.IncreaseLokasi(LayananId, TglEd, qty: 10);
        _sut.SaveChanges(batch);

        var workerA = _sut.LoadEntity(batch).Value;
        var workerB = _sut.LoadEntity(batch).Value;

        workerA.IncreaseLokasi(LayananId, TglEd, qty: 5);
        _sut.SaveChanges(workerA);

        // Divergent delta so stale snapshot is detectable when Versions collide.
        workerB.IncreaseLokasi(LayananId, TglEd, qty: 1);
        var act = () => _sut.SaveChanges(workerB);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*concurrency*");
    }

    [Fact]
    public void SaveChanges_OccConflict_WhenSameDeltaStale()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        var batch = StockBatchModel.Create(BrgId, DoId, hpp: 100m, TglMasuk);
        batch.IncreaseLokasi(LayananId, TglEd, qty: 10);
        _sut.SaveChanges(batch);

        var workerA = _sut.LoadEntity(batch).Value;
        var workerB = _sut.LoadEntity(batch).Value;

        workerA.IncreaseLokasi(LayananId, TglEd, qty: 5);
        _sut.SaveChanges(workerA);

        workerB.IncreaseLokasi(LayananId, TglEd, qty: 5);
        var act = () => _sut.SaveChanges(workerB);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*concurrency*");
    }

    [Fact]
    public void SaveChanges_MultiMutation_ThenOneSave_PersistsFinalQtyAndVersion()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        var batch = StockBatchModel.Create(BrgId, DoId, hpp: 100m, TglMasuk);
        batch.IncreaseLokasi(LayananId, TglEd, qty: 10);
        _sut.SaveChanges(batch);

        var reloaded = _sut.LoadEntity(batch).Value;
        var versionAtLoad = reloaded.Version;
        var lokasiVersionAtLoad = reloaded.ListLokasi.Single().Version;

        reloaded.IncreaseLokasi(LayananId, TglEd, qty: 3);
        reloaded.IncreaseLokasi(LayananId, TglEd, qty: 2);
        _sut.SaveChanges(reloaded);

        reloaded.QtySisa.Should().Be(15);
        reloaded.Version.Should().Be(versionAtLoad + 2);
        reloaded.PersistedVersion.Should().Be(reloaded.Version);
        var lokasi = reloaded.ListLokasi.Single();
        lokasi.QtySisa.Should().Be(15);
        lokasi.Version.Should().Be(lokasiVersionAtLoad + 2);
        lokasi.PersistedVersion.Should().Be(lokasi.Version);

        var actual = _sut.LoadEntity(batch).Value;
        actual.QtySisa.Should().Be(15);
        actual.Version.Should().Be(versionAtLoad + 2);
        actual.ListLokasi.Single().QtySisa.Should().Be(15);
        actual.ListLokasi.Single().Version.Should().Be(lokasiVersionAtLoad + 2);
    }

    [Fact]
    public void SaveChanges_DepletedLokasi_Retained()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        var batch = StockBatchModel.Create(BrgId, DoId, hpp: 100m, TglMasuk);
        batch.IncreaseLokasi(LayananId, TglEd, qty: 5);
        _sut.SaveChanges(batch);

        var reloaded = _sut.LoadEntity(batch).Value;
        reloaded.DecreaseLokasi(LayananId, TglEd, qty: 5);
        _sut.SaveChanges(reloaded);

        var actual = _sut.LoadEntity(batch).Value;
        actual.ListLokasi.Should().ContainSingle();
        actual.ListLokasi.Single().QtySisa.Should().Be(0);
        actual.QtySisa.Should().Be(0);
    }

    [Fact]
    public void LoadByNaturalKey_FindsBatch()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        var batch = StockBatchModel.Create(BrgId, DoId, hpp: 100m, TglMasuk);
        batch.IncreaseLokasi(LayananId, TglEd, qty: 3);
        _sut.SaveChanges(batch);

        var loaded = _sut.LoadByNaturalKey(BrgId, DoId);
        loaded.HasValue.Should().BeTrue();
        loaded.Value.StokBatchId.Should().Be(batch.StokBatchId);
    }

    [Fact]
    public void ListAllocationCandidates_ExcludesDepleted()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        var batch = StockBatchModel.Create(BrgId, DoId, hpp: 100m, TglMasuk);
        batch.IncreaseLokasi(LayananId, TglEd, qty: 5);
        batch.IncreaseLokasi("LY002", TglEd, qty: 2);
        _sut.SaveChanges(batch);

        var reloaded = _sut.LoadEntity(batch).Value;
        reloaded.DecreaseLokasi(LayananId, TglEd, qty: 5);
        _sut.SaveChanges(reloaded);

        var candidates = _sut.ListAllocationCandidates(BrgId, LayananId).ToList();
        candidates.Should().BeEmpty();

        var other = _sut.ListAllocationCandidates(BrgId, "LY002").ToList();
        other.Should().ContainSingle();
        other[0].QtySisa.Should().Be(2);
    }
}
