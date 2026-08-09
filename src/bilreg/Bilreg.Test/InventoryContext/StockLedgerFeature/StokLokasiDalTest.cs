using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.InventoryContext.StockLedgerFeature;

public class StokLokasiDalTest
{
    private readonly StokLokasiDal _sut = new(ConnStringHelper.GetTestEnv());
    private readonly StockBatchDal _batchDal = new(ConnStringHelper.GetTestEnv());

    private static readonly DateTime TglMasuk = new(2026, 3, 1, 10, 0, 0);
    private static readonly DateTime TglEd = new(2027, 6, 30);

    private void EnsureParentBatch(string batchId)
    {
        _batchDal.Insert(new StockBatchDto(
            batchId, "BRG0000000001", "DO00000001", 10, 100m, TglMasuk, "",
            0, "U001", TglMasuk, "U001", TglMasuk));
    }

    private static StokLokasiDto FakerDto(string lokasiId = "STLTEST00001", string batchId = "STBTEST00001") =>
        new(
            StokLokasiId: lokasiId,
            StokBatchId: batchId,
            BrgId: "BRG0000000001",
            BrgMasukReffId: "DO00000001",
            LayananId: "LY001",
            TglEd: TglEd,
            NoBatch: "NB-1",
            QtySisa: 10,
            Version: 0,
            TglMasuk: TglMasuk,
            CrtUser: "U001",
            CrtDate: TglMasuk,
            UpdUser: "U001",
            UpdDate: TglMasuk);

    [Fact]
    public void InsertGet_RoundTrip_PreservesDenormalizedTglMasuk()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        EnsureParentBatch("STBTEST00001");
        var expected = FakerDto();
        _sut.Insert(expected);

        var actual = _sut.GetData(LocationStockBalanceModel.Key(expected.StokLokasiId));

        actual.Should().NotBeNull();
        actual.Should().BeEquivalentTo(expected);
        actual.TglMasuk.Should().Be(TglMasuk);
    }

    [Fact]
    public void UpdateToZero_RetainsDepletedRow()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        EnsureParentBatch("STBTEST00001");
        var inserted = FakerDto();
        _sut.Insert(inserted);

        var depleted = inserted with { QtySisa = 0, Version = 1, UpdUser = "U002" };
        var affected = _sut.UpdateConditional(depleted, expectedVersion: 0);

        affected.Should().Be(1);
        var actual = _sut.GetData(LocationStockBalanceModel.Key(inserted.StokLokasiId));
        actual.Should().NotBeNull();
        actual.QtySisa.Should().Be(0);
        actual.Version.Should().Be(1);
        actual.TglMasuk.Should().Be(TglMasuk);
    }
}
