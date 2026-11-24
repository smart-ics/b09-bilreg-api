using Bilreg.Domain.ApotekContext.PenjualanFeature;
using Bilreg.Domain.ApotekContext.Shared;
using FluentAssertions;

namespace Bilreg.Test.ApotekContext.PenjualanFeature;

public class PenjualanModelTest
{
    private static List<PenjualanItemModel> CreateSampleItems()
    {
        return new List<PenjualanItemModel>
        {
            PenjualanItemModel.Create(
                noUrut: 1,
                penjualanItemId: "DU2730748A001",
                brgId: "BRG001",
                brgName: "Amoxicillin 500mg",
                satuanId: "KAP",
                satuanName: "Kaplet",
                qty: 10,
                hargaSatuan: 2000m,
                diskon: 1000m,
                biaya: 1500m,
                taxProsen: 11m,
                taxRupiah: 2090m, // (20000 - 1000) * 11%
                fee: 500m,
                etiket: "3x1 sehari",
                etiketQty: 3,
                etiketHari: 3,
                etiketKdPakai: "SES",
                etiketCatatan: "Habiskan",
                isRacik: false,
                isKomponen: false,
                racikId: "",
                isBeliBebas: false,
                nilaiKlaim: 0)
        };
    }

    [Fact]
    public void PenjualanKey_enforces_DU_prefix_and_10_character_length()
    {
        var validKey = new PenjualanKey("DU2730748A");
        validKey.PenjualanId.Should().Be("DU2730748A");

        var actWrongPrefix = () => new PenjualanKey("ASI2730748");
        actWrongPrefix.Should().Throw<ApotekDomainException>()
            .WithMessage("*Harus memiliki panjang 10 karakter dengan prefix 'DU'*");

        var actTooShort = () => new PenjualanKey("DU123");
        actTooShort.Should().Throw<ApotekDomainException>();

        var actNull = () => new PenjualanKey(null!);
        actNull.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateFromTelaah_creates_prescription_based_sale_with_correct_invariants()
    {
        // Arrange
        var items = CreateSampleItems();
        var now = new DateTime(2026, 10, 18, 10, 30, 0);

        // Act
        var model = PenjualanModel.CreateFromTelaah(
            resepId: "ARX0001",
            regId: "REG12345",
            pasienName: "Budi Santoso",
            dokterId: "DOK001",
            layananId: "APT01",
            layananResepId: "POLI01",
            tipeJaminanId: "JKN00",
            kelasId: "3",
            petugasId: "PETUGAS1",
            penjualanDate: now,
            items: items,
            pasienAlamat: "Jl. Merdeka No. 1",
            sex: "L",
            diskonLain: 0,
            biayaLain: 1000,
            pembulatan: 0,
            bulat: 0);

        // Assert
        model.PenjualanId.Should().StartWith("DU").And.HaveLength(10);
        model.JenisPenjualan.Should().Be(JenisPenjualanEnum.PrescriptionBased);
        model.Status.Should().Be(PenjualanStatusEnum.Issued);
        model.ResepId.Should().Be("ARX0001");
        model.RegId.Should().Be("REG12345");
        model.PasienName.Should().Be("Budi Santoso");
        model.DokterId.Should().Be("DOK001");
        model.LayananId.Should().Be("APT01");
        model.LayananResepId.Should().Be("POLI01");
        model.TipeJaminanId.Should().Be("JKN00");
        model.Sex.Should().Be("L");

        // Physical handover decoupled invariants
        model.TrsMutasiId.Should().BeEmpty();
        ApotekDate.IsEmpty(model.TglSerah).Should().BeTrue();
        model.JamSerah.Should().Be("00:00:00");
        model.PetugasSerah.Should().BeEmpty();

        // Not voided
        model.IsVoid.Should().BeFalse();
        ApotekDate.IsEmpty(model.TglVoid).Should().BeTrue();

        // Financial totals:
        // Item SubTotal = 19,000; Biaya = 1,500; TaxRupiah = 2,090; Fee = 500
        // Total Item = 23,090
        // Header: SubTotal = 23,090; BiayaLain = 1,000; GrandTotal = 24,090
        model.SumSubTotal.Should().Be(19000m);
        model.SumBiaya.Should().Be(1500m);
        model.SumTaxRupiah.Should().Be(2090m);
        model.SubTotal.Should().Be(23090m);
        model.GrandTotal.Should().Be(24090m);
        model.Sisa.Should().Be(24090m);
    }

    [Fact]
    public void CreateDirect_creates_registered_direct_sale_without_resep_id()
    {
        // Arrange
        var items = CreateSampleItems();
        var now = new DateTime(2026, 10, 18, 11, 0, 0);

        // Act
        var model = PenjualanModel.CreateDirect(
            regId: "REG67890",
            pasienName: "Siti Rahma",
            dokterId: "DOK002",
            layananId: "APT01",
            tipeJaminanId: "00000",
            kelasId: "1",
            petugasId: "PETUGAS2",
            penjualanDate: now,
            items: items,
            pasienAlamat: "Jl. Sudirman No. 5",
            sex: "P");

        // Assert
        model.PenjualanId.Should().StartWith("DU").And.HaveLength(10);
        model.JenisPenjualan.Should().Be(JenisPenjualanEnum.RegisteredDirect);
        model.Status.Should().Be(PenjualanStatusEnum.Issued);
        model.ResepId.Should().BeEmpty();
        model.RegId.Should().Be("REG67890");
        model.PasienName.Should().Be("Siti Rahma");
        model.DokterId.Should().Be("DOK002");
        model.TipeJaminanId.Should().Be("00000");

        model.TrsMutasiId.Should().BeEmpty();
        ApotekDate.IsEmpty(model.TglSerah).Should().BeTrue();
        model.IsVoid.Should().BeFalse();
    }

    [Fact]
    public void CreateWalkIn_creates_otc_sale_with_empty_reg_and_resep_and_marks_items_belibebas()
    {
        // Arrange
        var item = PenjualanItemModel.Create(
            noUrut: 1,
            penjualanItemId: "DU2730748A001",
            brgId: "BRG005",
            brgName: "Minyak Kayu Putih 60ml",
            satuanId: "BTL",
            satuanName: "Botol",
            qty: 2,
            hargaSatuan: 25000m,
            isBeliBebas: false); // initially false

        var now = new DateTime(2026, 10, 18, 12, 0, 0);

        // Act
        var model = PenjualanModel.CreateWalkIn(
            pembeliName: "Pembeli Umum Non-Pasien",
            pembeliAlamat: "Pekalongan",
            layananId: "APT01",
            petugasId: "KASIR1",
            penjualanDate: now,
            items: new[] { item },
            pembeliKota: "Pekalongan");

        // Assert
        model.PenjualanId.Should().StartWith("DU").And.HaveLength(10);
        model.JenisPenjualan.Should().Be(JenisPenjualanEnum.WalkInOTC);
        model.Status.Should().Be(PenjualanStatusEnum.Issued);
        model.RegId.Should().BeEmpty();
        model.ResepId.Should().BeEmpty();
        model.DokterId.Should().BeEmpty();
        model.PasienName.Should().Be("Pembeli Umum Non-Pasien");
        model.PasienAlamat.Should().Be("Pekalongan");
        model.PasienKota.Should().Be("Pekalongan");
        model.TipeJaminanId.Should().Be("00000");

        // Items must be marked IsBeliBebas = true
        model.Items.Should().ContainSingle();
        model.Items[0].IsBeliBebas.Should().BeTrue();

        model.TrsMutasiId.Should().BeEmpty();
        ApotekDate.IsEmpty(model.TglSerah).Should().BeTrue();
    }

    [Fact]
    public void UpdateHandoverInfo_updates_dispensing_and_stock_mutation_columns()
    {
        var model = PenjualanModel.CreateFromTelaah(
            "ARX1", "REG1", "Pasien", "DOK1", "APT1", "POLI1", "00000", "3", "USR1",
            DateTime.Now, CreateSampleItems());

        var handoverTime = new DateTime(2026, 10, 18, 14, 15, 0);
        model.UpdateHandoverInfo(handoverTime, "14:15:00", "PETUGAS_SERAH", "DI2730001A");

        model.TglSerah.Should().Be(handoverTime);
        model.JamSerah.Should().Be("14:15:00");
        model.PetugasSerah.Should().Be("PETUGAS_SERAH");
        model.TrsMutasiId.Should().Be("DI2730001A");
    }

    [Fact]
    public void Void_with_tata_rekening_permission_succeeds_and_soft_voids_items()
    {
        var model = PenjualanModel.CreateFromTelaah(
            "ARX1", "REG1", "Pasien", "DOK1", "APT1", "POLI1", "00000", "3", "USR1",
            DateTime.Now, CreateSampleItems());

        var voidTime = new DateTime(2026, 10, 18, 15, 0, 0);

        // Act - tataRekeningAllows = true
        model.Void("SUPERVISOR1", voidTime, "Salah entri resep dokter", tataRekeningAllows: true);

        // Assert
        model.IsVoid.Should().BeTrue();
        model.Status.Should().Be(PenjualanStatusEnum.Cancelled);
        model.TglVoid.Should().Be(voidTime);
        model.JamVoid.Should().Be("15:00:00");
        model.PetugasVoid.Should().Be("SUPERVISOR1");
        model.VoidReason.Should().Be("Salah entri resep dokter");

        // Items should be soft-voided
        model.Items.Should().OnlyContain(x => x.IsVoid);

        // Totals drop to 0
        model.SubTotal.Should().Be(0m);
        model.GrandTotal.Should().Be(0m);
        model.Sisa.Should().Be(0m);
    }

    [Fact]
    public void Void_rejected_by_ADR_APT_003_when_tata_rekening_does_not_allow()
    {
        var model = PenjualanModel.CreateFromTelaah(
            "ARX1", "REG1", "Pasien", "DOK1", "APT1", "POLI1", "00000", "3", "USR1",
            DateTime.Now, CreateSampleItems());

        var voidTime = new DateTime(2026, 10, 18, 15, 0, 0);

        // Act - tataRekeningAllows = false (Billing closed/finalized)
        var act = () => model.Void("KASIR1", voidTime, "Batal transaksi", tataRekeningAllows: false);

        act.Should().Throw<ApotekDomainException>()
            .WithMessage("*ditolak oleh Tata Rekening sesuai aturan ADR-APT-003*");

        model.IsVoid.Should().BeFalse();
        model.Status.Should().Be(PenjualanStatusEnum.Issued);
    }

    [Fact]
    public void Double_void_throws_exception()
    {
        var model = PenjualanModel.CreateFromTelaah(
            "ARX1", "REG1", "Pasien", "DOK1", "APT1", "POLI1", "00000", "3", "USR1",
            DateTime.Now, CreateSampleItems());

        model.Void("ADMIN", DateTime.Now, "Alasan 1", tataRekeningAllows: true);

        var act = () => model.Void("ADMIN", DateTime.Now, "Alasan 2", tataRekeningAllows: true);
        act.Should().Throw<ApotekDomainException>()
            .WithMessage("*sudah dibatalkan sebelumnya*");
    }

    [Fact]
    public void Voided_sale_rejects_handover_update()
    {
        var model = PenjualanModel.CreateFromTelaah(
            "ARX1", "REG1", "Pasien", "DOK1", "APT1", "POLI1", "00000", "3", "USR1",
            DateTime.Now, CreateSampleItems());

        model.Void("ADMIN", DateTime.Now, "Alasan", tataRekeningAllows: true);

        var act = () => model.UpdateHandoverInfo(DateTime.Now, "10:00:00", "PETUGAS", "DI001");
        act.Should().Throw<ApotekDomainException>()
            .WithMessage("*Tidak dapat melakukan penyerahan obat*");
    }

    [Fact]
    public void PaymentClearance_updates_kas_and_status()
    {
        var model = PenjualanModel.CreateDirect(
            "REG1", "Pasien", "DOK1", "APT1", "00000", "3", "USR1",
            DateTime.Now, CreateSampleItems());

        var grandTotal = model.GrandTotal;
        model.SetPaymentClearance("KW2730001", kas: grandTotal, bank: 0, kartuKredit: 0);

        model.Status.Should().Be(PenjualanStatusEnum.FinanciallyCleared);
        model.TrsKwitansiId.Should().Be("KW2730001");
        model.Kas.Should().Be(grandTotal);
        model.Sisa.Should().Be(0m);
    }

    [Fact]
    public void SetChargeId_records_tata_rekening_charge_id()
    {
        var model = PenjualanModel.CreateFromTelaah(
            "ARX1", "REG1", "Pasien", "DOK1", "APT1", "POLI1", "JKN00", "3", "USR1",
            DateTime.Now, CreateSampleItems());

        model.SetChargeId("CHG273001");
        model.TrsChargeId.Should().Be("CHG273001");
    }

    [Fact]
    public void Create_with_empty_items_throws_exception()
    {
        var actTelaah = () => PenjualanModel.CreateFromTelaah(
            "ARX1", "REG1", "Pasien", "DOK1", "APT1", "POLI1", "00000", "3", "USR1",
            DateTime.Now, []);
        actTelaah.Should().Throw<ApotekDomainException>();

        var actDirect = () => PenjualanModel.CreateDirect(
            "REG1", "Pasien", "DOK1", "APT1", "00000", "3", "USR1",
            DateTime.Now, []);
        actDirect.Should().Throw<ApotekDomainException>();

        var actWalkIn = () => PenjualanModel.CreateWalkIn(
            "Pembeli", "Alamat", "APT1", "USR1",
            DateTime.Now, []);
        actWalkIn.Should().Throw<ApotekDomainException>();
    }
}
