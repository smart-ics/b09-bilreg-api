using Bilreg.Application.PaymentContext.TataRekeningFeature;
using Bilreg.Application.PaymentContext.TataRekeningFeature.Dtos;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PaymentContext.DepositFeature;
using Bilreg.Domain.PaymentContext.RekapCetakFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Infrastructure.PaymentContext.TataRekeningFeature;
using FluentAssertions;

namespace Bilreg.Test.PaymentContext.TataRekeningFeature;

public class TataRekeningDtoTest
{
    private static readonly DateTime FinalizationDate = new(2026, 6, 17, 14, 0, 0);

    [Fact]
    public void FromModel_MapsStatusFinalizationAndPhase1Fields()
    {
        var model = new TataRekeningModel(
            "REG-001",
            TataRekeningStatusEnum.Finalized,
            new TataRekeningFinalizationType("KSR-01", FinalizationDate),
            [],
            [],
            FinancialVerificationStatusEnum.Valid,
            new FinancialVerificationInfo("VER-01", FinalizationDate),
            isFinancialResponsibilityAllocated: true,
            settlementInitiated: true,
            version: 4);

        var dto = BilrgTataRekeningDto.FromModel(model);

        dto.RegId.Should().Be("REG-001");
        dto.Status.Should().Be((int)TataRekeningStatusEnum.Finalized);
        dto.PetugasVerif.Should().Be("KSR-01");
        dto.FinalizationDate.Should().Be(FinalizationDate);
        dto.FinVerifStatus.Should().Be((int)FinancialVerificationStatusEnum.Valid);
        dto.IsAllocated.Should().BeTrue();
        dto.SettlementInitiated.Should().BeTrue();
        dto.Version.Should().Be(4);
    }

    [Fact]
    public void ToHeaderParts_MapsStatusAndFinalizationInfo()
    {
        var dto = TataRekeningDtoTestHelper.BuildHeaderDto("REG-002", TataRekeningStatusEnum.Closed) with
        {
            PetugasVerif = "KSR-02",
            FinalizationDate = FinalizationDate
        };

        var (status, finalizationInfo) = dto.ToHeaderParts();

        status.Should().Be(TataRekeningStatusEnum.Closed);
        finalizationInfo.PetugasVerif.Should().Be("KSR-02");
        finalizationInfo.FinalizationDate.Should().Be(FinalizationDate);
    }

    [Fact]
    public void ToPhase1Parts_MapsVerificationAllocationSettlementAndVersion()
    {
        var dto = TataRekeningDtoTestHelper.BuildHeaderDto("REG-003") with
        {
            FinVerifStatus = (int)FinancialVerificationStatusEnum.Valid,
            FinVerifPetugas = "VER-03",
            FinVerifDate = FinalizationDate,
            IsAllocated = true,
            SettlementInitiated = true,
            Version = 7
        };

        var (finVerifStatus, finVerifInfo, isAllocated, settlementInitiated, version) = dto.ToPhase1Parts();

        finVerifStatus.Should().Be(FinancialVerificationStatusEnum.Valid);
        finVerifInfo!.PetugasVerif.Should().Be("VER-03");
        isAllocated.Should().BeTrue();
        settlementInitiated.Should().BeTrue();
        version.Should().Be(7);
    }

    [Fact]
    public void ToBillSummaryDto_MapsAllExpandedPropertiesFromTrsBillType()
    {
        var tglTrs = new DateTime(2026, 6, 16, 8, 30, 0);
        var bill = new TrsBillType(
            "BILL-EXP-01",
            BillModulGroup.Jasa,
            tglTrs,
            new RegReff("REG-EXP-01", "RM-01", "Pasien A"),
            new LayananReff("LAY-01", "Poli Penyakit Dalam"),
            KelasType.Default.ToReff(),
            AuditInfoType.Default,
            new RekapCetakReff("RK-01", "Pemeriksaan Dokter"),
            new TrsBillNilaiType(150_000m, 0, 0, 0),
            new TrsBillKetType("Konsultasi Spesialis", "Ket2", "REF-KONS-01", 1, "MAIN-01"),
            [], [], []);

        var dto = TataRekeningApplicationMapper.ToBillSummaryDto(bill);

        dto.TrsBillingId.Should().Be("BILL-EXP-01");
        dto.RegId.Should().Be("REG-EXP-01");
        dto.ModulGroup.Should().Be(BillModulGroup.Jasa);
        dto.NilaiTotal.Should().Be(150_000m);
        dto.FinancialTotal.Should().Be(150_000m);
        dto.TglTrs.Should().Be(tglTrs);
        dto.LayananName.Should().Be("Poli Penyakit Dalam");
        dto.Keterangan.Should().Be("Konsultasi Spesialis");
        dto.RefBiaya.Should().Be("REF-KONS-01");
        dto.RekapCetakName.Should().Be("Pemeriksaan Dokter");
        dto.Diskon.Should().Be(0m);
        dto.PetugasMedisName.Should().Be(string.Empty);
    }

    [Fact]
    public void ToBillSummaryDto_WithDiskonAndPetugasMedis_MapsCorrectly()
    {
        var tglTrs = new DateTime(2026, 6, 16, 8, 30, 0);

        var transWithoutDoctor = new TrsBill2TransEventType(
            1,
            new TrsBill2KomponenType("DT-01", "Sewa Kamar"),
            TrsBillJenisBayarType.Pdp,
            100_000m,
            new PpaReff("", ""),
            TrsBill2CoaType.Default);

        var transWithDoctor = new TrsBill2TransEventType(
            2,
            new TrsBill2KomponenType("DT-02", "Jasa Dokter"),
            TrsBillJenisBayarType.Pdp,
            150_000m,
            new PpaReff("DOK-01", "dr. Budi Santoso, Sp.PD"),
            TrsBill2CoaType.Default);

        var bill = new TrsBillType(
            "BILL-EXP-02",
            BillModulGroup.Jasa,
            tglTrs,
            new RegReff("REG-EXP-02", "RM-02", "Pasien B"),
            new LayananReff("LAY-01", "Poli Penyakit Dalam"),
            KelasType.Default.ToReff(),
            AuditInfoType.Default,
            new RekapCetakReff("RK-01", "Pemeriksaan Dokter"),
            new TrsBillNilaiType(250_000m, 25_000m, 0, 0),
            new TrsBillKetType("Konsultasi & Tindakan", "Ket2", "REF-KONS-02", 1, "MAIN-01"),
            [transWithoutDoctor, transWithDoctor], [], []);

        var dto = TataRekeningApplicationMapper.ToBillSummaryDto(bill);

        dto.TrsBillingId.Should().Be("BILL-EXP-02");
        dto.Diskon.Should().Be(25_000m);
        dto.PetugasMedisName.Should().Be("dr. Budi Santoso, Sp.PD");
    }

    [Fact]
    public void TataRekeningPatientContextDto_InitializesPropertiesCorrectly()
    {
        var dto = new TataRekeningPatientContextDto(
            PasienId: "PAS-001",
            PasienName: "Budi Santoso",
            Gender: "L",
            TglLahir: "1985-05-12",
            LayananId: "LAY-001",
            LayananName: "Rawat Inap Paviliun",
            JenisReg: "Ranap",
            TipeJaminanId: "JAM-BPJS",
            TipeJaminanName: "BPJS Kesehatan",
            KelasName: "Kelas 1",
            DpjpName: "dr. Andi, Sp.PD",
            AdvancePayment: 500_000m,
            DepositPayment: 1_000_000m);

        dto.PasienId.Should().Be("PAS-001");
        dto.PasienName.Should().Be("Budi Santoso");
        dto.Gender.Should().Be("L");
        dto.TglLahir.Should().Be("1985-05-12");
        dto.LayananId.Should().Be("LAY-001");
        dto.LayananName.Should().Be("Rawat Inap Paviliun");
        dto.JenisReg.Should().Be("Ranap");
        dto.TipeJaminanId.Should().Be("JAM-BPJS");
        dto.TipeJaminanName.Should().Be("BPJS Kesehatan");
        dto.KelasName.Should().Be("Kelas 1");
        dto.DpjpName.Should().Be("dr. Andi, Sp.PD");
        dto.AdvancePayment.Should().Be(500_000m);
        dto.DepositPayment.Should().Be(1_000_000m);
    }

    [Fact]
    public void ToPatientContextDto_WhenRegHasLayanan_MapsLayananIdAndAllPatientProperties()
    {
        var pasien = new PasienReff("PAS-001", "Budi Santoso", new DateOnly(1985, 5, 12), "L");
        var layanan = new LayananReff("LAY-01", "Rawat Inap Melati");
        var dokter = new PpaReff("DOK-01", "dr. Andi, Sp.PD");
        var kelas = new KelasReff("KLS-01", "Kelas 1");
        var jaminan = new TipeJaminanReff("JAM-01", "BPJS Kesehatan");

        var reg = new RegModel(
            "REG-001",
            new DateOnly(2026, 6, 1),
            AuditInfoType.Default, AuditInfoType.Default, AuditInfoType.Default, AuditInfoType.Default,
            JenisRegEnum.RegInap,
            pasien, jaminan, PolisModel.Default.ToReff(), kelas,
            CaraMasukDkType.Default, RujukanType.Default.ToReff(),
            dokter, layanan, KarcisType.Default.ToReff(),
            RegEligibilityType.Default, []);

        var saldo = new DepositSaldoModel("REG-001", 100_000m, 0m, 100_000m);

        var dto = TataRekeningApplicationMapper.ToPatientContextDto(reg, saldo, advancePayment: 25_000m);

        dto.PasienId.Should().Be("PAS-001");
        dto.PasienName.Should().Be("Budi Santoso");
        dto.Gender.Should().Be("L");
        dto.TglLahir.Should().Be("1985-05-12");
        dto.LayananId.Should().Be("LAY-01");
        dto.LayananName.Should().Be("Rawat Inap Melati");
        dto.JenisReg.Should().Be("RegInap");
        dto.TipeJaminanId.Should().Be("JAM-01");
        dto.TipeJaminanName.Should().Be("BPJS Kesehatan");
        dto.KelasName.Should().Be("Kelas 1");
        dto.DpjpName.Should().Be("dr. Andi, Sp.PD");
        dto.AdvancePayment.Should().Be(25_000m);
        dto.DepositPayment.Should().Be(100_000m);
    }

    [Fact]
    public void ToPatientContextDto_WhenRegHasNullLayanan_MapsEmptyLayananId()
    {
        var reg = new RegModel(
            "REG-002",
            new DateOnly(2026, 6, 1),
            AuditInfoType.Default, AuditInfoType.Default, AuditInfoType.Default, AuditInfoType.Default,
            JenisRegEnum.RegJalan,
            null!, null!, PolisModel.Default.ToReff(), null!,
            CaraMasukDkType.Default, RujukanType.Default.ToReff(),
            null!, null!, KarcisType.Default.ToReff(),
            RegEligibilityType.Default, []);

        var dto = TataRekeningApplicationMapper.ToPatientContextDto(reg, null);

        dto.LayananId.Should().BeEmpty();
        dto.LayananName.Should().BeEmpty();
        dto.DepositPayment.Should().Be(0m);
        dto.AdvancePayment.Should().Be(0m);
    }

    [Fact]
    public void TataRekeningBillingBreakdownDto_InitializesComponentsCorrectly()
    {
        var tagihanLalu = new TagihanLaluComponent(100_000m, 50_000m, 150_000m);
        var tagihanSaatIni = new TagihanSaatIniComponent(300_000m, 200_000m, 20_000m, 10_000m, 490_000m);
        var biayaAdministrasi = new BiayaAdministrasiComponent(25_000m, 10_000m, 500m, 35_500m);

        var dto = new TataRekeningBillingBreakdownDto(
            TagihanLalu: tagihanLalu,
            TagihanSaatIni: tagihanSaatIni,
            BiayaAdministrasi: biayaAdministrasi,
            GrandTotalKewajiban: 675_500m,
            SisaTagihanKasir: 675_500m);

        dto.TagihanLalu.Total.Should().Be(150_000m);
        dto.TagihanSaatIni.Total.Should().Be(490_000m);
        dto.BiayaAdministrasi.Total.Should().Be(35_500m);
        dto.GrandTotalKewajiban.Should().Be(675_500m);
        dto.SisaTagihanKasir.Should().Be(675_500m);
    }

    [Theory]
    [InlineData("LEX26A150001", 12)]      // NunaId.New("LEX") - 12 chars (Lab External Walk-In)
    [InlineData("LEX26A150001A", 13)]     // 13 chars
    [InlineData("LEX26A150001AB", 14)]    // 14 chars
    [InlineData("LEX26A150001ABC", 15)]   // 15 chars (Max capacity for BILRG_TataRekening.RegId VARCHAR(15))
    public void FromModel_AcceptsAndMapsRegIdWith12To15Characters(string regId, int expectedLength)
    {
        // AC-ARCH-09 & TD-09: Ensure BilrgTataRekeningDto and TataRekeningModel accept and map RegId of length 12 to 15 characters
        regId.Length.Should().Be(expectedLength);

        var model = TataRekeningModel.Create(regId);
        model.RegId.Should().Be(regId);
        model.RegId.Length.Should().Be(expectedLength);
        model.Status.Should().Be(TataRekeningStatusEnum.Opened);

        var dto = BilrgTataRekeningDto.FromModel(model);
        dto.RegId.Should().Be(regId);
        dto.RegId.Length.Should().Be(expectedLength);
        dto.Status.Should().Be((int)TataRekeningStatusEnum.Opened);

        var insertDto = BilrgTataRekeningDto.FromModelForInsert(model);
        insertDto.RegId.Should().Be(regId);
        insertDto.RegId.Length.Should().Be(expectedLength);
        insertDto.Version.Should().Be(1);
    }
}
