using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;
using Bilreg.Infrastructure.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Test.BillContext.KamarOperasiFeature;

public class OpCaseAktifDalTest
{
    private readonly OpCaseAktifDal _sut = new(ConnStringHelper.GetTestEnv());

    private static OpCaseAktifDto Faker()
        => new OpCaseAktifDto(
            OrderOpId: "A",
            OrderOpDate: new DateTime(2024, 1, 1, 10, 0, 0),
            PasienId: "B",
            OrderOpState: 1,
            PasienName: "C",
            TglLahir: "2000-01-01",
            Gender: "D"
        );

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
            opt => opt.Excluding(x => x.PasienName)
                .Excluding(x => x.TglLahir)
                .Excluding(x => x.Gender));
    }
    
    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var actual = _sut.ListData();
        actual.Should().ContainEquivalentOf(Faker(),
            opt => opt.Excluding(x => x.PasienName)
                .Excluding(x => x.TglLahir)
                .Excluding(x => x.Gender));
    }
}