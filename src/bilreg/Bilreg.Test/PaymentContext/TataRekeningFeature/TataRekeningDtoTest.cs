using Bilreg.Application.PaymentContext.TataRekeningFeature;
using Bilreg.Application.PaymentContext.TataRekeningFeature.Dtos;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
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
    }

    [Fact]
    public void TataRekeningPatientContextDto_InitializesPropertiesCorrectly()
    {
        var dto = new TataRekeningPatientContextDto(
            PasienId: "PAS-001",
            PasienName: "Budi Santoso",
            Gender: "L",
            TglLahir: "1985-05-12",
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
}
