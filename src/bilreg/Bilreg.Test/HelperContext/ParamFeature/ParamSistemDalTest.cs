using Bilreg.Domain.Shared.Param;
using Bilreg.Infrastructure.Shared.Helpers;
using Bilreg.Infrastructure.Shared.Param;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Test.HelperContext.ParamFeature;

public class ParamSistemDalTest
{
    private readonly ParamSistemDal _sut;

    public ParamSistemDalTest()
    {
        _sut = new ParamSistemDal(ConnStringHelper.GetTestEnv());
    }

    [Fact]
    public void InsertTest()
    {
        // Arrange
        using var trans = TransHelper.NewScope();
        var expected = new ParamSistemModel("A", "B", "C");
        _sut.Insert(expected);
    }
    [Fact]
    public void UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new ParamSistemModel("A", "B", "C");
        _sut.Update(expected);
    }
    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new ParamSistemModel("A", "B", "C");
        _sut.Delete(expected);
    }

    [Fact]
    public void GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new ParamSistemModel("A", "B", "C");
        _sut.Insert(expected);
        var actual = _sut.GetData("A");
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected =  new ParamSistemModel("A", "B", "C") ;
        _sut.Insert(expected);
        var actual = _sut.ListData();
        actual.Should().ContainEquivalentOf(expected);
    }
}