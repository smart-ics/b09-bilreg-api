using Bilreg.Infrastructure.Helpers;
using FluentAssertions;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Infrastructure.AdmisiContext.RujukanSub.CaraMasukDkAgg;

public class CaraMasukDkDalTest
{
    private readonly CaraMasukDkDal _sut = new(ConnStringHelper.GetTestEnv());

    private static ICaraMasukDkKey FakerKey()
        => CaraMasukDkType.Key("A");

    [Fact]
    public void GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        var actual = () => _sut.GetData(FakerKey());
        actual.Should().NotThrow<Exception>();
    }
    
    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var actual = () => _sut.ListData();
        actual.Should().NotThrow<Exception>();
    }
}