using Bilreg.Domain.IgdContext.TindakanIgdFeature;
using Bilreg.Infrastructure.IgdContext.TindakanIgdFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Test.IgdContext.TindakanIgdFeature;

public class TindakanIgdPpaDalTest
{
    private readonly TindakanIgdPpaDal _sut = new(ConnStringHelper.GetTestEnv());

    private static ITindakanIgdKey FakerKey() => TindakanIgdModel.Key("TTKI0000001");

    [Fact]
    public void InsertAndListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var items = new[]
        {
            new TindakanIgdPpaDto("TTKI0000001", "K01", "DK01", "Dr. A"),
            new TindakanIgdPpaDto("TTKI0000001", "K02", "PR01", "Ns. B")
        };

        _sut.Insert(items);
        var actual = _sut.ListData(FakerKey()).ToList();

        actual.Should().BeEquivalentTo(items);
    }

    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert([new TindakanIgdPpaDto("TTKI0000001", "K01", "DK01", "Dr. A")]);

        _sut.Delete(FakerKey());
        var actual = _sut.ListData(FakerKey()).ToList();

        actual.Should().BeEmpty();
    }
}
