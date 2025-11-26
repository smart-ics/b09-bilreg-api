using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Infrastructure.AdmisiContext.RegFeature;
using Bilreg.Infrastructure.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Test.AdmisiContext.RegFeature;

public class RegKomponenDalTest
{
    private readonly RegKomponenDal _sut = new(ConnStringHelper.GetTestEnv());

    private static IEnumerable<RegKomponenDto> FakerList()
        => new List<RegKomponenDto>
        {
            new RegKomponenDto(
                fs_kd_reg: "A",
                fs_kd_detil_tarif: "B",
                fn_tarif: 1003m,
                fn_diskon: 102m,
                fs_kd_petugas_medis: "C",
                fs_nm_detil_tarif: "D",
                fs_nm_petugas_medis: "E"
            ),
            new RegKomponenDto(
                fs_kd_reg: "A",
                fs_kd_detil_tarif: "F",
                fn_tarif: 2001m,
                fn_diskon: 201m,
                fs_kd_petugas_medis: "G",
                fs_nm_detil_tarif: "H",
                fs_nm_petugas_medis: "I"
            )
        };

    private static IRegKey FakerKey()
        => RegModel.Key("A");

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
            opt => opt.Excluding(x => x.fs_nm_detil_tarif)
                .Excluding(x => x.fs_nm_petugas_medis));
    }
}
