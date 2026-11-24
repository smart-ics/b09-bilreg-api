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
            fs_kd_kamar_tipe: "X",
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
            fs_nm_kelas: "",
            fs_kd_kelas_tindakan: "",
            fd_tgl_jam_in: "2026-07-17 09:00:00");

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
    public void Insert_WithLayananDk_ShouldPersistLayananDk()
    {
        using var trans = TransHelper.NewScope();
        var key = "TPKB_DK_0001";
        var dto = Faker() with
        {
            fs_kd_trs = key,
            fs_kd_layanan_dk = "88"
        };

        _sut.Insert(dto);

        var actual = _sut.GetData(PakaiBedModel.Key(key));
        actual.Should().NotBeNull();
        actual!.fs_kd_layanan_dk.Should().Be("88");
    }

    [Fact]
    public void Insert_WithKomponenTarif_ShouldPersistToBed2()
    {
        using var trans = TransHelper.NewScope();
        var key = "TPKB000001";
        var komponenList = new List<PakaiBedKomponenDto>
        {
            new(key, "01", 100000, 1),
            new(key, "02", 50000, 1)
        };
        var dto = Faker() with
        {
            fs_kd_trs = key,
            fs_kd_kelas_tindakan = "T01",
            fd_tgl_jam_in = "2026-07-17 09:00:00",
            list_komponen = komponenList
        };

        _sut.Insert(dto);

        var actual = _sut.GetData(PakaiBedModel.Key(key));
        actual.Should().NotBeNull();
        actual.list_komponen.Should().NotBeNull();
        actual.list_komponen.Should().HaveCount(2);
        actual.list_komponen![0].fs_kd_detil_tarif.Should().Be("01");
        actual.list_komponen![0].fn_tarif.Should().Be(100000);
        actual.list_komponen![1].fs_kd_detil_tarif.Should().Be("02");
        actual.list_komponen![1].fn_tarif.Should().Be(50000);
    }

    [Fact]
    public void GetData_WhenKelasTindakanPopulated_ShouldReturnKelasTindakanAndMapToModel()
    {
        using var trans = TransHelper.NewScope();
        var key = "TPKB_KT_0001";
        var dto = Faker() with
        {
            fs_kd_trs = key,
            fs_kd_kelas = "T01",
            fs_kd_kelas_tindakan = "T02",
            fd_tgl_jam_in = "2026-07-17 09:00:00"
        };
        _sut.Insert(dto);

        var actual = _sut.GetData(PakaiBedModel.Key(key));
        actual.Should().NotBeNull();
        actual!.fs_kd_kelas.Should().Be("T01");
        actual.fs_kd_kelas_tindakan.Should().Be("T02");
        actual.fd_tgl_jam_in.Should().Be("2026-07-17 09:00:00");

        var model = actual.ToModel();
        model.KelasTindakanId.Should().Be("T02");
        model.Kelas.KelasId.Should().Be("T01");
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
                VALUES ('TGAB0001', 'Bed Gabung', '01', 1, 'REG_IBU01', 1);
            ELSE
                UPDATE ta_bed SET fs_kd_reg = 'REG_IBU01', fn_status = 1 WHERE fs_kd_bed = 'TGAB0001';

            IF NOT EXISTS (SELECT 1 FROM ta_registrasi WHERE fs_kd_reg = 'REG_IBU01')
                INSERT INTO ta_registrasi (fs_kd_reg, fs_kd_jenis_inap, fs_reg_ibu)
                VALUES ('REG_IBU01', '2', '');
            ELSE
                UPDATE ta_registrasi SET fs_kd_jenis_inap = '2', fs_reg_ibu = '' WHERE fs_kd_reg = 'REG_IBU01';

            IF NOT EXISTS (SELECT 1 FROM ta_registrasi WHERE fs_kd_reg = 'REG_BAYI01')
                INSERT INTO ta_registrasi (fs_kd_reg, fs_kd_jenis_inap, fs_reg_ibu)
                VALUES ('REG_BAYI01', '3', 'REG_IBU01');
            ELSE
                UPDATE ta_registrasi SET fs_reg_ibu = 'REG_IBU01', fs_kd_jenis_inap = '3' WHERE fs_kd_reg = 'REG_BAYI01';
        ");

        var motherDto = Faker() with {
            fs_kd_trs = "TPKB_IBU_01",
            fs_kd_bed = "TGAB0001",
            fs_kd_reg = "REG_IBU01",
            fn_tarif = 250000,
            fd_tgl_out = "3000-01-01",
            fd_tgl_void = "3000-01-01"
        };
        _sut.Insert(motherDto);

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

        // 5. Verify zero ta_registrasi mutation
        var babyReg = conn.QueryFirstOrDefault("SELECT fs_kd_jenis_inap, fs_reg_ibu FROM ta_registrasi WHERE fs_kd_reg = 'REG_BAYI01'");
        Assert.NotNull((object?)babyReg);
        ((string)babyReg!.fs_kd_jenis_inap).Should().Be("3");
        ((string)babyReg.fs_reg_ibu).Should().Be("REG_IBU01");
    }

    [Fact]
    public void Insert_WhenBabySoloBed_ShouldUpdateBedOccupancyStatus()
    {
        using var trans = TransHelper.NewScope();
        using var conn = new SqlConnection(ConnStringHelper.Get(ConnStringHelper.GetTestEnv().Value));

        // 1. Setup master data and bed for solo baby
        conn.Execute(@"
            IF NOT EXISTS (SELECT 1 FROM ta_kamar_tipe WHERE fs_kd_kamar_tipe = '9')
                INSERT INTO ta_kamar_tipe (fs_kd_kamar_tipe, fs_nm_kamar_tipe, fb_gabung, fb_aktif, fb_default_tipe)
                VALUES ('9', 'Tipe Gabung', 1, 1, 0);

            IF NOT EXISTS (SELECT 1 FROM ta_bed WHERE fs_kd_bed = 'TBED_S01')
                INSERT INTO ta_bed (fs_kd_bed, fs_nm_bed, fs_kd_kamar, fn_status, fs_kd_reg, fb_aktif)
                VALUES ('TBED_S01', 'Bed Bayi Mandiri 1', '01', 0, '', 1);
            ELSE
                UPDATE ta_bed SET fs_kd_reg = '', fn_status = 0 WHERE fs_kd_bed = 'TBED_S01';

            IF NOT EXISTS (SELECT 1 FROM ta_registrasi WHERE fs_kd_reg = 'RG_IBUS01')
                INSERT INTO ta_registrasi (fs_kd_reg, fs_kd_jenis_inap, fs_reg_ibu)
                VALUES ('RG_IBUS01', '2', '');

            IF NOT EXISTS (SELECT 1 FROM ta_registrasi WHERE fs_kd_reg = 'RG_BAYS01')
                INSERT INTO ta_registrasi (fs_kd_reg, fs_kd_jenis_inap, fs_reg_ibu)
                VALUES ('RG_BAYS01', '3', 'RG_IBUS01');
            ELSE
                UPDATE ta_registrasi SET fs_reg_ibu = 'RG_IBUS01', fs_kd_jenis_inap = '3' WHERE fs_kd_reg = 'RG_BAYS01';
        ");

        var babySoloDto = Faker() with {
            fs_kd_trs = "TPKB_BS01",
            fs_kd_bed = "TBED_S01",
            fs_kd_reg = "RG_BAYS01",
            fs_kd_kamar_tipe = "9",
            fn_tarif = 175000
        };

        // 2. Insert solo baby bed allocation
        _sut.Insert(babySoloDto);

        // 3. Verify ta_bed is updated to occupied (fn_status = 1) with baby's registration
        var bedRow = conn.QueryFirstOrDefault("SELECT fn_status, fs_kd_reg FROM ta_bed WHERE fs_kd_bed = 'TBED_S01'");
        Assert.NotNull((object?)bedRow);
        ((int)bedRow!.fn_status).Should().Be(1);
        ((string)bedRow.fs_kd_reg).Should().Be("RG_BAYS01");

        // 4. Verify ta_trs_bed records baby's stay with full tariff
        var actual = _sut.GetData(PakaiBedModel.Key("TPKB_BS01"));
        actual.Should().NotBeNull();
        actual!.fs_kd_reg.Should().Be("RG_BAYS01");
        actual.fn_tarif.Should().Be(175000);

        // 5. Verify ta_registrasi remains completely unmutated
        var babyReg = conn.QueryFirstOrDefault("SELECT fs_kd_jenis_inap, fs_reg_ibu FROM ta_registrasi WHERE fs_kd_reg = 'RG_BAYS01'");
        Assert.NotNull((object?)babyReg);
        ((string)babyReg!.fs_kd_jenis_inap).Should().Be("3");
        ((string)babyReg.fs_reg_ibu).Should().Be("RG_IBUS01");
    }

    [Fact]
    public void Update_WhenBabySoloBed_ShouldMaintainBedOccupancyStatus()
    {
        using var trans = TransHelper.NewScope();
        using var conn = new SqlConnection(ConnStringHelper.Get(ConnStringHelper.GetTestEnv().Value));

        // 1. Setup master data and bed for solo baby
        conn.Execute(@"
            IF NOT EXISTS (SELECT 1 FROM ta_kamar_tipe WHERE fs_kd_kamar_tipe = '9')
                INSERT INTO ta_kamar_tipe (fs_kd_kamar_tipe, fs_nm_kamar_tipe, fb_gabung, fb_aktif, fb_default_tipe)
                VALUES ('9', 'Tipe Gabung', 1, 1, 0);

            IF NOT EXISTS (SELECT 1 FROM ta_bed WHERE fs_kd_bed = 'TBED_S02')
                INSERT INTO ta_bed (fs_kd_bed, fs_nm_bed, fs_kd_kamar, fn_status, fs_kd_reg, fb_aktif)
                VALUES ('TBED_S02', 'Bed Bayi Mandiri 2', '01', 0, '', 1);
            ELSE
                UPDATE ta_bed SET fs_kd_reg = '', fn_status = 0 WHERE fs_kd_bed = 'TBED_S02';

            IF NOT EXISTS (SELECT 1 FROM ta_registrasi WHERE fs_kd_reg = 'RG_IBUS02')
                INSERT INTO ta_registrasi (fs_kd_reg, fs_kd_jenis_inap, fs_reg_ibu)
                VALUES ('RG_IBUS02', '2', '');

            IF NOT EXISTS (SELECT 1 FROM ta_registrasi WHERE fs_kd_reg = 'RG_BAYS02')
                INSERT INTO ta_registrasi (fs_kd_reg, fs_kd_jenis_inap, fs_reg_ibu)
                VALUES ('RG_BAYS02', '3', 'RG_IBUS02');
            ELSE
                UPDATE ta_registrasi SET fs_reg_ibu = 'RG_IBUS02', fs_kd_jenis_inap = '3' WHERE fs_kd_reg = 'RG_BAYS02';
        ");

        var babySoloDto = Faker() with {
            fs_kd_trs = "TPKB_BS02",
            fs_kd_bed = "TBED_S02",
            fs_kd_reg = "RG_BAYS02",
            fs_kd_kamar_tipe = "9",
            fn_tarif = 150000
        };
        _sut.Insert(babySoloDto);

        // 2. Update solo baby transaction (e.g. rate adjustment or detail change)
        var updatedBabyDto = babySoloDto with { fn_tarif = 180000 };
        _sut.Update(updatedBabyDto);

        // 3. Verify ta_bed remains occupied by baby
        var bedRow = conn.QueryFirstOrDefault("SELECT fn_status, fs_kd_reg FROM ta_bed WHERE fs_kd_bed = 'TBED_S02'");
        Assert.NotNull((object?)bedRow);
        ((int)bedRow!.fn_status).Should().Be(1);
        ((string)bedRow.fs_kd_reg).Should().Be("RG_BAYS02");

        var actual = _sut.GetData(PakaiBedModel.Key("TPKB_BS02"));
        actual.Should().NotBeNull();
        actual!.fn_tarif.Should().Be(180000);
    }

    [Fact]
    public void Update_WhenRoomingIn_ShouldNotOverwriteMotherBed()
    {
        using var trans = TransHelper.NewScope();
        using var conn = new SqlConnection(ConnStringHelper.Get(ConnStringHelper.GetTestEnv().Value));

        // 1. Setup master data for rooming-in
        conn.Execute(@"
            IF NOT EXISTS (SELECT 1 FROM ta_kamar_tipe WHERE fs_kd_kamar_tipe = '9')
                INSERT INTO ta_kamar_tipe (fs_kd_kamar_tipe, fs_nm_kamar_tipe, fb_gabung, fb_aktif, fb_default_tipe)
                VALUES ('9', 'Tipe Gabung', 1, 1, 0);

            IF NOT EXISTS (SELECT 1 FROM ta_bed WHERE fs_kd_bed = 'TGAB0002')
                INSERT INTO ta_bed (fs_kd_bed, fs_nm_bed, fs_kd_kamar, fn_status, fs_kd_reg, fb_aktif)
                VALUES ('TGAB0002', 'Bed Gabung 2', '01', 0, '', 1);
            ELSE
                UPDATE ta_bed SET fs_kd_reg = '', fn_status = 0 WHERE fs_kd_bed = 'TGAB0002';

            IF NOT EXISTS (SELECT 1 FROM ta_registrasi WHERE fs_kd_reg = 'REG_IBU02')
                INSERT INTO ta_registrasi (fs_kd_reg, fs_kd_jenis_inap, fs_reg_ibu)
                VALUES ('REG_IBU02', '2', '');

            IF NOT EXISTS (SELECT 1 FROM ta_registrasi WHERE fs_kd_reg = 'REG_BAY02')
                INSERT INTO ta_registrasi (fs_kd_reg, fs_kd_jenis_inap, fs_reg_ibu)
                VALUES ('REG_BAY02', '3', 'REG_IBU02');
            ELSE
                UPDATE ta_registrasi SET fs_reg_ibu = 'REG_IBU02', fs_kd_jenis_inap = '3' WHERE fs_kd_reg = 'REG_BAY02';
        ");

        var motherDto = Faker() with {
            fs_kd_trs = "TPKB_IB02",
            fs_kd_bed = "TGAB0002",
            fs_kd_reg = "REG_IBU02",
            fn_tarif = 250000,
            fd_tgl_out = "3000-01-01",
            fd_tgl_void = "3000-01-01"
        };
        _sut.Insert(motherDto);

        var babyDto = Faker() with {
            fs_kd_trs = "TPKB_BY02",
            fs_kd_bed = "TGAB0002",
            fs_kd_reg = "REG_BAY02",
            fs_kd_kamar_tipe = "9",
            fn_tarif = 0
        };
        _sut.Insert(babyDto);

        // Verify mother owns the bed initially
        var bedRegInitial = conn.ExecuteScalar<string>("SELECT fs_kd_reg FROM ta_bed WHERE fs_kd_bed = 'TGAB0002'");
        bedRegInitial.Should().Be("REG_IBU02");

        // 2. Update babyDto (e.g. changing note or doctor or time)
        var updatedBabyDto = babyDto with { fs_kd_petugas = "USR2" };
        _sut.Update(updatedBabyDto);

        // 3. Verify ta_bed still holds mother's registration (not overwritten on Update)
        var bedRegAfter = conn.ExecuteScalar<string>("SELECT fs_kd_reg FROM ta_bed WHERE fs_kd_bed = 'TGAB0002'");
        bedRegAfter.Should().Be("REG_IBU02");
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
                VALUES ('TGAB_V01', 'Bed Rawat Gabung V1', '01', 1, 'RGMOTH01', 1);
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
                VALUES ('TGAB_V02', 'Bed Rawat Gabung V2', '01', 1, 'RGMOTH02', 1);
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

    [Fact]
    public async Task VoidBedAndRoomChargeAsync_WhenMotherBedVoided_PreservesRegistrations_AndRevertsBothWaitingLists()
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(ConnStringHelper.GetTestEnv().Value));
        try
        {
            conn.Execute(@"
                IF NOT EXISTS (SELECT 1 FROM ta_kamar_tipe WHERE fs_kd_kamar_tipe = '9')
                    INSERT INTO ta_kamar_tipe (fs_kd_kamar_tipe, fs_nm_kamar_tipe, fb_gabung, fb_aktif, fb_default_tipe)
                    VALUES ('9', 'Tipe Gabung', 1, 1, 0);
                ELSE
                    UPDATE ta_kamar_tipe SET fb_gabung = 1 WHERE fs_kd_kamar_tipe = '9';

                DELETE FROM BILRG_BedWaitingList WHERE WaitingListId IN ('WL_M03', 'WL_B03');
                DELETE FROM ta_trs_bed WHERE fs_kd_trs IN ('TPKB_M03', 'TPKB_B03');
                DELETE FROM ta_bed WHERE fs_kd_bed = 'TGAB_V03';
                DELETE FROM ta_registrasi WHERE fs_kd_reg IN ('RGMOTH03', 'RGBABY03');

                INSERT INTO ta_registrasi (fs_kd_reg, fs_kd_jenis_inap, fs_reg_ibu) VALUES ('RGMOTH03', '1', '');
                INSERT INTO ta_registrasi (fs_kd_reg, fs_kd_jenis_inap, fs_reg_ibu) VALUES ('RGBABY03', '3', 'RGMOTH03');

                INSERT INTO ta_bed (fs_kd_bed, fs_nm_bed, fs_kd_kamar, fn_status, fs_kd_reg, fb_aktif)
                VALUES ('TGAB_V03', 'Bed Rawat Gabung V3', '01', 1, 'RGMOTH03', 1);

                IF OBJECT_ID('BILRG_BedWaitingList', 'U') IS NOT NULL
                BEGIN
                    INSERT INTO BILRG_BedWaitingList (
                        WaitingListId, WaitingListStatus, RegId, PasienId,
                        KelasId, KelasName, BangsalId, BangsalName,
                        Priority, AdmittedBedId,
                        CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
                    VALUES (
                        'WL_M03', 2, 'RGMOTH03', 'MR_M03',
                        '3', 'Kelas 3', 'BM001', 'Bangsal Maternitas',
                        1, 'TGAB_V03',
                        'test', GETUTCDATE(), 'test', GETUTCDATE(), '', '3000-01-01');

                    INSERT INTO BILRG_BedWaitingList (
                        WaitingListId, WaitingListStatus, RegId, PasienId,
                        KelasId, KelasName, BangsalId, BangsalName,
                        Priority, AdmittedBedId,
                        CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
                    VALUES (
                        'WL_B03', 2, 'RGBABY03', 'MR_B03',
                        '3', 'Kelas 3', 'BB001', 'Bangsal Perinatologi',
                        1, 'TGAB_V03',
                        'test', GETUTCDATE(), 'test', GETUTCDATE(), '', '3000-01-01');
                END
            ");

            var motherDto = Faker() with
            {
                fs_kd_trs = "TPKB_M03",
                fs_kd_bed = "TGAB_V03",
                fs_kd_reg = "RGMOTH03",
                fs_kd_layanan = "BM001",
                fn_tarif = 250000,
                fd_tgl_out = "3000-01-01",
                fd_tgl_void = "3000-01-01"
            };

            var babyDto = Faker() with
            {
                fs_kd_trs = "TPKB_B03",
                fs_kd_bed = "TGAB_V03",
                fs_kd_reg = "RGBABY03",
                fs_kd_layanan = "BB001",
                fs_kd_kamar_tipe = "9",
                fn_tarif = 0,
                fd_tgl_out = "3000-01-01",
                fd_tgl_void = "3000-01-01"
            };

            _sut.Insert(motherDto);
            _sut.Insert(babyDto);

            // Void mother's rooming-in bed
            var now = new DateTime(2026, 11, 5, 10, 0, 0);
            var affectedBangsals = await _sut.VoidBedAndRoomChargeAsync("TPKB_M03", "nurse_v", "Batal check-in ibu & bayi", now);

            // 1. Assert: Mother registration preserved without mutation (remains '1', NOT updated to '2')
            var motherReg = conn.QueryFirstOrDefault("SELECT fs_kd_jenis_inap, fs_reg_ibu FROM ta_registrasi WHERE fs_kd_reg = 'RGMOTH03'");
            Assert.NotNull((object?)motherReg);
            ((string)motherReg!.fs_kd_jenis_inap).Should().Be("1");

            // 2. Assert: Baby registration preserved as Perinatal '3' without mutation (NOT changed to '2')
            var babyReg = conn.QueryFirstOrDefault("SELECT fs_kd_jenis_inap, fs_reg_ibu FROM ta_registrasi WHERE fs_kd_reg = 'RGBABY03'");
            Assert.NotNull((object?)babyReg);
            ((string)babyReg!.fs_kd_jenis_inap).Should().Be("3");
            ((string)babyReg.fs_reg_ibu).Should().Be("RGMOTH03");

            // 3. Assert: Both Waiting List records reverted to status 0 and allocation cleared
            var motherWl = conn.QueryFirstOrDefault("SELECT WaitingListStatus, AdmittedBedId FROM BILRG_BedWaitingList WHERE WaitingListId = 'WL_M03'");
            Assert.NotNull((object?)motherWl);
            ((int)motherWl!.WaitingListStatus).Should().Be(0);
            ((string)motherWl.AdmittedBedId).Should().BeEmpty();

            var babyWl = conn.QueryFirstOrDefault("SELECT WaitingListStatus, AdmittedBedId FROM BILRG_BedWaitingList WHERE WaitingListId = 'WL_B03'");
            Assert.NotNull((object?)babyWl);
            ((int)babyWl!.WaitingListStatus).Should().Be(0);
            ((string)babyWl.AdmittedBedId).Should().BeEmpty();

            // 4. Assert: Both mother and baby bangsal IDs returned in affected list
            affectedBangsals.Should().Contain("BM001");
            affectedBangsals.Should().Contain("BB001");
        }
        finally
        {
            conn.Execute(@"
                DELETE FROM BILRG_BedWaitingList WHERE WaitingListId IN ('WL_M03', 'WL_B03');
                DELETE FROM ta_trs_bed WHERE fs_kd_trs IN ('TPKB_M03', 'TPKB_B03');
                DELETE FROM ta_bed WHERE fs_kd_bed = 'TGAB_V03';
                DELETE FROM ta_registrasi WHERE fs_kd_reg IN ('RGMOTH03', 'RGBABY03');
            ");
        }
    }

[Fact]
    public async Task VoidBedAndRoomChargeAsync_WhenTaksakaRoomChargeExists_CleansUpRoomCharge2AndSoftVoidsRoomCharge()
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(ConnStringHelper.GetTestEnv().Value));
        const string testBedId = "TBEDTK9";
        const string testRegId = "RGTK99001";
        const string testPakaiBedId = "TPKB_TK099";
        const string testRcId = "TPKB_TK099-1106";
        try
        {
            conn.Execute(@"
                DELETE FROM ta_trs_roomcharge2 WHERE fs_kd_trs = @rcId;
                DELETE FROM ta_trs_roomcharge_log WHERE fs_kd_trs = @rcId;
                DELETE FROM ta_trs_roomcharge WHERE fs_kd_trs = @rcId;
                DELETE FROM ta_trs_bed WHERE fs_kd_trs = @pakaiBedId;
                DELETE FROM ta_bed WHERE fs_kd_bed = @bedId;
                DELETE FROM ta_registrasi WHERE fs_kd_reg = @regId;

                INSERT INTO ta_registrasi (fs_kd_reg, fs_kd_jenis_inap, fs_reg_ibu) VALUES (@regId, '1', '');
                INSERT INTO ta_bed (fs_kd_bed, fs_nm_bed, fs_kd_kamar, fn_status, fs_kd_reg, fb_aktif)
                VALUES (@bedId, 'Bed Test Taksaka', '01', 1, @regId, 1);
            ", new { rcId = testRcId, pakaiBedId = testPakaiBedId, bedId = testBedId, regId = testRegId });

            var bedDto = Faker() with
            {
                fs_kd_trs = testPakaiBedId,
                fs_kd_bed = testBedId,
                fs_kd_reg = testRegId,
                fs_kd_layanan = "TL001",
                fn_tarif = 250000,
                fd_tgl_in = "2026-11-06",
                fd_tgl_out = "3000-01-01",
                fd_tgl_void = "3000-01-01"
            };
            _sut.Insert(bedDto);

            conn.Execute(@"
                INSERT INTO ta_trs_roomcharge (
                    fs_kd_trs, fd_tgl_trs, fs_jam_trs, fs_kd_petugas, fs_kd_reg,
                    fs_kd_layanan, fs_kd_bed, fn_tarif, fn_diskon, fn_total,
                    fs_ket, fn_qty, fb_gen_nm_kelas, fs_kd_tipe_jaminan, fn_nilai_klaim,
                    fs_kd_trs_dx, fs_kd_pakai_bed
                ) VALUES (
                    @rcId, '2026-11-06', '06:00:00', 'taksaka', @regId,
                    'TL001', @bedId, 250000, 0, 250000,
                    'Taksaka daily charge', 1, 0, '', 0,
                    '', ''
                );

                INSERT INTO ta_trs_roomcharge2 (
                    fs_kd_trs, fs_kd_detil_tarif, fn_tarif, fn_diskon, fn_total,
                    fs_kd_rek_pdpt, fs_kd_rek_diskon
                ) VALUES (
                    @rcId, '01', 250000, 0, 250000,
                    '', ''
                );
            ", new { rcId = testRcId, pakaiBedId = testPakaiBedId, bedId = testBedId, regId = testRegId });

            var now = new DateTime(2026, 11, 6, 11, 0, 0);
            await _sut.VoidBedAndRoomChargeAsync(testPakaiBedId, "usr_void", "Batal bed taksaka", now);

            // Assert 1: ta_trs_roomcharge2 is DELETED
            var rc2Count = conn.ExecuteScalar<int>("SELECT COUNT(1) FROM ta_trs_roomcharge2 WHERE fs_kd_trs = @rcId", new { rcId = testRcId });
            rc2Count.Should().Be(0);

            // Assert 2: ta_trs_roomcharge_log has snapshot with fb_status_void = 1
            var logCount = conn.ExecuteScalar<int>("SELECT COUNT(1) FROM ta_trs_roomcharge_log WHERE fs_kd_trs = @rcId AND fb_status_void = 1", new { rcId = testRcId });
            logCount.Should().BeGreaterThan(0);

            // Assert 3: ta_trs_roomcharge is physically DELETED matching legacy VB6
            var rcCount = conn.ExecuteScalar<int>("SELECT COUNT(1) FROM ta_trs_roomcharge WHERE fs_kd_trs = @rcId", new { rcId = testRcId });
            rcCount.Should().Be(0);

            // Assert 4: ta_trs_bed is voided
            var updatedBed = _sut.GetData(PakaiBedModel.Key(testPakaiBedId));
            updatedBed.Should().NotBeNull();
            updatedBed!.fd_tgl_void.Should().NotBe("3000-01-01");
        }
        finally
        {
            conn.Execute(@"
                DELETE FROM ta_trs_roomcharge2 WHERE fs_kd_trs = @rcId;
                DELETE FROM ta_trs_roomcharge_log WHERE fs_kd_trs = @rcId;
                DELETE FROM ta_trs_roomcharge WHERE fs_kd_trs = @rcId;
                DELETE FROM ta_trs_bed WHERE fs_kd_trs = @pakaiBedId;
                DELETE FROM ta_bed WHERE fs_kd_bed = @bedId;
                DELETE FROM ta_registrasi WHERE fs_kd_reg = @regId;
            ", new { rcId = testRcId, pakaiBedId = testPakaiBedId, bedId = testBedId, regId = testRegId });
        }
    }

    [Fact]
    public async Task VoidBedAndRoomChargeAsync_WhenRoomChargeAlreadyPaidAtCashier_ThrowsExceptionAndPreventsVoid()
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(ConnStringHelper.GetTestEnv().Value));
        const string testBedId = "TBEDTK8";
        const string testRegId = "RGTK99002";
        const string testPakaiBedId = "TPKB_TK098";
        const string testRcId = "TPKB_TK098-1106";
        try
        {
            conn.Execute(@"
                DELETE FROM ta_trs_roomcharge2 WHERE fs_kd_trs = @rcId;
                DELETE FROM ta_trs_roomcharge_log WHERE fs_kd_trs = @rcId;
                DELETE FROM ta_trs_roomcharge WHERE fs_kd_trs = @rcId;
                DELETE FROM ta_trs_bed WHERE fs_kd_trs = @pakaiBedId;
                DELETE FROM ta_bed WHERE fs_kd_bed = @bedId;
                DELETE FROM ta_registrasi WHERE fs_kd_reg = @regId;

                INSERT INTO ta_registrasi (fs_kd_reg, fs_kd_jenis_inap, fs_reg_ibu) VALUES (@regId, '1', '');
                INSERT INTO ta_bed (fs_kd_bed, fs_nm_bed, fs_kd_kamar, fn_status, fs_kd_reg, fb_aktif)
                VALUES (@bedId, 'Bed Test Taksaka 2', 'KM01', 1, @regId, 1);
            ", new { rcId = testRcId, pakaiBedId = testPakaiBedId, bedId = testBedId, regId = testRegId });

            var bedDto = Faker() with
            {
                fs_kd_trs = testPakaiBedId,
                fs_kd_bed = testBedId,
                fs_kd_reg = testRegId,
                fs_kd_layanan = "TL001",
                fn_tarif = 250000,
                fd_tgl_in = "2026-11-06",
                fd_tgl_out = "3000-01-01",
                fd_tgl_void = "3000-01-01"
            };
            _sut.Insert(bedDto);

            conn.Execute(@"
                INSERT INTO ta_trs_roomcharge (
                    fs_kd_trs, fd_tgl_trs, fs_jam_trs, fs_kd_petugas, fs_kd_reg,
                    fs_kd_layanan, fs_kd_bed, fn_tarif, fn_diskon, fn_total,
                    fs_ket, fn_qty, fb_gen_nm_kelas, fs_kd_tipe_jaminan, fn_nilai_klaim,
                    fs_kd_trs_dx, fs_kd_pakai_bed
                ) VALUES (
                    @rcId, '2026-11-06', '06:00:00', 'taksaka', @regId,
                    'TL001', @bedId, 250000, 0, 250000,
                    'Taksaka paid charge', 1, 0, '', 0,
                    'KAS001', ''
                );
            ", new { rcId = testRcId, pakaiBedId = testPakaiBedId, bedId = testBedId, regId = testRegId });

            var now = new DateTime(2026, 11, 6, 11, 0, 0);

            var act = async () => await _sut.VoidBedAndRoomChargeAsync(testPakaiBedId, "usr_void", "Batal bed tapi sudah bayar", now);
            await act.Should().ThrowAsync<SqlException>()
                .WithMessage("*sudah terbayar di kasir*");

            var bedStillActive = _sut.GetData(PakaiBedModel.Key(testPakaiBedId));
            bedStillActive.Should().NotBeNull();
            bedStillActive!.fd_tgl_void.Should().Be("3000-01-01");
        }
        finally
        {
            conn.Execute(@"
                DELETE FROM ta_trs_roomcharge2 WHERE fs_kd_trs = @rcId;
                DELETE FROM ta_trs_roomcharge_log WHERE fs_kd_trs = @rcId;
                DELETE FROM ta_trs_roomcharge WHERE fs_kd_trs = @rcId;
                DELETE FROM ta_trs_bed WHERE fs_kd_trs = @pakaiBedId;
                DELETE FROM ta_bed WHERE fs_kd_bed = @bedId;
                DELETE FROM ta_registrasi WHERE fs_kd_reg = @regId;
            ", new { rcId = testRcId, pakaiBedId = testPakaiBedId, bedId = testBedId, regId = testRegId });
        }
    }
    [Fact]
    public async Task VoidBedAndRoomChargeAsync_WhenVoidingBabyBed_PreservesJenisInapPerinatalWithoutMutation()
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(ConnStringHelper.GetTestEnv().Value));
        const string testBedId = "TGAB_V04";
        const string testMotherRegId = "RGMOTH04";
        const string testBabyRegId = "RGBABY04";
        const string testMotherPakaiBedId = "TPKB_M04";
        const string testBabyPakaiBedId = "TPKB_B04";
        try
        {
            conn.Execute(@"
                IF NOT EXISTS (SELECT 1 FROM ta_kamar_tipe WHERE fs_kd_kamar_tipe = '9')
                    INSERT INTO ta_kamar_tipe (fs_kd_kamar_tipe, fs_nm_kamar_tipe, fb_gabung, fb_aktif, fb_default_tipe)
                    VALUES ('9', 'Tipe Gabung', 1, 1, 0);
                ELSE
                    UPDATE ta_kamar_tipe SET fb_gabung = 1 WHERE fs_kd_kamar_tipe = '9';

                DELETE FROM BILRG_BedWaitingList WHERE WaitingListId IN ('WL_B04');
                DELETE FROM ta_trs_bed WHERE fs_kd_trs IN (@motherPakaiBedId, @babyPakaiBedId);
                DELETE FROM ta_bed WHERE fs_kd_bed = @bedId;
                DELETE FROM ta_registrasi WHERE fs_kd_reg IN (@motherRegId, @babyRegId);

                INSERT INTO ta_registrasi (fs_kd_reg, fs_kd_jenis_inap, fs_reg_ibu) VALUES (@motherRegId, '2', '');
                INSERT INTO ta_registrasi (fs_kd_reg, fs_kd_jenis_inap, fs_reg_ibu) VALUES (@babyRegId, '3', @motherRegId);

                INSERT INTO ta_bed (fs_kd_bed, fs_nm_bed, fs_kd_kamar, fn_status, fs_kd_reg, fb_aktif)
                VALUES (@bedId, 'Bed Rawat Gabung V4', '01', 1, @motherRegId, 1);

                IF OBJECT_ID('BILRG_BedWaitingList', 'U') IS NOT NULL
                BEGIN
                    INSERT INTO BILRG_BedWaitingList (
                        WaitingListId, WaitingListStatus, RegId, PasienId,
                        KelasId, KelasName, BangsalId, BangsalName,
                        Priority, AdmittedBedId,
                        CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
                    VALUES (
                        'WL_B04', 2, @babyRegId, 'MR_B04',
                        '3', 'Kelas 3', 'BB001', 'Bangsal Perinatologi',
                        1, @bedId,
                        'test', GETUTCDATE(), 'test', GETUTCDATE(), '', '3000-01-01');
                END
            ", new
            {
                bedId = testBedId,
                motherRegId = testMotherRegId,
                babyRegId = testBabyRegId,
                motherPakaiBedId = testMotherPakaiBedId,
                babyPakaiBedId = testBabyPakaiBedId
            });

            var motherDto = Faker() with
            {
                fs_kd_trs = testMotherPakaiBedId,
                fs_kd_bed = testBedId,
                fs_kd_reg = testMotherRegId,
                fs_kd_layanan = "BM001",
                fn_tarif = 250000,
                fd_tgl_out = "3000-01-01",
                fd_tgl_void = "3000-01-01"
            };

            var babyDto = Faker() with
            {
                fs_kd_trs = testBabyPakaiBedId,
                fs_kd_bed = testBedId,
                fs_kd_reg = testBabyRegId,
                fs_kd_layanan = "BB001",
                fs_kd_kamar_tipe = "9",
                fn_tarif = 0,
                fd_tgl_out = "3000-01-01",
                fd_tgl_void = "3000-01-01"
            };

            _sut.Insert(motherDto);
            _sut.Insert(babyDto);

            // Void ONLY baby's transaction
            var now = new DateTime(2026, 11, 10, 10, 0, 0);
            await _sut.VoidBedAndRoomChargeAsync(testBabyPakaiBedId, "nurse_v", "Batal gabung bayi (tetap perinatal)", now);

            // Assert 1: Baby transaction is voided
            var updatedBaby = _sut.GetData(PakaiBedModel.Key(testBabyPakaiBedId));
            updatedBaby.Should().NotBeNull();
            updatedBaby!.fd_tgl_void.Should().NotBe("3000-01-01");

            // Assert 2: Mother transaction remains active
            var updatedMother = _sut.GetData(PakaiBedModel.Key(testMotherPakaiBedId));
            updatedMother.Should().NotBeNull();
            updatedMother!.fd_tgl_void.Should().Be("3000-01-01");

            // Assert 3: ta_registrasi.fs_kd_jenis_inap for baby remains '3' (Perinatal) and is NOT mutated to '2'
            var babyReg = conn.QueryFirstOrDefault("SELECT fs_kd_jenis_inap, fs_reg_ibu FROM ta_registrasi WHERE fs_kd_reg = @regId", new { regId = testBabyRegId });
            Assert.NotNull((object?)babyReg);
            ((string)babyReg!.fs_kd_jenis_inap).Should().Be("3");
            ((string)babyReg.fs_kd_jenis_inap).Should().NotBe("2");
            ((string)babyReg.fs_reg_ibu).Should().Be(testMotherRegId);

            // Assert 4: Mother registration remains '2' (Obstetri) intact
            var motherReg = conn.QueryFirstOrDefault("SELECT fs_kd_jenis_inap FROM ta_registrasi WHERE fs_kd_reg = @regId", new { regId = testMotherRegId });
            Assert.NotNull((object?)motherReg);
            ((string)motherReg!.fs_kd_jenis_inap).Should().Be("2");

            // Assert 5: ta_bed remains occupied by mother (fn_status = 1)
            var bedRow = conn.QueryFirstOrDefault("SELECT fn_status, fs_kd_reg FROM ta_bed WHERE fs_kd_bed = @bedId", new { bedId = testBedId });
            Assert.NotNull((object?)bedRow);
            ((int)bedRow!.fn_status).Should().Be(1);
            ((string)bedRow.fs_kd_reg).Should().Be(testMotherRegId);
        }
        finally
        {
            conn.Execute(@"
                DELETE FROM BILRG_BedWaitingList WHERE WaitingListId IN ('WL_B04');
                DELETE FROM ta_trs_bed WHERE fs_kd_trs IN (@motherPakaiBedId, @babyPakaiBedId);
                DELETE FROM ta_bed WHERE fs_kd_bed = @bedId;
                DELETE FROM ta_registrasi WHERE fs_kd_reg IN (@motherRegId, @babyRegId);
            ", new
            {
                bedId = testBedId,
                motherRegId = testMotherRegId,
                babyRegId = testBabyRegId,
                motherPakaiBedId = testMotherPakaiBedId,
                babyPakaiBedId = testBabyPakaiBedId
            });
        }
    }

    [Fact]
    public async Task GetRoomingInBabyTrsIdsAsync_WhenBabyRegIdNullOrEmpty_DoesNotThrowNullReferenceException()
    {
        // Act & Assert
        var actNull = async () => await _sut.GetRoomingInBabyTrsIdsAsync("NONEXISTENT_BED", null);
        await actNull.Should().NotThrowAsync<NullReferenceException>();

        var actEmpty = async () => await _sut.GetRoomingInBabyTrsIdsAsync("NONEXISTENT_BED", "");
        await actEmpty.Should().NotThrowAsync<NullReferenceException>();
    }

    [Fact]
    public async Task ReleaseRoomingInBabyAsync_WhenBabyRegIdNullOrEmpty_DoesNotThrowNullReferenceException()
    {
        // Act & Assert
        var actNull = async () => await _sut.ReleaseRoomingInBabyAsync("NONEXISTENT_BED", null, "USR1", DateTime.Now);
        await actNull.Should().NotThrowAsync<NullReferenceException>();

        var actEmpty = async () => await _sut.ReleaseRoomingInBabyAsync("NONEXISTENT_BED", "", "USR1", DateTime.Now);
        await actEmpty.Should().NotThrowAsync<NullReferenceException>();
    }
}
