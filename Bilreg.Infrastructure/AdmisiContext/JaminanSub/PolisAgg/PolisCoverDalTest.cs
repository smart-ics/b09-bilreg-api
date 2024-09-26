using Bilreg.Domain.AdmisiContext.JaminanSub.PolisAgg;
using Bilreg.Infrastructure.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Infrastructure.AdmisiContext.JaminanSub.PolisAgg;

public class PolisCoverDalTest
{
    private readonly PolisCoverDal _sut;

    public PolisCoverDalTest()
    {
        _sut = new PolisCoverDal(ConnStringHelper.GetTestEnv());
    }

    private PolisCoverModel Faker()
    {
        return new PolisCoverDto
        {
            fs_kd_polis = "A",
            fs_kd_status = "B",
            fs_mr = "C",
            fs_nm_pasien = ""
        };
    }
    [Fact]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(new List<PolisCoverModel>(){Faker()});
    }

    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Delete(Faker());
    }

    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(new List<PolisCoverModel>(){Faker()});
        var actual = _sut.ListData(Faker());
        actual.Should().ContainEquivalentOf(Faker());
    }
}