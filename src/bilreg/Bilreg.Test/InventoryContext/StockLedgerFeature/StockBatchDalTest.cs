using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.InventoryContext.StockLedgerFeature;

public class StockBatchDalTest
{
    private readonly StockBatchDal _sut = new(ConnStringHelper.GetTestEnv());

    private static StockBatchDto FakerDto(string id = "STBTEST00001") =>
        new(
            StokBatchId: id,
            BrgId: "BRG0000000001",
            BrgMasukReffId: "DO00000001",
            QtySisa: 10,
            Hpp: 1500.50m,
            TglMasuk: new DateTime(2026, 3, 1, 10, 0, 0),
            PoReffId: "PO00000001",
            Version: 0,
            CrtUser: "U001",
            CrtDate: new DateTime(2026, 3, 1, 9, 0, 0),
            UpdUser: "U001",
            UpdDate: new DateTime(2026, 3, 1, 9, 0, 0));

    [Fact]
    public void InsertGet_RoundTrip()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        var expected = FakerDto();
        _sut.Insert(expected);

        var actual = _sut.GetData(StockBatchModel.Key(expected.StokBatchId));

        actual.Should().NotBeNull();
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void UpdateConditional_AssignsFinalVersion_WhenExpectedMatches()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        var inserted = FakerDto();
        _sut.Insert(inserted);

        var update = inserted with { QtySisa = 7, Version = 1, UpdUser = "U002" };
        var affected = _sut.UpdateConditional(update, expectedVersion: 0);
        var stale = _sut.UpdateConditional(update with { QtySisa = 1, Version = 2 }, expectedVersion: 0);

        affected.Should().Be(1);
        stale.Should().Be(0);
        var actual = _sut.GetData(StockBatchModel.Key(inserted.StokBatchId));
        actual.QtySisa.Should().Be(7);
        actual.Version.Should().Be(1);
    }
}
