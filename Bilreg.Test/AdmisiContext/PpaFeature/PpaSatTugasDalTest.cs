using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Infrastructure.AdmisiContext.PpaFeature;
using Bilreg.Infrastructure.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Test.AdmisiContext.PpaFeature;

public class PpaSatTugasDalTest
{
    private readonly PpaSatTugasDal _sut = new(ConnStringHelper.GetTestEnv());

    private static IEnumerable<PpaSatTugasDto> FakerList()
        => new List<PpaSatTugasDto>
        {
            new PpaSatTugasDto(
                fs_kd_peg: "A",
                fs_kd_sat_tugas: "B",
                fn_utama: 1,
                fs_kd_profesi: "C",
                fs_nm_sat_tugas: "D",
                fs_nm_profesi: "E"
            )
        };

    private static IPpaKey FakerKey()
        => PpaType.Key("A");

    private static ISatTugasKey FakerSatTugasKey()
        => SatTugasType.Key("B");

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
    public void ListDataByPetugasMedisTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(FakerList());
        var actual = _sut.ListData(FakerKey());
        actual.Should().BeEquivalentTo(FakerList(),
            opt => opt
                .Excluding(x => x.fs_nm_sat_tugas)
                .Excluding(x => x.fs_kd_profesi)
                .Excluding(x => x.fs_nm_profesi));
    }
}