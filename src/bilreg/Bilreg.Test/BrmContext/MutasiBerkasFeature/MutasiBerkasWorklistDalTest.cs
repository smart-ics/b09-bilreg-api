using Bilreg.Application.BrmContext.MutasiBerkasFeature;
using Bilreg.Infrastructure.BrmContext.MutasiBerkasFeature;
using Bilreg.Infrastructure.Shared.Helpers;
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
        var activeDto = Faker("MK26100091", "RG26100091", "00-99-88-71", layananId: "POL91", tglVoid: "3000-01-01");
        var voidedDto = Faker("MK26100092", "RG26100092", "00-99-88-72", layananId: "POL91", tglVoid: "2026-10-17");

        _mutasiDal.Insert(activeDto);
        _mutasiDal.Insert(voidedDto);

        var filter = new MutasiBerkasWorklistFilter(LayananId: "POL91");
        var results = _sut.QueryWorklist(filter).ToList();

        results.Should().Contain(x => x.MutasiId == "MK26100091");
        results.Should().NotContain(x => x.MutasiId == "MK26100092");
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
}
