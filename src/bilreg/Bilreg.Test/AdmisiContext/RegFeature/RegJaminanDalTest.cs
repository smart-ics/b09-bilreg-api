//  resharper disable inconsistentnaming

using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Infrastructure.AdmisiContext.RegFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Test.AdmisiContext.RegFeature;

public class RegJaminanDalTest
{
    private readonly RegJaminanDal _sut = new(ConnStringHelper.GetTestEnv());

    private static RegJaminanDto Faker()
        => new RegJaminanDto(
            fs_kd_reg: "A",
            fs_kd_polis: "B",
            fs_no_polis: "C",
            fs_atas_nama: "D"
        );

    private static IRegKey FakerKey()
        => RegModel.Key("A");

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
            opt => opt.Excluding(x => x.fs_no_polis)
                .Excluding(x => x.fs_atas_nama));
    }
}
