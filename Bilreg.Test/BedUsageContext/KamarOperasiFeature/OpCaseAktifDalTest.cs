using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;
using Bilreg.Infrastructure.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.BedUsageContext.KamarOperasiFeature;

public class OpCaseAktifDalTest
{
    private readonly OpCaseAktifDal _sut = new(ConnStringHelper.GetTestEnv());

    private static OpCaseAktifDto Faker()
        => new OpCaseAktifDto("A", new DateTime(2023, 1, 1), "B", 1, "C", "2000-01-01", 
            "M", "D", 2, 3, new DateTime(2023, 1, 2), "E", "F");

    private static IOrderOpKey FakerKey()
        => OrderOpModel.Key("A");

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
                .Excluding(x => x.NamaOperasi)
                .Excluding(x => x.EstimasiDurasi)
                .Excluding(x => x.UrgencyLevel)
                .Excluding(x => x.PreferedDate)
                .Excluding(x => x.DokterId)
                .Excluding(x => x.DokterName));
    }
    
    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var actual = _sut.ListData();
        actual.Should().ContainEquivalentOf(Faker(),
            opt => opt
                .Excluding(x => x.PasienName)
                .Excluding(x => x.TglLahir)
                .Excluding(x => x.Gender)
                .Excluding(x => x.NamaOperasi)
                .Excluding(x => x.EstimasiDurasi)
                .Excluding(x => x.UrgencyLevel)
                .Excluding(x => x.PreferedDate)
                .Excluding(x => x.DokterId)
                .Excluding(x => x.DokterName));
    }
}