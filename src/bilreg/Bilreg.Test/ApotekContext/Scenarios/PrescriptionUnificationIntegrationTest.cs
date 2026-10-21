using System.Data.SqlClient;
using Bilreg.Application.ApotekContext.Shared;
using Bilreg.Application.ApotekContext.TelaahResepFeature.UseCases;
using Bilreg.Application.SalesContext.ResepFeature.UseCases;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ApotekContext.TelaahResepFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.SalesContext.ResepFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Infrastructure.ApotekContext.TelaahResepFeature;
using Bilreg.Infrastructure.ApotekContext.WorklistFeature;
using Bilreg.Infrastructure.SalesContext.ResepFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Bilreg.Test.ApotekContext.Support;
using Dapper;
using FluentAssertions;
using TipeBrgType = Bilreg.Domain.BrgContext.PricingPolicyFeature.TipeBrgType;

namespace Bilreg.Test.ApotekContext.Scenarios;

[Collection("ResepDatabaseTests")]
public class PrescriptionUnificationIntegrationTest
{
    private static readonly object SchemaLock = new();

    private readonly AptWorklistDal _worklistDal;
    private readonly ResepDal _resepDal;
    private readonly ResepBrgDal _resepBrgDal;
    private readonly ResepRepo _resepRepo;
    private readonly TelaahResepRepo _telaahRepo;
    private readonly AllowAllAuth _auth = new();

    public PrescriptionUnificationIntegrationTest()
    {
        EnsureSchema();
        var opt = ConnStringHelper.GetTestEnv();
        _worklistDal = new AptWorklistDal(opt);
        _resepDal = new ResepDal(opt);
        _resepBrgDal = new ResepBrgDal(opt);
        _resepRepo = new ResepRepo(_resepDal, _resepBrgDal);
        _telaahRepo = new TelaahResepRepo(new TelaahDal(opt), new TelaahItemDal(opt), new TelaahKajianDal(opt));
    }

    [Fact]
    public async Task EndToEnd_ResepCreated_AppearsInWorklist_ApprovedLocksDobill_VoidRejected()
    {
        var run = Guid.NewGuid().ToString("N")[..8];
        var resepId = $"K{run}";
        var regId = $"RG{run}";
        var noRm = $"RM{run}";
        var drId = $"D{run}";
        var poliId = $"P{run[..3]}";
        var brgId = $"B{run[..4]}";

        try
        {
            // Setup master data context in devTest database
            SeedContext(regId, noRm, $"Pasien {run}", drId, $"Dr {run}", poliId, $"Poli {run}");

            // 1. Resep created by Doctor (master clinical tables: ta_trs_kartu_periksa, ta_trs_kartu_periksa3)
            var resepModel = new ResepModel(
                resepId,
                new RegReff(regId, noRm, $"Pasien {run}"),
                new BodyMetricType(165m, 60m, 75m),
                new DokterReff(drId, $"Dr {run}"),
                new LayananReff(poliId, $"Poli {run}"),
                new LayananReff(poliId, $"Poli {run}"),
                UrgenitasType.Load("1", "CITO"),
                TipeBrgType.Default.ToReff(),
                iter: 0,
                description: "Catatan Cito Integration Test",
                tglRencanaPeriksa: DateTime.Today,
                resepAsalReffId: AppConst.DASH,
                voidReason: AppConst.DASH,
                auditTrail: AuditTrailType.Create("doctor1", DateTime.Now),
                listObat: []
            );
            var obat = BrgObatType.Default with { BrgId = brgId, BrgName = $"Obat {run}" };
            resepModel.AddObat(obat, new SatuanType("TAB", "Tablet"), 10m, 0, "3x1", "Sesudah makan", "Aturan Pakai");
            _resepRepo.SaveChanges(resepModel);

            // Verify: Immediately visible in AptWorklistDal.ListTelaah()
            var worklist = _worklistDal.ListTelaah().ToList();
            var item = worklist.FirstOrDefault(x => x.ResepId == resepId);
            item.Should().NotBeNull();
            item!.ResepId.Should().Be(resepId);
            item.PasienName.Should().Be($"Pasien {run}");
            item.NoRm.Should().Be(noRm);
            item.DokterName.Should().Be($"Dr {run}");
            item.PoliName.Should().Be($"Poli {run}");
            item.Cito.Should().BeTrue();
            item.CatatanResep.Should().Be("Catatan Cito Integration Test");
            item.Status.Should().Be((int)TelaahStatusEnum.Available);

            // 2. Apoteker starts telaah and completes review (Approved)
            var startHandler = new TelaahStartHandler(_telaahRepo, _resepRepo, _auth);
            var startResponse = await startHandler.Handle(new TelaahStartCmd("pharmacist1", resepId, 0), default);
            startResponse.TelaahResepId.Should().NotBeNullOrWhiteSpace();

            var updateItemHandler = new TelaahUpdateItemHandler(_telaahRepo, _auth);
            var updateResponse = await updateItemHandler.Handle(new TelaahUpdateItemCmd(
                "pharmacist1",
                startResponse.TelaahResepId,
                startResponse.Version,
                ItemNo: 1,
                Disposition: TelaahDispositionEnum.AcceptedAsPrescribed,
                AcceptedBrgId: brgId,
                AcceptedBrgName: $"Obat {run}",
                AcceptedQty: 10m,
                Reason: "Dosis dan indikasi sesuai"
            ), default);

            var completeHandler = new TelaahCompleteHandler(_telaahRepo, _resepDal, _auth);
            var completeResponse = await completeHandler.Handle(new TelaahCompleteCmd(
                "pharmacist1",
                updateResponse.TelaahResepId,
                updateResponse.Version
            ), default);
            completeResponse.Status.Should().Be(TelaahStatusEnum.Approved);

            // Verify: ta_trs_kartu_periksa.fn_status_dobill updated to 1
            var opt = ConnStringHelper.GetTestEnv().Value;
            using (var conn = new SqlConnection(ConnStringHelper.Get(opt)))
            {
                var statusDobill = conn.ExecuteScalar<int>(
                    "SELECT fn_status_dobill FROM ta_trs_kartu_periksa WHERE fs_kd_trs = @resepId",
                    new { resepId });
                statusDobill.Should().Be(1);
            }

            // Verify: Worklist no longer lists the approved prescription
            var worklistAfter = _worklistDal.ListTelaah().ToList();
            worklistAfter.Should().NotContain(x => x.ResepId == resepId);

            // 3. Doctor void attempt on processed prescription (fn_status_dobill == 1) is rejected
            var voidHandler = new ResepVoidHandler(_resepRepo, _resepDal);
            var act = () => voidHandler.Handle(new ResepVoidCommand(resepId, drId, "Dokter salah input"), default);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage($"Resep '{resepId}' telah diproses/didispensasi farmasi sehingga tidak dapat dibatalkan.");
        }
        finally
        {
            Cleanup(resepId, regId, noRm, drId, poliId);
        }
    }

    private static void EnsureSchema()
    {
        lock (SchemaLock)
        {
            var opt = ConnStringHelper.GetTestEnv();
            using var conn = new SqlConnection(ConnStringHelper.Get(opt.Value));
            conn.Open();

            conn.Execute("""
                IF OBJECT_ID('FARPU_Urgenitas') IS NULL
                CREATE TABLE FARPU_Urgenitas
                (
                    UrgenitasId VARCHAR(5) NOT NULL PRIMARY KEY,
                    UrgenitasName VARCHAR(50) NOT NULL
                );

                IF OBJECT_ID('tb_tipe_barang') IS NULL
                CREATE TABLE tb_tipe_barang
                (
                    fs_kd_tipe_barang VARCHAR(5) NOT NULL PRIMARY KEY,
                    fs_nm_tipe_barang VARCHAR(50) NOT NULL
                );

                IF OBJECT_ID('ta_trs_kartu_periksa3') IS NULL
                CREATE TABLE ta_trs_kartu_periksa3
                (
                    fs_kd_trs          VARCHAR(10) NOT NULL CONSTRAINT DF_ta_trs_kartu_periksa3_fs_kd_trs DEFAULT(''),
                    fn_no_urut         INT NOT NULL CONSTRAINT DF_ta_trs_kartu_periksa3_fn_no_urut DEFAULT(0),
                    fs_kd_barang       VARCHAR(15) NOT NULL CONSTRAINT DF_ta_trs_kartu_periksa3_fs_kd_barang DEFAULT(''),
                    fs_nm_barang       VARCHAR(100) NOT NULL CONSTRAINT DF_ta_trs_kartu_periksa3_fs_nm_barang DEFAULT(''),
                    fn_qty_barang      DECIMAL(18,2) NOT NULL CONSTRAINT DF_ta_trs_kartu_periksa3_fn_qty_barang DEFAULT(0),
                    fs_kd_satuan       VARCHAR(5) NOT NULL CONSTRAINT DF_ta_trs_kartu_periksa3_fs_kd_satuan DEFAULT(''),
                    fn_iter_barang     INT NOT NULL CONSTRAINT DF_ta_trs_kartu_periksa3_fn_iter_barang DEFAULT(0),
                    fb_racik           BIT NOT NULL CONSTRAINT DF_ta_trs_kartu_periksa3_fb_racik DEFAULT(0),
                    fb_komponen        BIT NOT NULL CONSTRAINT DF_ta_trs_kartu_periksa3_fb_komponen DEFAULT(0),
                    fs_kd_racik        VARCHAR(15) NOT NULL CONSTRAINT DF_ta_trs_kartu_periksa3_fs_kd_racik DEFAULT(''),
                    fn_qty_racik       DECIMAL(18,2) NOT NULL CONSTRAINT DF_ta_trs_kartu_periksa3_fn_qty_racik DEFAULT(0),
                    fs_racik_qty       VARCHAR(50) NOT NULL CONSTRAINT DF_ta_trs_kartu_periksa3_fs_racik_qty DEFAULT(''),
                    fs_keterangan      VARCHAR(100) NOT NULL CONSTRAINT DF_ta_trs_kartu_periksa3_fs_keterangan DEFAULT(''),
                    fs_etiket_desc     VARCHAR(100) NOT NULL CONSTRAINT DF_ta_trs_kartu_periksa3_fs_etiket_desc DEFAULT(''),
                    fn_etiket_qty      INT NOT NULL CONSTRAINT DF_ta_trs_kartu_periksa3_fn_etiket_qty DEFAULT(0),
                    fn_etiket_hari     DECIMAL(18,2) NOT NULL CONSTRAINT DF_ta_trs_kartu_periksa3_fn_etiket_hari DEFAULT(0),
                    fs_etiket_kd_pakai VARCHAR(3) NOT NULL CONSTRAINT DF_ta_trs_kartu_periksa3_fs_etiket_kd_pakai DEFAULT(''),
                    fs_etiket_catatan  VARCHAR(70) NOT NULL CONSTRAINT DF_ta_trs_kartu_periksa3_fs_etiket_catatan DEFAULT(''),
                    fs_udd_waktu       VARCHAR(30) NOT NULL CONSTRAINT DF_ta_trs_kartu_periksa3_fs_udd_waktu DEFAULT(''),
                    fb_void            BIT NOT NULL CONSTRAINT DF_ta_trs_kartu_periksa3_fb_void DEFAULT(0),
                    CONSTRAINT PK_ta_trs_kartu_periksa3 PRIMARY KEY CLUSTERED(fs_kd_trs, fn_no_urut)
                );

                IF OBJECT_ID('tb_satuan') IS NULL
                CREATE TABLE tb_satuan
                (
                    fs_kd_satuan VARCHAR(5) NOT NULL PRIMARY KEY,
                    fs_nm_satuan VARCHAR(30) NOT NULL
                );

                IF NOT EXISTS (SELECT 1 FROM tb_satuan WHERE fs_kd_satuan = 'TAB')
                    INSERT INTO tb_satuan (fs_kd_satuan, fs_nm_satuan) VALUES ('TAB', 'Tablet');

                IF COL_LENGTH('ta_trs_kartu_periksa3', 'fs_etiket_kd_pakai') IS NULL
                    ALTER TABLE ta_trs_kartu_periksa3 ADD fs_etiket_kd_pakai VARCHAR(3) NOT NULL CONSTRAINT DF_ta_trs_kartu_periksa3_fs_etiket_kd_pakai DEFAULT('');
                IF COL_LENGTH('ta_trs_kartu_periksa3', 'fs_etiket_catatan') IS NULL
                    ALTER TABLE ta_trs_kartu_periksa3 ADD fs_etiket_catatan VARCHAR(70) NOT NULL CONSTRAINT DF_ta_trs_kartu_periksa3_fs_etiket_catatan DEFAULT('');
                IF COL_LENGTH('ta_trs_kartu_periksa3', 'fs_udd_waktu') IS NULL
                    ALTER TABLE ta_trs_kartu_periksa3 ADD fs_udd_waktu VARCHAR(30) NOT NULL CONSTRAINT DF_ta_trs_kartu_periksa3_fs_udd_waktu DEFAULT('');
                IF COL_LENGTH('ta_trs_kartu_periksa3', 'fb_void') IS NULL
                    ALTER TABLE ta_trs_kartu_periksa3 ADD fb_void BIT NOT NULL CONSTRAINT DF_ta_trs_kartu_periksa3_fb_void DEFAULT(0);
                """);
        }
    }

    private void SeedContext(string regId, string noRm, string pasienName, string dokterId, string dokterName, string layananId, string poliName)
    {
        var opt = ConnStringHelper.GetTestEnv().Value;
        using var conn = new SqlConnection(ConnStringHelper.Get(opt));
        conn.Execute("IF NOT EXISTS (SELECT 1 FROM tc_mr WHERE fs_mr = @noRm) INSERT INTO tc_mr (fs_mr, fs_nm_pasien) VALUES (@noRm, @pasienName)", new { noRm, pasienName });
        conn.Execute("IF NOT EXISTS (SELECT 1 FROM ta_registrasi WHERE fs_kd_reg = @regId) INSERT INTO ta_registrasi (fs_kd_reg, fs_mr) VALUES (@regId, @noRm)", new { regId, noRm });
        conn.Execute("IF NOT EXISTS (SELECT 1 FROM td_peg WHERE fs_kd_peg = @dokterId) INSERT INTO td_peg (fs_kd_peg, fs_nm_peg) VALUES (@dokterId, @dokterName)", new { dokterId, dokterName });
        conn.Execute("IF NOT EXISTS (SELECT 1 FROM ta_layanan WHERE fs_kd_layanan = @layananId) INSERT INTO ta_layanan (fs_kd_layanan, fs_nm_layanan) VALUES (@layananId, @poliName)", new { layananId, poliName });
        conn.Execute("IF NOT EXISTS (SELECT 1 FROM tb_satuan WHERE fs_kd_satuan = 'TAB') INSERT INTO tb_satuan (fs_kd_satuan, fs_nm_satuan) VALUES ('TAB', 'Tablet')");
    }

    private void Cleanup(string resepId, string regId, string noRm, string dokterId, string layananId)
    {
        var opt = ConnStringHelper.GetTestEnv().Value;
        using var conn = new SqlConnection(ConnStringHelper.Get(opt));
        conn.Execute("DELETE FROM BILRG_AptTelaahResepItem WHERE TelaahResepId IN (SELECT TelaahResepId FROM BILRG_AptTelaahResep WHERE ResepId = @resepId)", new { resepId });
        conn.Execute("DELETE FROM BILRG_AptTelaahResepKajian WHERE TelaahResepId IN (SELECT TelaahResepId FROM BILRG_AptTelaahResep WHERE ResepId = @resepId)", new { resepId });
        conn.Execute("DELETE FROM BILRG_AptTelaahResep WHERE ResepId = @resepId", new { resepId });
        conn.Execute("IF OBJECT_ID('ta_trs_kartu_periksa3') IS NOT NULL DELETE FROM ta_trs_kartu_periksa3 WHERE fs_kd_trs = @resepId", new { resepId });
        conn.Execute("DELETE FROM ta_trs_kartu_periksa_resep WHERE fs_kd_trs = @resepId", new { resepId });
        conn.Execute("DELETE FROM ta_trs_kartu_periksa WHERE fs_kd_trs = @resepId", new { resepId });
        conn.Execute("DELETE FROM ta_registrasi WHERE fs_kd_reg = @regId", new { regId });
        conn.Execute("DELETE FROM tc_mr WHERE fs_mr = @noRm", new { noRm });
        conn.Execute("DELETE FROM td_peg WHERE fs_kd_peg = @dokterId", new { dokterId });
        conn.Execute("DELETE FROM ta_layanan WHERE fs_kd_layanan = @layananId", new { layananId });
    }
}
