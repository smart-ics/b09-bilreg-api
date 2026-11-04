using System.Data.SqlClient;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.PakaiBedFeature;
using Bilreg.Infrastructure.BedUsageContext.PakaiBedFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
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

    [Fact]
    public void Insert_WhenRoomingIn_ShouldNotUpdateBedRegistration()
    {
        using var trans = TransHelper.NewScope();
        using var conn = new SqlConnection(ConnStringHelper.Get(ConnStringHelper.GetTestEnv().Value));

        // 1. Setup master data for rooming-in
        conn.Execute(@"
            IF NOT EXISTS (SELECT 1 FROM ta_kamar_tipe WHERE fs_kd_kamar_tipe = '9')
                INSERT INTO ta_kamar_tipe (fs_kd_kamar_tipe, fs_nm_kamar_tipe, fb_gabung, fb_aktif, fb_default_tipe)
                VALUES ('9', 'Tipe Gabung', 1, 1, 0);
            ELSE
                UPDATE ta_kamar_tipe SET fb_gabung = 1 WHERE fs_kd_kamar_tipe = '9';

            IF NOT EXISTS (SELECT 1 FROM ta_bed WHERE fs_kd_bed = 'TGAB0001')
                INSERT INTO ta_bed (fs_kd_bed, fs_nm_bed, fs_kd_kamar, fn_status, fs_kd_reg, fb_aktif)
                VALUES ('TGAB0001', 'Bed Gabung', 'KM01', 1, 'REG_IBU01', 1);
            ELSE
                UPDATE ta_bed SET fs_kd_reg = 'REG_IBU01', fn_status = 1 WHERE fs_kd_bed = 'TGAB0001';

            IF NOT EXISTS (SELECT 1 FROM ta_registrasi WHERE fs_kd_reg = 'REG_BAYI01')
                INSERT INTO ta_registrasi (fs_kd_reg, fs_reg_ibu)
                VALUES ('REG_BAYI01', 'REG_IBU01');
            ELSE
                UPDATE ta_registrasi SET fs_reg_ibu = 'REG_IBU01' WHERE fs_kd_reg = 'REG_BAYI01';
        ");

        var babyDto = Faker() with {
            fs_kd_trs = "TPKB_BAYI_01",
            fs_kd_bed = "TGAB0001",
            fs_kd_reg = "REG_BAYI01",
            fs_kd_kamar_tipe = "9",
            fn_tarif = 0
        };

        // 2. Insert rooming-in baby
        _sut.Insert(babyDto);

        // 3. Verify ta_bed still holds mother's registration (not overwritten)
        var bedReg = conn.ExecuteScalar<string>("SELECT fs_kd_reg FROM ta_bed WHERE fs_kd_bed = 'TGAB0001'");
        bedReg.Should().Be("REG_IBU01");

        // 4. Verify ta_trs_bed records the baby's stay
        var actual = _sut.GetData(PakaiBedModel.Key("TPKB_BAYI_01"));
        actual.Should().NotBeNull();
        actual!.fs_kd_reg.Should().Be("REG_BAYI01");
        actual.fn_tarif.Should().Be(0);
    }

    [Fact]
    public async Task VoidBedAndRoomChargeAsync_WhenMotherBedVoided_CascadesVoidToRoomingInBaby_AndReleasesPhysicalBed()
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(ConnStringHelper.GetTestEnv().Value));
        try
        {
            // 1. Setup physical bed and rooming-in records
            conn.Execute(@"
                IF NOT EXISTS (SELECT 1 FROM ta_kamar_tipe WHERE fs_kd_kamar_tipe = '9')
                    INSERT INTO ta_kamar_tipe (fs_kd_kamar_tipe, fs_nm_kamar_tipe, fb_gabung, fb_aktif, fb_default_tipe)
                    VALUES ('9', 'Tipe Gabung', 1, 1, 0);
                ELSE
                    UPDATE ta_kamar_tipe SET fb_gabung = 1 WHERE fs_kd_kamar_tipe = '9';

                DELETE FROM ta_trs_bed WHERE fs_kd_trs IN ('TPKB_M01', 'TPKB_B01');
                DELETE FROM ta_bed WHERE fs_kd_bed = 'TGAB_V01';
                DELETE FROM ta_registrasi WHERE fs_kd_reg IN ('RGMOTH01', 'RGBABY01');

                INSERT INTO ta_registrasi (fs_kd_reg, fs_reg_ibu) VALUES ('RGMOTH01', '');
                INSERT INTO ta_registrasi (fs_kd_reg, fs_reg_ibu) VALUES ('RGBABY01', 'RGMOTH01');

                INSERT INTO ta_bed (fs_kd_bed, fs_nm_bed, fs_kd_kamar, fn_status, fs_kd_reg, fb_aktif)
                VALUES ('TGAB_V01', 'Bed Rawat Gabung V1', 'KM01', 1, 'RGMOTH01', 1);
            ");

            var motherDto = Faker() with
            {
                fs_kd_trs = "TPKB_M01",
                fs_kd_bed = "TGAB_V01",
                fs_kd_reg = "RGMOTH01",
                fs_kd_layanan = "TL001",
                fn_tarif = 250000,
                fd_tgl_out = "3000-01-01",
                fd_tgl_void = "3000-01-01"
            };

            var babyDto = Faker() with
            {
                fs_kd_trs = "TPKB_B01",
                fs_kd_bed = "TGAB_V01",
                fs_kd_reg = "RGBABY01",
                fs_kd_layanan = "TL002",
                fs_kd_kamar_tipe = "9",
                fn_tarif = 0,
                fd_tgl_out = "3000-01-01",
                fd_tgl_void = "3000-01-01"
            };

            _sut.Insert(motherDto);
            _sut.Insert(babyDto);

            // 2. Void mother's transaction
            var now = new DateTime(2026, 11, 4, 14, 0, 0);
            var affectedBangsals = await _sut.VoidBedAndRoomChargeAsync("TPKB_M01", "nurse_v", "Batal check-in ibu", now);

            // 3. Assert: Mother transaction voided
            var updatedMother = _sut.GetData(PakaiBedModel.Key("TPKB_M01"));
            updatedMother.Should().NotBeNull();
            updatedMother!.fd_tgl_void.Should().NotBe("3000-01-01");

            // 4. Assert: Baby transaction cascade voided
            var updatedBaby = _sut.GetData(PakaiBedModel.Key("TPKB_B01"));
            updatedBaby.Should().NotBeNull();
            updatedBaby!.fd_tgl_void.Should().NotBe("3000-01-01");

            // 5. Assert: Physical bed ta_bed released (fn_status = 0, fs_kd_reg = '')
            var bedRow = conn.QueryFirstOrDefault("SELECT fn_status, fs_kd_reg FROM ta_bed WHERE fs_kd_bed = 'TGAB_V01'");
            Assert.NotNull((object?)bedRow);
            ((int)bedRow!.fn_status).Should().Be(0);
            ((string)bedRow.fs_kd_reg).Should().BeEmpty();

            // 6. Assert: Baby's affected bangsal returned
            affectedBangsals.Should().Contain("TL002");
        }
        finally
        {
            conn.Execute(@"
                DELETE FROM ta_trs_bed WHERE fs_kd_trs IN ('TPKB_M01', 'TPKB_B01');
                DELETE FROM ta_bed WHERE fs_kd_bed = 'TGAB_V01';
                DELETE FROM ta_registrasi WHERE fs_kd_reg IN ('RGMOTH01', 'RGBABY01');
            ");
        }
    }

    [Fact]
    public async Task VoidBedAndRoomChargeAsync_WhenRoomingInBabyVoided_ProtectsPhysicalBed_AndDoesNotReleaseBed()
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(ConnStringHelper.GetTestEnv().Value));
        try
        {
            // 1. Setup physical bed occupied by mother with rooming-in baby
            conn.Execute(@"
                IF NOT EXISTS (SELECT 1 FROM ta_kamar_tipe WHERE fs_kd_kamar_tipe = '9')
                    INSERT INTO ta_kamar_tipe (fs_kd_kamar_tipe, fs_nm_kamar_tipe, fb_gabung, fb_aktif, fb_default_tipe)
                    VALUES ('9', 'Tipe Gabung', 1, 1, 0);
                ELSE
                    UPDATE ta_kamar_tipe SET fb_gabung = 1 WHERE fs_kd_kamar_tipe = '9';

                DELETE FROM ta_trs_bed WHERE fs_kd_trs IN ('TPKB_M02', 'TPKB_B02');
                DELETE FROM ta_bed WHERE fs_kd_bed = 'TGAB_V02';
                DELETE FROM ta_registrasi WHERE fs_kd_reg IN ('RGMOTH02', 'RGBABY02');

                INSERT INTO ta_registrasi (fs_kd_reg, fs_reg_ibu) VALUES ('RGMOTH02', '');
                INSERT INTO ta_registrasi (fs_kd_reg, fs_reg_ibu) VALUES ('RGBABY02', 'RGMOTH02');

                INSERT INTO ta_bed (fs_kd_bed, fs_nm_bed, fs_kd_kamar, fn_status, fs_kd_reg, fb_aktif)
                VALUES ('TGAB_V02', 'Bed Rawat Gabung V2', 'KM01', 1, 'RGMOTH02', 1);
            ");

            var motherDto = Faker() with
            {
                fs_kd_trs = "TPKB_M02",
                fs_kd_bed = "TGAB_V02",
                fs_kd_reg = "RGMOTH02",
                fs_kd_layanan = "TL001",
                fn_tarif = 250000,
                fd_tgl_out = "3000-01-01",
                fd_tgl_void = "3000-01-01"
            };

            var babyDto = Faker() with
            {
                fs_kd_trs = "TPKB_B02",
                fs_kd_bed = "TGAB_V02",
                fs_kd_reg = "RGBABY02",
                fs_kd_layanan = "TL002",
                fs_kd_kamar_tipe = "9",
                fn_tarif = 0,
                fd_tgl_out = "3000-01-01",
                fd_tgl_void = "3000-01-01"
            };

            _sut.Insert(motherDto);
            _sut.Insert(babyDto);

            // 2. Void ONLY baby's transaction
            var now = new DateTime(2026, 11, 4, 14, 30, 0);
            await _sut.VoidBedAndRoomChargeAsync("TPKB_B02", "nurse_v", "Batal gabung bayi (pindah NICU)", now);

            // 3. Assert: Baby transaction is voided
            var updatedBaby = _sut.GetData(PakaiBedModel.Key("TPKB_B02"));
            updatedBaby.Should().NotBeNull();
            updatedBaby!.fd_tgl_void.Should().NotBe("3000-01-01");

            // 4. Assert: Mother transaction remains active
            var updatedMother = _sut.GetData(PakaiBedModel.Key("TPKB_M02"));
            updatedMother.Should().NotBeNull();
            updatedMother!.fd_tgl_void.Should().Be("3000-01-01");

            // 5. CRITICAL INVARIANT: Physical bed ta_bed is PROTECTED - mother is still occupying it!
            var bedRow = conn.QueryFirstOrDefault("SELECT fn_status, fs_kd_reg FROM ta_bed WHERE fs_kd_bed = 'TGAB_V02'");
            Assert.NotNull((object?)bedRow);
            ((int)bedRow!.fn_status).Should().Be(1);
            ((string)bedRow.fs_kd_reg).Should().Be("RGMOTH02");
        }
        finally
        {
            conn.Execute(@"
                DELETE FROM ta_trs_bed WHERE fs_kd_trs IN ('TPKB_M02', 'TPKB_B02');
                DELETE FROM ta_bed WHERE fs_kd_bed = 'TGAB_V02';
                DELETE FROM ta_registrasi WHERE fs_kd_reg IN ('RGMOTH02', 'RGBABY02');
            ");
        }
    }
}
