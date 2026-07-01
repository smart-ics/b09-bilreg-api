using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Infrastructure.BedUsageContext.WardFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Test.BillContext.TarifFeature;

public class KelasDkDalTest
{
    private readonly KelasDkDal _sut = new(ConnStringHelper.GetTestEnv());

    private static IKelasDkKey FakerKey() => KelasDkType.Key("1");

    [Fact]
    public void GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        var actual = _sut.GetData(FakerKey());
        actual.Should().NotBeNull();
    }
    
    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var actual = _sut.ListData();
        actual.Should().NotBeNull();
    }
}