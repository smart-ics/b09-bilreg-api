using System.Data.SqlClient;
using Bilreg.Application.BrmContext.MutasiBerkasFeature;
using Bilreg.Infrastructure.BrmContext.MutasiBerkasFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Test.BrmContext.MutasiBerkasFeature;

public class MutasiBerkasWorklistDalTest
{
    private readonly IOptions<DatabaseOptions> _opt = ConnStringHelper.GetTestEnv();
    private readonly MutasiBerkasDal _mutasiDal;
    private readonly MutasiBerkasWorklistDal _sut;

    public MutasiBerkasWorklistDalTest()
    {
        _mutasiDal = new MutasiBerkasDal(_opt);
        _sut = new MutasiBerkasWorklistDal(_opt);
    }

    private static MutasiBerkasDto Faker(
        string mutasiId = "MK26100001",
        string trsBuatId = "RG26100001",
        string mrId = "00-01-02-03",
        string layananId = "POL01",
        string tglPraktek = "2026-10-17",
        string sesiPoli = "1",
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
            FS_KD_LAYANAN = layananId,
            FD_TGL_PRAKTEK = tglPraktek,
            FS_JAM_PRAKTEK = "10:00",
            FS_KD_SESION_POLI = sesiPoli,
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
    public void QueryWorklist_WithFilters_ReturnsMatchingItems()
    {
        using var trans = TransHelper.NewScope();
        var dto = Faker("MK26100099", "RG26100099", "00-99-88-77", layananId: "POL99", tglPraktek: "2026-10-17", sesiPoli: "1", status: 1);
        _mutasiDal.Insert(dto);

        var filter = new MutasiBerkasWorklistFilter(
            TglPraktek: "2026-10-17",
            LayananId: "POL99",
            SesiPoli: "1",
            Status: 1
        );

        var results = _sut.QueryWorklist(filter).ToList();
        results.Should().Contain(x => x.MutasiId == "MK26100099" && x.MrId == "00-99-88-77");
    }

    [Fact]
    public void QueryWorklist_IgnoresVoidedRows()
    {
        using var trans = TransHelper.NewScope();
        var today = DateTime.Today.ToString("yyyy-MM-dd");
        var activeDto = Faker("MK26100091", "RG26100091", "00-99-88-71", layananId: "POL91", tglPraktek: today, tglVoid: "3000-01-01");
        var voidedDto = Faker("MK26100092", "RG26100092", "00-99-88-72", layananId: "POL91", tglPraktek: today, tglVoid: "2026-10-17");

        _mutasiDal.Insert(activeDto);
        _mutasiDal.Insert(voidedDto);

        var filter = new MutasiBerkasWorklistFilter(LayananId: "POL91");
        var results = _sut.QueryWorklist(filter).ToList();

        results.Should().Contain(x => x.MutasiId == "MK26100091");
        results.Should().NotContain(x => x.MutasiId == "MK26100092");
    }

    [Fact]
    public void QueryWorklist_NullOrEmptyTglPraktek_DefaultsToToday()
    {
        using var trans = TransHelper.NewScope();
        var today = DateTime.Today.ToString("yyyy-MM-dd");
        var yesterday = DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd");

        var todayDto = Faker("MK26100071", "RG26100071", "00-77-66-51", layananId: "POL71", tglPraktek: today);
        var pastDto = Faker("MK26100072", "RG26100072", "00-77-66-52", layananId: "POL71", tglPraktek: yesterday);

        _mutasiDal.Insert(todayDto);
        _mutasiDal.Insert(pastDto);

        var filter = new MutasiBerkasWorklistFilter(LayananId: "POL71");
        var results = _sut.QueryWorklist(filter).ToList();

        results.Should().Contain(x => x.MutasiId == "MK26100071");
        results.Should().NotContain(x => x.MutasiId == "MK26100072");
    }

    [Fact]
    public void QueryHistori_WithOrWithoutDashes_ReturnsAllHistory()
    {
        using var trans = TransHelper.NewScope();
        var mrDashed = "00-14-37-71";
        var mrPlain = "00143771";

        var dto1 = Faker("MK26100061", "RG26100061", mrDashed, jamMutasi: "08:00:00");
        var dto2 = Faker("MK26100062", "RG26100062", mrPlain, jamMutasi: "09:00:00");

        _mutasiDal.Insert(dto1);
        _mutasiDal.Insert(dto2);

        // Query with plain MR
        var resultsWithPlain = _sut.QueryHistori(mrPlain).ToList();
        resultsWithPlain.Should().Contain(x => x.MutasiId == "MK26100061");
        resultsWithPlain.Should().Contain(x => x.MutasiId == "MK26100062");

        // Query with dashed MR
        var resultsWithDashed = _sut.QueryHistori(mrDashed).ToList();
        resultsWithDashed.Should().Contain(x => x.MutasiId == "MK26100061");
        resultsWithDashed.Should().Contain(x => x.MutasiId == "MK26100062");
    }

    [Fact]
    public void QueryHistori_ReturnsChronologicalDescendingOrder()
    {
        using var trans = TransHelper.NewScope();
        var mrId = "00-88-77-66";
        var dto1 = Faker("MK26100081", "RG26100081", mrId, jamMutasi: "08:00:00");
        var dto2 = Faker("MP26100082", "RG26100081", mrId, jamMutasi: "09:30:00");
        var dto3 = Faker("MM26100083", "RG26100081", mrId, jamMutasi: "11:00:00");

        _mutasiDal.Insert(dto1);
        _mutasiDal.Insert(dto2);
        _mutasiDal.Insert(dto3);

        var results = _sut.QueryHistori(mrId).ToList();
        results.Should().HaveCountGreaterOrEqualTo(3);
        results[0].MutasiId.Should().Be("MM26100083");
        results[1].MutasiId.Should().Be("MP26100082");
        results[2].MutasiId.Should().Be("MK26100081");
    }

    private void InsertAntrian(
        string antrianTrsId,
        string tglAntrian,
        string layananId,
        int noUrut,
        string mrId,
        string regId,
        string tglVoid = "3000-01-01")
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt.Value));
        conn.Execute(@"
            INSERT INTO TA_TRS_ANTRIAN (FS_KD_TRS, FD_TGL_ANTRIAN, FS_KD_LAYANAN, FN_NO_URUT, FS_MR, FS_KD_REG, FD_TGL_VOID)
            VALUES (@antrianTrsId, @tglAntrian, @layananId, @noUrut, @mrId, @regId, @tglVoid)",
            new { antrianTrsId, tglAntrian, layananId, noUrut, mrId, regId, tglVoid });
    }

    private void InsertRegistrasi(
        string regId,
        string mrId,
        string dokterId = "DR001",
        string jamMasuk = "08:15:00",
        string tglMasuk = "2026-10-17")
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt.Value));
        conn.Execute(@"
            IF NOT EXISTS (SELECT 1 FROM TA_REGISTRASI WHERE FS_KD_REG = @regId)
            INSERT INTO TA_REGISTRASI (FS_KD_REG, FS_MR, FS_KD_MEDIS, FS_JAM_MASUK, FD_TGL_MASUK)
            VALUES (@regId, @mrId, @dokterId, @jamMasuk, @tglMasuk)",
            new { regId, mrId, dokterId, jamMasuk, tglMasuk });
    }

    [Fact]
    public void QueryWorklist_OutpatientQueueWithoutMutasi_ProjectsWithStatus0AndQueueNumber()
    {
        using var trans = TransHelper.NewScope();
        var today = DateTime.Today.ToString("yyyy-MM-dd");
        var regId = "RG26100551";
        var mrId = "00-55-11-22";
        var layananId = "POL55";

        InsertRegistrasi(regId, mrId, dokterId: "DR001", jamMasuk: "08:30:00", tglMasuk: today);
        InsertAntrian("AT26100551", today, layananId, 5, mrId, regId);

        var filter = new MutasiBerkasWorklistFilter(
            TglPraktek: today,
            LayananId: layananId
        );

        var results = _sut.QueryWorklist(filter).ToList();
        var item = results.FirstOrDefault(x => x.TrsBuatId == regId);

        item.Should().NotBeNull();
        item!.MutasiId.Should().BeEmpty();
        item.MrId.Should().Be(mrId);
        item.LayananId.Should().Be(layananId);
        item.Status.Should().Be(0); // Permintaan
        item.NoAntrian.Should().Be(5);
        item.JamPraktek.Should().Be("08:30:00");
    }

    [Fact]
    public void QueryWorklist_OutpatientQueueWithMutasi_ProjectsWithMutasiStatusAndQueueNumber()
    {
        using var trans = TransHelper.NewScope();
        var today = DateTime.Today.ToString("yyyy-MM-dd");
        var regId = "RG26100771";
        var mrId = "00-77-11-22";
        var layananId = "POL77";

        InsertRegistrasi(regId, mrId, dokterId: "DR001", jamMasuk: "09:00:00", tglMasuk: today);
        InsertAntrian("AT26100771", today, layananId, 12, mrId, regId);

        var mutasiDto = Faker(
            mutasiId: "MK26100771",
            trsBuatId: regId,
            mrId: mrId,
            layananId: layananId,
            tglPraktek: today,
            status: 2 // Dikirim
        );
        _mutasiDal.Insert(mutasiDto);

        var filter = new MutasiBerkasWorklistFilter(
            TglPraktek: today,
            LayananId: layananId
        );

        var results = _sut.QueryWorklist(filter).ToList();
        var item = results.FirstOrDefault(x => x.TrsBuatId == regId);

        item.Should().NotBeNull();
        item!.MutasiId.Should().Be("MK26100771");
        item.MrId.Should().Be(mrId);
        item.LayananId.Should().Be(layananId);
        item.Status.Should().Be(2); // Dikirim
        item.NoAntrian.Should().Be(12);
    }

    [Fact]
    public void QueryWorklist_OutpatientQueueVoided_DoesNotAppearInWorklist()
    {
        using var trans = TransHelper.NewScope();
        var today = DateTime.Today.ToString("yyyy-MM-dd");
        var regId = "RG26100881";
        var mrId = "00-88-11-22";
        var layananId = "POL88";

        InsertRegistrasi(regId, mrId, dokterId: "DR001", jamMasuk: "09:15:00", tglMasuk: today);
        InsertAntrian("AT26100881", today, layananId, 3, mrId, regId, tglVoid: today);

        var filter = new MutasiBerkasWorklistFilter(
            TglPraktek: today,
            LayananId: layananId
        );

        var results = _sut.QueryWorklist(filter).ToList();
        results.Should().NotContain(x => x.TrsBuatId == regId);
    }

    [Fact]
    public void QueryWorklist_NonAntrianInternalLoan_AppearsWithNoAntrianZero()
    {
        using var trans = TransHelper.NewScope();
        var today = DateTime.Today.ToString("yyyy-MM-dd");
        var internalLoanDto = Faker(
            mutasiId: "MK26100991",
            trsBuatId: "PJ26100099",
            mrId: "00-99-11-22",
            layananId: "POL99",
            tglPraktek: today,
            status: 1
        );
        _mutasiDal.Insert(internalLoanDto);

        var filter = new MutasiBerkasWorklistFilter(
            TglPraktek: today,
            LayananId: "POL99"
        );

        var results = _sut.QueryWorklist(filter).ToList();
        var item = results.FirstOrDefault(x => x.MutasiId == "MK26100991");

        item.Should().NotBeNull();
        item!.NoAntrian.Should().Be(0);
        item.Status.Should().Be(1);
    }
}
