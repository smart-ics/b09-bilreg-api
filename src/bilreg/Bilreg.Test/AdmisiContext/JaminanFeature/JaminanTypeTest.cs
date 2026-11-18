using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PaymentContext.TrsBillingFeature;
using Bilreg.Infrastructure.AdmisiContext.JaminanFeature;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.AdmisiContext.JaminanFeature;

public class JaminanTypeTest
{
    private static JaminanType CreateSampleJaminan(string piutangPulangCoaId = "1323000", string piutangPulangCoaName = "Piutang Asuransi")
    {
        var alamat = new AlamatType(["Jl. Sudirman 1", "Gedung A"], "Jakarta", "10110");
        var caraBayarDk = new CaraBayarDkType("DK01", "Perusahaan / Asuransi");
        var grupJaminan = new GroupJaminanReff("GRP01", "Asuransi Swasta");
        var tipeTarif = new JaminanTipeTarifType(
            new TipeTarifReff("TAR01", "Tarif Rajal"),
            new TipeTarifReff("TAR02", "Tarif Ranap"));
        var tipeBarang = new JaminanTipeBarangType(
            new TipeBrgType("BRG01", "Barang Rajal"),
            new TipeBrgType("BRG02", "Barang Ranap"));
        var rekening = new JaminanRekeningType(
            new CoaType("111", "PPDP Jasa Rajal"),
            new CoaType("112", "PPDP Obat Rajal"),
            new CoaType("113", "PPDP Jasa Ranap"),
            new CoaType("114", "PPDP Obat Ranap"),
            new CoaType(piutangPulangCoaId, piutangPulangCoaName));

        return new JaminanType(
            "JAM01", "Asuransi Sehat", true,
            alamat, caraBayarDk, grupJaminan, tipeTarif, rekening, tipeBarang);
    }

    [Fact]
    public void GivenValidParameters_WhenConstructJaminanType_ThenRekeningPiutangPulangIsAssignedCorrectly()
    {
        // Act
        var jaminan = CreateSampleJaminan("1323000", "Piutang Asuransi Mandiri");

        // Assert
        jaminan.JaminanId.Should().Be("JAM01");
        jaminan.JaminanName.Should().Be("Asuransi Sehat");
        jaminan.IsAktif.Should().BeTrue();
        jaminan.Rekening.PiutangPulang.CoaId.Should().Be("1323000");
        jaminan.Rekening.PiutangPulang.CoaName.Should().Be("Piutang Asuransi Mandiri");
    }

    [Fact]
    public void Given4ArgConstructor_WhenConstructJaminanRekeningType_ThenPiutangPulangDefaultsToCoaDefault()
    {
        // Act (backward-compatible 4-arg overload)
        var rekening = new JaminanRekeningType(
            new CoaType("111", "PPDP Jasa Rajal"),
            new CoaType("112", "PPDP Obat Rajal"),
            new CoaType("113", "PPDP Jasa Ranap"),
            new CoaType("114", "PPDP Obat Ranap"));

        // Assert
        rekening.PiutangPulang.Should().Be(CoaType.Default);
        rekening.PiutangPulang.CoaId.Should().Be("-");
    }

    [Fact]
    public void WhenDefault_ThenRekeningPiutangPulangIsDefault()
    {
        // Act
        var jaminan = JaminanType.Default;

        // Assert
        jaminan.JaminanId.Should().Be("-");
        jaminan.Rekening.PiutangPulang.Should().Be(CoaType.Default);
        jaminan.Rekening.PiutangPulang.CoaId.Should().Be("-");
    }

    [Fact]
    public void WhenUmum_ThenRekeningPiutangPulangIsDefault()
    {
        // Act
        var jaminan = JaminanType.Umum;

        // Assert
        jaminan.JaminanId.Should().Be("000");
        jaminan.JaminanName.Should().Be("Umum");
        jaminan.Rekening.PiutangPulang.Should().Be(CoaType.Default);
    }

    [Fact]
    public void GivenJaminanTypeWithPiutangPulang_WhenFromModel_ThenJaminanDtoPopulatesFsPiutPulangFields()
    {
        // Arrange
        var model = CreateSampleJaminan("1323000", "Piutang Asuransi Sehat");

        // Act
        var dto = JaminanDto.FromModel(model);

        // Assert
        dto.fs_piut_pulang.Should().Be("1323000");
        dto.fs_nm_piut_pulang.Should().Be("Piutang Asuransi Sehat");
    }

    [Fact]
    public void GivenJaminanDtoWithFsPiutPulang_WhenToModel_ThenJaminanTypeRekeningPiutangPulangIsHydrated()
    {
        // Arrange
        var model = CreateSampleJaminan("1323000", "Piutang Asuransi Sehat");
        var dto = JaminanDto.FromModel(model);

        // Act
        var reconstructedModel = dto.ToModel();

        // Assert
        reconstructedModel.Rekening.PiutangPulang.CoaId.Should().Be("1323000");
        reconstructedModel.Rekening.PiutangPulang.CoaName.Should().Be("Piutang Asuransi Sehat");
        reconstructedModel.Should().BeEquivalentTo(model);
    }
}
