using Bilreg.Domain.BrmContext.MutasiBerkasFeature;
using Bilreg.Infrastructure.BrmContext.MutasiBerkasFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Test.BrmContext.MutasiBerkasFeature;

public class VolumeBerkasDalTest
{
    private readonly IOptions<DatabaseOptions> _opt = ConnStringHelper.GetTestEnv();
    private readonly VolumeBerkasDal _sut;

    public VolumeBerkasDalTest()
    {
        _sut = new VolumeBerkasDal(_opt);
    }

    private static VolumeBerkasDto Faker(
        string regId = "RG26100001",
        string mrId = "00-01-02-03",
        decimal pinjamRi = 1m,
        decimal pinjamRj = 2m,
        decimal kembaliRi = 0m,
        decimal kembaliRj = 0m,
        string tglLast = "2026-10-17",
        string jamLast = "10:00:00") =>
        new()
        {
            FS_KD_REG = regId,
            FS_MR = mrId,
            FN_QTY_BERKAS_PINJAM = pinjamRi + pinjamRj,
            FN_QTY_BERKAS_KEMBALI = kembaliRi + kembaliRj,
            FN_QTY_BERKAS_PINJAM_RI = pinjamRi,
            FN_QTY_BERKAS_KEMBALI_RI = kembaliRi,
            FN_QTY_BERKAS_PINJAM_RJ = pinjamRj,
            FN_QTY_BERKAS_KEMBALI_RJ = kembaliRj,
            FD_TGL_LAST = tglLast,
            FS_JAM_LAST = jamLast
        };

    [Fact]
    public void UT1_InsertTest()
    {
        using var trans = TransHelper.NewScope();
        var dto = Faker();
        _sut.Insert(dto);

        var actual = _sut.GetData(dto.FS_KD_REG);
        actual.Should().NotBeNull();
        actual!.FS_KD_REG.Should().Be(dto.FS_KD_REG);
        actual.FS_MR.Should().Be(dto.FS_MR);
        actual.FN_QTY_BERKAS_PINJAM.Should().Be(3m);
        actual.FN_QTY_BERKAS_PINJAM_RI.Should().Be(1m);
        actual.FN_QTY_BERKAS_PINJAM_RJ.Should().Be(2m);
    }

    [Fact]
    public void UT2_UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        var dto = Faker();
        _sut.Insert(dto);

        var updated = dto with
        {
            FN_QTY_BERKAS_KEMBALI = 3m,
            FN_QTY_BERKAS_KEMBALI_RI = 1m,
            FN_QTY_BERKAS_KEMBALI_RJ = 2m,
            FS_JAM_LAST = "12:00:00"
        };
        _sut.Update(updated);

        var actual = _sut.GetData(dto.FS_KD_REG);
        actual.Should().NotBeNull();
        actual!.FN_QTY_BERKAS_KEMBALI.Should().Be(3m);
        actual.FN_QTY_BERKAS_KEMBALI_RI.Should().Be(1m);
        actual.FN_QTY_BERKAS_KEMBALI_RJ.Should().Be(2m);
        actual.FS_JAM_LAST.Should().Be("12:00:00");
    }

    [Fact]
    public void UT3_Upsert_InsertWhenNotExistsTest()
    {
        using var trans = TransHelper.NewScope();
        var dto = Faker("RG26100099", "00-01-02-99");
        _sut.Upsert(dto);

        var actual = _sut.GetData("RG26100099");
        actual.Should().NotBeNull();
        actual!.FS_MR.Should().Be("00-01-02-99");
    }

    [Fact]
    public void UT4_Upsert_UpdateWhenExistsTest()
    {
        using var trans = TransHelper.NewScope();
        var dto = Faker("RG26100088", "00-01-02-88");
        _sut.Insert(dto);

        var updated = dto with
        {
            FN_QTY_BERKAS_KEMBALI = 1m,
            FN_QTY_BERKAS_KEMBALI_RI = 1m
        };
        _sut.Upsert(updated);

        var actual = _sut.GetData("RG26100088");
        actual.Should().NotBeNull();
        actual!.FN_QTY_BERKAS_KEMBALI.Should().Be(1m);
        actual.FN_QTY_BERKAS_KEMBALI_RI.Should().Be(1m);
    }

    [Fact]
    public void UT5_DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        var dto = Faker();
        _sut.Insert(dto);

        _sut.Delete(dto.FS_KD_REG);

        var actual = _sut.GetData(dto.FS_KD_REG);
        actual.Should().BeNull();
    }

    [Fact]
    public void UT6_GetByMrTest()
    {
        using var trans = TransHelper.NewScope();
        var dto1 = Faker("RG26100001", "00-01-02-03", jamLast: "08:00:00");
        var dto2 = Faker("RG26100002", "00-01-02-03", jamLast: "11:00:00");
        _sut.Insert(dto1);
        _sut.Insert(dto2);

        var latest = _sut.GetByMr("00-01-02-03");
        latest.Should().NotBeNull();
        latest!.FS_KD_REG.Should().Be("RG26100002");
        latest.FS_JAM_LAST.Should().Be("11:00:00");
    }
}
