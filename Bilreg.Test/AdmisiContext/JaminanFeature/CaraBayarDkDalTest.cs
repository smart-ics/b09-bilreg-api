using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Infrastructure.AdmisiContext.JaminanFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Test.AdmisiContext.JaminanFeature;

public class CaraBayarDkDalTest
{
    private readonly CaraBayarDkDal _sut;

    public CaraBayarDkDalTest()
    {
        _sut = new CaraBayarDkDal(ConnStringHelper.GetTestEnv());
    }

    [Fact]
    public void GivenNonExistData_ThenReturnNull_Test()
    {
        // ARRANGE
        using var trans = TransHelper.NewScope();
        var expected = new CaraBayarDkType("A", "B");

        // ACT
        var actual = _sut.GetData(expected);

        // ASSERT
        actual.Should().BeNull();
    }

    [Fact]
    public void GivenEmptyData_ThenReturnNull_Test()
    {
        // ARRANGE
        using var trans = TransHelper.NewScope();
        var exepected = new CaraBayarDkType("1", "Membayar Sendiri");
        // ACT
        var actual = _sut.ListData();

        // ASSERT
        var actualFirst = actual.First(x => x.CaraBayarDkId == "1");
        actualFirst.Should().BeEquivalentTo(exepected);
    }
}