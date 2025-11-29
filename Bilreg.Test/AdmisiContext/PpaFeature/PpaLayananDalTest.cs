using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Infrastructure.AdmisiContext.PpaFeature;
using Bilreg.Infrastructure.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Test.AdmisiContext.PpaFeature;

public class PpaLayananDalTest
{
    private readonly PpaLayananDal _sut = new(ConnStringHelper.GetTestEnv());

    private static IEnumerable<PpaLayananDto> FakerList()
        => new List<PpaLayananDto>
        {
            new PpaLayananDto(
                fs_kd_peg: "A",
                fs_kd_layanan: "B",
                fb_utama: 1,
                fs_nm_peg: "C",
                fs_nm_layanan: "D"
            ),
            new PpaLayananDto(
                fs_kd_peg: "A",
                fs_kd_layanan: "E",
                fb_utama: 0,
                fs_nm_peg: "F",
                fs_nm_layanan: "G"
            )
        };

    private static IPpaKey FakerKey()
        => PpaType.Key("A");

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
            opt => opt.Excluding(x => x.fs_nm_peg)
                .Excluding(x => x.fs_nm_layanan));
    }
}