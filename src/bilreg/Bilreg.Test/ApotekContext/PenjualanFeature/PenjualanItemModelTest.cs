using Bilreg.Domain.ApotekContext.PenjualanFeature;
using Bilreg.Domain.ApotekContext.Shared;
using FluentAssertions;

namespace Bilreg.Test.ApotekContext.PenjualanFeature;

public class PenjualanItemModelTest
{
    [Fact]
    public void Create_regular_item_calculates_subtotal_and_total_correctly()
    {
        // Arrange & Act
        var item = PenjualanItemModel.Create(
            noUrut: 1,
            penjualanItemId: "DU2730748A001",
            brgId: "BRG001",
            brgName: "Paracetamol 500mg",
            satuanId: "TAB",
            satuanName: "Tablet",
            qty: 10,
            hargaSatuan: 1000m,
            diskon: 500m,
            biaya: 2000m, // tuslah
            taxProsen: 11m,
            taxRupiah: 0m, // auto calculate from subtotal: (10000 - 500) * 11% = 1045
            fee: 1500m,
            etiket: "3x1 sehari sesudah makan",
            etiketQty: 3,
            etiketHari: 3,
            etiketKdPakai: "SES",
            etiketCatatan: "Bila demam",
            isRacik: false,
            isKomponen: false,
            racikId: "",
            isBeliBebas: false,
            nilaiKlaim: 0);

        // Assert
        item.NoUrut.Should().Be(1);
        item.PenjualanItemId.Should().Be("DU2730748A001");
        item.BrgId.Should().Be("BRG001");
        item.BrgName.Should().Be("Paracetamol 500mg");
        item.SatuanId.Should().Be("TAB");
        item.SatuanName.Should().Be("Tablet");
        item.Qty.Should().Be(10);
        item.HargaSatuan.Should().Be(1000m);
        item.Diskon.Should().Be(500m);
        item.Biaya.Should().Be(2000m);
        item.SubTotal.Should().Be(9500m); // (10 * 1000) - 500
        item.TaxProsen.Should().Be(11m);
        item.TaxRupiah.Should().Be(1045m); // 9500 * 0.11
        item.Fee.Should().Be(1500m);
        item.Total.Should().Be(14045m); // 9500 + 2000 + 1045 + 1500
        item.Etiket.Should().Be("3x1 sehari sesudah makan");
        item.EtiketQty.Should().Be(3);
        item.EtiketHari.Should().Be(3);
        item.EtiketKdPakai.Should().Be("SES");
        item.EtiketCatatan.Should().Be("Bila demam");
        item.IsRacik.Should().BeFalse();
        item.IsKomponen.Should().BeFalse();
        item.RacikId.Should().BeEmpty();
        item.IsBeliBebas.Should().BeFalse();
        item.NilaiKlaim.Should().Be(0);
        item.IsVoid.Should().BeFalse();
    }

    [Fact]
    public void Create_racik_container_and_components_relationship_valid()
    {
        var container = PenjualanItemModel.Create(
            noUrut: 1,
            penjualanItemId: "DU2730748A001",
            brgId: "RCK01",
            brgName: "Kapsul Racik Batuk",
            satuanId: "BKS",
            satuanName: "Bungkus",
            qty: 10,
            hargaSatuan: 0,
            biaya: 3000,
            fee: 5000,
            isRacik: true,
            isKomponen: false,
            racikId: "");

        var component = PenjualanItemModel.Create(
            noUrut: 2,
            penjualanItemId: "DU2730748A002",
            brgId: "BRG002",
            brgName: "Ambroxol 30mg",
            satuanId: "TAB",
            satuanName: "Tablet",
            qty: 10,
            hargaSatuan: 500,
            isRacik: false,
            isKomponen: true,
            racikId: container.PenjualanItemId);

        container.IsRacik.Should().BeTrue();
        container.IsKomponen.Should().BeFalse();
        component.IsRacik.Should().BeFalse();
        component.IsKomponen.Should().BeTrue();
        component.RacikId.Should().Be("DU2730748A001");
    }

    [Fact]
    public void Rejects_item_that_is_both_racik_container_and_komponen()
    {
        var act = () => new PenjualanItemModel(
            noUrut: 1,
            penjualanItemId: "DU2730748A001",
            brgId: "BRG001",
            brgName: "Obat",
            satuanId: "TAB",
            satuanName: "Tablet",
            qty: 1,
            hargaSatuan: 100,
            diskon: 0,
            biaya: 0,
            taxProsen: 0,
            taxRupiah: 0,
            fee: 0,
            total: 100,
            etiket: "",
            etiketQty: 0,
            etiketHari: 0,
            etiketKdPakai: "",
            etiketCatatan: "",
            isRacik: true,
            isKomponen: true,
            racikId: "DU2730748A001",
            isBeliBebas: false,
            nilaiKlaim: 0);

        act.Should().Throw<ApotekDomainException>()
            .WithMessage("*tidak boleh menjadi sediaan racikan sekaligus komponen racikan*");
    }

    [Fact]
    public void Rejects_komponen_without_racik_id()
    {
        var act = () => new PenjualanItemModel(
            noUrut: 1,
            penjualanItemId: "DU2730748A001",
            brgId: "BRG001",
            brgName: "Obat",
            satuanId: "TAB",
            satuanName: "Tablet",
            qty: 1,
            hargaSatuan: 100,
            diskon: 0,
            biaya: 0,
            taxProsen: 0,
            taxRupiah: 0,
            fee: 0,
            total: 100,
            etiket: "",
            etiketQty: 0,
            etiketHari: 0,
            etiketKdPakai: "",
            etiketCatatan: "",
            isRacik: false,
            isKomponen: true,
            racikId: "",
            isBeliBebas: false,
            nilaiKlaim: 0);

        act.Should().Throw<ApotekDomainException>()
            .WithMessage("*Komponen racikan wajib mencantumkan RacikId induk*");
    }

    [Fact]
    public void Rejects_zero_or_negative_qty()
    {
        var act = () => PenjualanItemModel.Create(
            noUrut: 1,
            penjualanItemId: "DU2730748A001",
            brgId: "BRG001",
            brgName: "Obat",
            satuanId: "TAB",
            satuanName: "Tablet",
            qty: 0,
            hargaSatuan: 100);

        act.Should().Throw<ApotekDomainException>()
            .WithMessage("*Kuantitas barang (Qty) harus lebih besar dari nol*");
    }

    [Fact]
    public void Rejects_negative_financial_values()
    {
        var actHarga = () => PenjualanItemModel.Create(1, "DU2730748A001", "B1", "N1", "S1", "N", 1, -100);
        actHarga.Should().Throw<ArgumentException>();

        var actDiskon = () => PenjualanItemModel.Create(1, "DU2730748A001", "B1", "N1", "S1", "N", 1, 100, diskon: -10);
        actDiskon.Should().Throw<ArgumentException>();

        var actBiaya = () => PenjualanItemModel.Create(1, "DU2730748A001", "B1", "N1", "S1", "N", 1, 100, biaya: -10);
        actBiaya.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Void_marks_is_void_true()
    {
        var item = PenjualanItemModel.Create(1, "DU2730748A001", "B1", "N1", "S1", "N", 1, 1000);
        item.IsVoid.Should().BeFalse();

        item.Void();

        item.IsVoid.Should().BeTrue();
    }

    [Fact]
    public void Mark_beli_bebas_sets_flag_true()
    {
        var item = PenjualanItemModel.Create(1, "DU2730748A001", "B1", "N1", "S1", "N", 1, 1000, isBeliBebas: false);
        item.IsBeliBebas.Should().BeFalse();

        item.MarkBeliBebas();

        item.IsBeliBebas.Should().BeTrue();
    }

    [Fact]
    public void Set_no_urut_updates_sequence_number()
    {
        var item = PenjualanItemModel.Create(1, "DU2730748A001", "B1", "N1", "S1", "N", 1, 1000);
        item.SetNoUrut(5);
        item.NoUrut.Should().Be(5);

        var act = () => item.SetNoUrut(0);
        act.Should().Throw<ArgumentException>();
    }
}
