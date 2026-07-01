using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Infrastructure.BedUsageContext.RoomRateFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.BedUsageContext.RoomRateFeature;

public class RoomRateKomponenDalTest
{
    private readonly RoomRateKomponenDal _sut = new(ConnStringHelper.GetTestEnv());

    private static IEnumerable<RoomRateDto> FakerList()
        => new List<RoomRateDto>
        {
            new RoomRateDto(
                fs_kd_kamar: "A",
                fs_kd_detil_tarif: "B",
                fs_kd_tipe_kamar: "C",
                fn_tarif: 100000,
                fn_no_urut: 1,
                fs_kd_kelas: "D",
                fn_harike: 1,
                fs_nm_kamar: "E",
                fs_nm_detil_tarif: "F",
                fs_nm_tipe_kamar: "G",
                fs_nm_kelas: "H"
            ),
            new RoomRateDto(
                fs_kd_kamar: "A",
                fs_kd_detil_tarif: "I",
                fs_kd_tipe_kamar: "J",
                fn_tarif: 200000,
                fn_no_urut: 2,
                fs_kd_kelas: "K",
                fn_harike: 2,
                fs_nm_kamar: "L",
                fs_nm_detil_tarif: "M",
                fs_nm_tipe_kamar: "N",
                fs_nm_kelas: "O"
            )
        };

    private static IKamarKey FakerKey()
        => KamarType.Key("A");

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
            opt => opt.Excluding(x => x.fs_nm_kamar)
                .Excluding(x => x.fs_nm_detil_tarif)
                .Excluding(x => x.fs_nm_tipe_kamar)
                .Excluding(x => x.fs_nm_kelas));
    }
}