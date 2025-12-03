using Bilreg.Domain.PasienContext.StatusSosialFeature;
using Bilreg.Infrastructure.PasienContext.StatusSosialFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Test.PasienContext.StatusSosialFeature;

public class PendidikanDkDalTest
{
    private readonly PendidikanDkDal _sut;

    public PendidikanDkDalTest()
    {
        _sut = new PendidikanDkDal(ConnStringHelper.GetTestEnv());
    }

    [Fact]
    public void UT1_InsertTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(new PendidikanDkType("A", "B"));
    }

    [Fact]
    public void UT2_UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Update(new PendidikanDkType("A", "B"));
    }
    [Fact]
    public void UT3_DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Delete(new PendidikanDkType("A", "B"));
    }

    [Fact]
    public void UT4_GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new PendidikanDkType("A", "B");
        _sut.Insert(expected);
        var actual = _sut.GetData(expected).Value;
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void UT5_ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new PendidikanDkType("A", "B");
        _sut.Insert(new PendidikanDkType("A", "B"));
        var actual = _sut.ListData().Value;
        actual.Should().ContainEquivalentOf(expected);
    }
}