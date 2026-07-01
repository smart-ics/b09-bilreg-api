using Farinv.Domain.BrgContext.PricingPolicyFeature;
using Farinv.Infrastructure.BrgContext.PricingPolicyFeature;
using Farinv.Infrastructure.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Farinv.Test.BrgContext.PricingPolicyFeature;

public class TipeBrgDalTest
{
    private readonly TipeBrgDal _sut = new(ConnStringHelper.GetTestEnv());

    private static TipeBrgDto Faker()
        => new TipeBrgDto(
            fs_kd_tipe_barang: "T1",
            fs_nm_tipe_barang: "Tipe Barang A",
            fb_aktif: true,
            fn_biaya_per_barang: 10000,
            fn_biaya_per_racik: 5000,
            fn_profit: 2000,
            fn_tax: 1000,
            fn_diskon: 500
        );

    private static ITipeBrgKey FakerKey()
        => TipeBrgType.Default with { TipeBrgId = "T1" };

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
        _sut.Delete(FakerKey());
    }

    [Fact]
    public void GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var actual = _sut.GetData(FakerKey());
        actual.Should().BeEquivalentTo(Faker());
    }

    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var actual = _sut.ListData();
        actual.Should().ContainEquivalentOf(Faker());
    }
}
