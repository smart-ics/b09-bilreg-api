using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.PakaiBedFeature;
using Bilreg.Infrastructure.BedUsageContext.PakaiBedFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.BedUsageContext.PakaiBedFeature;

public class PakaiBedDalTest
{
    private readonly PakaiBedDal _sut = new(ConnStringHelper.GetTestEnv());

    private static PakaiBedDto Faker()
        => new(
            fs_kd_trs: "TPKB0000000001",
            fd_tgl_in: "2026-07-17",
            fs_jam_in: "09:00:00",
            fs_kd_petugas: "USR1",
            fd_tgl_out: "3000-01-01",
            fs_jam_out: "00:00:00",
            fs_kd_petugas_out: "",
            fs_kd_reg: "TREG000001",
            fs_kd_layanan: "TL001",
            fs_kd_layanan_dk: "",
            fs_kd_kamar_tipe: "1",
            fs_kd_bed: "TBED0001",
            fn_tarif: 150000,
            fd_tgl_void: "3000-01-01",
            fs_jam_void: "00:00:00",
            fs_kd_petugas_void: "",
            fd_tgl_entry: "2026-07-17",
            fs_jam_entry: "09:00:00",
            fs_kd_kelas: "T01",
            fs_mr: "",
            fs_nm_pasien: "",
            fs_nm_layanan: "",
            fs_nm_bed: "",
            fb_bed_aktif: false,
            fs_nm_kamar_tipe: "",
            fb_kamar_tipe_aktif: false,
            fs_nm_kelas: "");

    private static IPakaiBed FakerKey()
        => PakaiBedModel.Key("TPKB0000000001");

    private static IRegKey RegKey()
        => RegModel.Key("TREG000001");

    [Fact]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
    }

    [Fact]
    public void UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());

        _sut.Update(Faker() with { fn_tarif = 250000 });

        var actual = _sut.GetData(FakerKey());
        actual.fn_tarif.Should().Be(250000);
    }

    [Fact]
    public void GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());

        var actual = _sut.GetData(FakerKey());

        actual.Should().BeEquivalentTo(Faker());
    }

    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());

        var actual = _sut.ListData(RegKey());

        actual.Should().ContainEquivalentOf(Faker());
    }

    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());

        _sut.Delete(FakerKey());

        _sut.GetData(FakerKey()).Should().BeNull();
    }
}
