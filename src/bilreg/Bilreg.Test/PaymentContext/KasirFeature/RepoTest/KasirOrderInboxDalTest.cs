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
    public void ListOrderInbox_WhenCalledWithoutTargetDate_ExecutesWithoutError()
    {
        // Act
        var result = _sut.ListOrderInbox();

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void ListOrderInbox_WhenTargetDateProvided_ExecutesWithoutError()
    {
        // Act
        var result = _sut.ListOrderInbox("2026-11-11");

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void ListOrderInbox_WhenPendingBillingExistsOnTargetDate_IncludesInInbox()
    {
        // Arrange
        var testDate = "2026-11-11";
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
                @BillId, 1, @TargetDate, '09:00:00', @RegId, '',
                '', '', 75000, 0, 0, 0,
                75000, 'Konsultasi Rawat Jalan Inbox Test', '', '', '',
                1, '', '2026-11-11 09:00:00'
            )
            """;

        try
        {
            conn.Execute(insertBillingSql, new { BillId = billId, TargetDate = testDate, RegId = regId });

            // Act
            var inbox = _sut.ListOrderInbox(testDate).ToList();

            // Assert
            var item = inbox.FirstOrDefault(x => x.RegId == regId);
            item.Should().NotBeNull();
            item!.Type.Should().Be("PENERIMAAN");
            item.Subtype.Should().Be("Tagihan Layanan RJ/IGD");
            item.TotalNilai.Should().Be(75000m);
            item.Status.Should().Be("MENUNGGU_BAYAR");
            item.UnitName.Should().Be("-");
            item.PenjaminName.Should().Be("UMUM/MANDIRI");
        }
        finally
        {
            conn.Execute("DELETE FROM ta_trs_billing WHERE fs_kd_trs = @BillId", new { BillId = billId });
        }
    }
}
