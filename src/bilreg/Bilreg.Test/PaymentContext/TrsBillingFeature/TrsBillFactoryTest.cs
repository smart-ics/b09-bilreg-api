using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.BedUsageContext.BhpRanapFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PaymentContext.RekapCetakFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
using Bilreg.Domain.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.PaymentContext.TrsBillingFeature;

public class TrsBillFactoryTest
{
    [Fact]
    public void GivenBhpRanapInpatient_WhenCreateFromBhpRanap_ThenTrsBillHasMatchingFieldsAndInpatientCoa()
    {
        // Arrange
        var reg = CreateInpatientReg("REG-INAP-001", "J0101");
        var layanan = CreateLayanan("LY-MELATI", "Bangsal Melati");
        var jaminan = CreateJaminan("J01");
        var groupRek = CreateGroupRek("GR-BHP", "BHP Medis", "RK-BHP");
        var audit = new AuditInfoType("nurse-01", new DateTime(2026, 6, 16, 9, 30, 0));
        var createdAt = new DateTime(2026, 6, 16, 9, 30, 0);

        var bhp = BhpRanapModel.Create(
            pakaiBedId: "BED-101",
            regId: reg.RegId,
            layananId: layanan.LayananId,
            bhpItemId: "BHP-INFUS-01",
            bhpItemName: "Infus Set Makro",
            qty: 2,
            price: 25000m,
            audit: audit);

        // Act
        var result = TrsBillFactory.CreateFromBhpRanap(bhp, reg, layanan, jaminan, groupRek, createdAt);

        // Assert
        result.Should().NotBeNull();
        result.TrsBillingId.Should().Be(bhp.BhpRanapId);
        result.ModulGroup.Should().Be(BillModulGroup.Obat);
        result.TglTrs.Should().Be(createdAt);
        result.Reg.RegId.Should().Be(reg.RegId);
        result.Layanan.LayananId.Should().Be(layanan.LayananId);
        result.Kelas.KelasId.Should().Be(reg.Kelas.KelasId);
        result.RekapCetak.RekapCetakId.Should().Be("RK-BHP");

        // Billing Keterangan
        result.Keterangan.RefBiaya.Should().Be("BHP-INFUS-01");
        result.Keterangan.Qty.Should().Be(2m);
        result.Keterangan.TrsMainId.Should().Be(bhp.BhpRanapId);
        result.Keterangan.Keterangan.Should().Be("[GR-BHP] Infus Set Makro 2");

        // Nilai
        result.Nilai.SubTotal.Should().Be(50000m);
        result.Nilai.Total.Should().Be(50000m);
        result.Nilai.Diskon.Should().Be(0m);
        result.Nilai.Tax.Should().Be(0m);
        result.Nilai.Biaya.Should().Be(0m);

        // Transaction Components & COA mapping
        result.ListTransaction.Should().HaveCount(1);
        var trans = result.ListTransaction.First();
        trans.NoUrut.Should().Be(1);
        trans.Komponen.BillKompId.Should().Be("GR-BHP");
        trans.Komponen.BillKompName.Should().Be("BHP Medis");
        trans.JenisBayar.Should().Be(TrsBillJenisBayarType.Pdp);
        trans.Nilai.Should().Be(50000m);
        trans.PetugasMedis.Should().Be(PpaType.Default.ToReff());

        // Account mapping (TD-02: PpdpObatRanap, RekJual, RekPersediaan, RekDisc)
        trans.Coa.Ppdp.CoaId.Should().Be("PPDP-OBAT-RI");
        trans.Coa.Pdpt.CoaId.Should().Be("REK-JUAL-BHP");
        trans.Coa.Persediaan.CoaId.Should().Be("REK-PERSED-BHP");
        trans.Coa.Disc.CoaId.Should().Be("REK-DISC-BHP");
    }

    [Fact]
    public void GivenMismatchedJaminan_WhenCreateFromBhpRanap_ThenThrowsArgumentException()
    {
        // Arrange
        var reg = CreateInpatientReg("REG-INAP-002", "J0101");
        var layanan = CreateLayanan("LY-MELATI", "Bangsal Melati");
        var jaminan = CreateJaminan("J02"); // Mismatched: expected J01
        var groupRek = CreateGroupRek("GR-BHP", "BHP Medis", "RK-BHP");
        var audit = new AuditInfoType("nurse-01", DateTime.Now);

        var bhp = BhpRanapModel.Create(
            pakaiBedId: "BED-101",
            regId: reg.RegId,
            layananId: layanan.LayananId,
            bhpItemId: "BHP-01",
            bhpItemName: "Kassa Steril",
            qty: 1,
            price: 5000m,
            audit: audit);

        // Act
        Action act = () => TrsBillFactory.CreateFromBhpRanap(bhp, reg, layanan, jaminan, groupRek);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Jaminan tidak sesuai registrasi*");
    }

    [Fact]
    public void GivenMismatchedRegId_WhenCreateFromBhpRanap_ThenThrowsArgumentException()
    {
        // Arrange
        var reg = CreateInpatientReg("REG-INAP-003", "J0101");
        var layanan = CreateLayanan("LY-MELATI", "Bangsal Melati");
        var jaminan = CreateJaminan("J01");
        var groupRek = CreateGroupRek("GR-BHP", "BHP Medis", "RK-BHP");
        var audit = new AuditInfoType("nurse-01", DateTime.Now);

        var bhp = BhpRanapModel.Create(
            pakaiBedId: "BED-101",
            regId: "REG-INAP-OTHER", // Mismatched RegId
            layananId: layanan.LayananId,
            bhpItemId: "BHP-01",
            bhpItemName: "Spuit 5cc",
            qty: 1,
            price: 3000m,
            audit: audit);

        // Act
        Action act = () => TrsBillFactory.CreateFromBhpRanap(bhp, reg, layanan, jaminan, groupRek);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Registrasi tidak sesuai dengan BHP Ranap*");
    }

    private static RegModel CreateInpatientReg(string regId, string tipeJaminanId)
    {
        return new RegModel(
            regId,
            new DateOnly(2026, 6, 16),
            new AuditInfoType("admin", new DateTime(2026, 6, 16, 8, 0, 0)),
            AuditInfoType.Default,
            AuditInfoType.Default,
            AuditInfoType.Default,
            JenisRegEnum.RegInap,
            PasienModel.Default.ToReff(),
            new TipeJaminanReff(tipeJaminanId, "BPJS Kesehatan"),
            PolisModel.Default.ToReff(),
            new KelasReff("KL-01", "Kelas I"),
            CaraMasukDkType.Default,
            RujukanType.Default.ToReff(),
            PpaType.Default.ToReff(),
            LayananType.Default.ToReff(),
            KarcisType.Default.ToReff(),
            RegEligibilityType.Default,
            []);
    }

    private static LayananType CreateLayanan(string layananId, string layananName)
    {
        return new LayananType(layananId, layananName, true,
            InstalasiType.Default.ToReff(), LayananDkType.Default.ToReff(),
            TipeLayananDkType.Default, InstalasiDkType.Default,
            Bilreg.Domain.AccountingContext.UnitFeature.UnitType.Default.ToReff(),
            new PoliBpjsReff("-", "-"));
    }

    private static JaminanType CreateJaminan(string jaminanId)
    {
        return JaminanType.Default with
        {
            JaminanId = jaminanId,
            Rekening = new JaminanRekeningType(
                new CoaType("JASA-RJ", ""), new CoaType("PPDP-OBAT-RJ", ""),
                new CoaType("JASA-RI", ""), new CoaType("PPDP-OBAT-RI", ""))
        };
    }

    private static GroupRekType CreateGroupRek(string groupRekId, string groupRekName, string rekapCetakId)
    {
        return new GroupRekType(
            groupRekId, groupRekName, GroupRekDkType.Default, rekapCetakId,
            new CoaType("REK-JUAL-BHP", ""),
            new CoaType("REK-JUAL-RJ-BHP", ""),
            new CoaType("REK-DISC-BHP", ""),
            new CoaType("REK-TAX-BHP", ""),
            new CoaType("REK-PERSED-BHP", ""));
    }
}
