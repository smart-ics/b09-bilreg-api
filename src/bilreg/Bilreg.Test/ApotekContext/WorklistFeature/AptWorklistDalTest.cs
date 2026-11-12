using System.Data.SqlClient;
using Bilreg.Domain.ApotekContext.TelaahResepFeature;
using Bilreg.Infrastructure.ApotekContext.WorklistFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using FluentAssertions;

namespace Bilreg.Test.ApotekContext.WorklistFeature;

[Collection("ResepDatabaseTests")]
public class AptWorklistDalTest
{
    private readonly AptWorklistDal _sut;

    public AptWorklistDalTest()
    {
        var opt = ConnStringHelper.GetTestEnv();
        _sut = new(opt);
    }

    [Fact]
    public void ListTelaah_includes_unstarted_resep_without_telaah_row()
    {
        var run = Guid.NewGuid().ToString("N")[..8];
        var resepId = $"K{run}";
        var regId = $"RG{run}";
        var noRm = $"RM{run}";
        var drId = $"D{run}";
        var poliId = $"P{run[..3]}";

        SeedContext(regId, noRm, $"Pasien {run}", drId, $"Dokter {run}", poliId, $"Poli {run}");
        InsertTestResep(resepId, regId, drId, poliId, bb: 55.5m, tb: 160m, lpb: 70m, urgenitas: "1", catatan: "Catatan Cito");

        var items = _sut.ListTelaah();

        var item = items.FirstOrDefault(x => x.ResepId == resepId);
        item.Should().NotBeNull();
        item!.ResepId.Should().Be(resepId);
        item.TelaahResepId.Should().BeEmpty();
        item.PasienName.Should().Be($"Pasien {run}");
        item.NoRm.Should().Be(noRm);
        item.DokterName.Should().Be($"Dokter {run}");
        item.PoliName.Should().Be($"Poli {run}");
        item.Bb.Should().Be(55.5m);
        item.Tb.Should().Be(160m);
        item.Lpb.Should().Be(70m);
        item.Cito.Should().BeTrue();
        item.CatatanResep.Should().Be("Catatan Cito");
        item.Status.Should().Be((int)TelaahStatusEnum.Available);
        item.StartedAt.Date.Should().Be(DateTime.Today);

        Cleanup(resepId, regId, noRm, drId, poliId);
    }

    [Fact]
    public void ListTelaah_includes_under_review_and_excludes_terminal_reviews()
    {
        var run = Guid.NewGuid().ToString("N")[..8];
        var resepId = $"K{run}";
        var telaahId = $"T{run}";

        InsertTestResep(resepId);
        InsertTelaah(telaahId, resepId, (int)TelaahStatusEnum.UnderReview, DateTime.Now);

        var underReview = _sut.ListTelaah();
        underReview.Should().Contain(x => x.ResepId == resepId && x.TelaahResepId == telaahId && x.Status == (int)TelaahStatusEnum.UnderReview);

        UpdateTelaahStatus(telaahId, (int)TelaahStatusEnum.Approved);

        var afterComplete = _sut.ListTelaah();
        afterComplete.Should().NotContain(x => x.ResepId == resepId);

        Cleanup(resepId);
    }

    [Fact]
    public void ListTelaah_excludes_billed_or_voided_or_not_today_prescriptions()
    {
        var run = Guid.NewGuid().ToString("N")[..8];
        var resepBilled = $"KB{run}";
        var resepVoided = $"KV{run}";
        var resepYesterday = $"KY{run}";
        var resepNotResep = $"KN{run}";

        InsertTestResep(resepBilled, statusDobill: 1);
        InsertTestResep(resepVoided, tglVoid: DateTime.Today.ToString("yyyy-MM-dd"));
        InsertTestResep(resepYesterday, tglTrs: DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd"));
        InsertTestResep(resepNotResep, fbResep: false);

        var items = _sut.ListTelaah();

        items.Should().NotContain(x => x.ResepId == resepBilled);
        items.Should().NotContain(x => x.ResepId == resepVoided);
        items.Should().NotContain(x => x.ResepId == resepYesterday);
        items.Should().NotContain(x => x.ResepId == resepNotResep);

        Cleanup(resepBilled);
        Cleanup(resepVoided);
        Cleanup(resepYesterday);
        Cleanup(resepNotResep);
    }

    [Fact]
    public void ListTelaah_orders_cito_before_normal()
    {
        var run = Guid.NewGuid().ToString("N")[..8];
        var resepNormal = $"RN{run}";
        var resepCito = $"RC{run}";

        InsertTestResep(resepNormal, urgenitas: "0", jamTrs: "08:00:00");
        InsertTestResep(resepCito, urgenitas: "1", jamTrs: "09:00:00");

        var items = _sut.ListTelaah();

        var list = items.ToList();
        var idxNormal = list.FindIndex(x => x.ResepId == resepNormal);
        var idxCito = list.FindIndex(x => x.ResepId == resepCito);

        idxNormal.Should().BeGreaterThan(-1);
        idxCito.Should().BeGreaterThan(-1);
        idxCito.Should().BeLessThan(idxNormal);

        Cleanup(resepNormal);
        Cleanup(resepCito);
    }

    private void SeedContext(string regId, string noRm, string pasienName, string dokterId, string dokterName, string layananId, string poliName)
    {
        var opt = ConnStringHelper.GetTestEnv().Value;
        using var conn = new SqlConnection(ConnStringHelper.Get(opt));
        conn.Execute("IF NOT EXISTS (SELECT 1 FROM tc_mr WHERE fs_mr = @noRm) INSERT INTO tc_mr (fs_mr, fs_nm_pasien) VALUES (@noRm, @pasienName)", new { noRm, pasienName });
        conn.Execute("IF NOT EXISTS (SELECT 1 FROM ta_registrasi WHERE fs_kd_reg = @regId) INSERT INTO ta_registrasi (fs_kd_reg, fs_mr) VALUES (@regId, @noRm)", new { regId, noRm });
        conn.Execute("IF NOT EXISTS (SELECT 1 FROM td_peg WHERE fs_kd_peg = @dokterId) INSERT INTO td_peg (fs_kd_peg, fs_nm_peg) VALUES (@dokterId, @dokterName)", new { dokterId, dokterName });
        conn.Execute("IF NOT EXISTS (SELECT 1 FROM ta_layanan WHERE fs_kd_layanan = @layananId) INSERT INTO ta_layanan (fs_kd_layanan, fs_nm_layanan) VALUES (@layananId, @poliName)", new { layananId, poliName });
    }

    private void InsertTestResep(
        string resepId,
        string regId = "REG1",
        string dokterId = "DR1",
        string layananId = "LY01",
        decimal bb = 60m,
        decimal tb = 165m,
        decimal lpb = 75m,
        string urgenitas = "0",
        string catatan = "Catatan Resep",
        int statusDobill = 0,
        string tglVoid = "3000-01-01",
        bool fbResep = true,
        string? tglTrs = null,
        string jamTrs = "10:00:00")
    {
        var opt = ConnStringHelper.GetTestEnv().Value;
        using var conn = new SqlConnection(ConnStringHelper.Get(opt));
        tglTrs ??= DateTime.Today.ToString("yyyy-MM-dd");

        conn.Execute("""
            INSERT INTO ta_trs_kartu_periksa (
                fs_kd_trs, fd_tgl_trs, fs_jam_trs, fs_kd_reg, fs_kd_layanan, fs_kd_medis_resep,
                fn_berat_badan, fn_tinggi_badan, fn_lpb, fs_kd_urgenitas, fb_resep,
                fn_status_dobill, fd_tgl_void, fs_kd_petugas
            ) VALUES (
                @resepId, @tglTrs, @jamTrs, @regId, @layananId, @dokterId,
                @bb, @tb, @lpb, @urgenitas, @fbResep,
                @statusDobill, @tglVoid, 'tester'
            )
            """, new
        {
            resepId, tglTrs, jamTrs, regId, layananId, dokterId,
            bb, tb, lpb, urgenitas, fbResep, statusDobill, tglVoid
        });

        if (!string.IsNullOrEmpty(catatan))
        {
            conn.Execute("""
                INSERT INTO ta_trs_kartu_periksa_resep (fs_kd_trs, fs_catatan_resep)
                VALUES (@resepId, @catatan)
                """, new { resepId, catatan });
        }
    }

    private void InsertTelaah(string telaahId, string resepId, int status, DateTime startedAt)
    {
        var opt = ConnStringHelper.GetTestEnv().Value;
        using var conn = new SqlConnection(ConnStringHelper.Get(opt));
        conn.Execute("""
            INSERT INTO BILRG_AptTelaahResep (
                TelaahResepId, ResepId, ResepKerjaId, RegId, TelaahStatus, PharmacistId,
                StartedAt, CompletedAt, Version, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate
            ) VALUES (
                @telaahId, @resepId, @resepId, 'REG1', @status, 'pharm',
                @startedAt, '3000-01-01', 1, 'tester', @startedAt, 'tester', @startedAt, '', '3000-01-01'
            )
            """, new { telaahId, resepId, status, startedAt });
    }

    private void UpdateTelaahStatus(string telaahId, int status)
    {
        var opt = ConnStringHelper.GetTestEnv().Value;
        using var conn = new SqlConnection(ConnStringHelper.Get(opt));
        conn.Execute("UPDATE BILRG_AptTelaahResep SET TelaahStatus = @status WHERE TelaahResepId = @telaahId", new { telaahId, status });
    }

    private void Cleanup(string resepId, string? regId = null, string? noRm = null, string? dokterId = null, string? layananId = null)
    {
        var opt = ConnStringHelper.GetTestEnv().Value;
        using var conn = new SqlConnection(ConnStringHelper.Get(opt));
        conn.Execute("DELETE FROM ta_trs_kartu_periksa_resep WHERE fs_kd_trs = @resepId", new { resepId });
        conn.Execute("DELETE FROM ta_trs_kartu_periksa WHERE fs_kd_trs = @resepId", new { resepId });
        conn.Execute("DELETE FROM BILRG_AptTelaahResep WHERE ResepId = @resepId OR ResepKerjaId = @resepId", new { resepId });
        if (regId != null) conn.Execute("DELETE FROM ta_registrasi WHERE fs_kd_reg = @regId", new { regId });
        if (noRm != null) conn.Execute("DELETE FROM tc_mr WHERE fs_mr = @noRm", new { noRm });
        if (dokterId != null) conn.Execute("DELETE FROM td_peg WHERE fs_kd_peg = @dokterId", new { dokterId });
        if (layananId != null) conn.Execute("DELETE FROM ta_layanan WHERE fs_kd_layanan = @layananId", new { layananId });
    }
}
