using Bilreg.Domain.AccountingContext.UnitFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.SalesContext.PenjualanFeature;
using Bilreg.Domain.SalesContext.ResepFeature;
using Bilreg.Domain.SalesContext.ReturJualFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using LayananType = Bilreg.Domain.AdmisiContext.LayananFeature.LayananType;
using TipeBrgType = Bilreg.Domain.BrgContext.PricingPolicyFeature.TipeBrgType;

namespace Bilreg.Test.SalesContext.ReturJualFeature;

public class ReturJualModelTest
{
    private const string ReturJualId = "RUTST00001";

    private static BrgReff Brg(string id, string name) => new(id, name);

    private static SatuanType Satuan(string id = "TAB", string name = "Tablet") =>
        SatuanType.Create(id, name);

    private static PenjualanReff Penjualan() => new(
        "DUTST00001",
        new DateTime(2026, 8, 6, 10, 0, 0),
        new RegReff("RG00000001", "MR0001", "Pasien Tes"));

    private static LayananReff Layanan() => new("LYJ01", "Apotek RJ");

    private static TipeJaminanReff TipeJaminan() => new("00000", "Umum");

    private static ReturableItemType ReturableItem(
        string brgId = "BRG01", decimal qtyReturSisa = 10, decimal hargaJual = 1000)
        => new(Brg(brgId, "Obat A"), Satuan(), 10, hargaJual, qtyReturSisa);

    private static ReturJualModel BuildEmptyRetur()
    {
        return new ReturJualModel(
            ReturJualId,
            Penjualan(),
            Layanan(),
            "Barang tidak sesuai",
            TipeJaminan(),
            TipeBrgType.Default.ToReff(),
            NilaiReturJualType.Default,
            AuditTrailType.Create("U1", new DateTime(2026, 8, 6, 10, 0, 0)),
            []);
    }

    [Fact]
    public void Create_AssignsIdPrefixAndHeaderFromArgs()
    {
        var retur = ReturJualModel.Create(PenjualanModel(), BuildLayanan(), "Rusak", "U1");

        retur.ReturJualId.Should().StartWith("RU");
        retur.Penjualan.PenjualanId.Should().Be("DUTST00001");
        retur.Layanan.LayananId.Should().Be("LYJ01");
        retur.Reason.Should().Be("Rusak");
        retur.TipeJaminan.TipeJaminanId.Should().Be("00000");
        retur.AuditTrail.Created.UserId.Should().Be("U1");
        retur.AuditTrail.IsVoided.Should().BeFalse();
    }

    [Fact]
    public void AddItem_ComputesNilaiAndRenumbers()
    {
        var retur = BuildEmptyRetur();

        retur.AddItem(ReturableItem("BRG01", qtyReturSisa: 10, hargaJual: 1000), 4, 900, 10);

        var item = retur.ListItem.Single();
        item.NoUrut.Should().Be(1);
        item.ReturJualItemId.Should().Be(ReturJualId + "001");
        item.QtyJual.Should().Be(10);
        item.QtyRetur.Should().Be(4);
        item.Nilai.SubTotalJual.Should().Be(10000);
        item.Nilai.SubTotalRetur.Should().Be(3600);
        item.Nilai.SubTotalTax.Should().Be(40);
        item.Nilai.Total.Should().Be(3640);
        retur.Nilai.SumSubTotalJual.Should().Be(10000);
        retur.Nilai.SumSubTotalRetur.Should().Be(3600);
        retur.Nilai.SumTax.Should().Be(40);
    }

    [Fact]
    public void AddItem_WithZeroQty_Throws()
    {
        var retur = BuildEmptyRetur();

        var act = () => retur.AddItem(ReturableItem(), 0, 900, 10);

        act.Should().Throw<ArgumentException>().WithMessage("*lebih besar dari 0*");
    }

    [Fact]
    public void AddItem_WithQtyBeyondSisa_Throws()
    {
        var retur = BuildEmptyRetur();

        var act = () => retur.AddItem(ReturableItem(qtyReturSisa: 5), 6, 900, 10);

        act.Should().Throw<ArgumentException>().WithMessage("*sisa qty retur*");
    }

    [Fact]
    public void AddItem_DuplicateBrg_Throws()
    {
        var retur = BuildEmptyRetur();
        retur.AddItem(ReturableItem("BRG01"), 1, 900, 0);

        var act = () => retur.AddItem(ReturableItem("BRG01"), 1, 900, 0);

        act.Should().Throw<ArgumentException>().WithMessage("*duplikasi*");
    }

    [Fact]
    public void RemoveItem_RemovesAndRenumbers()
    {
        var retur = BuildEmptyRetur();
        retur.AddItem(ReturableItem("BRG01"), 1, 900, 0);
        retur.AddItem(ReturableItem("BRG02"), 1, 900, 0);

        retur.RemoveItem(ReturJualId + "001");

        retur.ListItem.Should().HaveCount(1);
        retur.ListItem.Single().Brg.BrgId.Should().Be("BRG02");
        retur.ListItem.Single().NoUrut.Should().Be(1);
    }

    [Fact]
    public void RemoveItem_MissingItem_Throws()
    {
        var retur = BuildEmptyRetur();

        var act = () => retur.RemoveItem("NONEXIST001");

        act.Should().Throw<KeyNotFoundException>().WithMessage("*tidak ditemukan*");
    }

    [Fact]
    public void ChangeQtyRetur_RecomputesNilai()
    {
        var retur = BuildEmptyRetur();
        retur.AddItem(ReturableItem("BRG01", qtyReturSisa: 10, hargaJual: 1000), 4, 900, 10);
        retur.SetPembulatan(0);

        retur.ChangeQtyRetur(ReturJualId + "001", 6, 10);

        var item = retur.ListItem.Single();
        item.QtyRetur.Should().Be(6);
        item.Nilai.SubTotalRetur.Should().Be(5400);
        item.Nilai.SubTotalTax.Should().Be(60);
        retur.Nilai.SumSubTotalRetur.Should().Be(5400);
        retur.Nilai.GrandTotal.Should().Be(5460);
    }

    [Fact]
    public void ChangeQtyRetur_BeyondSisa_Throws()
    {
        var retur = BuildEmptyRetur();
        retur.AddItem(ReturableItem("BRG01", qtyReturSisa: 10), 4, 900, 0);

        var act = () => retur.ChangeQtyRetur(ReturJualId + "001", 11, 10);

        act.Should().Throw<ArgumentException>().WithMessage("*sisa qty retur*");
    }

    [Fact]
    public void SetPembulatan_RoundsGrandTotalUp()
    {
        var retur = BuildEmptyRetur();
        retur.AddItem(ReturableItem("BRG01", hargaJual: 1000), 4, 900, 25);

        retur.SetPembulatan(100);

        retur.Nilai.SumSubTotalRetur.Should().Be(3600);
        retur.Nilai.SumTax.Should().Be(100);
        retur.Nilai.Pembulatan.Should().Be(0);
        retur.Nilai.GrandTotal.Should().Be(3700);
    }

    [Fact]
    public void SetPembulatan_Negative_Throws()
    {
        var retur = BuildEmptyRetur();

        var act = () => retur.SetPembulatan(-1);

        act.Should().Throw<ArgumentException>().WithMessage("*tidak boleh kurang dari 0*");
    }

    [Fact]
    public void ApplyNilaiItem_UpdatesLineNilaiAndTotals()
    {
        var retur = BuildEmptyRetur();
        retur.AddItem(ReturableItem("BRG01", qtyReturSisa: 10, hargaJual: 1000), 4, 900, 10);
        retur.SetPembulatan(0);

        retur.ApplyNilaiItem(ReturJualId + "001", 800, 5);

        var item = retur.ListItem.Single();
        item.Nilai.HargaRetur.Should().Be(800);
        item.Nilai.SubTotalRetur.Should().Be(3200);
        item.Nilai.SubTotalTax.Should().Be(20);
        item.Nilai.Total.Should().Be(3220);
        retur.Nilai.SumSubTotalRetur.Should().Be(3200);
        retur.Nilai.GrandTotal.Should().Be(3220);
    }

    [Fact]
    public void Void_MarksHeaderAndLinesVoided_AndZeroesTotals()
    {
        var retur = BuildEmptyRetur();
        retur.AddItem(ReturableItem("BRG01"), 4, 900, 10);

        retur.Void("U2");

        retur.AuditTrail.IsVoided.Should().BeTrue();
        retur.AuditTrail.Voided.UserId.Should().Be("U2");
        retur.ListItem.Single().IsVoided.Should().BeTrue();
        retur.Nilai.SumSubTotalRetur.Should().Be(0);
        retur.Nilai.GrandTotal.Should().Be(0);
    }

    [Fact]
    public void Mutate_WhenVoided_Throws()
    {
        var retur = BuildEmptyRetur();
        retur.AddItem(ReturableItem("BRG01"), 1, 900, 0);
        retur.Void("U2");

        var act = () => retur.AddItem(ReturableItem("BRG02"), 1, 900, 0);

        act.Should().Throw<InvalidOperationException>().WithMessage("*sudah void*");
    }

    private static PenjualanModel PenjualanModel()
    {
        return new PenjualanModel(
            "DUTST00001",
            "KPTEST0001",
            new RegReff("RG00000001", "MR0001", "Pasien Tes"),
            new DokterReff("DR00000001", "Dr Tes"),
            Layanan(),
            new LayananReff("LYR01", "Poli Dalam"),
            TipeJaminan(),
            TipeBrgType.Default.ToReff(),
            NilaiPenjualanType.Default,
            AuditTrailType.Create("U1", new DateTime(2026, 8, 6, 10, 0, 0)),
            []);
    }

    private static LayananType BuildLayanan()
    {
        return new LayananType(
            "LYJ01", "Apotek RJ", true,
            InstalasiType.Default.ToReff(),
            LayananDkType.Default.ToReff(),
            TipeLayananDkType.Default,
            InstalasiDkType.Default,
            new UnitReff("-", "-"),
            new PoliBpjsReff("-", "-"));
    }
}
