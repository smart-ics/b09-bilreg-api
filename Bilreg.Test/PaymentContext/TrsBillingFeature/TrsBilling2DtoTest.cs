using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
using Bilreg.Domain.PaymentContext.TrsBillingFeature;
using Bilreg.Infrastructure.PaymentContext.TrsBillingFeature;
using FluentAssertions;

namespace Bilreg.Test.PaymentContext.TrsBillingFeature;

public class TrsBilling2DtoTest
{
    private const string BillingId = "BIL-100";
    private const string RegId = "REG-00001234";

    private static TrsBill2KomponenType JasaKomponen() => new("DT-001", "Detil Tarif A");

    private static TrsBill2CoaType SampleCoa() => new(
        new CoaType("PPDP-01", ""),
        new CoaType("PDPT-01", ""),
        CoaType.Default,
        CoaType.Default,
        CoaType.Default,
        CoaType.Default);

    [Fact]
    public void FromModelTrans_JasaModul_MapsKomponenToDetilTarif()
    {
        var model = new TrsBill2TransEventType(
            1,
            JasaKomponen(),
            TrsBillJenisBayarType.Pdp,
            75_000.50m,
            new PpaReff("MED-01", "Dr. Satu"),
            SampleCoa());

        var dto = TaTrsBilling2Dto.FromModelTrans(model, BillingId, modul: 0);

        dto.fs_kd_trs.Should().Be(BillingId);
        dto.fn_no_urut.Should().Be(1);
        dto.fs_kd_jenis_bayar.Should().Be("PDP");
        dto.fn_trs_p.Should().Be(75_000.50m);
        dto.fn_trs_n.Should().Be(0m);
        dto.fs_kd_trs_bayar.Should().Be(BillingId);
        dto.fd_tgl_bayar.Should().Be("3000-01-01");
        dto.fs_jam_bayar.Should().Be("00:00:00");
        dto.fs_kd_petugas_kasir.Should().Be("");
        dto.fs_kd_petugas_medis.Should().Be("MED-01");
        dto.fs_kd_detil_tarif.Should().Be("DT-001");
        dto.fs_kd_grup_rek.Should().Be("");
        dto.fs_nm_detil_tarif.Should().Be("Detil Tarif A");
        dto.fs_nm_grup_rek.Should().Be("");
        dto.fs_nm_peg_medis.Should().Be("Dr. Satu");
        dto.fs_kd_rek_ppdp.Should().Be("PPDP-01");
        dto.fs_kd_rek_pdpt.Should().Be("PDPT-01");
    }

    [Fact]
    public void FromModelTrans_ObatModul_MapsKomponenToGrupRek()
    {
        var komponen = new TrsBill2KomponenType("GR-01", "Grup Rek A");
        var model = new TrsBill2TransEventType(
            2,
            komponen,
            TrsBillJenisBayarType.Byl,
            1_000m,
            new PpaReff("MED-02", "Dr. Dua"),
            SampleCoa());

        var dto = TaTrsBilling2Dto.FromModelTrans(model, BillingId, modul: 1);

        dto.fs_kd_detil_tarif.Should().Be("");
        dto.fs_kd_grup_rek.Should().Be("GR-01");
        dto.fs_nm_detil_tarif.Should().Be("");
        dto.fs_nm_grup_rek.Should().Be("Grup Rek A");
    }

    [Fact]
    public void FromModelDischarge_MapsPaymentKasirAndNilai()
    {
        var tglBayar = new DateTime(2026, 6, 17, 15, 45, 0);
        var model = new TrsBill2DischargeEventType(
            3,
            JasaKomponen(),
            TrsBillJenisBayarType.Kas,
            30_000m,
            new PpaReff("MED-03", "Dr. Tiga"),
            "KSR-99",
            "IGNORED",
            tglBayar);

        var dto = TaTrsBilling2Dto.FromModelDischarge(model, BillingId, modul: 0, RegId);

        dto.fs_kd_trs.Should().Be(BillingId);
        dto.fn_no_urut.Should().Be(3);
        dto.fs_kd_jenis_bayar.Should().Be("KAS");
        dto.fn_trs_p.Should().Be(0m);
        dto.fn_trs_n.Should().Be(30_000m);
        dto.fs_kd_trs_bayar.Should().Be("RO00001234");
        dto.fd_tgl_bayar.Should().Be(tglBayar.ToString("yyyy-mm-dd"));
        dto.fs_jam_bayar.Should().Be("15:45:00");
        dto.fs_kd_petugas_kasir.Should().Be("KSR-99");
        dto.fs_kd_petugas_medis.Should().Be("MED-03");
        dto.fs_kd_detil_tarif.Should().Be("DT-001");
        dto.fs_nm_peg_kasir.Should().Be("KSR-99");
        dto.fs_nm_peg_medis.Should().Be("Dr. Tiga");
    }

    [Fact]
    public void FromModelPayment_ReturnsPositiveEntryAndKasCounterEntry()
    {
        var tglBayar = new DateTime(2026, 6, 18, 9, 0, 0);
        var model = new TrsBill2PaymentEventType(
            4,
            JasaKomponen(),
            TrsBillJenisBayarType.Hut,
            new PaymentType("PAY-001", "Hutang", false),
            20_000m,
            tglBayar,
            new PpaReff("MED-04", "Dr. Empat"));

        var (positive, counter) = TaTrsBilling2Dto.FromModelPayment(
            model, BillingId, modul: 0, paymentId: "PAY-001");

        positive.fs_kd_trs.Should().Be(BillingId);
        positive.fn_no_urut.Should().Be(4);
        positive.fs_kd_jenis_bayar.Should().Be("HUT");
        positive.fn_trs_p.Should().Be(0m);
        positive.fn_trs_n.Should().Be(20_000m);
        positive.fs_kd_trs_bayar.Should().Be("PAY-001");
        positive.fd_tgl_bayar.Should().Be(tglBayar.ToString("yyyy-mm-dd"));
        positive.fs_jam_bayar.Should().Be("09:00:00");
        positive.fs_kd_petugas_medis.Should().Be("MED-04");

        counter.fs_kd_jenis_bayar.Should().Be("KAS");
        counter.fn_trs_p.Should().Be(0m);
        counter.fn_trs_n.Should().Be(20_000m);
        counter.fs_kd_trs_bayar.Should().Be("PAY-001");
    }

    [Fact]
    public void ToModel_TransDto_MapsToTransEvent()
    {
        var dto = new TaTrsBilling2Dto(
            BillingId, 1,
            "PDP", 50_000m, 0m,
            BillingId, "3000-01-01", "00:00:00",
            "", "MED-01",
            "DT-001", "",
            "PPDP-01", "PDPT-01", "", "", "", "", "",
            "Detil Tarif A", "", "", "Dr. Satu");

        var result = dto.ToModel(modul: 0);

        result.Should().BeOfType<TrsBill2TransEventType>();
        var trans = (TrsBill2TransEventType)result;
        trans.NoUrut.Should().Be(1);
        trans.JenisBayar.JenisBayarId.Should().Be("PDP");
        trans.Nilai.Should().Be(50_000m);
        trans.Komponen.BillKompId.Should().Be("DT-001");
        trans.PetugasMedis.PpaId.Should().Be("MED-01");
        trans.Coa.Ppdp.CoaId.Should().Be("PPDP-01");
        trans.Coa.Pdpt.CoaId.Should().Be("PDPT-01");
    }

    [Fact]
    public void ToModel_DischargeDto_MapsToDischargeEvent()
    {
        var dto = new TaTrsBilling2Dto(
            BillingId, 2,
            "KAS", 0m, 25_000m,
            "RO00001234", "2026-06-17", "15:45:00",
            "KSR-01", "MED-02",
            "DT-002", "",
            "", "", "", "", "", "", "",
            "Detil B", "", "KSR-01", "Dr. Dua");

        var result = dto.ToModel(modul: 0);

        result.Should().BeOfType<TrsBill2DischargeEventType>();
        var discharge = (TrsBill2DischargeEventType)result;
        discharge.NoUrut.Should().Be(2);
        discharge.JenisBayar.JenisBayarId.Should().Be("KAS");
        discharge.Nilai.Should().Be(25_000m);
        discharge.PetugasKasir.Should().Be("KSR-01");
        discharge.TrsBayarId.Should().Be("RO00001234");
        discharge.TglBayar.Should().Be(new DateTime(2026, 6, 17));
        discharge.Komponen.BillKompId.Should().Be("DT-002");
        discharge.PetugasMedis.PpaId.Should().Be("MED-02");
    }

    [Fact]
    public void ToModel_PaymentDto_MapsToPaymentEvent()
    {
        var dto = new TaTrsBilling2Dto(
            BillingId, 3,
            "HUT", 15_000m, 0m,
            "PAY-777", "2026-06-18", "09:00:00",
            "", "MED-05",
            "DT-003", "",
            "", "", "", "", "", "", "",
            "Detil C", "", "", "Dr. Lima");

        var result = dto.ToModel(modul: 0);

        result.Should().BeOfType<TrsBill2PaymentEventType>();
        var payment = (TrsBill2PaymentEventType)result;
        payment.NoUrut.Should().Be(3);
        payment.JenisBayar.JenisBayarId.Should().Be("HUT");
        payment.Nilai.Should().Be(15_000m);
        payment.Payment.PaymentId.Should().Be("PAY-777");
        payment.TglBayar.Should().Be(new DateTime(2026, 6, 18));
        payment.Komponen.BillKompId.Should().Be("DT-003");
    }

    [Fact]
    public void ToModel_ObatModul_UsesGrupRekForKomponen()
    {
        var dto = new TaTrsBilling2Dto(
            BillingId, 1,
            "PDP", 10_000m, 0m,
            BillingId, "3000-01-01", "00:00:00",
            "", "MED-01",
            "", "GR-99",
            "", "", "", "", "", "", "",
            "", "Grup Rek", "", "");

        var result = dto.ToModel(modul: 1);

        result.Should().BeOfType<TrsBill2TransEventType>();
        ((TrsBill2TransEventType)result).Komponen.BillKompId.Should().Be("GR-99");
    }

    [Fact]
    public void RoundTrip_Trans_FromModelTransThenToModel_PreservesBusinessFields()
    {
        var source = new TrsBill2TransEventType(
            5,
            JasaKomponen(),
            TrsBillJenisBayarType.Tax,
            9_999.99m,
            new PpaReff("MED-06", "Dr. Enam"),
            SampleCoa());

        var dto = TaTrsBilling2Dto.FromModelTrans(source, BillingId, modul: 0);
        var roundTripped = (TrsBill2TransEventType)dto.ToModel(modul: 0);

        roundTripped.NoUrut.Should().Be(source.NoUrut);
        roundTripped.JenisBayar.JenisBayarId.Should().Be(source.JenisBayar.JenisBayarId);
        roundTripped.Nilai.Should().Be(source.Nilai);
        roundTripped.Komponen.BillKompId.Should().Be(source.Komponen.BillKompId);
        roundTripped.PetugasMedis.PpaId.Should().Be(source.PetugasMedis.PpaId);
        roundTripped.Coa.Ppdp.CoaId.Should().Be(source.Coa.Ppdp.CoaId);
        roundTripped.Coa.Pdpt.CoaId.Should().Be(source.Coa.Pdpt.CoaId);
    }

    [Fact]
    public void FromModelTrans_ZeroNilai_PreservesZero()
    {
        var model = new TrsBill2TransEventType(
            1,
            JasaKomponen(),
            TrsBillJenisBayarType.Pdp,
            0m,
            PpaType.Default.ToReff(),
            SampleCoa());

        var dto = TaTrsBilling2Dto.FromModelTrans(model, BillingId, modul: 0);

        dto.fn_trs_p.Should().Be(0m);
        dto.fn_trs_n.Should().Be(0m);
    }

    [Fact]
    public void ToModel_KasCounterEntry_ThrowsArgumentException()
    {
        var dto = new TaTrsBilling2Dto(
            BillingId, 4,
            "KAS", 0m, 20_000m,
            "PAY-001", "2026-06-18", "09:00:00",
            "", "MED-04",
            "DT-001", "",
            "", "", "", "", "", "", "",
            "", "", "", "");

        Action act = () => dto.ToModel(modul: 0);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("fs_kd_jenis_bayar");
    }
}
