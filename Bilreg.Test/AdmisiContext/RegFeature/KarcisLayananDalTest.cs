using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Infrastructure.AdmisiContext.RegFeature;
using Bilreg.Infrastructure.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Test.AdmisiContext.RegFeature;

public class KarcisLayananDalTest
{
    private readonly KarcisLayananDal _sut = new(ConnStringHelper.GetTestEnv());

    private static IEnumerable<KarcisLayananDto> FakerList()
        => new List<KarcisLayananDto>
        {
            new KarcisLayananDto(
                fs_kd_karcis: "A",
                fs_kd_layanan: "B",
                fs_nm_layanan: "C"
            ),
            new KarcisLayananDto(
                fs_kd_karcis: "A",
                fs_kd_layanan: "D",
                fs_nm_layanan: "E"
            )
        };

    private static IKarcisKey FakerKey()
        => KarcisType.Key("A");

    [Fact]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(FakerList());
    }

    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Delete(FakerKey());
    }

    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(FakerList());
        var actual = _sut.ListData(FakerKey());
        actual.Should().BeEquivalentTo(FakerList(),
            opt => opt.Excluding(x => x.fs_nm_layanan));
    }
}
