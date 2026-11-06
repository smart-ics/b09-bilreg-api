using Bilreg.Application.PaymentContext.TataRekeningFeature;
using Bilreg.Application.PaymentContext.TataRekeningFeature.Dtos;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PaymentContext.RekapCetakFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
using Bilreg.Domain.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.PaymentContext.TataRekeningFeature;

public class TataRekeningApplicationMapperTest
{
    private const string DefaultRegId = "REG-TEST-001";

    private static TrsBillType CreateBill(
        string billingId,
        BillModulGroup modulGroup,
        decimal subTotal,
        decimal diskon = 0m,
        decimal tax = 0m,
        decimal biaya = 0m,
        string rekapCetakName = "Pemeriksaan Dokter",
        string keterangan = "Konsultasi Dokter")
    {
        var regReff = new RegReff(DefaultRegId, "PAS-001", "Pasien Test");
        var layananReff = new LayananReff("LAY-01", "Layanan Rawat Jalan");
        var kelasReff = KelasType.Default.ToReff();
        var rekapCetakReff = new RekapCetakReff("RK-01", rekapCetakName);
        var nilai = new TrsBillNilaiType(subTotal, diskon, tax, biaya);
        var ket = new TrsBillKetType(keterangan, "", "REF-01", 1, "MAIN-01");

        return new TrsBillType(
            billingId,
            modulGroup,
            new DateTime(2026, 6, 16, 9, 0, 0),
            regReff,
            layananReff,
            kelasReff,
            AuditInfoType.Default,
            rekapCetakReff,
            nilai,
            ket,
            [], [], []);
    }

    private static TataRekeningModel CreateModel(params TrsBillType[] bills)
    {
        return new TataRekeningModel(
            DefaultRegId,
            TataRekeningStatusEnum.Opened,
            TataRekeningFinalizationType.Default,
            listTataRekeningPayment: [],
            listTrsBill: bills);
    }

    [Fact]
    public void ToBillingBreakdownDto_WhenMeteraiInRekapOrKeterangan_SegregatesToMeteraiComponent()
    {
        // Arrange
        var pureJasaBill = CreateBill("B1", BillModulGroup.Jasa, 100_000m, rekapCetakName: "Tindakan Medis", keterangan: "Injeksi");
        var meteraiRekapBill = CreateBill("B2", BillModulGroup.Jasa, 10_000m, rekapCetakName: "Bea Meterai", keterangan: "Biaya Dokumen");
        var meteraiKetBill = CreateBill("B3", BillModulGroup.Jasa, 10_000m, rekapCetakName: "Lain-Lain", keterangan: "Materai 10000");

        var model = CreateModel(pureJasaBill, meteraiRekapBill, meteraiKetBill);

        // Act
        var breakdown = TataRekeningApplicationMapper.ToBillingBreakdownDto(model);

        // Assert
        breakdown.BiayaAdministrasi.Meterai.Should().Be(20_000m);
        breakdown.TagihanSaatIni.Jasa.Should().Be(100_000m);
        breakdown.TagihanSaatIni.Total.Should().Be(100_000m);
        breakdown.BiayaAdministrasi.Total.Should().Be(20_000m);
        breakdown.GrandTotalKewajiban.Should().Be(120_000m);
        breakdown.SisaTagihanKasir.Should().Be(120_000m);
    }

    [Fact]
    public void ToBillingBreakdownDto_WhenPembulatanInRekapOrKeterangan_SegregatesToPembulatanComponent()
    {
        // Arrange
        var pureJasaBill = CreateBill("B1", BillModulGroup.Jasa, 150_000m, rekapCetakName: "Tindakan Medis", keterangan: "Pemeriksaan Fisik");
        var pembulatanRekapBill = CreateBill("B2", BillModulGroup.Jasa, -250m, rekapCetakName: "Pembulatan", keterangan: "Adjustment");
        var pembulatanKetBill = CreateBill("B3", BillModulGroup.Jasa, 50m, rekapCetakName: "Lain-Lain", keterangan: "Bulat Kasir");

        var model = CreateModel(pureJasaBill, pembulatanRekapBill, pembulatanKetBill);

        // Act
        var breakdown = TataRekeningApplicationMapper.ToBillingBreakdownDto(model);

        // Assert
        breakdown.BiayaAdministrasi.Pembulatan.Should().Be(-200m);
        breakdown.TagihanSaatIni.Jasa.Should().Be(150_000m);
        breakdown.TagihanSaatIni.Total.Should().Be(150_000m);
        breakdown.BiayaAdministrasi.Total.Should().Be(-200m);
        breakdown.GrandTotalKewajiban.Should().Be(149_800m);
        breakdown.SisaTagihanKasir.Should().Be(149_800m);
    }

    [Fact]
    public void ToBillingBreakdownDto_WhenAdministrasiInRekapOrKeterangan_SegregatesToAdministrasiComponent()
    {
        // Arrange: Karcis, ADM, and ADMINISTRASI keywords
        var pureJasaBill = CreateBill("B1", BillModulGroup.Jasa, 120_000m, rekapCetakName: "Tindakan", keterangan: "Tindakan");
        var karcisBill = CreateBill("B2", BillModulGroup.Jasa, 25_000m, rekapCetakName: "Karcis Pendaftaran", keterangan: "REG: Karcis");
        var admBill = CreateBill("B3", BillModulGroup.Jasa, 15_000m, rekapCetakName: "Biaya ADM", keterangan: "Administrasi Poli");

        var model = CreateModel(pureJasaBill, karcisBill, admBill);

        // Act
        var breakdown = TataRekeningApplicationMapper.ToBillingBreakdownDto(model);

        // Assert: Jasa only has 120_000 (pure), while karcis + adm are in Administrasi
        breakdown.TagihanSaatIni.Jasa.Should().Be(120_000m);
        breakdown.BiayaAdministrasi.Administrasi.Should().Be(40_000m);
        breakdown.BiayaAdministrasi.Total.Should().Be(40_000m);
        breakdown.GrandTotalKewajiban.Should().Be(160_000m);
    }

    [Fact]
    public void ToBillingBreakdownDto_WhenNonTransaksionalBiayaPresentOnPureBills_AggregatesIntoAdministrasi()
    {
        // Arrange: Pure Jasa and Obat bills with embedded Nilai.Biaya
        var jasaBill = CreateBill("B1", BillModulGroup.Jasa, 200_000m, biaya: 5_000m, rekapCetakName: "Pemeriksaan Dokter", keterangan: "Konsul");
        var obatBill = CreateBill("B2", BillModulGroup.Obat, 100_000m, biaya: 2_500m, rekapCetakName: "Farmasi", keterangan: "Resep Obat");

        var model = CreateModel(jasaBill, obatBill);

        // Act
        var breakdown = TataRekeningApplicationMapper.ToBillingBreakdownDto(model);

        // Assert
        breakdown.TagihanSaatIni.Jasa.Should().Be(200_000m);
        breakdown.TagihanSaatIni.Obat.Should().Be(100_000m);
        breakdown.TagihanSaatIni.Total.Should().Be(300_000m);
        breakdown.BiayaAdministrasi.Administrasi.Should().Be(7_500m);
        breakdown.BiayaAdministrasi.Total.Should().Be(7_500m);
        breakdown.GrandTotalKewajiban.Should().Be(307_500m);
    }

    [Fact]
    public void ToBillingBreakdownDto_CompositeScenarioWithAllComponents_VerifiesPreciseBalance()
    {
        // Arrange
        // Jasa pure: 200,000, diskon 20,000, tax 10,000, biaya 5,000
        var billJasa = CreateBill("B-JASA", BillModulGroup.Jasa, 200_000m, diskon: 20_000m, tax: 10_000m, biaya: 5_000m,
            rekapCetakName: "Pemeriksaan Dokter", keterangan: "Konsul Spesialis");

        // Obat pure: 150,000, diskon 10_000, tax 5_000, biaya 0
        var billObat = CreateBill("B-OBAT", BillModulGroup.Obat, 150_000m, diskon: 10_000m, tax: 5_000m,
            rekapCetakName: "Apotek Rawat Jalan", keterangan: "Antibiotik");

        // Karcis (ModulGroup Jasa): 25,000
        var billKarcis = CreateBill("B-KARCIS", BillModulGroup.Jasa, 25_000m,
            rekapCetakName: "Karcis", keterangan: "REG: Karcis Rajal");

        // Meterai (ModulGroup Jasa): 10,000
        var billMeterai = CreateBill("B-METERAI", BillModulGroup.Jasa, 10_000m,
            rekapCetakName: "METERAI", keterangan: "Bea Meterai");

        // Pembulatan (ModulGroup Jasa): -150
        var billPembulatan = CreateBill("B-BULAT", BillModulGroup.Jasa, -150m,
            rekapCetakName: "PEMBULATAN", keterangan: "Pembulatan");

        var model = CreateModel(billJasa, billObat, billKarcis, billMeterai, billPembulatan);

        // Act
        var breakdown = TataRekeningApplicationMapper.ToBillingBreakdownDto(
            model,
            depositPayment: 50_000m,
            advancePayment: 25_000m);

        // Assert TagihanSaatIni
        // Jasa pure = 200_000
        breakdown.TagihanSaatIni.Jasa.Should().Be(200_000m);
        // Obat pure = 150_000
        breakdown.TagihanSaatIni.Obat.Should().Be(150_000m);
        // Diskon = 20_000 + 10_000 = 30_000
        breakdown.TagihanSaatIni.Diskon.Should().Be(30_000m);
        // Tax = 10_000 + 5_000 = 15_000
        breakdown.TagihanSaatIni.Pajak.Should().Be(15_000m);
        // TagihanSaatIni.Total = 200_000 + 150_000 - 30_000 + 15_000 = 335_000
        breakdown.TagihanSaatIni.Total.Should().Be(335_000m);

        // Assert BiayaAdministrasi
        // Administrasi = 25_000 (karcis) + 5_000 (biaya non-transaksional dari billJasa) = 30_000
        breakdown.BiayaAdministrasi.Administrasi.Should().Be(30_000m);
        // Meterai = 10_000
        breakdown.BiayaAdministrasi.Meterai.Should().Be(10_000m);
        // Pembulatan = -150
        breakdown.BiayaAdministrasi.Pembulatan.Should().Be(-150m);
        // BiayaAdministrasi.Total = 30_000 + 10_000 - 150 = 39_850
        breakdown.BiayaAdministrasi.Total.Should().Be(39_850m);

        // GrandTotalKewajiban = 0 (lalu) + 335_000 (saat ini) + 39_850 (administrasi) = 374_850
        breakdown.GrandTotalKewajiban.Should().Be(374_850m);

        // Deduction = deposit (50_000) + advance (25_000) = 75_000
        // SisaTagihanKasir = 374_850 - 75_000 = 299_850
        breakdown.SisaTagihanKasir.Should().Be(299_850m);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("BYKAS")]
    [InlineData("bykas")]
    [InlineData("-")]
    public void ToPaymentType_WhenPaymentIsByKasAndCoaIdIsInvalidOrByKas_FallsBackTo1499901(string dummyCoaId)
    {
        // Arrange
        var input = new PaymentAllocationInputDto(
            PaymentId: "BYKAS",
            PaymentName: "KAS",
            IsTipeJaminan: false,
            NilaiJasa: 100_000m,
            NilaiObat: 50_000m,
            CoaId: dummyCoaId,
            CoaName: "KAS");

        // Act
        var result = TataRekeningApplicationMapper.ToPaymentType(input);

        // Assert
        result.Payment.PaymentId.Should().Be("BYKAS");
        result.Coa.CoaId.Should().Be("1499901");
        result.Coa.CoaName.Should().Be("Rekening Piutang Kasir Perantara");
    }

    [Fact]
    public void ToPaymentType_WhenPaymentIsByKasAndCoaIdIs1499901WithEmptyName_FillsStandardCoaName()
    {
        // Arrange
        var input = new PaymentAllocationInputDto(
            PaymentId: "BYKAS",
            PaymentName: "KAS",
            IsTipeJaminan: false,
            NilaiJasa: 100_000m,
            NilaiObat: 50_000m,
            CoaId: "1499901",
            CoaName: "");

        // Act
        var result = TataRekeningApplicationMapper.ToPaymentType(input);

        // Assert
        result.Coa.CoaId.Should().Be("1499901");
        result.Coa.CoaName.Should().Be("Rekening Piutang Kasir Perantara");
    }

    [Fact]
    public void ToPaymentType_WhenPaymentIsByKasAndValidCustomCoa_PreservesCustomCoa()
    {
        // Arrange
        var input = new PaymentAllocationInputDto(
            PaymentId: "BYKAS",
            PaymentName: "KAS",
            IsTipeJaminan: false,
            NilaiJasa: 100_000m,
            NilaiObat: 50_000m,
            CoaId: "1110001",
            CoaName: "Kasir Rawat Jalan");

        // Act
        var result = TataRekeningApplicationMapper.ToPaymentType(input);

        // Assert
        result.Coa.CoaId.Should().Be("1110001");
        result.Coa.CoaName.Should().Be("Kasir Rawat Jalan");
    }

    [Fact]
    public void ToPaymentType_WhenPaymentIsNotByKas_PreservesInputCoa()
    {
        // Arrange
        var input = new PaymentAllocationInputDto(
            PaymentId: "BYPRI",
            PaymentName: "Hutang Pribadi",
            IsTipeJaminan: false,
            NilaiJasa: 80_000m,
            NilaiObat: 20_000m,
            CoaId: "1321000",
            CoaName: "Piutang Pasien Pribadi");

        // Act
        var result = TataRekeningApplicationMapper.ToPaymentType(input);

        // Assert
        result.Payment.PaymentId.Should().Be("BYPRI");
        result.Coa.CoaId.Should().Be("1321000");
        result.Coa.CoaName.Should().Be("Piutang Pasien Pribadi");
    }
}
