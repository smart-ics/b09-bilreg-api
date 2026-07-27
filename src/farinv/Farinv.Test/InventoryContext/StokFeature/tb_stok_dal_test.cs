using Farinv.Domain.BrgContext.BrgFeature;
using Farinv.Domain.InventoryContext.StokFeature;
using Farinv.Infrastructure.Helpers;
using Farinv.Infrastructure.InventoryContext.StokFeature;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

//  resharper disable inconsistentnaming
namespace Farinv.Test.InventoryContext.StokFeature;

public class tb_stok_dalTest
{
    private readonly tb_stok_dal _sut = new(ConnStringHelper.GetTestEnv());

    private static tb_stok_dto Faker()
        => new tb_stok_dto("A", "B", "C", "D", "E", "F", "G", 
            1, 2, 3, "H", "I", "J", "K", "L", "M", "N", "O");

    private static IBrgKey FakerBrgKey()
        => BrgObatType.Key("B");

    private static ILayananKey FakerLayananKey()
        => LayananType.Key("C");

    [Fact]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
    }
    
    [Fact]
    public void UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Update(Faker());
    }

    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Delete("A");
    }

    [Fact]
    public void GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var actual = _sut.GetData("A");
        actual.Should().BeEquivalentTo(Faker(),
            opt => opt
                .Excluding(x => x.fs_nm_barang)
                .Excluding(x => x.fs_nm_layanan));
    }
    
    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var actual = _sut.ListData(FakerBrgKey(), FakerLayananKey());
        actual.Should().ContainEquivalentOf(Faker(),
            opt => opt
                .Excluding(x => x.fs_nm_barang)
                .Excluding(x => x.fs_nm_layanan));
    }
}