using System.Data.SqlClient;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.InventoryContext.StockLedgerFeature;

public class StokMutasiDalTest
{
    private readonly StokMutasiDal _sut = new(ConnStringHelper.GetTestEnv());

    private static readonly DateTime Tgl = new(2026, 3, 1, 10, 0, 0);

    private static StokMutasiDto FakerDto(
        string mutasiId = "STMTEST00001",
        string lokasiId = "STLTEST00001",
        string trsReffId = "TR00000001") =>
        new(
            StokMutasiId: mutasiId,
            StokLokasiId: lokasiId,
            StokBatchId: "STBTEST00001",
            BrgId: "BRG0000000001",
            BrgMasukReffId: "DO00000001",
            LayananId: "LY001",
            TglEd: new DateTime(2027, 6, 30),
            TrsReffId: trsReffId,
            MovementKind: (int)MovementKindEnum.GoodsReceipt,
            QtyIn: 10,
            QtyOut: 0,
            Hpp: 100m,
            PoReffId: "",
            TglMutasi: Tgl,
            ReversesMutasiId: "",
            CrtUser: "U001",
            CrtDate: Tgl,
            UpdUser: "U001",
            UpdDate: Tgl);

    [Fact]
    public void Insert_AndExists_RoundTrip()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        var expected = FakerDto();
        _sut.Insert(expected);

        _sut.Exists(expected.TrsReffId, expected.MovementKind, expected.StokLokasiId)
            .Should().BeTrue();
        var list = _sut.ListByTrsReffId(expected.TrsReffId).ToList();
        list.Should().ContainSingle();
        list[0].Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void Insert_DuplicateIdempotencyKey_Fails()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        var first = FakerDto("STMTEST00001");
        var duplicate = FakerDto("STMTEST00002"); // same TrsReffId + Kind + StokLokasiId

        _sut.Insert(first);
        var act = () => _sut.Insert(duplicate);

        act.Should().Throw<SqlException>();
    }
}
