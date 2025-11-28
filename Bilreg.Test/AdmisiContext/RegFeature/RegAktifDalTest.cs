using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Infrastructure.AdmisiContext.RegFeature;
using Bilreg.Infrastructure.Helpers;
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
            JenisRawat: "D",
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

    private static Periode FakerPeriode()
        => new Periode(new DateTime(2024, 1, 1), new DateTime(2024, 1, 31));

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
        var regAktif = Faker();
        _sut.Insert(regAktif);
        
        var actual = _sut.ListData(FakerPeriode());
        actual.Should().ContainEquivalentOf(regAktif,
            opt => opt.Excluding(x => x.PasienName)
                      .Excluding(x => x.TglLahir)
                      .Excluding(x => x.Gender)
                      .Excluding(x => x.LayananName)
                      .Excluding(x => x.TipeJaminanName)
                      .Excluding(x => x.DokterName));
    }
}
