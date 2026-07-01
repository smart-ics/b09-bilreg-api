using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Infrastructure.AdmisiContext.RegFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Test.AdmisiContext.RegFeature;

public class KarcisKomponenDalTest
{
    private readonly KarcisKomponenDal _sut = new(ConnStringHelper.GetTestEnv());

    private static IEnumerable<KarcisKomponenDto> FakerList()
        => new List<KarcisKomponenDto>
        {
            new KarcisKomponenDto(
                fs_kd_karcis: "A",
                fs_kd_detil_tarif: "B",
                fn_tarif: 1001m,
                fs_nm_detil_tarif: "C"
            ),
            new KarcisKomponenDto(
                fs_kd_karcis: "A",
                fs_kd_detil_tarif: "D",
                fn_tarif: 20015m,
                fs_nm_detil_tarif: "E"
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
            opt => opt.Excluding(x => x.fs_nm_detil_tarif));
    }
}
