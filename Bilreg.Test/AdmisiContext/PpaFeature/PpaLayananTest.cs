using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Infrastructure.AdmisiContext.PpaFeature;
using Bilreg.Infrastructure.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Test.AdmisiContext.PpaFeature;

public class PpaLayananTest
{
    private readonly PpaLayananDal _sut = new(ConnStringHelper.GetTestEnv());

    private static PpaLayananDto Faker()
        => new PpaLayananDto("A", "B", 1, "C");
    
    [Fact]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(new List<PpaLayananDto>{Faker()});
    }
    
    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Delete(PpaType.Key("A"));
    }

    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(new List<PpaLayananDto>{Faker()});
        var actual = _sut.ListData(PpaType.Key("A"));
        actual.Should().ContainEquivalentOf(Faker());
    }
}