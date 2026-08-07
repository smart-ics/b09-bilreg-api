using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.BrgContext.PricingPolicyFeature;
using Bilreg.Domain.InventoryContext.StokFeature;
using Bilreg.Domain.SalesContext.PenjualanFeature;
using Bilreg.Domain.SalesContext.ResepFeature;
using Bilreg.Domain.SalesContext.Shared;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;

namespace Bilreg.Test.SalesContext.PenjualanFeature;

public class PenjualanModelTest
{
    private static BrgObatType Brg(string id, string name) =>
        BrgObatType.Default with { BrgId = id, BrgName = name };

    private static SatuanType Satuan(string id = "TAB", string name = "Tablet") =>
        SatuanType.Create(id, name);

    private static EtiketType Etiket() =>
        EtiketType.Create("3x1", "3x1 tablet", AppConst.DASH);

    private static LayananType LayananJual() =>
        LayananType.Create("LYJ01", "Apotek RJ", JenisLokasiType.Default);

    private static TipeJaminanReff TipeJaminan() =>
        new("00000", "Umum");

    private static ResepModel BuildResep(params ResepObatType[] obat)
    {
        return new ResepModel(
            "KPTEST0001",
            new RegReff("RG00000001", "MR0001", "Pasien Tes"),
            BodyMetricType.Default(),
            new DokterReff("DR00000001", "Dr Tes"),
            new LayananReff("LYR01", "Poli Dalam"),
            UrgenitasType.Default,
            TipeBrgType.Default.ToReff(),
            0,
            AppConst.DASH,
            AuditTrailType.Create("U1", new DateTime(2026, 8, 6, 10, 0, 0)),
            obat);
    }

    private static PenjualanModel BuildEmptyPenjualan(string id = "DUTST00001")
    {
        return new PenjualanModel(
            id,
            "KPTEST0001",
            new RegReff("RG00000001", "MR0001", "Pasien Tes"),
            new DokterReff("DR00000001", "Dr Tes"),
            new LayananReff("LYJ01", "Apotek RJ"),
            new LayananReff("LYR01", "Poli Dalam"),
            TipeJaminan(),
            TipeBrgType.Default.ToReff(),
            NilaiPenjualanType.Default,
            AuditTrailType.Create("U1", new DateTime(2026, 8, 6, 10, 0, 0)),
            []);
    }

    [Fact]
    public void CreateFromResep_CopiesHeaderAndItems()
    {
        var resep = BuildResep(
            ResepObatType.Create(1, Brg("BRG01", "Obat A"), Satuan(), 10, 0, "3x1", "3x1 tablet", AppConst.DASH),
            ResepObatType.Create(2, Brg("BRG02", "Obat B"), Satuan(), 5, 0, "2x1", "2x1 tablet", AppConst.DASH));

        var penjualan = PenjualanModel.CreateFromResep(resep, TipeJaminan(), LayananJual(), "U1");

        penjualan.PenjualanId.Should().StartWith("DU");
        penjualan.ResepId.Should().Be("KPTEST0001");
        penjualan.Register.RegId.Should().Be("RG00000001");
        penjualan.Dokter.DokterId.Should().Be("DR00000001");
        penjualan.Layanan.LayananId.Should().Be("LYJ01");
        penjualan.LayananResep.LayananId.Should().Be("LYR01");
        penjualan.TipeJaminan.TipeJaminanId.Should().Be("00000");
        penjualan.ListItem.Should().HaveCount(2);
        penjualan.ListItem.Select(x => x.NoUrut).Should().Equal(1, 2);
        penjualan.ListItem.First().PenjualanItemId.Should().Be(penjualan.PenjualanId + "001");
        penjualan.Nilai.GrandTotal.Should().Be(0);
        penjualan.AuditTrail.Created.UserId.Should().Be("U1");
        penjualan.AuditTrail.IsVoided.Should().BeFalse();
    }

    [Fact]
    public void CreateFromResep_CopiesRacikItems()
    {
        var obatRacik = ResepObatType.Create(1, Brg("RACIK1", "Racikan"), Satuan(), 1, 0, "3x1", "3x1 tablet", AppConst.DASH);
        obatRacik.AddItemRacik(Brg("BRG01", "Komponen A"), Satuan(), 2, 0.5m, "0.5");
        obatRacik.AddItemRacik(Brg("BRG02", "Komponen B"), Satuan(), 1, 0.25m, "0.25");

        var penjualan = PenjualanModel.CreateFromResep(BuildResep(obatRacik), TipeJaminan(), LayananJual(), "U1");

        var item = penjualan.ListItem.Single();
        item.ListItemRacik.Should().HaveCount(2);
        item.ListItemRacik.Select(x => x.Brg.BrgId).Should().Equal("BRG01", "BRG02");
    }

    [Fact]
    public void AddItem_AddsLineAndRecalculates()
    {
        var penjualan = BuildEmptyPenjualan();
        var nilai = NilaiItemType.Create(2, 1000, 0, 100, 0, 0, 0, 0, 0);

        penjualan.AddItem(Brg("BRG01", "Obat A"), Satuan(), 2, Etiket(), nilai);

        var item = penjualan.ListItem.Single();
        item.NoUrut.Should().Be(1);
        item.PenjualanItemId.Should().Be("DUTST00001001");
        item.Qty.Should().Be(2);
        item.Nilai.SubTotal.Should().Be(2000);
        penjualan.Nilai.SumSubTotal.Should().Be(2000);
        penjualan.Nilai.SumBiaya.Should().Be(100);
        penjualan.Nilai.GrandTotal.Should().Be(2100);
    }

    [Fact]
    public void AddItem_DuplicateBrg_Throws()
    {
        var penjualan = BuildEmptyPenjualan();
        var brg = Brg("BRG01", "Obat A");
        penjualan.AddItem(brg, Satuan(), 1, Etiket(), NilaiItemType.Default);

        var act = () => penjualan.AddItem(brg, Satuan(), 1, Etiket(), NilaiItemType.Default);

        act.Should().Throw<ArgumentException>().WithMessage("*duplikasi*");
    }

    [Fact]
    public void RemoveItem_RemovesAndRenumbers()
    {
        var penjualan = BuildEmptyPenjualan();
        penjualan.AddItem(Brg("BRG01", "Obat A"), Satuan(), 1, Etiket(), NilaiItemType.Default);
        penjualan.AddItem(Brg("BRG02", "Obat B"), Satuan(), 1, Etiket(), NilaiItemType.Default);

        penjualan.RemoveItem(Brg("BRG01", "Obat A"));

        penjualan.ListItem.Should().HaveCount(1);
        penjualan.ListItem.Single().Brg.BrgId.Should().Be("BRG02");
        penjualan.ListItem.Single().NoUrut.Should().Be(1);
    }

    [Fact]
    public void AddItemRacik_AddsUnderParent()
    {
        var penjualan = BuildEmptyPenjualan();
        var parent = Brg("RACIK1", "Racikan");
        penjualan.AddItem(parent, Satuan(), 1, Etiket(), NilaiItemType.Default);

        penjualan.AddItemRacik(parent, Brg("BRG01", "Komponen A"), Satuan(), 2, 0.5m, "0.5");

        penjualan.ListItem.Single().ListItemRacik.Should().ContainSingle(x => x.Brg.BrgId == "BRG01");
    }

    [Fact]
    public void AddItemRacik_MissingParent_Throws()
    {
        var penjualan = BuildEmptyPenjualan();

        var act = () => penjualan.AddItemRacik(
            Brg("RACIK1", "Racikan"),
            Brg("BRG01", "Komponen A"),
            Satuan(),
            1,
            1,
            "1");

        act.Should().Throw<KeyNotFoundException>().WithMessage("*tidak ditemukan*");
    }

    [Fact]
    public void RemoveItemRacik_WhenLastRacik_RemovesParent()
    {
        var penjualan = BuildEmptyPenjualan();
        var parent = Brg("RACIK1", "Racikan");
        penjualan.AddItem(parent, Satuan(), 1, Etiket(), NilaiItemType.Default);
        penjualan.AddItemRacik(parent, Brg("BRG01", "Komponen A"), Satuan(), 1, 1, "1");

        penjualan.RemoveItemRacik(parent, Brg("BRG01", "Komponen A"));

        penjualan.ListItem.Should().BeEmpty();
    }

    [Fact]
    public void ApplyNilaiItem_UpdatesLineAndHeaderTotals()
    {
        var penjualan = BuildEmptyPenjualan();
        var brg = Brg("BRG01", "Obat A");
        penjualan.AddItem(brg, Satuan(), 2, Etiket(), NilaiItemType.Default);

        penjualan.ApplyNilaiItem(brg, NilaiItemType.Create(2, 5000, 500, 200, 0, 0, 0, 0, 0));

        var item = penjualan.ListItem.Single();
        item.Nilai.Harga.Should().Be(5000);
        item.Nilai.SubTotal.Should().Be(9500);
        penjualan.Nilai.SumSubTotal.Should().Be(9500);
        penjualan.Nilai.SumBiaya.Should().Be(200);
        penjualan.Nilai.GrandTotal.Should().Be(9700);
    }

    [Fact]
    public void SetHeaderAdjustment_AppliesDiskonAndBiayaLain()
    {
        var penjualan = BuildEmptyPenjualan();
        penjualan.AddItem(
            Brg("BRG01", "Obat A"),
            Satuan(),
            1,
            Etiket(),
            NilaiItemType.Create(1, 10000, 0, 0, 0, 0, 0, 0, 0));

        penjualan.SetHeaderAdjustment(diskonLain: 1000, biayaLain: 500, pembulatan: 0, bulat: 0);

        penjualan.Nilai.DiskonLain.Should().Be(1000);
        penjualan.Nilai.BiayaLain.Should().Be(500);
        penjualan.Nilai.GrandTotal.Should().Be(9500);
    }

    [Fact]
    public void Void_MarksHeaderAndLinesVoided()
    {
        var penjualan = BuildEmptyPenjualan();
        penjualan.AddItem(
            Brg("BRG01", "Obat A"),
            Satuan(),
            1,
            Etiket(),
            NilaiItemType.Create(1, 1000, 0, 0, 0, 0, 0, 0, 0));

        penjualan.Void("U2");

        penjualan.AuditTrail.IsVoided.Should().BeTrue();
        penjualan.AuditTrail.Voided.UserId.Should().Be("U2");
        penjualan.ListItem.Single().IsVoided.Should().BeTrue();
        penjualan.Nilai.GrandTotal.Should().Be(0);
    }

    [Fact]
    public void AddItem_WhenVoided_Throws()
    {
        var penjualan = BuildEmptyPenjualan();
        penjualan.Void("U2");

        var act = () => penjualan.AddItem(
            Brg("BRG01", "Obat A"),
            Satuan(),
            1,
            Etiket(),
            NilaiItemType.Default);

        act.Should().Throw<InvalidOperationException>().WithMessage("*sudah void*");
    }
}
