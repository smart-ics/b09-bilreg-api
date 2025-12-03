using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Infrastructure.AdmisiContext.JaminanFeature;

public class PolisCoverDalTest
{
    private readonly PolisCoverDal _sut = new(ConnStringHelper.GetTestEnv());

    private static IEnumerable<PolisCoverDto> FakerList()
        => new List<PolisCoverDto>
        {
            new PolisCoverDto(
                fs_kd_polis: "A",
                fs_mr: "B",
                fs_kd_status: "C",
                fs_nm_pasien: "D",
                fd_tgl_lahir: "E",
                fs_jns_kelamin: "F"
            ),
            new PolisCoverDto(
                fs_kd_polis: "A",
                fs_mr: "G",
                fs_kd_status: "H",
                fs_nm_pasien: "I",
                fd_tgl_lahir: "J",
                fs_jns_kelamin: "K"
            )
        };

    private static IPolisKey FakerKey()
        => PolisModel.Key("A");

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
            opt => opt.Excluding(x => x.fs_nm_pasien)
                .Excluding(x => x.fd_tgl_lahir)
                .Excluding(x => x.fs_jns_kelamin));
    }
}