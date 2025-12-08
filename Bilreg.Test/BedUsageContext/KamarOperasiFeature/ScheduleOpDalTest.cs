using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.BedUsageContext.KamarOperasiFeature;

public class ScheduleOpDalTest
{
    private readonly ScheduleOpDal _sut = new(ConnStringHelper.GetTestEnv());

    private static ScheduleOpDto Faker()
        => new ScheduleOpDto("A", new DateTime(2023,1,2), "B", new DateTime(2023, 1, 1), "C", new DateTime(2023, 1, 2), "D", 
            new DateTime(2023, 1, 3), "E", "F", 1, 120, new DateTime(2023, 1, 4), "G", "H", "I", 
            new DateTime(2023, 1, 5), "J", "K", "2023-01-06", "M", "N", "O");

    private static IScheduleOpKey FakerKey()
        => ScheduleOpModel.Key("A");

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
                .Excluding(x => x.OrderDate)
                .Excluding(x => x.NamaOperasi)
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
        var actual = _sut.ListData();
        actual.Should().ContainEquivalentOf(Faker(),
            opt => opt
                .Excluding(x => x.OrderDate)
                .Excluding(x => x.NamaOperasi)
                .Excluding(x => x.PasienName)
                .Excluding(x => x.TglLahir)
                .Excluding(x => x.Gender)
                .Excluding(x => x.KamarName)
                .Excluding(x => x.PpaName));
    }
}