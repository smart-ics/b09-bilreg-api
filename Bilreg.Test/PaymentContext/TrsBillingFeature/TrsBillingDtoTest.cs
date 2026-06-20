using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PaymentContext.RekapCetakFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
using Bilreg.Domain.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Infrastructure.PaymentContext.TrsBillingFeature;
using FluentAssertions;

namespace Bilreg.Test.PaymentContext.TrsBillingFeature;

public class TrsBillingDtoTest
{
    private static readonly DateTime TglTrs = new(2026, 6, 17, 10, 30, 45);

    private static TrsBillType CreateJasaBill()
    {
        return new TrsBillType(
            "BIL-001",
            BillModulGroup.Jasa,
            TglTrs,
            new RegReff("REG-12345", "MR-99", "Patient Name"),
            new LayananReff("LAY-01", "Layanan Name"),
            new KelasReff("KLS-01", "Kelas Name"),
            new AuditInfoType("USR-01", TglTrs),
            new RekapCetakReff("RK-01", "Rekap Name"),
            new TrsBillNilaiType(100_000m, 10_000m, 5_000m, 2_000m),
            new TrsBillKetType("Ket 1", "Ket 2", "REF-01", 3m, "MAIN-01"),
            [],
            [],
            []);
    }

    [Fact]
    public void FromModel_CopiesAllPersistedFields()
    {
        var model = CreateJasaBill();

        var dto = TrsBillingDto.FromModel(model);

        dto.fs_kd_trs.Should().Be("BIL-001");
        dto.fn_modul.Should().Be(0);
        dto.fd_tgl_trs.Should().Be("2026-06-17");
        dto.fs_jam_trs.Should().Be("10:30:45");
        dto.fd_tgl_jam_trs.Should().Be("2026-06-17 10:30:45");
        dto.fs_kd_reg.Should().Be("REG-12345");
        dto.fs_kd_layanan.Should().Be("LAY-01");
        dto.fs_kd_kelas.Should().Be("KLS-01");
        dto.fs_kd_rekap_cetak.Should().Be("RK-01");
        dto.fs_kd_petugas.Should().Be("USR-01");
        dto.fn_sub_total.Should().Be(100_000m);
        dto.fn_diskon.Should().Be(10_000m);
        dto.fn_biaya.Should().Be(2_000m);
        dto.fn_tax.Should().Be(5_000m);
        dto.fn_total.Should().Be(97_000m);
        dto.fs_keterangan.Should().Be("Ket 1");
        dto.fs_keterangan2.Should().Be("Ket 2");
        dto.fs_kd_ref_biaya.Should().Be("REF-01");
        dto.fn_qty.Should().Be(3m);
        dto.fs_kd_trs_main.Should().Be("MAIN-01");
        dto.fs_mr.Should().Be("MR-99");
        dto.fs_nm_pasien.Should().Be("Patient Name");
        dto.fs_nm_layanan.Should().Be("Layanan Name");
        dto.fs_nm_kelas.Should().Be("Kelas Name");
        dto.fs_nm_rekap_cetak.Should().Be("Rekap Name");
    }

    [Fact]
    public void FromModel_ObatModul_MapsModulAsOne()
    {
        var model = CreateJasaBill() with { ModulGroup = BillModulGroup.Obat };

        var dto = TrsBillingDto.FromModel(model);

        dto.fn_modul.Should().Be(1);
    }

    [Fact]
    public void ToModel_CopiesAllPersistedFields()
    {
        var dto = new TrsBillingDto(
            "BIL-002",
            0,
            "2026-06-18",
            "14:15:30",
            "2026-06-18 14:15:30",
            "REG-99999",
            "LAY-02",
            "KLS-02",
            "RK-02",
            "USR-02",
            50_000m,
            5_000m,
            1_000m,
            2_500m,
            48_500m,
            "Desc A",
            "Desc B",
            "REF-02",
            0m,
            "",
            "MR-02",
            "Pasien B",
            "Layanan B",
            "Kelas B",
            "Rekap B");

        var model = dto.ToModel([]);

        model.TrsBillingId.Should().Be("BIL-002");
        model.ModulGroup.Should().Be(BillModulGroup.Jasa);
        model.TglTrs.Should().Be(new DateTime(2026, 6, 18, 14, 15, 30));
        model.Reg.RegId.Should().Be("REG-99999");
        model.Reg.PasienId.Should().Be("MR-02");
        model.Reg.PasienName.Should().Be("Pasien B");
        model.Layanan.LayananId.Should().Be("LAY-02");
        model.Layanan.LayananName.Should().Be("Layanan B");
        model.Kelas.KelasId.Should().Be("KLS-02");
        model.Kelas.KelasName.Should().Be("Kelas B");
        model.RekapCetak.RekapCetakId.Should().Be("RK-02");
        model.RekapCetak.RekapCetakName.Should().Be("Rekap B");
        model.AuditInfo.UserId.Should().Be("USR-02");
        model.AuditInfo.Timestamp.Should().Be(new DateTime(2026, 6, 18, 14, 15, 30));
        model.Nilai.SubTotal.Should().Be(50_000m);
        model.Nilai.Diskon.Should().Be(5_000m);
        model.Nilai.Biaya.Should().Be(1_000m);
        model.Nilai.Tax.Should().Be(2_500m);
        model.Nilai.Total.Should().Be(48_500m);
        model.Keterangan.Keterangan.Should().Be("Desc A");
        model.Keterangan.Keterangan2.Should().Be("Desc B");
        model.Keterangan.RefBiaya.Should().Be("REF-02");
        model.Keterangan.Qty.Should().Be(0m);
        model.Keterangan.TrsMainId.Should().Be("");
        model.ListTransaction.Should().BeEmpty();
        model.ListDischarge.Should().BeEmpty();
        model.ListPayment.Should().BeEmpty();
    }

    [Fact]
    public void ToModel_ObatModul_MapsModulGroupObat()
    {
        var dto = new TrsBillingDto(
            "BIL-003", 1, "2026-01-01", "00:00:00", "2026-01-01 00:00:00",
            "REG-01", "LAY-01", "KLS-01", "RK-01", "USR-01",
            0m, 0m, 0m, 0m, 0m,
            "", "", "", 0m, "",
            "", "", "", "", "");

        var model = dto.ToModel([]);

        model.ModulGroup.Should().Be(BillModulGroup.Obat);
    }

    [Fact]
    public void ToModel_WithBill2Events_PreservesChildCollections()
    {
        var transDto = TaTrsBilling2Dto.FromModelTrans(
            new TrsBill2TransEventType(
                1,
                new TrsBill2KomponenType("DT-01", "Detil"),
                TrsBillJenisBayarType.Pdp,
                10_000m,
                new PpaReff("MED-01", "Dr"),
                new TrsBill2CoaType(
                    new CoaType("PPDP", ""),
                    new CoaType("PDPT", ""),
                    CoaType.Default,
                    CoaType.Default,
                    CoaType.Default,
                    CoaType.Default)),
            "BIL-004",
            0);
        var dischargeDto = new TaTrsBilling2Dto(
            "BIL-004", 2,
            "KAS", 0m, 5_000m,
            "RO00001234", "2026-06-19", "00:00:00",
            "KSR-01", "MED-02",
            "DT-02", "",
            "", "", "", "", "", "", "",
            "Detil 2", "", "KSR-01", "Dr 2");

        var dto = TrsBillingDto.FromModel(CreateJasaBill() with { TrsBillingId = "BIL-004" });
        var bill2 = new ITrsBill2Event[]
        {
            transDto.ToModel(0),
            dischargeDto.ToModel(0)
        };

        var model = dto.ToModel(bill2);

        model.ListTransaction.Should().HaveCount(1);
        model.ListDischarge.Should().HaveCount(1);
        model.ListPayment.Should().BeEmpty();
        model.ListTransaction.First().NoUrut.Should().Be(1);
        model.ListDischarge.First().NoUrut.Should().Be(2);
    }

    [Fact]
    public void ToView_CopiesCoreFieldsWithoutBill2Collections()
    {
        var dto = new TrsBillingDto(
            "BIL-005",
            0,
            "2026-03-10",
            "08:00:00",
            "2026-03-10 08:00:00",
            "REG-VIEW",
            "LAY-V",
            "KLS-V",
            "RK-V",
            "USR-V",
            12_345.67m,
            100m,
            50m,
            25m,
            12_320.67m,
            "View ket",
            "View ket 2",
            "REF-V",
            2m,
            "MAIN-V",
            "MR-V",
            "Pasien View",
            "Layanan View",
            "Kelas View",
            "Rekap View");

        var view = dto.ToView();

        view.TrsBillingId.Should().Be("BIL-005");
        view.ModulGroup.Should().Be(BillModulGroup.Jasa);
        view.TglTrs.Should().Be(new DateTime(2026, 3, 10, 8, 0, 0));
        view.Reg.RegId.Should().Be("REG-VIEW");
        view.Nilai.SubTotal.Should().Be(12_345.67m);
        view.Keterangan.Keterangan.Should().Be("View ket");
        view.Keterangan.Qty.Should().Be(2m);
    }

    [Fact]
    public void RoundTrip_FromModelThenToModel_PreservesBusinessFields()
    {
        var source = CreateJasaBill();

        var dto = TrsBillingDto.FromModel(source);
        var roundTripped = dto.ToModel([]);

        roundTripped.TrsBillingId.Should().Be(source.TrsBillingId);
        roundTripped.ModulGroup.Should().Be(source.ModulGroup);
        roundTripped.TglTrs.Should().Be(source.TglTrs);
        roundTripped.Reg.RegId.Should().Be(source.Reg.RegId);
        roundTripped.Reg.PasienId.Should().Be(source.Reg.PasienId);
        roundTripped.Reg.PasienName.Should().Be(source.Reg.PasienName);
        roundTripped.Layanan.LayananId.Should().Be(source.Layanan.LayananId);
        roundTripped.Kelas.KelasId.Should().Be(source.Kelas.KelasId);
        roundTripped.RekapCetak.RekapCetakId.Should().Be(source.RekapCetak.RekapCetakId);
        roundTripped.AuditInfo.UserId.Should().Be(source.AuditInfo.UserId);
        roundTripped.Nilai.SubTotal.Should().Be(source.Nilai.SubTotal);
        roundTripped.Nilai.Diskon.Should().Be(source.Nilai.Diskon);
        roundTripped.Nilai.Biaya.Should().Be(source.Nilai.Biaya);
        roundTripped.Nilai.Tax.Should().Be(source.Nilai.Tax);
        roundTripped.Keterangan.Keterangan.Should().Be(source.Keterangan.Keterangan);
        roundTripped.Keterangan.Keterangan2.Should().Be(source.Keterangan.Keterangan2);
        roundTripped.Keterangan.RefBiaya.Should().Be(source.Keterangan.RefBiaya);
        roundTripped.Keterangan.Qty.Should().Be(source.Keterangan.Qty);
        roundTripped.Keterangan.TrsMainId.Should().Be(source.Keterangan.TrsMainId);
    }

    [Fact]
    public void FromModel_ZeroAmounts_PreservesZeroValues()
    {
        var model = CreateJasaBill() with
        {
            Nilai = new TrsBillNilaiType(0m, 0m, 0m, 0m),
            Keterangan = new TrsBillKetType("", "", "", 0m, "")
        };

        var dto = TrsBillingDto.FromModel(model);

        dto.fn_sub_total.Should().Be(0m);
        dto.fn_diskon.Should().Be(0m);
        dto.fn_biaya.Should().Be(0m);
        dto.fn_tax.Should().Be(0m);
        dto.fn_total.Should().Be(0m);
        dto.fn_qty.Should().Be(0m);
        dto.fs_keterangan.Should().Be("");
        dto.fs_kd_trs_main.Should().Be("");
    }
}
