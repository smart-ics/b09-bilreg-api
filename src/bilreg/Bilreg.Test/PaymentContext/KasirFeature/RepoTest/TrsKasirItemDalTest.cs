using System.Data.SqlClient;
using Bilreg.Application.PaymentContext.KasirFeature;
using Bilreg.Infrastructure.PaymentContext.KasirFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.PaymentContext.KasirFeature.RepoTest;

public class TrsKasirItemDalTest
{
    private readonly TrsKasirItemDal _sut;
    private readonly DatabaseOptions _opt;

    public TrsKasirItemDalTest()
    {
        _opt = ConnStringHelper.GetTestEnv().Value;
        _sut = new TrsKasirItemDal(ConnStringHelper.GetTestEnv());
    }

    [Fact]
    public void Insert_SingleItem_And_ListDataByTrsKasir_ReturnsPersistedItem()
    {
        // Arrange
        var trsKasirId = "TX" + Guid.NewGuid().ToString("N")[..8];
        var billId = "BL" + Guid.NewGuid().ToString("N")[..8];
        var regId = "RG" + Guid.NewGuid().ToString("N")[..8];
        var kasirItemId = $"{trsKasirId}-01";

        var item = new TrsKasirItemDto(
            KasirItemId: kasirItemId,
            TrsKasirId: trsKasirId,
            NoUrut: 1,
            BillId: billId,
            RegId: regId,
            LayananId: "LAY01",
            Keterangan: "Pemeriksaan Dokter",
            NilaiBill: 150_000m,
            NilaiBayar: 150_000m,
            crttgl: "2026-11-11",
            crtjam: "10:00:00",
            crtusr: "KASIR01");

        try
        {
            // Act
            _sut.Insert(item);
            var result = _sut.ListDataByTrsKasir(trsKasirId).ToList();

            // Assert
            result.Should().HaveCount(1);
            result[0].KasirItemId.Should().Be(kasirItemId);
            result[0].TrsKasirId.Should().Be(trsKasirId);
            result[0].BillId.Should().Be(billId);
            result[0].RegId.Should().Be(regId);
            result[0].LayananId.Should().Be("LAY01");
            result[0].Keterangan.Should().Be("Pemeriksaan Dokter");
            result[0].NilaiBill.Should().Be(150_000m);
            result[0].NilaiBayar.Should().Be(150_000m);
            result[0].NoUrut.Should().Be(1);
        }
        finally
        {
            _sut.DeleteByTrsKasir(trsKasirId);
        }
    }

    [Fact]
    public void Insert_MultipleItems_PersistsAllWithOrdering()
    {
        // Arrange
        var trsKasirId = "TX" + Guid.NewGuid().ToString("N")[..8];
        var regId = "RG" + Guid.NewGuid().ToString("N")[..8];

        var items = new List<TrsKasirItemDto>
        {
            new($"{trsKasirId}-01", trsKasirId, 1, "BL01", regId, "L01", "Item 1", 100_000m, 100_000m),
            new($"{trsKasirId}-02", trsKasirId, 2, "BL02", regId, "L02", "Item 2", 200_000m, 200_000m)
        };

        try
        {
            // Act
            _sut.Insert(items);
            var result = _sut.ListDataByTrsKasir(trsKasirId).ToList();

            // Assert
            result.Should().HaveCount(2);
            result[0].NoUrut.Should().Be(1);
            result[0].BillId.Should().Be("BL01");
            result[1].NoUrut.Should().Be(2);
            result[1].BillId.Should().Be("BL02");
        }
        finally
        {
            _sut.DeleteByTrsKasir(trsKasirId);
        }
    }

    [Fact]
    public void ListPaidBillIdsByReg_WhenStatusBerhasil_ReturnsBillId()
    {
        // Arrange
        var trsKasirId = "TX" + Guid.NewGuid().ToString("N")[..8];
        var billId = "BL" + Guid.NewGuid().ToString("N")[..8];
        var regId = "RG" + Guid.NewGuid().ToString("N")[..8];

        var item = new TrsKasirItemDto(
            KasirItemId: $"{trsKasirId}-01",
            TrsKasirId: trsKasirId,
            NoUrut: 1,
            BillId: billId,
            RegId: regId,
            LayananId: "L01",
            Keterangan: "Layanan Paid",
            NilaiBill: 50_000m,
            NilaiBayar: 50_000m);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        // Insert dummy header in BILRG_TrsKasir with StatusTrs = 'BERHASIL'
        const string insertHdrSql = """
            INSERT INTO BILRG_TrsKasir (
                TrsKasirId, TglTrs, JamTrs, TglJamTrs, JenisTrs, ShiftId, KasirId,
                RegId, PasienId, PasienName, OrderRef, Keterangan, TotalNilai,
                StatusTrs, TglVoid, JamVoid, PetugasVoid, AlasanVoid,
                crttgl, crtjam, crtusr, updtgl, updjam, updusr
            ) VALUES (
                @TrsKasirId, '2026-11-11', '10:00:00', '2026-11-11 10:00:00', 'TERIMA', 'SH01', 'KASIR01',
                @RegId, 'P01', 'Test Pasien', '', 'Test', 50000,
                'BERHASIL', '3000-01-01', '00:00:00', '', '',
                '2026-11-11', '10:00:00', 'TEST', '3000-01-01', '00:00:00', ''
            )
            """;

        try
        {
            conn.Execute(insertHdrSql, new { TrsKasirId = trsKasirId, RegId = regId });
            _sut.Insert(item);

            // Act
            var paidBills = _sut.ListPaidBillIdsByReg(regId).ToList();

            // Assert
            paidBills.Should().Contain(billId);
        }
        finally
        {
            _sut.DeleteByTrsKasir(trsKasirId);
            conn.Execute("DELETE FROM BILRG_TrsKasir WHERE TrsKasirId = @TrsKasirId", new { TrsKasirId = trsKasirId });
        }
    }

    [Fact]
    public void ListUnpaidBillingByReg_WhenBillPaidWithBerhasil_ExcludesFromUnpaidList()
    {
        // Arrange
        var trsKasirId = "TX" + Guid.NewGuid().ToString("N")[..8];
        var billId1 = "BL" + Guid.NewGuid().ToString("N")[..8];
        var billId2 = "BL" + Guid.NewGuid().ToString("N")[..8];
        var regId = "RG" + Guid.NewGuid().ToString("N")[..8];

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));

        const string insertBillingSql = """
            INSERT INTO ta_trs_billing (
                fs_kd_trs, fn_modul, fd_tgl_trs, fs_jam_trs, fs_kd_reg, fs_kd_layanan,
                fs_kd_kelas, fs_kd_petugas, fn_sub_total, fn_diskon, fn_biaya, fn_tax,
                fn_total, fs_keterangan, fs_keterangan2, fs_kd_rekap_cetak, fs_kd_ref_biaya,
                fn_qty, fs_kd_trs_main, fd_tgl_jam_trs
            ) VALUES (
                @BillId, 1, '2026-11-11', '09:00:00', @RegId, '',
                '', '', 100000, 0, 0, 0,
                100000, @Keterangan, '', '', '',
                1, '', '2026-11-11 09:00:00'
            )
            """;

        const string insertKasirHdrSql = """
            INSERT INTO BILRG_TrsKasir (
                TrsKasirId, TglTrs, JamTrs, TglJamTrs, JenisTrs, ShiftId, KasirId,
                RegId, PasienId, PasienName, OrderRef, Keterangan, TotalNilai,
                StatusTrs, TglVoid, JamVoid, PetugasVoid, AlasanVoid,
                crttgl, crtjam, crtusr, updtgl, updjam, updusr
            ) VALUES (
                @TrsKasirId, '2026-11-11', '10:00:00', '2026-11-11 10:00:00', 'TERIMA', 'SH01', 'KASIR01',
                @RegId, 'P01', 'Test Pasien', '', 'Test', 100000,
                'BERHASIL', '3000-01-01', '00:00:00', '', '',
                '2026-11-11', '10:00:00', 'TEST', '3000-01-01', '00:00:00', ''
            )
            """;

        try
        {
            // Insert 2 billing items
            conn.Execute(insertBillingSql, new { BillId = billId1, RegId = regId, Keterangan = "Bill 1 (Paid)" });
            conn.Execute(insertBillingSql, new { BillId = billId2, RegId = regId, Keterangan = "Bill 2 (Unpaid)" });

            // Pay billId1 with Status BERHASIL
            conn.Execute(insertKasirHdrSql, new { TrsKasirId = trsKasirId, RegId = regId });
            _sut.Insert(new TrsKasirItemDto($"{trsKasirId}-01", trsKasirId, 1, billId1, regId, "", "Bill 1", 100000, 100000));

            // Act
            var unpaidList = _sut.ListUnpaidBillingByReg(regId).ToList();

            // Assert
            unpaidList.Should().ContainSingle(x => x.BillId == billId2);
            unpaidList.Should().NotContain(x => x.BillId == billId1);
        }
        finally
        {
            _sut.DeleteByTrsKasir(trsKasirId);
            conn.Execute("DELETE FROM BILRG_TrsKasir WHERE TrsKasirId = @TrsKasirId", new { TrsKasirId = trsKasirId });
            conn.Execute("DELETE FROM ta_trs_billing WHERE fs_kd_trs IN (@Bill1, @Bill2)", new { Bill1 = billId1, Bill2 = billId2 });
        }
    }

    [Fact]
    public void ListUnpaidBillingByReg_WhenTicketBillWithBillIdEqualsRegIdIsPaid_ReturnsRemainingUnpaidItems()
    {
        // Arrange
        var trsKasirId = "TX" + Guid.NewGuid().ToString("N")[..8];
        var regId = "RG" + Guid.NewGuid().ToString("N")[..8];
        var otherBillId = "BL" + Guid.NewGuid().ToString("N")[..8];

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));

        const string insertBillingSql = """
            INSERT INTO ta_trs_billing (
                fs_kd_trs, fn_modul, fd_tgl_trs, fs_jam_trs, fs_kd_reg, fs_kd_layanan,
                fs_kd_kelas, fs_kd_petugas, fn_sub_total, fn_diskon, fn_biaya, fn_tax,
                fn_total, fs_keterangan, fs_keterangan2, fs_kd_rekap_cetak, fs_kd_ref_biaya,
                fn_qty, fs_kd_trs_main, fd_tgl_jam_trs
            ) VALUES (
                @BillId, 1, '2026-11-11', '09:00:00', @RegId, '',
                '', '', @Nilai, 0, 0, 0,
                @Nilai, @Keterangan, '', '', '',
                1, '', '2026-11-11 09:00:00'
            )
            """;

        const string insertKasirHdrSql = """
            INSERT INTO BILRG_TrsKasir (
                TrsKasirId, TglTrs, JamTrs, TglJamTrs, JenisTrs, ShiftId, KasirId,
                RegId, PasienId, PasienName, OrderRef, Keterangan, TotalNilai,
                StatusTrs, TglVoid, JamVoid, PetugasVoid, AlasanVoid,
                crttgl, crtjam, crtusr, updtgl, updjam, updusr
            ) VALUES (
                @TrsKasirId, '2026-11-11', '10:00:00', '2026-11-11 10:00:00', 'TERIMA', 'SH01', 'KASIR01',
                @RegId, 'P01', 'Test Pasien', '', 'Test Partial Karcis', 1000,
                'BERHASIL', '3000-01-01', '00:00:00', '', '',
                '2026-11-11', '10:00:00', 'TEST', '3000-01-01', '00:00:00', ''
            )
            """;

        try
        {
            // Insert ticket item (fs_kd_trs == fs_kd_reg) and another unpaid item on the same registration
            conn.Execute(insertBillingSql, new { BillId = regId, RegId = regId, Nilai = 1000m, Keterangan = "Biaya Karcis/Registrasi (Paid)" });
            conn.Execute(insertBillingSql, new { BillId = otherBillId, RegId = regId, Nilai = 50000m, Keterangan = "Tindakan RJ (Unpaid)" });

            // Pay only ticket item (BillId = RegId) with Status BERHASIL
            conn.Execute(insertKasirHdrSql, new { TrsKasirId = trsKasirId, RegId = regId });
            _sut.Insert(new TrsKasirItemDto($"{trsKasirId}-01", trsKasirId, 1, regId, regId, "", "Biaya Karcis", 1000m, 1000m));

            // Act
            var unpaidList = _sut.ListUnpaidBillingByReg(regId).ToList();

            // Assert: Remaining unpaid item is still returned, while paid ticket (BillId = RegId) is excluded
            unpaidList.Should().ContainSingle(x => x.BillId == otherBillId && x.Nilai == 50000m);
            unpaidList.Should().NotContain(x => x.BillId == regId);
        }
        finally
        {
            _sut.DeleteByTrsKasir(trsKasirId);
            conn.Execute("DELETE FROM BILRG_TrsKasir WHERE TrsKasirId = @TrsKasirId", new { TrsKasirId = trsKasirId });
            conn.Execute("DELETE FROM ta_trs_billing WHERE fs_kd_trs IN (@Bill1, @Bill2)", new { Bill1 = regId, Bill2 = otherBillId });
        }
    }

    [Fact]
    public void ListUnpaidBillingByReg_WhenTotalBayarGteTotalBill_ExcludesAllItems()
    {
        // Arrange
        var trsKasirId1 = "TX" + Guid.NewGuid().ToString("N")[..8];
        var trsKasirId2 = "TX" + Guid.NewGuid().ToString("N")[..8];
        var billId1 = "BL" + Guid.NewGuid().ToString("N")[..8];
        var billId2 = "BL" + Guid.NewGuid().ToString("N")[..8];
        var regId = "RG" + Guid.NewGuid().ToString("N")[..8];

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));

        const string insertBillingSql = """
            INSERT INTO ta_trs_billing (
                fs_kd_trs, fn_modul, fd_tgl_trs, fs_jam_trs, fs_kd_reg, fs_kd_layanan,
                fs_kd_kelas, fs_kd_petugas, fn_sub_total, fn_diskon, fn_biaya, fn_tax,
                fn_total, fs_keterangan, fs_keterangan2, fs_kd_rekap_cetak, fs_kd_ref_biaya,
                fn_qty, fs_kd_trs_main, fd_tgl_jam_trs
            ) VALUES (
                @BillId, 1, '2026-11-11', '09:00:00', @RegId, '',
                '', '', @Nilai, 0, 0, 0,
                @Nilai, @Keterangan, '', '', '',
                1, '', '2026-11-11 09:00:00'
            )
            """;

        const string insertKasirHdrSql = """
            INSERT INTO BILRG_TrsKasir (
                TrsKasirId, TglTrs, JamTrs, TglJamTrs, JenisTrs, ShiftId, KasirId,
                RegId, PasienId, PasienName, OrderRef, Keterangan, TotalNilai,
                StatusTrs, TglVoid, JamVoid, PetugasVoid, AlasanVoid,
                crttgl, crtjam, crtusr, updtgl, updjam, updusr
            ) VALUES (
                @TrsKasirId, '2026-11-11', '10:00:00', '2026-11-11 10:00:00', 'TERIMA', 'SH01', 'KASIR01',
                @RegId, 'P01', 'Test Pasien', '', 'Test', @TotalNilai,
                'BERHASIL', '3000-01-01', '00:00:00', '', '',
                '2026-11-11', '10:00:00', 'TEST', '3000-01-01', '00:00:00', ''
            )
            """;

        try
        {
            // Two bills total 150,000 (100k + 50k)
            conn.Execute(insertBillingSql, new { BillId = billId1, RegId = regId, Nilai = 100000m, Keterangan = "Bill 1" });
            conn.Execute(insertBillingSql, new { BillId = billId2, RegId = regId, Nilai = 50000m, Keterangan = "Bill 2" });

            // Two payments total 150,000 (100k + 50k) with Status BERHASIL
            conn.Execute(insertKasirHdrSql, new { TrsKasirId = trsKasirId1, RegId = regId, TotalNilai = 100000m });
            conn.Execute(insertKasirHdrSql, new { TrsKasirId = trsKasirId2, RegId = regId, TotalNilai = 50000m });

            // Act
            var unpaidList = _sut.ListUnpaidBillingByReg(regId).ToList();

            // Assert: All items should be excluded because TotalBayar (150k) >= TotalBill (150k)
            unpaidList.Should().BeEmpty();
        }
        finally
        {
            conn.Execute("DELETE FROM BILRG_TrsKasir WHERE TrsKasirId IN (@Tx1, @Tx2)", new { Tx1 = trsKasirId1, Tx2 = trsKasirId2 });
            conn.Execute("DELETE FROM ta_trs_billing WHERE fs_kd_trs IN (@Bill1, @Bill2)", new { Bill1 = billId1, Bill2 = billId2 });
        }
    }

    [Fact]
    public void ListUnpaidBillingByReg_WhenKasirPaymentVoid_IncludesUnpaidItems()
    {
        // Arrange
        var trsKasirId = "TX" + Guid.NewGuid().ToString("N")[..8];
        var billId = "BL" + Guid.NewGuid().ToString("N")[..8];
        var regId = "RG" + Guid.NewGuid().ToString("N")[..8];

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));

        const string insertBillingSql = """
            INSERT INTO ta_trs_billing (
                fs_kd_trs, fn_modul, fd_tgl_trs, fs_jam_trs, fs_kd_reg, fs_kd_layanan,
                fs_kd_kelas, fs_kd_petugas, fn_sub_total, fn_diskon, fn_biaya, fn_tax,
                fn_total, fs_keterangan, fs_keterangan2, fs_kd_rekap_cetak, fs_kd_ref_biaya,
                fn_qty, fs_kd_trs_main, fd_tgl_jam_trs
            ) VALUES (
                @BillId, 1, '2026-11-11', '09:00:00', @RegId, '',
                '', '', 100000, 0, 0, 0,
                100000, 'Bill Void Test', '', '', '',
                1, '', '2026-11-11 09:00:00'
            )
            """;

        const string insertKasirHdrSql = """
            INSERT INTO BILRG_TrsKasir (
                TrsKasirId, TglTrs, JamTrs, TglJamTrs, JenisTrs, ShiftId, KasirId,
                RegId, PasienId, PasienName, OrderRef, Keterangan, TotalNilai,
                StatusTrs, TglVoid, JamVoid, PetugasVoid, AlasanVoid,
                crttgl, crtjam, crtusr, updtgl, updjam, updusr
            ) VALUES (
                @TrsKasirId, '2026-11-11', '10:00:00', '2026-11-11 10:00:00', 'TERIMA', 'SH01', 'KASIR01',
                @RegId, 'P01', 'Test Pasien', '', 'Test', 100000,
                'VOID', '2026-11-11', '11:00:00', 'SPV01', 'Salah input',
                '2026-11-11', '10:00:00', 'TEST', '3000-01-01', '00:00:00', ''
            )
            """;

        try
        {
            conn.Execute(insertBillingSql, new { BillId = billId, RegId = regId });
            conn.Execute(insertKasirHdrSql, new { TrsKasirId = trsKasirId, RegId = regId });

            // Act
            var unpaidList = _sut.ListUnpaidBillingByReg(regId).ToList();

            // Assert: VOID transaction does not exclude the bill
            unpaidList.Should().ContainSingle(x => x.BillId == billId);
        }
        finally
        {
            conn.Execute("DELETE FROM BILRG_TrsKasir WHERE TrsKasirId = @TrsKasirId", new { TrsKasirId = trsKasirId });
            conn.Execute("DELETE FROM ta_trs_billing WHERE fs_kd_trs = @BillId", new { BillId = billId });
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ListUnpaidBillingByReg_WhenRegIdEmpty_ReturnsEmpty(string? regId)
    {
        // Act
        var result = _sut.ListUnpaidBillingByReg(regId!);

        // Assert
        result.Should().BeEmpty();
    }
}
