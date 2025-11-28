using Bilreg.Domain.BillContext.TindakanFeature;
using Bilreg.Infrastructure.BillContext.TindakanFeature;
using Bilreg.Infrastructure.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Test.BillContext.TarifFeature;

public class KomponenSatTugasDalTest
{
    private readonly KomponenSatTugasDal _sut = new(ConnStringHelper.GetTestEnv());

    private static IEnumerable<KomponenSatTugasDto> FakerList()
        => new List<KomponenSatTugasDto>
        {
            new KomponenSatTugasDto(
                fs_kd_detil_tarif: "A",
                fs_kd_sat_tugas: "B",
                fs_nm_sat_tugas: "C",
                fs_kd_profesi: "D",
                fs_nm_profesi: "E"
            ),
            new KomponenSatTugasDto(
                fs_kd_detil_tarif: "A",
                fs_kd_sat_tugas: "F",
                fs_nm_sat_tugas: "G",
                fs_kd_profesi: "H",
                fs_nm_profesi: "I"
            )
        };

    private static IKomponenKey FakerKey()
        => KomponenType.Key("A");

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
            opt => opt.Excluding(x => x.fs_nm_sat_tugas)
                .Excluding(x => x.fs_kd_profesi)
                .Excluding(x => x.fs_nm_profesi));
    }
}
