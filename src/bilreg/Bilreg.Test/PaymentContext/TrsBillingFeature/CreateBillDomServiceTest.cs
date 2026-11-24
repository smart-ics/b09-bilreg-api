using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.BedUsageContext.BhpRanapFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.BrgContext.PricingPolicyFeature;
using Bilreg.Domain.ChargeContext.AmbulanceFeature;
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

    [Fact]
    public void GivenOpenedTataRekening_WhenFromAmbulanceUsage_ThenCreatesBillWithMappedItemsAndCoa()
    {
        var reg = CreateRegWithKomponen("REG-AMB-01");
        var tataRekening = TataRekeningModel.Create(reg.RegId);

        var transport = TrsTransportModel.Create(
            TransportTypeEnum.OutboundReferral,
            ChargingMethodEnum.Wilayah,
            "IGD",
            "USER1",
            "TP001",
            new DateTime(2026, 6, 15, 10, 0, 0),
            regId: reg.RegId,
            tujuanTransportId: "TJ01",
            konstanta: 1.5m,
            lokasiAsal: "RS",
            lokasiTujuan: "RS Rujukan",
            keterangan: "Antar Pasien Rujukan");

        var item1 = new TrsTransportItemModel("TP001", "KOMP-01", 100_000m, 1m, "DOKTER1", 10_000m);
        var item2 = new TrsTransportItemModel("TP001", "KOMP-02", 50_000m, 2m, "PERAWAT1", 0m);
        transport.AddItem(item1);
        transport.AddItem(item2);

        var jaminan = JaminanType.Default with
        {
            JaminanId = reg.TipeJaminan.TipeJaminanId.Length >= 3 ? reg.TipeJaminan.TipeJaminanId[..3] : reg.TipeJaminan.TipeJaminanId,
            Rekening = new JaminanRekeningType(
                new CoaType("PPDP-RAJAL", ""), new CoaType("PPDP-OBAT-RAJAL", ""),
                new CoaType("PPDP-RANAP", ""), new CoaType("PPDP-OBAT-RANAP", ""))
        };

        var groupKomp = GroupKomponenType.Default;
        var listReffKomp = new List<KomponenType>
        {
            new("KOMP-01", "Sewa Ambulans", groupKomp, new CoaType("PDPT-01", ""), new CoaType("DISC-01", ""), []),
            new("KOMP-02", "Jasa Pendamping", groupKomp, new CoaType("PDPT-02", ""), new CoaType("DISC-02", ""), [])
        };

        var rekapCetak = RekapCetakType.Default with { RekapCetakId = "RK-AMB" };

        var result = _sut.FromAmbulanceUsage(tataRekening, transport, reg, jaminan, listReffKomp, rekapCetak);

        result.Should().NotBeNull();
        result.TrsBillingId.Should().Be("TP001");
        result.ModulGroup.Should().Be(BillModulGroup.Jasa);
        result.Reg.RegId.Should().Be(reg.RegId);
        result.Keterangan.TrsMainId.Should().Be("TP001");
        result.Keterangan.Keterangan.Should().Be("Antar Pasien Rujukan");
        result.Keterangan.Keterangan2.Should().Be("RS Rujukan");
        result.RekapCetak.RekapCetakId.Should().Be("RK-AMB");

        result.Nilai.SubTotal.Should().Be(200_000m);
        result.Nilai.Diskon.Should().Be(10_000m);
        result.Nilai.Total.Should().Be(190_000m);

        result.ListTransaction.Should().HaveCount(2);

        var ev1 = result.ListTransaction.First();
        ev1.Komponen.BillKompId.Should().Be("KOMP-01");
        ev1.Komponen.BillKompName.Should().Be("Sewa Ambulans");
        ev1.Nilai.Should().Be(90_000m);
        ev1.PetugasMedis.PpaId.Should().Be("DOKTER1");
        ev1.Coa.Ppdp.CoaId.Should().Be("PPDP-RAJAL");
        ev1.Coa.Pdpt.CoaId.Should().Be("PDPT-01");
        ev1.Coa.Disc.CoaId.Should().Be("DISC-01");

        var ev2 = result.ListTransaction.Last();
        ev2.Komponen.BillKompId.Should().Be("KOMP-02");
        ev2.Komponen.BillKompName.Should().Be("Jasa Pendamping");
        ev2.Nilai.Should().Be(100_000m);
        ev2.PetugasMedis.PpaId.Should().Be("PERAWAT1");
        ev2.Coa.Ppdp.CoaId.Should().Be("PPDP-RAJAL");
        ev2.Coa.Pdpt.CoaId.Should().Be("PDPT-02");
    }

    [Fact]
    public void GivenCancelledTransportWithStandbyFee_WhenFromAmbulanceUsage_ThenCreatesBillWithStandbyItem()
    {
        var reg = CreateRegWithKomponen("REG-AMB-02");
        var tataRekening = TataRekeningModel.Create(reg.RegId);

        var transport = TrsTransportModel.Create(
            TransportTypeEnum.OutboundReferral,
            ChargingMethodEnum.Wilayah,
            "IGD",
            "USER1",
            "TP002",
            DateTime.Now,
            regId: reg.RegId,
            tujuanTransportId: "TJ01",
            konstanta: 1m);

        transport.Dispatch("AMB01", "DRV01", "B1234XYZ", 100m);
        transport.Cancel("Dibatalkan keluarga", biayaBatal: 75_000m);

        var jaminan = JaminanType.Default with
        {
            JaminanId = reg.TipeJaminan.TipeJaminanId.Length >= 3 ? reg.TipeJaminan.TipeJaminanId[..3] : reg.TipeJaminan.TipeJaminanId,
            Rekening = new JaminanRekeningType(
                new CoaType("PPDP-RAJAL", ""), new CoaType("PPDP-OBAT-RAJAL", ""),
                new CoaType("PPDP-RANAP", ""), new CoaType("PPDP-OBAT-RANAP", ""))
        };

        var result = _sut.FromAmbulanceUsage(tataRekening, transport, reg, jaminan, []);

        result.Should().NotBeNull();
        result.Nilai.Total.Should().Be(75_000m);
        result.ListTransaction.Should().HaveCount(1);
        result.ListTransaction.First().Komponen.BillKompId.Should().Be("STANDBY");
        result.ListTransaction.First().Nilai.Should().Be(75_000m);
    }

    [Fact]
    public void GivenClosedTataRekening_WhenFromAmbulanceUsage_ThenThrowsInvalidOperationException()
    {
        var reg = CreateRegWithKomponen("REG-AMB-03");
        var tataRekening = TataRekeningModel.Create(reg.RegId);
        tataRekening.Close();

        var transport = TrsTransportModel.Create(
            TransportTypeEnum.OutboundReferral,
            ChargingMethodEnum.Wilayah,
            "IGD",
            "USER1",
            "TP003",
            DateTime.Now,
            regId: reg.RegId,
            tujuanTransportId: "TJ01");

        var jaminan = JaminanType.Default with
        {
            JaminanId = reg.TipeJaminan.TipeJaminanId.Length >= 3 ? reg.TipeJaminan.TipeJaminanId[..3] : reg.TipeJaminan.TipeJaminanId
        };

        Action act = () => _sut.FromAmbulanceUsage(tataRekening, transport, reg, jaminan, []);

        act.Should().Throw<InvalidOperationException>().WithMessage("*OPEN*");
    }

    [Fact]
    public void GivenDifferentReg_WhenFromAmbulanceUsage_ThenThrowsArgumentException()
    {
        var reg = CreateRegWithKomponen("REG-AMB-04");
        var tataRekening = TataRekeningModel.Create("REG-OTHER");

        var transport = TrsTransportModel.Create(
            TransportTypeEnum.OutboundReferral,
            ChargingMethodEnum.Wilayah,
            "IGD",
            "USER1",
            "TP004",
            DateTime.Now,
            regId: reg.RegId,
            tujuanTransportId: "TJ01");

        var jaminan = JaminanType.Default with
        {
            JaminanId = reg.TipeJaminan.TipeJaminanId.Length >= 3 ? reg.TipeJaminan.TipeJaminanId[..3] : reg.TipeJaminan.TipeJaminanId
        };

        Action act = () => _sut.FromAmbulanceUsage(tataRekening, transport, reg, jaminan, []);

        act.Should().Throw<ArgumentException>().WithParameterName("tataRekening");
    }

    [Fact]
    public void GivenNonRegAmbulance_WhenFromAmbulanceUsage_ThenThrowsArgumentException()
    {
        var reg = CreateRegWithKomponen("REG-AMB-05");
        var tataRekening = TataRekeningModel.Create(reg.RegId);

        var transport = TrsTransportModel.Create(
            TransportTypeEnum.OutboundReferral,
            ChargingMethodEnum.Wilayah,
            "IGD",
            "USER1",
            "TP005",
            DateTime.Now,
            regId: "",
            tujuanTransportId: "TJ01");

        var jaminan = JaminanType.Default with
        {
            JaminanId = reg.TipeJaminan.TipeJaminanId.Length >= 3 ? reg.TipeJaminan.TipeJaminanId[..3] : reg.TipeJaminan.TipeJaminanId
        };

        Action act = () => _sut.FromAmbulanceUsage(tataRekening, transport, reg, jaminan, []);

        act.Should().Throw<ArgumentException>().WithMessage("*non-registrasi*");
    }

    [Fact]
    public void GivenMismatchedRegId_WhenFromAmbulanceUsage_ThenThrowsArgumentException()
    {
        var reg = CreateRegWithKomponen("REG-AMB-06");
        var tataRekening = TataRekeningModel.Create(reg.RegId);

        var transport = TrsTransportModel.Create(
            TransportTypeEnum.OutboundReferral,
            ChargingMethodEnum.Wilayah,
            "IGD",
            "USER1",
            "TP006",
            DateTime.Now,
            regId: "REG-MISMATCH",
            tujuanTransportId: "TJ01");

        var jaminan = JaminanType.Default with
        {
            JaminanId = reg.TipeJaminan.TipeJaminanId.Length >= 3 ? reg.TipeJaminan.TipeJaminanId[..3] : reg.TipeJaminan.TipeJaminanId
        };

        Action act = () => _sut.FromAmbulanceUsage(tataRekening, transport, reg, jaminan, []);

        act.Should().Throw<ArgumentException>().WithMessage("*tidak sesuai dengan registrasi*");
    }

    [Fact]
    public void GivenOpenedTataRekening_WhenFromBhpRanap_ThenCreatesObatBillWithGroupRekAndCoa()
    {
        var reg = CreateInpatientReg("REG-BHP-01");
        var tataRekening = TataRekeningModel.Create(reg.RegId);
        var layanan = new LayananType("LY-MELATI", "Bangsal Melati", true,
            InstalasiType.Default.ToReff(), LayananDkType.Default.ToReff(),
            TipeLayananDkType.Default, InstalasiDkType.Default,
            Bilreg.Domain.AccountingContext.UnitFeature.UnitType.Default.ToReff(),
            new PoliBpjsReff("-", "-"));

        var audit = new AuditInfoType("nurse1", new DateTime(2026, 6, 16, 9, 0, 0));
        var bhp = BhpRanapModel.Create(
            pakaiBedId: "BED-01",
            regId: reg.RegId,
            layananId: layanan.LayananId,
            bhpItemId: "BHP-01",
            bhpItemName: "Infus Set Makro",
            qty: 2,
            price: 25000m,
            audit: audit);

        var jaminan = JaminanType.Default with
        {
            JaminanId = reg.TipeJaminan.TipeJaminanId.Length >= 3 ? reg.TipeJaminan.TipeJaminanId[..3] : reg.TipeJaminan.TipeJaminanId,
            Rekening = new JaminanRekeningType(
                new CoaType("JASA-RJ", ""), new CoaType("PPDP-OBAT-RJ", ""),
                new CoaType("JASA-RI", ""), new CoaType("PPDP-OBAT-RI", ""))
        };

        var groupRek = new GroupRekType(
            "GR-BHP", "BHP Medis", GroupRekDkType.Default, "RK01",
            new CoaType("REK-JUAL-BHP", ""),
            new CoaType("REK-JUAL-RJ-BHP", ""),
            new CoaType("REK-DISC", ""),
            new CoaType("REK-TAX", ""),
            new CoaType("REK-PERSED-BHP", ""));

        var result = _sut.FromBhpRanap(tataRekening, bhp, reg, layanan, jaminan, groupRek);

        result.Should().NotBeNull();
        result.ModulGroup.Should().Be(BillModulGroup.Obat);
        result.TrsBillingId.Should().Be(bhp.BhpRanapId);
        result.Keterangan.TrsMainId.Should().Be(bhp.BhpRanapId);
        result.Keterangan.RefBiaya.Should().Be("BHP-01");
        result.Keterangan.Keterangan.Should().Be("[GR-BHP] Infus Set Makro 2");
        result.RekapCetak.RekapCetakId.Should().Be("RK01");
        result.Nilai.Total.Should().Be(50000m);

        result.ListTransaction.Should().HaveCount(1);
        var pdpEvent = result.ListTransaction.First();
        pdpEvent.JenisBayar.Should().Be(TrsBillJenisBayarType.Pdp);
        pdpEvent.Nilai.Should().Be(50000m);
        pdpEvent.Coa.Ppdp.CoaId.Should().Be("PPDP-OBAT-RI");
        pdpEvent.Coa.Pdpt.CoaId.Should().Be("REK-JUAL-BHP");
        pdpEvent.Coa.Persediaan.CoaId.Should().Be("REK-PERSED-BHP");
        pdpEvent.Coa.Disc.CoaId.Should().Be("REK-DISC");
    }

    [Fact]
    public void GivenClosedTataRekening_WhenFromBhpRanap_ThenThrowsInvalidOperationException()
    {
        var reg = CreateInpatientReg("REG-BHP-02");
        var tataRekening = TataRekeningModel.Create(reg.RegId);
        tataRekening.Close();

        var layanan = new LayananType("LY-MELATI", "Bangsal Melati", true,
            InstalasiType.Default.ToReff(), LayananDkType.Default.ToReff(),
            TipeLayananDkType.Default, InstalasiDkType.Default,
            Bilreg.Domain.AccountingContext.UnitFeature.UnitType.Default.ToReff(),
            new PoliBpjsReff("-", "-"));

        var audit = new AuditInfoType("nurse1", DateTime.Now);
        var bhp = BhpRanapModel.Create("BED-01", reg.RegId, layanan.LayananId, "BHP-01", "Spuit", 1, 5000m, audit);

        var jaminan = JaminanType.Default with { JaminanId = reg.TipeJaminan.TipeJaminanId.Length >= 3 ? reg.TipeJaminan.TipeJaminanId[..3] : reg.TipeJaminan.TipeJaminanId };
        var groupRek = new GroupRekType("G1", "BHP", GroupRekDkType.Default);

        Action act = () => _sut.FromBhpRanap(tataRekening, bhp, reg, layanan, jaminan, groupRek);

        act.Should().Throw<InvalidOperationException>().WithMessage("*OPEN*");
    }

    [Fact]
    public void GivenDifferentReg_WhenFromBhpRanap_ThenThrowsArgumentException()
    {
        var reg = CreateInpatientReg("REG-BHP-03");
        var tataRekening = TataRekeningModel.Create("REG-DIFF");

        var layanan = new LayananType("LY-MELATI", "Bangsal Melati", true,
            InstalasiType.Default.ToReff(), LayananDkType.Default.ToReff(),
            TipeLayananDkType.Default, InstalasiDkType.Default,
            Bilreg.Domain.AccountingContext.UnitFeature.UnitType.Default.ToReff(),
            new PoliBpjsReff("-", "-"));

        var audit = new AuditInfoType("nurse1", DateTime.Now);
        var bhp = BhpRanapModel.Create("BED-01", reg.RegId, layanan.LayananId, "BHP-01", "Spuit", 1, 5000m, audit);

        var jaminan = JaminanType.Default with { JaminanId = reg.TipeJaminan.TipeJaminanId.Length >= 3 ? reg.TipeJaminan.TipeJaminanId[..3] : reg.TipeJaminan.TipeJaminanId };
        var groupRek = new GroupRekType("G1", "BHP", GroupRekDkType.Default);

        Action act = () => _sut.FromBhpRanap(tataRekening, bhp, reg, layanan, jaminan, groupRek);

        act.Should().Throw<ArgumentException>().WithParameterName("tataRekening");
    }

    private static RegModel CreateInpatientReg(string regId)
    {
        return new RegModel(
            regId,
            new DateOnly(2026, 6, 16),
            new AuditInfoType("tester", new DateTime(2026, 6, 16, 8, 0, 0)),
            AuditInfoType.Default,
            AuditInfoType.Default,
            AuditInfoType.Default,
            JenisRegEnum.RegInap,
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
    }
}
