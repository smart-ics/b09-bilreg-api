using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Infrastructure.AdmisiContext.RegFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.AdmisiContext.RegFeature;

public class RegAktifDalTest
{
    private readonly RegAktifDal _sut = new(ConnStringHelper.GetTestEnv());

    private static RegAktifDto Faker()
        => new RegAktifDto(
            RegId: "A",
            RegDate: new DateTime(2024, 1, 2),
            PasienId: "C",
            JenisReg: "D",
            LayananId: "E",
            DokterId: "F",
            TipeJaminanId: "G",
            PasienName: "H",
            TglLahir: "2000-01-03",
            Gender: "I",
            LayananName: "J",
            TipeJaminanName: "K",
            DokterName: "L"
        );

    private static IRegKey FakerKey()
        => RegModel.Key("A");

    private static ILayananKey FakerLayananKey()
        => LayananType.Default with { LayananId = "E" };

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
            opt => opt.Excluding(x => x.PasienName)
                      .Excluding(x => x.TglLahir)
                      .Excluding(x => x.Gender)
                      .Excluding(x => x.LayananName)
                      .Excluding(x => x.TipeJaminanName)
                      .Excluding(x => x.DokterName));
    }

    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var testData = Faker();
        _sut.Insert(testData);

        var actual = _sut.ListData(FakerLayananKey());

        actual.Should().ContainEquivalentOf(testData, opt => opt
            .Excluding(x => x.PasienName) // Data dari JOIN
            .Excluding(x => x.TglLahir)   // Data dari JOIN
            .Excluding(x => x.Gender)     // Data dari JOIN
            .Excluding(x => x.LayananName) // Data dari JOIN
            .Excluding(x => x.DokterName) // Data dari JOIN
            .Excluding(x => x.TipeJaminanName) // Data dari JOIN
        );
    }
}
