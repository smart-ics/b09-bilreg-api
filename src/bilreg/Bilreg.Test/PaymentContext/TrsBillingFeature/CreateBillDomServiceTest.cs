using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.BrgContext.PricingPolicyFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PaymentContext.RekapCetakFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.SalesContext.PenjualanFeature;
using Bilreg.Domain.SalesContext.ResepFeature;
using Bilreg.Test.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
using Bilreg.Domain.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using TipeBrgType = Bilreg.Domain.BrgContext.PricingPolicyFeature.TipeBrgType;

namespace Bilreg.Test.PaymentContext.TrsBillingFeature;

public class CreateBillDomServiceTest
{
    private readonly CreateBillDomService _sut;

    public CreateBillDomServiceTest()
    {
        _sut = new CreateBillDomService();
    }

    [Fact]
    public void UT01_GivenClosedTataRekening_WhenCreateFromRegistration_ThenShouldThrowInvalidOperationException()
    {
        var reg = CreateRegWithKomponen("REG-001");
        var tataRekening = TataRekeningModel.Create(reg.RegId);
        tataRekening.Close();

        Action act = () => _sut.FromReg(
            tataRekening,
            reg,
            KarcisType.Default,
            JaminanType.Default,
            PpaType.Default,
            []);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*berstatus OPEN*");
    }

    [Fact]
    public void UT02_GivenOpenedTataRekening_WhenCreateFromRegistration_ThenShouldCreateTrsBill()
    {
        var reg = CreateRegWithKomponen("REG-002");
        var tataRekening = TataRekeningModel.Create(reg.RegId);

        var result = _sut.FromReg(
            tataRekening,
            reg,
            KarcisType.Default,
            JaminanType.Default,
            PpaType.Default,
            []);

        result.Should().NotBeNull();
        result.TrsBillingId.Should().Be(reg.RegId);
        result.Reg.RegId.Should().Be(reg.RegId);
        result.ListTransaction.Should().HaveCount(1);
        tataRekening.ListTrsBill.Should().BeEmpty();
    }

    [Fact]
    public void UT04_GivenFinalizedTataRekening_WhenCreateFromRegistration_ThenShouldThrowInvalidOperationException()
    {
        var reg = CreateRegWithKomponen("REG-004");
        var tataRekening = HydrateOpened(reg.RegId, CreateMinimalBill(reg.RegId, 10_000m));
        tataRekening.Close();
        TataRekeningDomainTestHelper.VerifyAndAllocate(
            tataRekening,
            [new TataRekeningPaymentType(PaymentType.ByKas, 10_000m, 0m, CoaType.Default)]);
        tataRekening.FinalizeFinancialResponsibility("kasir", DateTime.Now);

        Action act = () => _sut.FromReg(
            tataRekening,
            reg,
            KarcisType.Default,
            JaminanType.Default,
            PpaType.Default,
            []);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*berstatus OPEN*");
    }

    [Fact]
    public void UT05_GivenLunasTataRekening_WhenCreateFromRegistration_ThenShouldThrowInvalidOperationException()
    {
        var reg = CreateRegWithKomponen("REG-005");
        var tataRekening = HydrateOpened(reg.RegId, CreateMinimalBill(reg.RegId, 10_000m));
        tataRekening.Close();
        TataRekeningDomainTestHelper.VerifyAndAllocate(
            tataRekening,
            [new TataRekeningPaymentType(PaymentType.ByKas, 10_000m, 0m, CoaType.Default)]);
        tataRekening.FinalizeFinancialResponsibility("kasir", DateTime.Now);
        tataRekening.Pay(
            [new TataRekeningPaymentType(PaymentType.ByKas, 10_000m, 0m, CoaType.Default)],
            "PAY-001",
            DateTime.Now);

        Action act = () => _sut.FromReg(
            tataRekening,
            reg,
            KarcisType.Default,
            JaminanType.Default,
            PpaType.Default,
            []);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*berstatus OPEN*");
    }

    [Fact]
    public void UT06_GivenDifferentRegistration_WhenCreateFromRegistration_ThenShouldThrowArgumentException()
    {
        var reg = CreateRegWithKomponen("REG-003");
        var tataRekening = TataRekeningModel.Create("REG-LAIN");

        Action act = () => _sut.FromReg(
            tataRekening,
            reg,
            KarcisType.Default,
            JaminanType.Default,
            PpaType.Default,
            []);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("tataRekening");
    }

    private static TataRekeningModel HydrateOpened(string regId, params TrsBillType[] listTrsBill) =>
        new(regId, TataRekeningStatusEnum.Opened, TataRekeningFinalizationType.Default, [], listTrsBill);

    private static RegModel CreateRegWithKomponen(string regId)
    {
        var komponen = new RegKomponenType(
            new KomponenReff("KOMP-01", "Komponen Test"),
            PpaType.Default.ToReff(),
            15000m,
            1000m);

        return new RegModel(
            regId,
            new DateOnly(2026, 6, 14),
            new AuditInfoType("tester", new DateTime(2026, 6, 14, 8, 0, 0)),
            AuditInfoType.Default,
            AuditInfoType.Default,
            AuditInfoType.Default,
            JenisRegEnum.RegJalan,
            PasienModel.Default.ToReff(),
            TipeJaminanType.Default.ToReff(),
            PolisModel.Default.ToReff(),
            KelasType.Default.ToReff(),
            CaraMasukDkType.Default,
            RujukanType.Default.ToReff(),
            PpaType.Default.ToReff(),
            LayananType.Default.ToReff(),
            KarcisType.Default.ToReff(),
            RegEligibilityType.Default,
            [komponen]);
    }

    private static TrsBill2CoaType ValidPdpCoa => new(
        new CoaType("PPDP-01", ""),
        new CoaType("PDPT-01", ""),
        CoaType.Default,
        CoaType.Default,
        CoaType.Default,
        CoaType.Default);

    private static TrsBillType CreateMinimalBill(string billId, decimal amount)
    {
        var komponen = new TrsBill2KomponenType("KOMP-01", "Komponen Test");
        var trans = TrsBill2TransEventType.Create(
            0,
            komponen,
            TrsBillJenisBayarType.Pdp,
            amount,
            PpaType.Default.ToReff(),
            ValidPdpCoa);

        return new TrsBillType(
            billId,
            BillModulGroup.Jasa,
            new DateTime(2026, 6, 16),
            new RegReff(billId, "-", "-"),
            LayananType.Default.ToReff(),
            KelasType.Default.ToReff(),
            AuditInfoType.Default,
            RekapCetakType.Default.ToReff(),
            new TrsBillNilaiType(amount, 0, 0, 0),
            TrsBillKetType.Default,
            [trans],
            [],
            []);
    }

    [Fact]
    public void GivenOpenedTataRekening_WhenFromPenjualanItem_ThenCreatesObatBillWithGroupRekAndCoa()
    {
        var reg = CreateRegWithKomponen("REG-PENJ-01");
        var tataRekening = TataRekeningModel.Create(reg.RegId);
        var layanan = new LayananType("LY01", "Poli Penyakit Dalam", true,
            InstalasiType.Default.ToReff(), LayananDkType.Default.ToReff(),
            TipeLayananDkType.Default, InstalasiDkType.Default,
            Bilreg.Domain.AccountingContext.UnitFeature.UnitType.Default.ToReff(),
            new PoliBpjsReff("-", "-"));

        var penjualan = PenjualanModel.CreateDirectReg(
            reg, layanan, TipeBrgType.Default.ToReff(), "USER1", new DateTime(2026, 6, 15, 10, 0, 0));

        var brg = BrgObatType.Default with { BrgId = "BRG01", BrgName = "Amoxicillin 500mg" };
        var satuan = SatuanType.Create("TAB", "Tablet");
        var etiket = EtiketType.Default;
        // Qty: 10, Harga: 1000, Diskon: 500, Biaya/Embalase: 200, Tax: 100 -> SubTotal: 9700, Total: 9800
        var nilai = NilaiItemType.Create(10, 1000, 500, 200, 1, 100, 0, 0, 0);
        penjualan.AddItem(brg, satuan, 10, etiket, nilai);
        var item = penjualan.ListItem.Single();

        var jaminan = JaminanType.Default with
        {
            JaminanId = reg.TipeJaminan.TipeJaminanId.Length >= 3 ? reg.TipeJaminan.TipeJaminanId[..3] : reg.TipeJaminan.TipeJaminanId,
            Rekening = new JaminanRekeningType(
                new CoaType("JASA-RJ", ""), new CoaType("PPDP-OBAT-RJ", ""),
                new CoaType("JASA-RI", ""), new CoaType("PPDP-OBAT-RI", ""))
        };

        var groupRek = new GroupRekType(
            "GR01", "Obat Paten", GroupRekDkType.Default, "RK01",
            new CoaType("REK-JUAL", ""),
            new CoaType("REK-JUAL-RJ", ""),
            new CoaType("REK-DISC", ""),
            new CoaType("REK-TAX", ""),
            new CoaType("REK-PERSED", ""));

        var result = _sut.FromPenjualanItem(tataRekening, penjualan, item, reg, jaminan, groupRek);

        result.Should().NotBeNull();
        result.ModulGroup.Should().Be(BillModulGroup.Obat);
        result.TrsBillingId.Should().Be(item.PenjualanItemId);
        result.TrsBillingId.Should().Be($"{penjualan.PenjualanId}-01");
        result.Keterangan.TrsMainId.Should().Be(penjualan.PenjualanId);
        result.Keterangan.Keterangan.Should().Be("[GR01] Amoxicillin 500mg 10 Tablet");
        result.Keterangan.RefBiaya.Should().Be("BRG01");
        result.RekapCetak.RekapCetakId.Should().Be("RK01");
        result.Nilai.Total.Should().Be(item.Nilai.Total);

        result.ListTransaction.Should().HaveCount(2);
        var pdpEvent = result.ListTransaction.First();
        pdpEvent.JenisBayar.Should().Be(TrsBillJenisBayarType.Pdp);
        pdpEvent.Nilai.Should().Be(item.Nilai.SubTotal);
        pdpEvent.Coa.Ppdp.CoaId.Should().Be("PPDP-OBAT-RJ");
        pdpEvent.Coa.Pdpt.CoaId.Should().Be("REK-JUAL-RJ");
        pdpEvent.Coa.Persediaan.CoaId.Should().Be("REK-PERSED");
        pdpEvent.Coa.Disc.CoaId.Should().Be("REK-DISC");

        var taxEvent = result.ListTransaction.Last();
        taxEvent.JenisBayar.Should().Be(TrsBillJenisBayarType.Tax);
        taxEvent.Nilai.Should().Be(item.Nilai.Tax);
        taxEvent.Coa.Pdpt.CoaId.Should().Be("REK-JUAL-RJ");
        taxEvent.Coa.Tax.CoaId.Should().Be("REK-TAX");
    }

    [Fact]
    public void GivenClosedTataRekening_WhenFromPenjualanItem_ThenThrowsInvalidOperationException()
    {
        var reg = CreateRegWithKomponen("REG-PENJ-02");
        var tataRekening = TataRekeningModel.Create(reg.RegId);
        tataRekening.Close();

        var layanan = new LayananType("LY01", "Poli", true,
            InstalasiType.Default.ToReff(), LayananDkType.Default.ToReff(),
            TipeLayananDkType.Default, InstalasiDkType.Default,
            Bilreg.Domain.AccountingContext.UnitFeature.UnitType.Default.ToReff(),
            new PoliBpjsReff("-", "-"));

        var penjualan = PenjualanModel.CreateDirectReg(reg, layanan, TipeBrgType.Default.ToReff(), "U1", DateTime.Now);
        penjualan.AddItem(BrgObatType.Default with { BrgId = "B01" }, SatuanType.Create("TAB", "Tab"), 1, EtiketType.Default, NilaiItemType.Default);
        var item = penjualan.ListItem.Single();

        var jaminan = JaminanType.Default with { JaminanId = reg.TipeJaminan.TipeJaminanId.Length >= 3 ? reg.TipeJaminan.TipeJaminanId[..3] : reg.TipeJaminan.TipeJaminanId };
        var groupRek = new GroupRekType("G1", "Obat", GroupRekDkType.Default);

        Action act = () => _sut.FromPenjualanItem(tataRekening, penjualan, item, reg, jaminan, groupRek);

        act.Should().Throw<InvalidOperationException>().WithMessage("*OPEN*");
    }

    [Fact]
    public void GivenDifferentReg_WhenFromPenjualanItem_ThenThrowsArgumentException()
    {
        var reg = CreateRegWithKomponen("REG-PENJ-03");
        var tataRekening = TataRekeningModel.Create("REG-DIFF");

        var layanan = new LayananType("LY01", "Poli", true,
            InstalasiType.Default.ToReff(), LayananDkType.Default.ToReff(),
            TipeLayananDkType.Default, InstalasiDkType.Default,
            Bilreg.Domain.AccountingContext.UnitFeature.UnitType.Default.ToReff(),
            new PoliBpjsReff("-", "-"));

        var penjualan = PenjualanModel.CreateDirectReg(reg, layanan, TipeBrgType.Default.ToReff(), "U1", DateTime.Now);
        penjualan.AddItem(BrgObatType.Default with { BrgId = "B01" }, SatuanType.Create("TAB", "Tab"), 1, EtiketType.Default, NilaiItemType.Default);
        var item = penjualan.ListItem.Single();

        var jaminan = JaminanType.Default with { JaminanId = reg.TipeJaminan.TipeJaminanId.Length >= 3 ? reg.TipeJaminan.TipeJaminanId[..3] : reg.TipeJaminan.TipeJaminanId };
        var groupRek = new GroupRekType("G1", "Obat", GroupRekDkType.Default);

        Action act = () => _sut.FromPenjualanItem(tataRekening, penjualan, item, reg, jaminan, groupRek);

        act.Should().Throw<ArgumentException>().WithParameterName("tataRekening");
    }
}
