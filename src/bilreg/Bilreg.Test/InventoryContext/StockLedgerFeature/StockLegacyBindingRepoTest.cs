using System.Data.SqlClient;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.InventoryContext.StockLedgerFeature;

public class StockLegacyBindingRepoTest
{
    private readonly StockLegacyBindingRepo _sut = new(
        new StokLegacyBindingDal(ConnStringHelper.GetTestEnv()));

    [Fact]
    public void Insert_AndFindByLegacyBukuId_RoundTrip()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        var binding = StockLegacyBindingModel.CreateMutasiBuku(
            stokMutasiId: "STMTEST00001",
            legacyBukuId: "BK00000001",
            trsReffId: "TR00000001");

        _sut.Insert(binding);

        var byBuku = _sut.FindByLegacyBukuId("BK00000001");
        byBuku.HasValue.Should().BeTrue();
        byBuku.Value.BindingId.Should().Be(binding.BindingId);
        byBuku.Value.StokMutasiId.Should().Be("STMTEST00001");

        var byMutasi = _sut.FindByStokMutasiId("STMTEST00001");
        byMutasi.HasValue.Should().BeTrue();
        byMutasi.Value.LegacyBukuId.Should().Be("BK00000001");
    }

    [Fact]
    public void Insert_DuplicateLegacyBukuId_Fails()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        var first = StockLegacyBindingModel.CreateMutasiBuku("STMTEST00001", "BK00000001", "TR00000001");
        var second = StockLegacyBindingModel.CreateMutasiBuku("STMTEST00002", "BK00000001", "TR00000002");

        _sut.Insert(first);
        var act = () => _sut.Insert(second);

        act.Should().Throw<SqlException>();
    }

    [Fact]
    public void Insert_DuplicateStokMutasiId_Fails()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        var first = StockLegacyBindingModel.CreateMutasiBuku("STMTEST00001", "BK00000001", "TR00000001");
        var second = StockLegacyBindingModel.CreateMutasiBuku("STMTEST00001", "BK00000002", "TR00000002");

        _sut.Insert(first);
        var act = () => _sut.Insert(second);

        act.Should().Throw<SqlException>();
    }
}
