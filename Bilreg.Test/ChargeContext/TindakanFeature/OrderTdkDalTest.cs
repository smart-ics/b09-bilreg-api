using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Infrastructure.ChargeContext.TindakanFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.ChargeContext.TindakanFeature;

public class OrderTdkDalTest
{
    private readonly OrderTdkDal _sut = new(ConnStringHelper.GetTestEnv());

    private static OrderTdkDto Faker()
        => new OrderTdkDto("A", new DateTime(2023, 1, 1), "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", 1, "L", 
            new DateTime(2023, 1, 2), "M", new DateTime(2023, 1, 3), "N", new DateTime(2023, 1, 4), "2020-10-01", "M");

    private static IOrderTdkKey FakerKey()
        => OrderTdkModel.Default with { OrderTdkId = "A" };

    private static IPasienKey FakerFilter()
        => PasienModel.Key("B");

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
                .Excluding(x => x.TglLahir)
                .Excluding(x => x.Gender));
    }
    
    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var actual = _sut.ListData(FakerFilter());
        actual.Should().ContainEquivalentOf(Faker(),
            opt => opt
                .Excluding(x => x.TglLahir)
                .Excluding(x => x.Gender));
    }
}