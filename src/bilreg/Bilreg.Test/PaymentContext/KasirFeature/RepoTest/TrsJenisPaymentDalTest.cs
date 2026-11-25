using Bilreg.Domain.PaymentContext.KasirFeature;
using Bilreg.Infrastructure.PaymentContext.KasirFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.PaymentContext.KasirFeature.RepoTest;

public class TrsJenisPaymentDalTest
{
    private readonly TrsJenisPaymentDal _sut;

    public TrsJenisPaymentDalTest()
    {
        _sut = new TrsJenisPaymentDal(ConnStringHelper.GetTestEnv());
    }

    [Theory]
    [InlineData("KBS1", "KAS RAWAT INAP", true, KasirConst.MetodeBayarTunai)]
    [InlineData("TUN01", "PEMBAYARAN TUNAI", true, KasirConst.MetodeBayarTunai)]
    [InlineData("KBNI1", "BNI DEBIT", true, KasirConst.MetodeBayarDebit)]
    [InlineData("KBCA1", "BCA", true, KasirConst.MetodeBayarDebit)]
    [InlineData("KBRI1", "BRI", true, KasirConst.MetodeBayarDebit)]
    [InlineData("KBNI2", "BNI KREDIT", false, KasirConst.MetodeBayarKredit)]
    [InlineData("KMAN2", "MANDIRI KREDIT", false, KasirConst.MetodeBayarKredit)]
    [InlineData("CRD01", "KARTU CREDIT MASTER", true, KasirConst.MetodeBayarKredit)]
    [InlineData("CRD02", "VISA CARD", false, KasirConst.MetodeBayarKredit)]
    [InlineData("GRBTN", "GIRO BTN YSMP", true, KasirConst.MetodeBayarTransfer)]
    [InlineData("KBGN", "GIRO NISP", true, KasirConst.MetodeBayarTransfer)]
    [InlineData("TRF01", "TRANSFER BCA", true, KasirConst.MetodeBayarTransfer)]
    [InlineData("QRS01", "QRIS BCA", true, KasirConst.MetodeBayarQris)]
    [InlineData("QRD01", "QRIS DANA", true, KasirConst.MetodeBayarQris)]
    public void DetermineKategori_GivenVariousKeywords_ClassifiesCorrectly(
        string kode, string nama, bool isKredit, string expectedCategory)
    {
        var result = TrsJenisPaymentDal.DetermineKategori(kode, nama, isKredit);
        result.Should().Be(expectedCategory);
    }

    [Fact]
    public void ListAktif_WhenCalled_ReturnsActiveJenisPaymentsWithCategories()
    {
        // Act
        var result = _sut.ListAktif().ToList();

        // Assert
        result.Should().NotBeEmpty();
        result.Should().OnlyContain(x => !string.IsNullOrWhiteSpace(x.KodeJenisPayment));
        result.Should().OnlyContain(x => !string.IsNullOrWhiteSpace(x.NamaJenisPayment));
        result.Should().OnlyContain(x => !string.IsNullOrWhiteSpace(x.CoaId));
        result.Should().OnlyContain(x => KasirConst.AllowedMetodeBayar.Contains(x.Kategori));

        // Check specific known entries from ta_jenis_payment
        result.Select(x => x.KodeJenisPayment).Should().Contain(["KBCA1", "KBNI1", "KBNI2", "KBRI1", "KMAN1", "KMAN2", "GRBTN"]);

        var bca = result.First(x => x.KodeJenisPayment == "KBCA1");
        bca.NamaJenisPayment.Should().Be("BCA");
        bca.CoaId.Should().Be("1130600");
        bca.Kategori.Should().Be(KasirConst.MetodeBayarDebit);

        var bniKredit = result.First(x => x.KodeJenisPayment == "KBNI2");
        bniKredit.NamaJenisPayment.Should().Be("BNI KREDIT");
        bniKredit.Kategori.Should().Be(KasirConst.MetodeBayarKredit);

        var giroBtn = result.First(x => x.KodeJenisPayment == "GRBTN");
        giroBtn.NamaJenisPayment.Should().Be("GIRO BTN YSMP");
        giroBtn.Kategori.Should().Be(KasirConst.MetodeBayarTransfer);
    }

    [Fact]
    public void ListAktifGrouped_WhenCalled_ReturnsFiveStandardCategories()
    {
        // Act
        var result = _sut.ListAktifGrouped().ToList();

        // Assert
        result.Should().HaveCount(5);
        result.Select(x => x.Kategori).Should().ContainInOrder(
            KasirConst.MetodeBayarTunai,
            KasirConst.MetodeBayarDebit,
            KasirConst.MetodeBayarKredit,
            KasirConst.MetodeBayarTransfer,
            KasirConst.MetodeBayarQris
        );

        var debitGroup = result.First(x => x.Kategori == KasirConst.MetodeBayarDebit);
        debitGroup.Items.Should().NotBeEmpty();
        debitGroup.Items.Select(x => x.KodeJenisPayment).Should().Contain(["KBCA1", "KBNI1", "KBRI1"]);

        var kreditGroup = result.First(x => x.Kategori == KasirConst.MetodeBayarKredit);
        kreditGroup.Items.Should().NotBeEmpty();
        kreditGroup.Items.Select(x => x.KodeJenisPayment).Should().Contain(["KBNI2", "KMAN2"]);

        var transferGroup = result.First(x => x.Kategori == KasirConst.MetodeBayarTransfer);
        transferGroup.Items.Should().NotBeEmpty();
        transferGroup.Items.Select(x => x.KodeJenisPayment).Should().Contain(["GRBTN", "KBGN"]);
    }

    [Fact]
    public void GetData_WhenFound_ReturnsPayment()
    {
        // Act
        var result = _sut.GetData("KBCA1");

        // Assert
        result.Should().NotBeNull();
        result!.KodeJenisPayment.Should().Be("KBCA1");
        result.NamaJenisPayment.Should().Be("BCA");
        result.CoaId.Should().Be("1130600");
        result.Kategori.Should().Be(KasirConst.MetodeBayarDebit);
    }

    [Fact]
    public void GetData_WhenNotFound_ReturnsNull()
    {
        // Act
        var result = _sut.GetData("NONEXISTENT");

        // Assert
        result.Should().BeNull();
    }
}
