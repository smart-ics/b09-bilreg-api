using Bilreg.Domain.PasienContext.StatusSosialFeature;
using Bilreg.Infrastructure.Helpers;
using Bilreg.Infrastructure.PasienContext.StatusSosialFeature;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Test.PasienContext.StatusSosialFeature;

public class AgamaDalTest
{
    private readonly AgamaDal _sut;

    public AgamaDalTest()
    {
        _sut = new AgamaDal(ConnStringHelper.GetTestEnv());
    }

    [Fact]
    public void UT1_InsertTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(new AgamaType("A", "B"));
    }

    [Fact]
    public void UT2_UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Update(new AgamaType("A", "B"));
    }
    [Fact]
    public void UT3_DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Delete(new AgamaType("A", "B"));
    }

    [Fact]
    public void UT4_GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new AgamaType("A", "B");
        _sut.Insert(expected);
        var actual = _sut.GetData(expected).Value;
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void UT5_ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new AgamaType("A", "B");
        _sut.Insert(new AgamaType("A", "B"));
        var actual = _sut.ListData().Value;
        actual.Should().ContainEquivalentOf(expected);
    }
}