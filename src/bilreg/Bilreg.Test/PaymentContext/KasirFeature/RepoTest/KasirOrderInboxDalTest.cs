using System.Data.SqlClient;
using Bilreg.Application.PaymentContext.KasirFeature;
using Bilreg.Infrastructure.PaymentContext.KasirFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.PaymentContext.KasirFeature.RepoTest;

public class KasirOrderInboxDalTest
{
    private readonly KasirOrderInboxDal _sut;
    private readonly DatabaseOptions _opt;

    public KasirOrderInboxDalTest()
    {
        _opt = ConnStringHelper.GetTestEnv().Value;
        _sut = new KasirOrderInboxDal(ConnStringHelper.GetTestEnv());
    }

    [Fact]
    public async Task ListOrderInbox_WhenCalledWithoutTargetDate_ExecutesWithoutError()
    {
        // Act
        var result = await _sut.ListOrderInboxAsync();

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task ListOrderInbox_WhenTargetDateProvided_ExecutesWithoutError()
    {
        // Act
        var result = await _sut.ListOrderInboxAsync("2026-11-11");

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task ListOrderInbox_WhenPendingBillingExistsOnTargetDate_IncludesInInbox()
    {
        // Arrange
        var testDate = "2026-11-11";
        var billId = "BL" + Guid.NewGuid().ToString("N")[..8];
        var regId = "RG" + Guid.NewGuid().ToString("N")[..8];

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));

        const string insertRegSql = """
            INSERT INTO ta_registrasi (fs_kd_reg, fs_mr, fs_kd_layanan, fd_tgl_masuk, fd_tgl_keluar, fd_tgl_void)
            VALUES (@RegId, '', '2RJ01', @TargetDate, '', '3000-01-01')
            """;

        const string insertRegAktifSql = """
            INSERT INTO BILRG_RegAktif (RegId, RegDate, PasienId, JenisReg, LayananId, DokterId, TipeJaminanId)
            VALUES (@RegId, @TargetDate, '', '0', '2RJ01', '', '')
            """;

        const string insertBillingSql = """
            INSERT INTO ta_trs_billing (
                fs_kd_trs, fn_modul, fd_tgl_trs, fs_jam_trs, fs_kd_reg, fs_kd_layanan,
                fs_kd_kelas, fs_kd_petugas, fn_sub_total, fn_diskon, fn_biaya, fn_tax,
                fn_total, fs_keterangan, fs_keterangan2, fs_kd_rekap_cetak, fs_kd_ref_biaya,
                fn_qty, fs_kd_trs_main, fd_tgl_jam_trs
            ) VALUES (
                @BillId, 1, @TargetDate, '09:00:00', @RegId, '2RJ01',
                '', '', 75000, 0, 0, 0,
                75000, 'Konsultasi Rawat Jalan Inbox Test', '', '', '',
                1, '', '2026-11-11 09:00:00'
            )
            """;

        try
        {
            conn.Execute(insertRegSql, new { RegId = regId, TargetDate = testDate });
            conn.Execute(insertRegAktifSql, new { RegId = regId, TargetDate = testDate });
            conn.Execute(insertBillingSql, new { BillId = billId, TargetDate = testDate, RegId = regId });

            // Act
            var inbox = (await _sut.ListOrderInboxAsync(testDate)).ToList();

            // Assert
            var item = inbox.FirstOrDefault(x => x.RegId == regId);
            item.Should().NotBeNull();
            item!.Type.Should().Be("PENERIMAAN");
            item.Subtype.Should().Be("Tagihan Layanan RJ/IGD");
            item.TotalNilai.Should().Be(75000m);
            item.Status.Should().Be("MENUNGGU_BAYAR");
            item.UnitName.Should().Be("POLI UMUM");
            item.PenjaminName.Should().Be("UMUM/MANDIRI");
        }
        finally
        {
            conn.Execute("DELETE FROM BILRG_RegAktif WHERE RegId = @RegId", new { RegId = regId });
            conn.Execute("DELETE FROM ta_trs_billing WHERE fs_kd_trs = @BillId", new { BillId = billId });
            conn.Execute("DELETE FROM ta_registrasi WHERE fs_kd_reg = @RegId", new { RegId = regId });
        }
    }

    [Fact]
    public async Task ListOrderInbox_WhenRanapPendingBillingExists_ExcludesFromSection4Inbox()
    {
        // Arrange
        var testDate = "2026-11-11";
        var billId = "BL" + Guid.NewGuid().ToString("N")[..8];
        var regId = "RG" + Guid.NewGuid().ToString("N")[..8];

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));

        // 3RI01 has fs_kd_instalasi = '02' (Rawat Inap)
        const string insertRegSql = """
            INSERT INTO ta_registrasi (fs_kd_reg, fs_mr, fs_kd_layanan, fd_tgl_masuk, fd_tgl_keluar, fd_tgl_void)
            VALUES (@RegId, '', '3RI01', @TargetDate, '', '3000-01-01')
            """;

        const string insertRegAktifSql = """
            INSERT INTO BILRG_RegAktif (RegId, RegDate, PasienId, JenisReg, LayananId, DokterId, TipeJaminanId)
            VALUES (@RegId, @TargetDate, '', '1', '3RI01', '', '')
            """;

        const string insertBillingSql = """
            INSERT INTO ta_trs_billing (
                fs_kd_trs, fn_modul, fd_tgl_trs, fs_jam_trs, fs_kd_reg, fs_kd_layanan,
                fs_kd_kelas, fs_kd_petugas, fn_sub_total, fn_diskon, fn_biaya, fn_tax,
                fn_total, fs_keterangan, fs_keterangan2, fs_kd_rekap_cetak, fs_kd_ref_biaya,
                fn_qty, fs_kd_trs_main, fd_tgl_jam_trs
            ) VALUES (
                @BillId, 1, @TargetDate, '09:00:00', @RegId, '3RI01',
                '', '', 150000, 0, 0, 0,
                150000, 'Visite Dokter Ranap Test', '', '', '',
                1, '', '2026-11-11 09:00:00'
            )
            """;

        try
        {
            conn.Execute(insertRegSql, new { RegId = regId, TargetDate = testDate });
            conn.Execute(insertRegAktifSql, new { RegId = regId, TargetDate = testDate });
            conn.Execute(insertBillingSql, new { BillId = billId, TargetDate = testDate, RegId = regId });

            // Act
            var inbox = (await _sut.ListOrderInboxAsync(testDate)).ToList();

            // Assert: Ranap billing must NOT appear in inbox Section 4
            var item = inbox.FirstOrDefault(x => x.RegId == regId);
            item.Should().BeNull();
        }
        finally
        {
            conn.Execute("DELETE FROM BILRG_RegAktif WHERE RegId = @RegId", new { RegId = regId });
            conn.Execute("DELETE FROM ta_trs_billing WHERE fs_kd_trs = @BillId", new { BillId = billId });
            conn.Execute("DELETE FROM ta_registrasi WHERE fs_kd_reg = @RegId", new { RegId = regId });
        }
    }

    [Fact]
    public async Task ListOrderInbox_WhenDischargedPatientPendingBillingExists_ExcludesFromSection4Inbox()
    {
        // Arrange
        var testDate = "2026-11-11";
        var billId = "BL" + Guid.NewGuid().ToString("N")[..8];
        var regId = "RG" + Guid.NewGuid().ToString("N")[..8];

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));

        // Discharged patient (fd_tgl_keluar is set)
        const string insertRegSql = """
            INSERT INTO ta_registrasi (fs_kd_reg, fs_mr, fs_kd_layanan, fd_tgl_keluar, fd_tgl_void)
            VALUES (@RegId, '', '2RJ01', @TargetDate, '3000-01-01')
            """;

        const string insertBillingSql = """
            INSERT INTO ta_trs_billing (
                fs_kd_trs, fn_modul, fd_tgl_trs, fs_jam_trs, fs_kd_reg, fs_kd_layanan,
                fs_kd_kelas, fs_kd_petugas, fn_sub_total, fn_diskon, fn_biaya, fn_tax,
                fn_total, fs_keterangan, fs_keterangan2, fs_kd_rekap_cetak, fs_kd_ref_biaya,
                fn_qty, fs_kd_trs_main, fd_tgl_jam_trs
            ) VALUES (
                @BillId, 1, @TargetDate, '09:00:00', @RegId, '2RJ01',
                '', '', 75000, 0, 0, 0,
                75000, 'Tindakan Discharged Test', '', '', '',
                1, '', '2026-11-11 09:00:00'
            )
            """;

        try
        {
            conn.Execute(insertRegSql, new { RegId = regId, TargetDate = testDate });
            conn.Execute(insertBillingSql, new { BillId = billId, TargetDate = testDate, RegId = regId });

            // Act
            var inbox = (await _sut.ListOrderInboxAsync(testDate)).ToList();

            // Assert: Discharged patient must NOT appear in Section 4 (PendingBills)
            var item = inbox.FirstOrDefault(x => x.RegId == regId);
            item.Should().BeNull();
        }
        finally
        {
            conn.Execute("DELETE FROM ta_trs_billing WHERE fs_kd_trs = @BillId", new { BillId = billId });
            conn.Execute("DELETE FROM ta_registrasi WHERE fs_kd_reg = @RegId", new { RegId = regId });
        }
    }

    [Fact]
    public async Task ListOrderInbox_WhenDischargedPatientWithBykasOnReg3_IncludesInSection1()
    {
        // Arrange
        var testDate = "2026-11-11";
        var regId = "RG" + Guid.NewGuid().ToString("N")[..8];

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));

        // Discharged patient with BYKAS on ta_registrasi3
        const string insertRegSql = """
            INSERT INTO ta_registrasi (fs_kd_reg, fs_mr, fs_kd_layanan, fd_tgl_keluar, fd_tgl_void)
            VALUES (@RegId, '', '2RJ01', @TargetDate, '3000-01-01')
            """;

        const string insertReg3Sql = """
            INSERT INTO ta_registrasi3 (
                fs_kd_reg, fs_kd_bayar, fn_jasa, fn_obat, fs_kd_rek,
                fs_kd_trs_kwitansi, fs_kd_trs_deposit_khusus, fd_tgl_serah
            ) VALUES (
                @RegId, 'BYKAS', 100000, 50000, '1499901',
                '', '', @TargetDate
            )
            """;

        try
        {
            conn.Execute(insertRegSql, new { RegId = regId, TargetDate = testDate });
            conn.Execute(insertReg3Sql, new { RegId = regId, TargetDate = testDate });

            // Act
            var inbox = (await _sut.ListOrderInboxAsync(testDate)).ToList();

            // Assert: Appears in Section 1 (Order Pembayaran Tagihan)
            var item = inbox.FirstOrDefault(x => x.RegId == regId);
            item.Should().NotBeNull();
            item!.Type.Should().Be("PENERIMAAN");
            item.Subtype.Should().Be("Order Pembayaran Tagihan");
            item.TotalNilai.Should().Be(150000m);
            item.Status.Should().Be("MENUNGGU_BAYAR");
            item.UnitName.Should().Be("POLI UMUM");
        }
        finally
        {
            conn.Execute("DELETE FROM ta_registrasi3 WHERE fs_kd_reg = @RegId", new { RegId = regId });
            conn.Execute("DELETE FROM ta_registrasi WHERE fs_kd_reg = @RegId", new { RegId = regId });
        }
    }

    [Fact]
    public async Task ListOrderInbox_WhenRanapDepositExistsOnTargetDate_IncludesInSection2()
    {
        // Arrange
        var testDate = "2026-11-11";
        var depId = "DP" + Guid.NewGuid().ToString("N")[..8];
        var regId = "RG" + Guid.NewGuid().ToString("N")[..8];

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));

        const string insertRegSql = """
            INSERT INTO ta_registrasi (fs_kd_reg, fs_mr, fs_kd_layanan, fd_tgl_masuk, fd_tgl_keluar, fd_tgl_void)
            VALUES (@RegId, '', '3RI01', @TargetDate, '', '3000-01-01')
            """;

        const string insertRegAktifSql = """
            INSERT INTO BILRG_RegAktif (RegId, RegDate, PasienId, JenisReg, LayananId, DokterId, TipeJaminanId)
            VALUES (@RegId, @TargetDate, '', '1', '3RI01', '', '')
            """;

        const string insertDepositSql = """
            INSERT INTO ta_trs_deposit (
                fs_kd_trs, fd_tgl_trs, fs_jam_trs, fs_kd_reg, fs_kd_layanan,
                fs_kd_petugas, fn_nilai_deposit, fn_total_bayar,
                fs_kd_petugas_kasir, fs_kd_trs_deposit_khusus, fd_tgl_void,
                crttgl, crtjam, crtusr
            ) VALUES (
                @DepId, @TargetDate, '08:30:00', @RegId, '3RI01',
                '', 500000, 0,
                '', '', '3000-01-01',
                @TargetDate, '08:30:00', 'TEST'
            )
            """;

        try
        {
            conn.Execute(insertRegSql, new { RegId = regId, TargetDate = testDate });
            conn.Execute(insertRegAktifSql, new { RegId = regId, TargetDate = testDate });
            conn.Execute(insertDepositSql, new { DepId = depId, RegId = regId, TargetDate = testDate });

            // Act
            var inbox = (await _sut.ListOrderInboxAsync(testDate)).ToList();

            // Assert: Appears in Section 2 (Order Pembayaran Deposit)
            var item = inbox.FirstOrDefault(x => x.OrderId == depId);
            item.Should().NotBeNull();
            item!.Type.Should().Be("PENERIMAAN");
            item.Subtype.Should().Be("Order Pembayaran Deposit");
            item.TotalNilai.Should().Be(500000m);
            item.Status.Should().Be("MENUNGGU_BAYAR");
            item.RegId.Should().Be(regId);
        }
        finally
        {
            conn.Execute("DELETE FROM ta_trs_deposit WHERE fs_kd_trs = @DepId", new { DepId = depId });
            conn.Execute("DELETE FROM BILRG_RegAktif WHERE RegId = @RegId", new { RegId = regId });
            conn.Execute("DELETE FROM ta_registrasi WHERE fs_kd_reg = @RegId", new { RegId = regId });
        }
    }

    [Fact]
    public async Task ListOrderInbox_WhenRefundDepositExistsOnTargetDate_IncludesInSection3()
    {
        // Arrange
        var testDate = "2026-11-11";
        var depId = "DP" + Guid.NewGuid().ToString("N")[..8];
        var regId = "RG" + Guid.NewGuid().ToString("N")[..8];

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));

        const string insertRegSql = """
            INSERT INTO ta_registrasi (fs_kd_reg, fs_mr, fs_kd_layanan, fd_tgl_masuk, fd_tgl_keluar, fd_tgl_void)
            VALUES (@RegId, '', '3RI01', @TargetDate, '', '3000-01-01')
            """;

        const string insertDepositSql = """
            INSERT INTO ta_trs_deposit (
                fs_kd_trs, fd_tgl_trs, fs_jam_trs, fs_kd_reg, fs_kd_layanan,
                fs_kd_petugas, fn_nilai_deposit, fn_total_bayar,
                fs_kd_petugas_kasir, fs_kd_trs_deposit_khusus, fd_tgl_void,
                crttgl, crtjam, crtusr
            ) VALUES (
                @DepId, @TargetDate, '14:00:00', @RegId, '3RI01',
                '', -200000, 0,
                '', '', '3000-01-01',
                @TargetDate, '14:00:00', 'TEST'
            )
            """;

        try
        {
            conn.Execute(insertRegSql, new { RegId = regId, TargetDate = testDate });
            conn.Execute(insertDepositSql, new { DepId = depId, RegId = regId, TargetDate = testDate });

            // Act
            var inbox = (await _sut.ListOrderInboxAsync(testDate)).ToList();

            // Assert: Appears in Section 3 (Pengembalian Deposit / Refund)
            var item = inbox.FirstOrDefault(x => x.OrderId == depId);
            item.Should().NotBeNull();
            item!.Type.Should().Be("PENGELUARAN");
            item.Subtype.Should().Be("Pengembalian Deposit / Refund");
            item.TotalNilai.Should().Be(200000m);
            item.Status.Should().Be("MENUNGGU_PENGELUARAN");
            item.RegId.Should().Be(regId);
        }
        finally
        {
            conn.Execute("DELETE FROM ta_trs_deposit WHERE fs_kd_trs = @DepId", new { DepId = depId });
            conn.Execute("DELETE FROM ta_registrasi WHERE fs_kd_reg = @RegId", new { RegId = regId });
        }
    }

    [Fact]
    public async Task ListOrderInbox_WhenActiveRjHasPartialPaymentAndBykasOnReg3_RetainsRemainingUnpaidTotalInSection4()
    {
        // Arrange
        var testDate = "2026-11-11";
        var regId = "RG" + Guid.NewGuid().ToString("N")[..8];
        var billId1 = "BL" + Guid.NewGuid().ToString("N")[..8];
        var billId2 = "BL" + Guid.NewGuid().ToString("N")[..8];
        var trsKasirId = "TX" + Guid.NewGuid().ToString("N")[..8];

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));

        try
        {
            InsertActiveRjRegistration(conn, regId, testDate);
            InsertBillingItem(conn, billId1, regId, testDate, 75000m, "Konsultasi RJ (Paid)");
            InsertBillingItem(conn, billId2, regId, testDate, 50000m, "Tindakan RJ (Unpaid)");
            InsertBykasReg3(conn, regId, testDate, trsKasirId);
            InsertPaidKasirItem(conn, trsKasirId, regId, billId1, testDate, 75000m);

            // Act
            var inbox = (await _sut.ListOrderInboxAsync(testDate)).ToList();

            // Assert: Order remains in Section 4 with unpaid remainder, and not duplicated in Section 1
            var items = inbox.Where(x => x.RegId == regId).ToList();
            items.Should().ContainSingle();
            items[0].Subtype.Should().Be("Tagihan Layanan RJ/IGD");
            items[0].TotalNilai.Should().Be(50000m);
            items[0].Status.Should().Be("MENUNGGU_BAYAR");
        }
        finally
        {
            CleanupPartialPaymentTestData(conn, regId, trsKasirId, [billId1, billId2]);
        }
    }

    [Fact]
    public async Task ListOrderInbox_WhenTicketBillWithBillIdEqualsRegIdIsPaid_RetainsRemainingUnpaidItemsInSection4()
    {
        // Arrange
        var testDate = "2026-11-11";
        var regId = "RG" + Guid.NewGuid().ToString("N")[..8];
        var otherBillId = "BL" + Guid.NewGuid().ToString("N")[..8];
        var trsKasirId = "TX" + Guid.NewGuid().ToString("N")[..8];

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));

        try
        {
            InsertActiveRjRegistration(conn, regId, testDate);
            InsertBillingItem(conn, regId, regId, testDate, 1000m, "Biaya Karcis/Registrasi (Paid)");
            InsertBillingItem(conn, otherBillId, regId, testDate, 50000m, "Tindakan RJ (Unpaid)");
            InsertPaidKasirItem(conn, trsKasirId, regId, regId, testDate, 1000m);

            // Act
            var inbox = (await _sut.ListOrderInboxAsync(testDate)).ToList();

            // Assert: Order remains in Section 4 with remaining unpaid total (excluding paid ticket BillId = RegId)
            var items = inbox.Where(x => x.RegId == regId).ToList();
            items.Should().ContainSingle();
            items[0].Subtype.Should().Be("Tagihan Layanan RJ/IGD");
            items[0].TotalNilai.Should().Be(50000m);
            items[0].Status.Should().Be("MENUNGGU_BAYAR");
        }
        finally
        {
            CleanupPartialPaymentTestData(conn, regId, trsKasirId, [regId, otherBillId]);
        }
    }

    [Fact]
    public async Task ListOrderInbox_WhenActiveRjPaysAllBillingItems_ExcludesFromSection4()
    {
        // Arrange
        var testDate = "2026-11-11";
        var regId = "RG" + Guid.NewGuid().ToString("N")[..8];
        var billId1 = "BL" + Guid.NewGuid().ToString("N")[..8];
        var billId2 = "BL" + Guid.NewGuid().ToString("N")[..8];
        var trsKasirId = "TX" + Guid.NewGuid().ToString("N")[..8];

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));

        try
        {
            InsertActiveRjRegistration(conn, regId, testDate);
            InsertBillingItem(conn, billId1, regId, testDate, 75000m, "Konsultasi RJ (Paid)");
            InsertBillingItem(conn, billId2, regId, testDate, 50000m, "Tindakan RJ (Paid)");
            InsertBykasReg3(conn, regId, testDate, trsKasirId);
            InsertPaidKasirHeader(conn, trsKasirId, regId, testDate, 125000m);
            InsertPaidKasirItemRow(conn, $"{trsKasirId}-01", trsKasirId, 1, billId1, regId, testDate, 75000m);
            InsertPaidKasirItemRow(conn, $"{trsKasirId}-02", trsKasirId, 2, billId2, regId, testDate, 50000m);

            // Act
            var inbox = (await _sut.ListOrderInboxAsync(testDate)).ToList();

            // Assert: Fully paid registration no longer appears in inbox
            inbox.Where(x => x.RegId == regId).Should().BeEmpty();
        }
        finally
        {
            CleanupPartialPaymentTestData(conn, regId, trsKasirId, [billId1, billId2]);
        }
    }

    [Fact]
    public async Task ListOrderInbox_WhenRegStillInRegAktifWithBykasOnReg3_ExcludesFromSection1()
    {
        // Arrange
        var testDate = "2026-11-11";
        var regId = "RG" + Guid.NewGuid().ToString("N")[..8];

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));

        try
        {
            conn.Execute("""
                INSERT INTO ta_registrasi (fs_kd_reg, fs_mr, fs_kd_layanan, fd_tgl_keluar, fd_tgl_void)
                VALUES (@RegId, '', '2RJ01', @TargetDate, '3000-01-01')
                """, new { RegId = regId, TargetDate = testDate });

            conn.Execute("""
                INSERT INTO BILRG_RegAktif (RegId, RegDate, PasienId, JenisReg, LayananId, DokterId, TipeJaminanId)
                VALUES (@RegId, @TargetDate, '', '0', '2RJ01', '', '')
                """, new { RegId = regId, TargetDate = testDate });

            InsertBykasReg3(conn, regId, testDate, trsDepositKhusus: string.Empty);

            // Act
            var inbox = (await _sut.ListOrderInboxAsync(testDate)).ToList();

            // Assert: Must NOT appear in Section 1 while still present in BILRG_RegAktif
            inbox.FirstOrDefault(x => x.RegId == regId && x.Subtype == "Order Pembayaran Tagihan")
                .Should().BeNull();
        }
        finally
        {
            conn.Execute("DELETE FROM ta_registrasi3 WHERE fs_kd_reg = @RegId", new { RegId = regId });
            conn.Execute("DELETE FROM BILRG_RegAktif WHERE RegId = @RegId", new { RegId = regId });
            conn.Execute("DELETE FROM ta_registrasi WHERE fs_kd_reg = @RegId", new { RegId = regId });
        }
    }

    private static void InsertActiveRjRegistration(SqlConnection conn, string regId, string testDate)
    {
        conn.Execute("""
            INSERT INTO ta_registrasi (fs_kd_reg, fs_mr, fs_kd_layanan, fd_tgl_masuk, fd_tgl_keluar, fd_tgl_void)
            VALUES (@RegId, '', '2RJ01', @TargetDate, '', '3000-01-01')
            """, new { RegId = regId, TargetDate = testDate });

        conn.Execute("""
            INSERT INTO BILRG_RegAktif (RegId, RegDate, PasienId, JenisReg, LayananId, DokterId, TipeJaminanId)
            VALUES (@RegId, @TargetDate, '', '0', '2RJ01', '', '')
            """, new { RegId = regId, TargetDate = testDate });
    }

    private static void InsertBillingItem(
        SqlConnection conn, string billId, string regId, string testDate, decimal total, string keterangan)
    {
        conn.Execute("""
            INSERT INTO ta_trs_billing (
                fs_kd_trs, fn_modul, fd_tgl_trs, fs_jam_trs, fs_kd_reg, fs_kd_layanan,
                fs_kd_kelas, fs_kd_petugas, fn_sub_total, fn_diskon, fn_biaya, fn_tax,
                fn_total, fs_keterangan, fs_keterangan2, fs_kd_rekap_cetak, fs_kd_ref_biaya,
                fn_qty, fs_kd_trs_main, fd_tgl_jam_trs
            ) VALUES (
                @BillId, 1, @TargetDate, '09:00:00', @RegId, '2RJ01',
                '', '', @Total, 0, 0, 0,
                @Total, @Keterangan, '', '', '',
                1, '', '2026-11-11 09:00:00'
            )
            """, new { BillId = billId, RegId = regId, TargetDate = testDate, Total = total, Keterangan = keterangan });
    }

    private static void InsertBykasReg3(SqlConnection conn, string regId, string testDate, string trsDepositKhusus)
    {
        conn.Execute("""
            INSERT INTO ta_registrasi3 (
                fs_kd_reg, fs_kd_bayar, fn_jasa, fn_obat, fs_kd_rek,
                fs_kd_trs_kwitansi, fs_kd_trs_deposit_khusus, fd_tgl_serah
            ) VALUES (
                @RegId, 'BYKAS', 100000, 50000, '1499901',
                '', @TrsDepositKhusus, @TargetDate
            )
            """, new { RegId = regId, TrsDepositKhusus = trsDepositKhusus, TargetDate = testDate });
    }

    private static void InsertPaidKasirItem(
        SqlConnection conn, string trsKasirId, string regId, string billId, string testDate, decimal totalNilai)
    {
        InsertPaidKasirHeader(conn, trsKasirId, regId, testDate, totalNilai);
        InsertPaidKasirItemRow(conn, $"{trsKasirId}-01", trsKasirId, 1, billId, regId, testDate, totalNilai);
    }

    private static void InsertPaidKasirHeader(
        SqlConnection conn, string trsKasirId, string regId, string testDate, decimal totalNilai)
    {
        conn.Execute("""
            INSERT INTO BILRG_TrsKasir (
                TrsKasirId, TglTrs, JamTrs, TglJamTrs, JenisTrs, ShiftId, KasirId,
                RegId, PasienId, PasienName, OrderRef, Keterangan, TotalNilai,
                StatusTrs, TglVoid, JamVoid, PetugasVoid, AlasanVoid,
                crttgl, crtjam, crtusr, updtgl, updjam, updusr
            ) VALUES (
                @TrsKasirId, @TargetDate, '10:00:00', @TglJamTrs, 'TERIMA', 'SH01', 'KASIR01',
                @RegId, '', 'Test Pasien', @RegId, 'Test Partial', @TotalNilai,
                'BERHASIL', '3000-01-01', '00:00:00', '', '',
                @TargetDate, '10:00:00', 'TEST', '3000-01-01', '00:00:00', ''
            )
            """, new { TrsKasirId = trsKasirId, TargetDate = testDate, TglJamTrs = $"{testDate} 10:00:00", RegId = regId, TotalNilai = totalNilai });
    }

    private static void InsertPaidKasirItemRow(
        SqlConnection conn, string kasirItemId, string trsKasirId, int noUrut, string billId, string regId, string testDate, decimal nilai)
    {
        conn.Execute("""
            INSERT INTO BILRG_TrsKasirItem (
                KasirItemId, TrsKasirId, NoUrut, BillId, RegId, LayananId,
                Keterangan, NilaiBill, NilaiBayar, crttgl, crtjam, crtusr
            ) VALUES (
                @KasirItemId, @TrsKasirId, @NoUrut, @BillId, @RegId, '2RJ01',
                'Item Paid', @Nilai, @Nilai, @TargetDate, '10:00:00', 'TEST'
            )
            """, new { KasirItemId = kasirItemId, TrsKasirId = trsKasirId, NoUrut = noUrut, BillId = billId, RegId = regId, Nilai = nilai, TargetDate = testDate });
    }

    private static void CleanupPartialPaymentTestData(SqlConnection conn, string regId, string trsKasirId, string[] billIds)
    {
        conn.Execute("DELETE FROM BILRG_TrsKasirItem WHERE TrsKasirId = @TrsKasirId", new { TrsKasirId = trsKasirId });
        conn.Execute("DELETE FROM BILRG_TrsKasir WHERE TrsKasirId = @TrsKasirId", new { TrsKasirId = trsKasirId });
        conn.Execute("DELETE FROM ta_registrasi3 WHERE fs_kd_reg = @RegId", new { RegId = regId });
        conn.Execute("DELETE FROM BILRG_RegAktif WHERE RegId = @RegId", new { RegId = regId });
        conn.Execute("DELETE FROM ta_trs_billing WHERE fs_kd_trs IN @BillIds", new { BillIds = billIds });
        conn.Execute("DELETE FROM ta_registrasi WHERE fs_kd_reg = @RegId", new { RegId = regId });
    }
}
