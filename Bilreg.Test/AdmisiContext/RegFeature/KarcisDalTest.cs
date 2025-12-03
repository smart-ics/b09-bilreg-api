using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Infrastructure.AdmisiContext.RegFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Test.AdmisiContext.RegFeature;

public class KarcisDalTest
{
    private readonly KarcisDal _sut = new(ConnStringHelper.GetTestEnv());

    private static KarcisDto Faker()
        => new("A", "B", 1001, "C", "D", "E", true,
             "F", "G", "H");

    private static IKarcisKey FakerKey()
        => KarcisType.Key("A");

    private static IInstalasiDkKey FakerInstalasiDkKey()
        => InstalasiDkType.Key("C");

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
            opt => opt.Excluding(x => x.fs_nm_instalasi_dk)
                      .Excluding(x => x.fs_nm_rekap_cetak)
                      .Excluding(x => x.fs_nm_tarif));
    }
    
    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var karcis = Faker();
        _sut.Insert(karcis);
        
        var actual = _sut.ListData(FakerInstalasiDkKey());
        actual.Should().ContainEquivalentOf(karcis,
            opt => opt.Excluding(x => x.fs_nm_instalasi_dk)
                      .Excluding(x => x.fs_nm_rekap_cetak)
                      .Excluding(x => x.fs_nm_tarif));
    }
}
