using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
using Bilreg.Domain.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.PaymentContext.TrsBillingFeature;

public class TrsBillFactoryTindakanTest
{
    [Fact]
    public void GivenTindakanWithMultiQty_WhenCreateFromTindakan_ThenTrsBillHasMatchingQtyAndComponentSubTotals()
    {
        // Arrange
        const int qty = 3;
        const decimal tarifSatuan = 25000m;
        const decimal expectedSubTotal = 75000m;

        var reg = new RegModel(
            "REG-01",
            new DateOnly(2026, 10, 16),
            new AuditInfoType("USER-01", new DateTime(2026, 10, 16, 8, 0, 0)),
            AuditInfoType.Default,
            AuditInfoType.Default,
            AuditInfoType.Default,
            JenisRegEnum.RegJalan,
            PasienModel.Default.ToReff(),
            new TipeJaminanReff("J0101", "BPJS Kesehatan"),
            PolisModel.Default.ToReff(),
            KelasType.Default.ToReff(),
            CaraMasukDkType.Default,
            RujukanType.Default.ToReff(),
            PpaType.Default.ToReff(),
            LayananType.Default.ToReff(),
            KarcisType.Default.ToReff(),
            RegEligibilityType.Default,
            []);

        var kompReff = new KomponenReff("KOMP-01", "Jasa Pelayanan");
        var listKomponen = new List<TindakanKomponenWithoutPpaType>
        {
            new(kompReff, 0, tarifSatuan, qty, expectedSubTotal)
        };

        var tarif = TarifType.Default with { TarifId = "TRF-01", TarifName = "Konsultasi Spesialis" };
        var jaminan = JaminanType.Default with { JaminanId = "J01", JaminanName = "BPJS" };

        var tindakan = new TindakanModel(
            "TDK-001",
            new DateTime(2026, 10, 16, 10, 0, 0),
            "",
            reg.ToReff(),
            LayananType.Default.ToReff(),
            KelasType.Default.ToReff(),
            TipeTarifType.Default.ToReff(),
            tarif.ToReff(),
            listKomponen,
            AuditTrailType.Create("USER-01", new DateTime(2026, 10, 16, 10, 0, 0)),
            qty: qty);

        var listReffKomp = new List<KomponenType>
        {
            KomponenType.Default with { KomponenId = "KOMP-01", KomponenName = "Jasa Pelayanan" }
        };

        // Act
        var result = TrsBillFactory.CreateFromTindakan(tindakan, reg, tarif, jaminan, listReffKomp, new DateTime(2026, 10, 16, 10, 0, 0));

        // Assert
        result.Should().NotBeNull();
        result.TrsBillingId.Should().Be("TDK-001");
        result.Keterangan.Qty.Should().Be(3m);
        result.Nilai.SubTotal.Should().Be(75000m);
        result.ListTransaction.Should().HaveCount(1);
        result.ListTransaction.First().Nilai.Should().Be(75000m);
        result.ListTransaction.Sum(x => x.Nilai).Should().Be(result.Nilai.SubTotal);
    }

    [Fact]
    public void GivenTindakanWithDiskonDokter_WhenCreateFromTindakan_ThenTrsBillHasDiscountAndZeroDoctorSubTotal()
    {
        // Arrange
        const int qty = 1;
        const decimal saranaTarif = 40000m;
        const decimal dokterTarif = 60000m;
        const decimal dokterDiskon = 60000m;

        var reg = new RegModel(
            "REG-01",
            new DateOnly(2026, 10, 16),
            new AuditInfoType("USER-01", new DateTime(2026, 10, 16, 8, 0, 0)),
            AuditInfoType.Default,
            AuditInfoType.Default,
            AuditInfoType.Default,
            JenisRegEnum.RegJalan,
            PasienModel.Default.ToReff(),
            new TipeJaminanReff("J0101", "BPJS Kesehatan"),
            PolisModel.Default.ToReff(),
            KelasType.Default.ToReff(),
            CaraMasukDkType.Default,
            RujukanType.Default.ToReff(),
            PpaType.Default.ToReff(),
            LayananType.Default.ToReff(),
            KarcisType.Default.ToReff(),
            RegEligibilityType.Default,
            []);

        var kompSaranaReff = new KomponenReff("KOMP-SARANA", "Jasa Sarana RS");
        var kompDokterReff = new KomponenReff("KOMP-DOKTER", "Jasa Medis Dokter");
        var ppaDokter = new PpaReff("DOK-01", "dr. Budi, Sp.PD");

        var listKomponen = new List<TindakanKomponenBase>
        {
            new TindakanKomponenWithoutPpaType(kompSaranaReff, 0, saranaTarif, qty, saranaTarif, 0m),
            new TindakanKomponenWithPpaType(kompDokterReff, ppaDokter, 1, dokterTarif, qty, 0m, dokterDiskon)
        };

        var tarif = TarifType.Default with { TarifId = "TRF-01", TarifName = "Konsultasi Spesialis" };
        var jaminan = JaminanType.Default with { JaminanId = "J01", JaminanName = "BPJS" };

        var tindakan = new TindakanModel(
            "TDK-002",
            new DateTime(2026, 10, 16, 10, 0, 0),
            "",
            reg.ToReff(),
            LayananType.Default.ToReff(),
            KelasType.Default.ToReff(),
            TipeTarifType.Default.ToReff(),
            tarif.ToReff(),
            listKomponen,
            AuditTrailType.Create("USER-01", new DateTime(2026, 10, 16, 10, 0, 0)),
            qty: qty,
            isDiskonDokter: true);

        var listReffKomp = new List<KomponenType>
        {
            KomponenType.Default with { KomponenId = "KOMP-SARANA", KomponenName = "Jasa Sarana RS" },
            KomponenType.Default with { KomponenId = "KOMP-DOKTER", KomponenName = "Jasa Medis Dokter" }
        };

        // Act
        var result = TrsBillFactory.CreateFromTindakan(tindakan, reg, tarif, jaminan, listReffKomp, new DateTime(2026, 10, 16, 10, 0, 0));

        // Assert
        result.Should().NotBeNull();
        result.TrsBillingId.Should().Be("TDK-002");
        result.Nilai.SubTotal.Should().Be(100000m); // Gross
        result.Nilai.Diskon.Should().Be(60000m);    // Doctor discount
        result.Nilai.Total.Should().Be(40000m);     // Net
        result.ListTransaction.Should().HaveCount(2);

        var saranaTrans = result.ListTransaction.First(x => x.Komponen.BillKompId == "KOMP-SARANA");
        saranaTrans.Nilai.Should().Be(40000m);

        var dokterTrans = result.ListTransaction.First(x => x.Komponen.BillKompId == "KOMP-DOKTER");
        dokterTrans.Nilai.Should().Be(0m); // Waived

        result.ListTransaction.Sum(x => x.Nilai).Should().Be(result.Nilai.Total);
    }

    [Fact]
    public void GivenTindakanWithDiskonDokterAndMultiQty_WhenCreateFromTindakan_ThenTrsBillReflectsProportionalDoctorDiscountAndZeroDoctorSubTotal()
    {
        // Arrange
        const int qty = 3;
        const decimal saranaTarif = 40000m;
        const decimal dokterTarif = 60000m;
        const decimal dokterDiskon = 60000m * qty; // 180000m

        var reg = new RegModel(
            "REG-01",
            new DateOnly(2026, 10, 16),
            new AuditInfoType("USER-01", new DateTime(2026, 10, 16, 8, 0, 0)),
            AuditInfoType.Default,
            AuditInfoType.Default,
            AuditInfoType.Default,
            JenisRegEnum.RegJalan,
            PasienModel.Default.ToReff(),
            new TipeJaminanReff("J0101", "BPJS Kesehatan"),
            PolisModel.Default.ToReff(),
            KelasType.Default.ToReff(),
            CaraMasukDkType.Default,
            RujukanType.Default.ToReff(),
            PpaType.Default.ToReff(),
            LayananType.Default.ToReff(),
            KarcisType.Default.ToReff(),
            RegEligibilityType.Default,
            []);

        var kompSaranaReff = new KomponenReff("KOMP-SARANA", "Jasa Sarana RS");
        var kompDokterReff = new KomponenReff("KOMP-DOKTER", "Jasa Medis Dokter");
        var ppaDokter = new PpaReff("DOK-01", "dr. Budi, Sp.PD");

        var listKomponen = new List<TindakanKomponenBase>
        {
            new TindakanKomponenWithoutPpaType(kompSaranaReff, 0, saranaTarif, qty, saranaTarif * qty, 0m),
            new TindakanKomponenWithPpaType(kompDokterReff, ppaDokter, 1, dokterTarif, qty, 0m, dokterDiskon)
        };

        var tarif = TarifType.Default with { TarifId = "TRF-01", TarifName = "Konsultasi Spesialis" };
        var jaminan = JaminanType.Default with { JaminanId = "J01", JaminanName = "BPJS" };

        var tindakan = new TindakanModel(
            "TDK-003",
            new DateTime(2026, 10, 16, 10, 0, 0),
            "",
            reg.ToReff(),
            LayananType.Default.ToReff(),
            KelasType.Default.ToReff(),
            TipeTarifType.Default.ToReff(),
            tarif.ToReff(),
            listKomponen,
            AuditTrailType.Create("USER-01", new DateTime(2026, 10, 16, 10, 0, 0)),
            qty: qty,
            isDiskonDokter: true);

        var listReffKomp = new List<KomponenType>
        {
            KomponenType.Default with { KomponenId = "KOMP-SARANA", KomponenName = "Jasa Sarana RS" },
            KomponenType.Default with { KomponenId = "KOMP-DOKTER", KomponenName = "Jasa Medis Dokter" }
        };

        // Act
        var result = TrsBillFactory.CreateFromTindakan(tindakan, reg, tarif, jaminan, listReffKomp, new DateTime(2026, 10, 16, 10, 0, 0));

        // Assert
        result.Should().NotBeNull();
        result.TrsBillingId.Should().Be("TDK-003");
        result.Keterangan.Qty.Should().Be(3m);
        result.Nilai.SubTotal.Should().Be(300000m); // Gross: (40k + 60k) * 3
        result.Nilai.Diskon.Should().Be(180000m);   // Doctor discount: 60k * 3
        result.Nilai.Total.Should().Be(120000m);    // Net: 40k * 3
        result.ListTransaction.Should().HaveCount(2);

        var saranaTrans = result.ListTransaction.First(x => x.Komponen.BillKompId == "KOMP-SARANA");
        saranaTrans.Nilai.Should().Be(120000m); // Non-discounted RS component untouched

        var dokterTrans = result.ListTransaction.First(x => x.Komponen.BillKompId == "KOMP-DOKTER");
        dokterTrans.Nilai.Should().Be(0m); // Waived doctor fee

        result.ListTransaction.Sum(x => x.Nilai).Should().Be(result.Nilai.Total);
    }
}
