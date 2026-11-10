using System.Data.SqlClient;
using Bilreg.Application.ChargeContext.RoomChargeFeature;
using Bilreg.Infrastructure.ChargeContext.RoomChargeFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Test.ChargeContext.RoomChargeFeature;

public class BedOccupancyQueryTest
{
    private readonly BedOccupancyQuery _sut = new(ConnStringHelper.GetTestEnv());

    private static void EnsureKamarTipeGabung(SqlConnection conn)
    {
        conn.Execute(@"
            IF NOT EXISTS (SELECT 1 FROM ta_kamar_tipe WHERE fs_kd_kamar_tipe = '9')
                INSERT INTO ta_kamar_tipe (fs_kd_kamar_tipe, fs_nm_kamar_tipe, fb_gabung, fb_aktif, fb_default_tipe)
                VALUES ('9', 'Tipe Gabung', 1, 1, 0);
            ELSE
                UPDATE ta_kamar_tipe SET fb_gabung = 1, fb_aktif = 1 WHERE fs_kd_kamar_tipe = '9';
        ");
    }

    [Fact]
    public void Scenario1_BayiMandiri_GivenTarifAktifAndNoMotherOnSameBed_ThenIsRoomingInFalse()
    {
        // Skenario 1 (Bayi Mandiri):
        // Pasien bayi (fs_kd_jenis_inap = '3', fn_tarif = 150000) menempati bed sendiri
        // tanpa transaksi ibu di bed tersebut -> IsRoomingIn = false, MotherPakaiBedId = "".
        using var trans = TransHelper.NewScope();
        using var conn = new SqlConnection(ConnStringHelper.Get(ConnStringHelper.GetTestEnv().Value));

        EnsureKamarTipeGabung(conn);

        var num = Random.Shared.Next(10000, 99999);
        var bangsalId = "TL001";
        var kamarTipeId = "9"; // Tipe kamar dengan fb_gabung = 1
        var bedBayi = $"B{num}";
        var regIbu = $"RI{num}";
        var regBayi = $"RB{num}";
        var trsBayi = $"TRB{num}";

        conn.Execute(@"
            INSERT INTO ta_bed (fs_kd_bed, fs_nm_bed, fs_kd_kamar, fn_status, fs_kd_reg, fb_aktif)
            VALUES (@bedBayi, 'Bed Bayi Mandiri', 'K01', 1, @regBayi, 1);

            INSERT INTO ta_registrasi (fs_kd_reg, fs_reg_ibu, fs_kd_jenis_inap, fs_mr, fs_kd_kelas)
            VALUES (@regBayi, @regIbu, '3', 'MR01', 'KL3');

            INSERT INTO ta_trs_bed (
                fs_kd_trs, fd_tgl_in, fs_jam_in, fs_kd_petugas, fd_tgl_out, fs_jam_out,
                fd_tgl_void, fs_jam_void, fs_kd_reg, fs_kd_layanan, fs_kd_kamar_tipe,
                fs_kd_bed, fn_tarif, fd_tgl_entry, fs_jam_entry, fs_kd_kelas
            ) VALUES (
                @trsBayi, '2026-11-10', '08:00:00', 'USR1', '3000-01-01', '00:00:00',
                '3000-01-01', '00:00:00', @regBayi, @bangsalId, @kamarTipeId,
                @bedBayi, 150000, '2026-11-10', '08:00:00', 'KL3'
            );
        ", new { bedBayi, regBayi, regIbu, trsBayi, bangsalId, kamarTipeId });

        // Act & Assert 1: GetOccupant
        var occupant = _sut.GetOccupant(trsBayi);
        occupant.Should().NotBeNull();
        occupant!.PakaiBedId.Should().Be(trsBayi);
        occupant.RegId.Should().Be(regBayi);
        occupant.IsRoomingIn.Should().BeFalse();
        occupant.MotherPakaiBedId.Should().BeEmpty();
        occupant.BaseTariff.Should().Be(150000m);

        // Act & Assert 2: GetActiveOccupantByReg
        var occupantByReg = _sut.GetActiveOccupantByReg(regBayi);
        occupantByReg.Should().NotBeNull();
        occupantByReg!.IsRoomingIn.Should().BeFalse();
        occupantByReg.MotherPakaiBedId.Should().BeEmpty();
        occupantByReg.BaseTariff.Should().Be(150000m);

        // Act & Assert 3: GetActiveOccupants by Bangsal
        var list = _sut.GetActiveOccupants(bangsalId, new DateTime(2026, 11, 10));
        var item = list.FirstOrDefault(x => x.PakaiBedId == trsBayi);
        item.Should().NotBeNull();
        item!.IsRoomingIn.Should().BeFalse();
        item.MotherPakaiBedId.Should().BeEmpty();
        item.BaseTariff.Should().Be(150000m);
    }

    [Fact]
    public void Scenario2_BayiRawatGabung_GivenTarifNolAndMotherOnSameBed_ThenIsRoomingInTrue()
    {
        // Skenario 2 (Bayi Rawat Gabung):
        // Pasien bayi (fn_tarif = 0) menempati bed yang sama dengan ibu (mother.fn_tarif = 150000, mother.fs_kd_trs = 'PB-IBU')
        // -> IsRoomingIn = true, MotherPakaiBedId = 'PB-IBU'.
        using var trans = TransHelper.NewScope();
        using var conn = new SqlConnection(ConnStringHelper.Get(ConnStringHelper.GetTestEnv().Value));

        EnsureKamarTipeGabung(conn);

        var num = Random.Shared.Next(10000, 99999);
        var bangsalId = "TL001";
        var kamarTipeId = "9";
        var sharedBed = $"B{num}";
        var regIbu = $"RI{num}";
        var regBayi = $"RB{num}";
        var trsIbu = $"TRI{num}";
        var trsBayi = $"TRB{num}";

        conn.Execute(@"
            INSERT INTO ta_bed (fs_kd_bed, fs_nm_bed, fs_kd_kamar, fn_status, fs_kd_reg, fb_aktif)
            VALUES (@sharedBed, 'Bed Bersama Ibu-Bayi', 'K01', 1, @regIbu, 1);

            -- Registrasi Ibu (Maternal, fs_kd_jenis_inap = '2')
            INSERT INTO ta_registrasi (fs_kd_reg, fs_reg_ibu, fs_kd_jenis_inap, fs_mr, fs_kd_kelas)
            VALUES (@regIbu, '', '2', 'MR_IBU', 'KL3');

            -- Registrasi Bayi (Perinatal, fs_kd_jenis_inap = '3', linked to regIbu)
            INSERT INTO ta_registrasi (fs_kd_reg, fs_reg_ibu, fs_kd_jenis_inap, fs_mr, fs_kd_kelas)
            VALUES (@regBayi, @regIbu, '3', 'MR_BAYI', 'KL3');

            -- Transaksi Bed Ibu (Tarif penuh 150000)
            INSERT INTO ta_trs_bed (
                fs_kd_trs, fd_tgl_in, fs_jam_in, fs_kd_petugas, fd_tgl_out, fs_jam_out,
                fd_tgl_void, fs_jam_void, fs_kd_reg, fs_kd_layanan, fs_kd_kamar_tipe,
                fs_kd_bed, fn_tarif, fd_tgl_entry, fs_jam_entry, fs_kd_kelas
            ) VALUES (
                @trsIbu, '2026-11-10', '08:00:00', 'USR1', '3000-01-01', '00:00:00',
                '3000-01-01', '00:00:00', @regIbu, @bangsalId, @kamarTipeId,
                @sharedBed, 150000, '2026-11-10', '08:00:00', 'KL3'
            );

            -- Transaksi Bed Bayi Rawat Gabung (fn_tarif = 0 pada sharedBed yang sama)
            INSERT INTO ta_trs_bed (
                fs_kd_trs, fd_tgl_in, fs_jam_in, fs_kd_petugas, fd_tgl_out, fs_jam_out,
                fd_tgl_void, fs_jam_void, fs_kd_reg, fs_kd_layanan, fs_kd_kamar_tipe,
                fs_kd_bed, fn_tarif, fd_tgl_entry, fs_jam_entry, fs_kd_kelas
            ) VALUES (
                @trsBayi, '2026-11-10', '09:00:00', 'USR1', '3000-01-01', '00:00:00',
                '3000-01-01', '00:00:00', @regBayi, @bangsalId, @kamarTipeId,
                @sharedBed, 0, '2026-11-10', '09:00:00', 'KL3'
            );
        ", new { sharedBed, regIbu, regBayi, trsIbu, trsBayi, bangsalId, kamarTipeId });

        // Act & Assert 1: GetOccupant
        var occupant = _sut.GetOccupant(trsBayi);
        occupant.Should().NotBeNull();
        occupant!.PakaiBedId.Should().Be(trsBayi);
        occupant.RegId.Should().Be(regBayi);
        occupant.IsRoomingIn.Should().BeTrue();
        occupant.MotherPakaiBedId.Should().Be(trsIbu);
        occupant.BaseTariff.Should().Be(0m);

        // Act & Assert 2: GetActiveOccupantByReg
        var occupantByReg = _sut.GetActiveOccupantByReg(regBayi);
        occupantByReg.Should().NotBeNull();
        occupantByReg!.IsRoomingIn.Should().BeTrue();
        occupantByReg.MotherPakaiBedId.Should().Be(trsIbu);
        occupantByReg.BaseTariff.Should().Be(0m);

        // Act & Assert 3: GetActiveOccupants by Bangsal
        var list = _sut.GetActiveOccupants(bangsalId, new DateTime(2026, 11, 10));
        var item = list.FirstOrDefault(x => x.PakaiBedId == trsBayi);
        item.Should().NotBeNull();
        item!.IsRoomingIn.Should().BeTrue();
        item.MotherPakaiBedId.Should().Be(trsIbu);
        item.BaseTariff.Should().Be(0m);
    }

    [Fact]
    public void Scenario3_IbuMelahirkan_GivenTarifAktif_ThenIsRoomingInFalse()
    {
        // Skenario 3 (Ibu):
        // Pasien ibu melahirkan (fs_kd_jenis_inap = '2', fn_tarif = 150000) -> IsRoomingIn = false.
        using var trans = TransHelper.NewScope();
        using var conn = new SqlConnection(ConnStringHelper.Get(ConnStringHelper.GetTestEnv().Value));

        EnsureKamarTipeGabung(conn);

        var num = Random.Shared.Next(10000, 99999);
        var bangsalId = "TL001";
        var kamarTipeId = "9";
        var bedIbu = $"B{num}";
        var regIbu = $"RI{num}";
        var trsIbu = $"TRI{num}";

        conn.Execute(@"
            INSERT INTO ta_bed (fs_kd_bed, fs_nm_bed, fs_kd_kamar, fn_status, fs_kd_reg, fb_aktif)
            VALUES (@bedIbu, 'Bed Ibu', 'K01', 1, @regIbu, 1);

            INSERT INTO ta_registrasi (fs_kd_reg, fs_reg_ibu, fs_kd_jenis_inap, fs_mr, fs_kd_kelas)
            VALUES (@regIbu, '', '2', 'MR_IBU', 'KL3');

            INSERT INTO ta_trs_bed (
                fs_kd_trs, fd_tgl_in, fs_jam_in, fs_kd_petugas, fd_tgl_out, fs_jam_out,
                fd_tgl_void, fs_jam_void, fs_kd_reg, fs_kd_layanan, fs_kd_kamar_tipe,
                fs_kd_bed, fn_tarif, fd_tgl_entry, fs_jam_entry, fs_kd_kelas
            ) VALUES (
                @trsIbu, '2026-11-10', '08:00:00', 'USR1', '3000-01-01', '00:00:00',
                '3000-01-01', '00:00:00', @regIbu, @bangsalId, @kamarTipeId,
                @bedIbu, 150000, '2026-11-10', '08:00:00', 'KL3'
            );
        ", new { bedIbu, regIbu, trsIbu, bangsalId, kamarTipeId });

        // Act & Assert 1: GetOccupant
        var occupant = _sut.GetOccupant(trsIbu);
        occupant.Should().NotBeNull();
        occupant!.PakaiBedId.Should().Be(trsIbu);
        occupant.RegId.Should().Be(regIbu);
        occupant.IsRoomingIn.Should().BeFalse();
        occupant.MotherPakaiBedId.Should().BeEmpty();
        occupant.BaseTariff.Should().Be(150000m);

        // Act & Assert 2: GetActiveOccupantByReg
        var occupantByReg = _sut.GetActiveOccupantByReg(regIbu);
        occupantByReg.Should().NotBeNull();
        occupantByReg!.IsRoomingIn.Should().BeFalse();
        occupantByReg.MotherPakaiBedId.Should().BeEmpty();
        occupantByReg.BaseTariff.Should().Be(150000m);

        // Act & Assert 3: GetActiveOccupants by Bangsal
        var list = _sut.GetActiveOccupants(bangsalId, new DateTime(2026, 11, 10));
        var item = list.FirstOrDefault(x => x.PakaiBedId == trsIbu);
        item.Should().NotBeNull();
        item!.IsRoomingIn.Should().BeFalse();
        item.MotherPakaiBedId.Should().BeEmpty();
        item.BaseTariff.Should().Be(150000m);
    }

    [Fact]
    public void Scenario4_BayiMandiriDenganIbuDiBedBerbeda_GivenSeparateBedsAndTarifAktif_ThenIsRoomingInFalse()
    {
        // Edge Case:
        // Ibu dan bayi sama-sama dirawat di bangsal yang sama (fb_gabung = 1),
        // regBayi mereferensikan regIbu di fs_reg_ibu,
        // namun ibu berada di bed A dan bayi berada di bed B mandiri dengan fn_tarif = 150000.
        // Verifikasi bahwa Physical Bed Pairing Rule memastikan bayi TIDAK divonis rawat gabung.
        using var trans = TransHelper.NewScope();
        using var conn = new SqlConnection(ConnStringHelper.Get(ConnStringHelper.GetTestEnv().Value));

        EnsureKamarTipeGabung(conn);

        var num1 = Random.Shared.Next(10000, 49999);
        var num2 = Random.Shared.Next(50000, 99999);
        var bangsalId = "TL001";
        var kamarTipeId = "9";
        var bedIbu = $"B{num1}";
        var bedBayi = $"B{num2}";
        var regIbu = $"RI{num1}";
        var regBayi = $"RB{num2}";
        var trsIbu = $"TRI{num1}";
        var trsBayi = $"TRB{num2}";

        conn.Execute(@"
            INSERT INTO ta_bed (fs_kd_bed, fs_nm_bed, fs_kd_kamar, fn_status, fs_kd_reg, fb_aktif)
            VALUES (@bedIbu, 'Bed Ibu', 'K01', 1, @regIbu, 1);

            INSERT INTO ta_bed (fs_kd_bed, fs_nm_bed, fs_kd_kamar, fn_status, fs_kd_reg, fb_aktif)
            VALUES (@bedBayi, 'Bed Bayi Terpisah', 'K01', 1, @regBayi, 1);

            INSERT INTO ta_registrasi (fs_kd_reg, fs_reg_ibu, fs_kd_jenis_inap, fs_mr, fs_kd_kelas)
            VALUES (@regIbu, '', '2', 'MR_IBU', 'KL3');

            INSERT INTO ta_registrasi (fs_kd_reg, fs_reg_ibu, fs_kd_jenis_inap, fs_mr, fs_kd_kelas)
            VALUES (@regBayi, @regIbu, '3', 'MR_BAYI', 'KL3');

            -- Transaksi Ibu di bedIbu
            INSERT INTO ta_trs_bed (
                fs_kd_trs, fd_tgl_in, fs_jam_in, fs_kd_petugas, fd_tgl_out, fs_jam_out,
                fd_tgl_void, fs_jam_void, fs_kd_reg, fs_kd_layanan, fs_kd_kamar_tipe,
                fs_kd_bed, fn_tarif, fd_tgl_entry, fs_jam_entry, fs_kd_kelas
            ) VALUES (
                @trsIbu, '2026-11-10', '08:00:00', 'USR1', '3000-01-01', '00:00:00',
                '3000-01-01', '00:00:00', @regIbu, @bangsalId, @kamarTipeId,
                @bedIbu, 150000, '2026-11-10', '08:00:00', 'KL3'
            );

            -- Transaksi Bayi di bedBayi (tempat tidur berbeda dengan ibu)
            INSERT INTO ta_trs_bed (
                fs_kd_trs, fd_tgl_in, fs_jam_in, fs_kd_petugas, fd_tgl_out, fs_jam_out,
                fd_tgl_void, fs_jam_void, fs_kd_reg, fs_kd_layanan, fs_kd_kamar_tipe,
                fs_kd_bed, fn_tarif, fd_tgl_entry, fs_jam_entry, fs_kd_kelas
            ) VALUES (
                @trsBayi, '2026-11-10', '08:30:00', 'USR1', '3000-01-01', '00:00:00',
                '3000-01-01', '00:00:00', @regBayi, @bangsalId, @kamarTipeId,
                @bedBayi, 150000, '2026-11-10', '08:30:00', 'KL3'
            );
        ", new { bedIbu, bedBayi, regIbu, regBayi, trsIbu, trsBayi, bangsalId, kamarTipeId });

        var occupant = _sut.GetOccupant(trsBayi);
        occupant.Should().NotBeNull();
        occupant!.IsRoomingIn.Should().BeFalse("karena bayi berada di tempat tidur berbeda dari ibu");
        occupant.MotherPakaiBedId.Should().BeEmpty();
        occupant.BaseTariff.Should().Be(150000m);
    }
}
