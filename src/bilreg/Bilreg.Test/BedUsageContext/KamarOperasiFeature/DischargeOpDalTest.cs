using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.BedUsageContext.KamarOperasiFeature;

public class DischargeOpDalTest
{
    private readonly DischargeOpDal _sut = new(ConnStringHelper.GetTestEnv());

    private static DischargeOpDto Faker()
        => new DischargeOpDto
        (
            "DO001",
            new DateTime(2023, 1, 4),
            "OP001",
            "PS001",
            "REG001",
            "KM001",
            "PPA001",
            1,
            "Post OP OK",
            "crt",
            new DateTime(2023, 1, 4),
            "upd",
            new DateTime(2023, 1, 4),
            "vod",
            new DateTime(2023, 1, 4),
            "Pname",
            "2000-01-01",
            "M",
            "Kname",
            "Ppaname"
        );

    private static IDischargeOpKey FakerKey()
        => DischargeOpModel.Key("DO001");

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
                .Excluding(x => x.PasienName)
                .Excluding(x => x.TglLahir)
                .Excluding(x => x.Gender)
                .Excluding(x => x.KamarName)
                .Excluding(x => x.PpaName));
    }

    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());

        var actual = _sut.ListData(new DateTime(2023, 1, 4));

        actual.Should().ContainEquivalentOf(Faker(),
            opt => opt
                .Excluding(x => x.PasienName)
                .Excluding(x => x.TglLahir)
                .Excluding(x => x.Gender)
                .Excluding(x => x.KamarName)
                .Excluding(x => x.PpaName));
    }
}
