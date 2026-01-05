using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Infrastructure.ChargeContext.TarifFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.ChargeContext.TarifFeature;

public class NilaiTarifDalTest
{
    private readonly NilaiTarifDal _sut = new(ConnStringHelper.GetTestEnv());

    private static NilaiTarifDto Faker()
        => new NilaiTarifDto("A", "B", "C", "D", 100000, "E", "F", "G");

    private static INilaiTarifKey FakerKey()
        => NilaiTarifType.Default with { NilaiTarifId = "A" };

    private static ITarifKey FakerTarifKey()
        => TarifType.Key("B");

    private static ILayananKey FakerLayananKey()
        => LayananType.Key("H");

    private static INilaiTarifVariant FakerVariant()
        => NilaiTarifType.Default with { TipeTarif = new TipeTarifReff("C", "-"), Kelas = new KelasReff("D", "-") };

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
        actual.Should().BeEquivalentTo(Faker(),
            opt => opt
                .Excluding(x => x.TarifName)
                .Excluding(x => x.TipeTarifName)
                .Excluding(x => x.KelasName));
    }
    
    [Fact]
    public void ListDataByTarifKeyTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var actual = _sut.ListData(FakerTarifKey());
        actual.Should().ContainEquivalentOf(Faker(),
            opt => opt
                .Excluding(x => x.TarifName)
                .Excluding(x => x.TipeTarifName)
                .Excluding(x => x.KelasName));
    }
}