using Bilreg.Domain.IgdContext.TindakanIgdFeature;
using Bilreg.Infrastructure.IgdContext.TindakanIgdFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Test.IgdContext.TindakanIgdFeature;

public class TindakanIgdBarangDalTest
{
    private readonly TindakanIgdBarangDal _sut = new(ConnStringHelper.GetTestEnv());

    private static ITindakanIgdKey FakerKey() => TindakanIgdModel.Key("TTKI0000001");

    [Fact]
    public void InsertAndListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var items = new[]
        {
            new TindakanIgdBarangDto("TTKI0000001", 1, "BRG01", "PCS", 2m, "PCS", 2m, 2m, 0m, true, "PK001", "", false),
            new TindakanIgdBarangDto("TTKI0000001", 2, "BRG02", "BOX", 1m, "PCS", 10m, 4m, 6m, false, "PK001", "", false)
        };

        _sut.Insert(items);
        var actual = _sut.ListData(FakerKey()).ToList();

        actual.Should().BeEquivalentTo(items);
    }

    [Fact]
    public void UpdateBillingAndMarkVoidTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert([
            new TindakanIgdBarangDto("TTKI0000001", 1, "BRG01", "PCS", 2m, "PCS", 2m, 2m, 0m, true, "PK001", "", false)
        ]);

        _sut.UpdateBilling("TTKI0000001", 1, "TTKI0000001-B01");
        _sut.MarkVoid(FakerKey());
        var actual = _sut.ListData(FakerKey()).Single();

        actual.TrsBillingId.Should().Be("TTKI0000001-B01");
        actual.IsVoid.Should().BeTrue();
    }

    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert([
            new TindakanIgdBarangDto("TTKI0000001", 1, "BRG01", "PCS", 2m, "PCS", 2m, 2m, 0m, true, "PK001", "", false)
        ]);

        _sut.Delete(FakerKey());
        var actual = _sut.ListData(FakerKey()).ToList();

        actual.Should().BeEmpty();
    }
}
