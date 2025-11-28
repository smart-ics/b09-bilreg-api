using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Infrastructure.Helpers;
using Bilreg.Infrastructure.PasienContext.PasienFeature;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Test.PasienContext.PasienFeature;

public class PasienKtpDalTest
{
    private readonly PasienKtpDal _sut = new(ConnStringHelper.GetTestEnv());

    private static PasienKtpDto Faker()
        => new PasienKtpDto(
            fs_kd_mr: "A",
            fs_nik: "B",
            fs_nama_ktp: "C",
            fs_alm_ktp: "D",
            fs_rt_ktp: "E",
            fs_rw_ktp: "F",
            fs_kd_kelurahan_ktp: "G",
            fs_kelurahan_ktp: "H",
            fs_kd_kecamatan_ktp: "I",
            fs_kecamatan_ktp: "J",
            fs_kd_kabupaten_ktp: "K",
            fs_kabupaten_ktp: "L",
            fs_kd_propinsi_ktp: "M",
            fs_propinsi_ktp: "N",
            fs_tempat_lahir: "O",
            fs_sex: "P",
            fd_tgl_lahir: "01012000",
            fs_gol_darah: "Q"
        );

    private static IPasienKey FakerKey()
        => PasienModel.Key("A");

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
        _sut.Update(Faker());
    }

    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Delete(FakerKey());
    }

    [Fact]
    public void GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var actual = _sut.GetData(FakerKey());
        actual.Should().BeEquivalentTo(Faker());
    }
}