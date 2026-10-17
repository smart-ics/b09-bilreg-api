using Bilreg.Domain.BrmContext.MutasiBerkasFeature;
using Bilreg.Infrastructure.BrmContext.MutasiBerkasFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Test.BrmContext.MutasiBerkasFeature;

public class MutasiBerkasDalTest
{
    private readonly IOptions<DatabaseOptions> _opt = ConnStringHelper.GetTestEnv();
    private readonly MutasiBerkasDal _sut;

    public MutasiBerkasDalTest()
    {
        _sut = new MutasiBerkasDal(_opt);
    }

    private static MutasiBerkasDto Faker(
        string mutasiId = "MK26100001",
        string trsBuatId = "RG26100001",
        string mrId = "00-01-02-03",
        int status = 1,
        string tglMutasi = "2026-10-17",
        string jamMutasi = "10:30:00",
        string tglVoid = "3000-01-01") =>
        new()
        {
            FS_KD_MUTASI = mutasiId,
            FS_KD_TRS_BUAT = trsBuatId,
            FS_MR = mrId,
            FD_TGL_MUTASI = tglMutasi,
            FS_JAM_MUTASI = jamMutasi,
            FS_KD_PETUGAS_MEDIS = "DR001",
            FS_KD_LAYANAN = "POL01",
            FD_TGL_PRAKTEK = "2026-10-17",
            FS_JAM_PRAKTEK = "10:00",
            FS_KD_SESION_POLI = "1",
            FN_STATUS = status,
            FS_KD_PETUGAS = "USER01",
            FN_CETAK = 1,
            FS_KD_PETUGAS_CETAK = "USER02",
            fd_tgl_cetak = "2026-10-17",
            fs_jam_cetak = "10:35:00",
            fd_tgl_void = tglVoid,
            fs_jam_void = tglVoid == "3000-01-01" ? "00:00:00" : "11:00:00",
            fs_kd_petugas_void = tglVoid == "3000-01-01" ? string.Empty : "USER03",
            CRTTGL = "2026-10-17",
            CRTJAM = "10:30:00",
            CRTIPA = "127.0.0.1",
            CRTVER = "1.0",
            CRTUSR = "USER01",
            UPDTGL = "2026-10-17",
            UPDJAM = "10:30:00",
            UPDIPA = "127.0.0.1",
            UPDVER = "1.0",
            UPDUSR = "USER01"
        };

    [Fact]
    public void UT1_InsertTest()
    {
        using var trans = TransHelper.NewScope();
        var dto = Faker();
        _sut.Insert(dto);

        var actual = _sut.GetByKey(dto.FS_KD_MUTASI);
        actual.Should().NotBeNull();
        actual!.FS_KD_MUTASI.Should().Be(dto.FS_KD_MUTASI);
        actual.FS_KD_TRS_BUAT.Should().Be(dto.FS_KD_TRS_BUAT);
        actual.FS_MR.Should().Be(dto.FS_MR);
        actual.FN_STATUS.Should().Be(1);
        actual.CRTTGL.Should().NotBeNullOrWhiteSpace();
        actual.CRTUSR.Should().Be("USER01");
    }

    [Fact]
    public void UT2_UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        var dto = Faker();
        _sut.Insert(dto);

        var updatedDto = dto with
        {
            FN_STATUS = 2,
            FS_KD_PETUGAS = "USER02",
            UPDTGL = "2026-10-17",
            UPDJAM = "11:00:00"
        };
        _sut.Update(updatedDto);

        var actual = _sut.GetByKey(dto.FS_KD_MUTASI);
        actual.Should().NotBeNull();
        actual!.FN_STATUS.Should().Be(2);
        actual.FS_KD_PETUGAS.Should().Be("USER02");
    }

    [Fact]
    public void UT3_DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        var dto = Faker();
        _sut.Insert(dto);

        _sut.Delete(MutasiBerkasModel.Key(dto.FS_KD_MUTASI));

        var actual = _sut.GetByKey(dto.FS_KD_MUTASI);
        actual.Should().BeNull();
    }

    [Fact]
    public void UT4_GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        var dto = Faker();
        _sut.Insert(dto);

        var actual = _sut.GetData(MutasiBerkasModel.Key(dto.FS_KD_MUTASI));
        actual.Should().NotBeNull();
        actual.FS_KD_MUTASI.Should().Be(dto.FS_KD_MUTASI);
        actual.FS_KD_LAYANAN.Should().Be(dto.FS_KD_LAYANAN);
        actual.FS_KD_PETUGAS_MEDIS.Should().Be(dto.FS_KD_PETUGAS_MEDIS);
        actual.MutasiBerkasId.Should().Be(dto.FS_KD_MUTASI);
    }

    [Fact]
    public void UT5_ListByTrsBuatTest()
    {
        using var trans = TransHelper.NewScope();
        var dto1 = Faker("MK26100001", "RG26100001", "00-01-02-03", status: 1, jamMutasi: "08:00:00");
        var dto2 = Faker("MP26100002", "RG26100001", "00-01-02-03", status: 2, jamMutasi: "09:00:00");
        _sut.Insert(dto1);
        _sut.Insert(dto2);

        var list = _sut.ListByTrsBuat("RG26100001").ToList();
        list.Should().HaveCount(2);
        list[0].FS_KD_MUTASI.Should().Be("MP26100002"); // Ordered DESC by jam
        list[1].FS_KD_MUTASI.Should().Be("MK26100001");
    }

    [Fact]
    public void UT6_GetLatestActiveByMrTest()
    {
        using var trans = TransHelper.NewScope();
        // Insert active mutasi 1
        var dto1 = Faker("MK26100001", "RG26100001", "00-01-02-03", status: 1, jamMutasi: "08:00:00", tglVoid: "3000-01-01");
        // Insert active mutasi 2 (later time)
        var dto2 = Faker("MP26100002", "RG26100001", "00-01-02-03", status: 2, jamMutasi: "09:00:00", tglVoid: "3000-01-01");
        // Insert voided mutasi 3 (even later time, but voided)
        var dto3 = Faker("MM26100003", "RG26100001", "00-01-02-03", status: 5, jamMutasi: "10:00:00", tglVoid: "2026-10-17");

        _sut.Insert(dto1);
        _sut.Insert(dto2);
        _sut.Insert(dto3);

        var latest = _sut.GetLatestActiveByMr("00-01-02-03");
        latest.Should().NotBeNull();
        latest!.FS_KD_MUTASI.Should().Be("MP26100002");
        latest.FN_STATUS.Should().Be(2);
    }
}
