using Bilreg.Domain.PurchaseContext.DeliveryFeature;
using Bilreg.Infrastructure.PurchaseContext.DeliveryFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.PurchaseContext.DeliveryFeature;

public class DeliveryOrderDalTest
{
    private readonly DeliveryOrderDal _orderDal;
    private readonly DeliveryOrderItemDal _itemDal;
    private readonly DeliveryInspectionDal _inspectionDal;

    public DeliveryOrderDalTest()
    {
        var opt = Options.Create(new DatabaseOptions
        {
            ServerName = "dev.smart-ics.com",
            DbName = "HOSPITAL_PKL"
        });
        _orderDal = new DeliveryOrderDal(opt);
        _itemDal = new DeliveryOrderItemDal(opt);
        _inspectionDal = new DeliveryInspectionDal(opt);
    }

    private static IDeliveryOrderKey FakerKey(string doId = "DO_TEST01") => new DeliveryOrderKey(doId);

    private static DeliveryOrderDto FakerOrder(string doId = "DO_TEST01")
    {
        return new DeliveryOrderDto
        {
            DoId = doId,
            DoDate = new DateTime(2026, 10, 15, 10, 30, 0),
            PetugasId = "USR01",
            PoId = "PO26100001",
            SuratJalanNo = "SJ-2026-9999",
            LayananId = "GDFAR",
            SupplierId = "SUP001",
            Total = 1500000m,
            Keterangan = "Pengiriman Obat Batch 1",
            PembelianBebas = false,
            TglVoid = new DateTime(3000, 1, 1),
            JamVoid = "00:00:00",
            PetugasVoid = string.Empty,
            CrtTgl = new DateTime(2026, 10, 15, 10, 0, 0),
            CrtUsr = "USR01",
            UpdTgl = new DateTime(3000, 1, 1),
            UpdUsr = string.Empty
        };
    }

    private static DeliveryOrderItemDto FakerItem(string doId = "DO_TEST01", int noUrut = 1)
    {
        return new DeliveryOrderItemDto
        {
            DoId = doId,
            NoUrut = noUrut,
            BrgId = "BRG0001",
            Harga = 50000m,
            Diskon = 5000m,
            Qty = 10m,
            SatuanId = "BOX",
            SubTotal = 450000m,
            Tax = 49500m,
            Total = 499500m,
            NoBatch = "BATCH-ABC-01",
            TglEd = new DateTime(2028, 6, 30),
            IsVoid = false,
            Konversi = 10,
            Het = 60000m,
            Biaya = 0m
        };
    }

    private static DeliveryInspectionDto FakerInspection(string doId = "DO_TEST01", int noUrut = 1)
    {
        return new DeliveryInspectionDto
        {
            DoId = doId,
            NoUrut = noUrut,
            BrgId = "BRG0001",
            PoId = "PO26100001",
            PoNoUrut = 1,
            QtyKirim = 10m,
            QtyDiterima = 10m,
            QtyDitolak = 0m,
            AlasanTolak = string.Empty,
            IsBonus = false,
            HasJaminanRetur = false,
            NoSuratJaminan = string.Empty,
            Status = "Draft",
            CreatedAt = new DateTime(2026, 10, 15, 10, 0, 0),
            CreatedBy = "USR01",
            UpdatedAt = new DateTime(2026, 10, 15, 10, 0, 0),
            UpdatedBy = "USR01"
        };
    }

    // ==========================================
    // DeliveryOrderDal Tests (TB_TRS_DO)
    // ==========================================

    [Fact]
    public void Order_Insert_Then_GetData_ShouldReturnMatchingValues()
    {
        using var trans = TransHelper.NewScope();
        var expected = FakerOrder("DO_TEST01");

        _orderDal.Insert(expected);
        var actual = _orderDal.GetData(FakerKey("DO_TEST01"));

        actual.Should().NotBeNull();
        actual.DoId.Should().Be(expected.DoId);
        actual.PetugasId.Should().Be(expected.PetugasId);
        actual.PoId.Should().Be(expected.PoId);
        actual.SuratJalanNo.Should().Be(expected.SuratJalanNo);
        actual.LayananId.Should().Be(expected.LayananId);
        actual.SupplierId.Should().Be(expected.SupplierId);
        actual.Total.Should().Be(expected.Total);
        actual.Keterangan.Should().Be(expected.Keterangan);
        actual.PembelianBebas.Should().Be(expected.PembelianBebas);
        actual.TglVoid.Year.Should().Be(3000);
    }

    [Fact]
    public void Order_Update_ShouldUpdateHeaderValues()
    {
        using var trans = TransHelper.NewScope();
        var initial = FakerOrder("DO_TEST02");
        _orderDal.Insert(initial);

        initial.Total = 2500000m;
        initial.Keterangan = "Updated Keterangan";
        initial.SuratJalanNo = "SJ-UPDATED-001";
        initial.UpdTgl = new DateTime(2026, 10, 15, 11, 0, 0);
        initial.UpdUsr = "USR02";
        _orderDal.Update(initial);

        var actual = _orderDal.GetData(FakerKey("DO_TEST02"));
        actual.Should().NotBeNull();
        actual.Total.Should().Be(2500000m);
        actual.Keterangan.Should().Be("Updated Keterangan");
        actual.SuratJalanNo.Should().Be("SJ-UPDATED-001");
        actual.UpdUsr.Should().Be("USR02");
    }

    [Fact]
    public void Order_Delete_ShouldRemoveRecord()
    {
        using var trans = TransHelper.NewScope();
        var order = FakerOrder("DO_TEST03");
        _orderDal.Insert(order);

        _orderDal.Delete(FakerKey("DO_TEST03"));

        var actual = _orderDal.GetData(FakerKey("DO_TEST03"));
        actual.Should().BeNull();
    }

    [Fact]
    public void Order_ListData_WithPeriode_ShouldIncludeInsertedRecord()
    {
        using var trans = TransHelper.NewScope();
        var order = FakerOrder("DO_TEST04");
        _orderDal.Insert(order);

        var periode = new Periode(new DateTime(2026, 10, 1), new DateTime(2026, 10, 31));
        var list = _orderDal.ListData(periode);

        list.Should().Contain(x => x.DoId == "DO_TEST04");
    }

    // ==========================================
    // DeliveryOrderItemDal Tests (TB_TRS_DO2)
    // ==========================================

    [Fact]
    public void Item_InsertSingle_And_ListData_ShouldReturnLineItems()
    {
        using var trans = TransHelper.NewScope();
        var header = FakerOrder("DO_IT01");
        _orderDal.Insert(header);

        var item = FakerItem("DO_IT01", 1);
        _itemDal.Insert(item);

        var list = _itemDal.ListData(FakerKey("DO_IT01")).ToList();

        list.Should().HaveCount(1);
        var actual = list[0];
        actual.DoId.Should().Be("DO_IT01");
        actual.NoUrut.Should().Be(1);
        actual.BrgId.Should().Be(item.BrgId);
        actual.Harga.Should().Be(item.Harga);
        actual.Diskon.Should().Be(item.Diskon);
        actual.Qty.Should().Be(item.Qty);
        actual.SatuanId.Should().Be(item.SatuanId);
        actual.SubTotal.Should().Be(item.SubTotal);
        actual.Tax.Should().Be(item.Tax);
        actual.Total.Should().Be(item.Total);
        actual.NoBatch.Should().Be(item.NoBatch);
        actual.Konversi.Should().Be(item.Konversi);
    }

    [Fact]
    public void Item_InsertBulk_ShouldInsertMultipleItems()
    {
        using var trans = TransHelper.NewScope();
        var header = FakerOrder("DO_IT02");
        _orderDal.Insert(header);

        var items = new List<DeliveryOrderItemDto>
        {
            FakerItem("DO_IT02", 1),
            FakerItem("DO_IT02", 2)
        };
        _itemDal.Insert(items);

        var list = _itemDal.ListData(FakerKey("DO_IT02")).ToList();
        list.Should().HaveCount(2);
        list.Select(x => x.NoUrut).Should().ContainInOrder(1, 2);
    }

    [Fact]
    public void Item_Delete_ShouldRemoveAllItemsForDeliveryOrder()
    {
        using var trans = TransHelper.NewScope();
        var header = FakerOrder("DO_IT03");
        _orderDal.Insert(header);

        var items = new List<DeliveryOrderItemDto>
        {
            FakerItem("DO_IT03", 1),
            FakerItem("DO_IT03", 2)
        };
        _itemDal.Insert(items);

        _itemDal.Delete(FakerKey("DO_IT03"));

        var list = _itemDal.ListData(FakerKey("DO_IT03"));
        list.Should().BeEmpty();
    }

    // ==========================================
    // DeliveryInspectionDal Tests (BILRG_DoInspeksi)
    // ==========================================

    [Fact]
    public void Inspection_Insert_Then_GetData_ShouldReturnMatchingInspection()
    {
        using var trans = TransHelper.NewScope();
        var inspection = FakerInspection("DO_IN01", 1);
        _inspectionDal.Insert(inspection);

        var actual = _inspectionDal.GetData(FakerKey("DO_IN01"), 1);

        actual.Should().NotBeNull();
        actual!.DoId.Should().Be("DO_IN01");
        actual.NoUrut.Should().Be(1);
        actual.BrgId.Should().Be(inspection.BrgId);
        actual.PoId.Should().Be(inspection.PoId);
        actual.PoNoUrut.Should().Be(inspection.PoNoUrut);
        actual.QtyKirim.Should().Be(inspection.QtyKirim);
        actual.QtyDiterima.Should().Be(inspection.QtyDiterima);
        actual.QtyDitolak.Should().Be(inspection.QtyDitolak);
        actual.Status.Should().Be("Draft");
    }

    [Fact]
    public void Inspection_Update_ShouldUpdateInspectionValues()
    {
        using var trans = TransHelper.NewScope();
        var inspection = FakerInspection("DO_IN02", 1);
        _inspectionDal.Insert(inspection);

        inspection.QtyDitolak = 2m;
        inspection.QtyDiterima = 8m;
        inspection.AlasanTolak = "Kemasan penyok";
        inspection.HasJaminanRetur = true;
        inspection.NoSuratJaminan = "SURAT-RETUR-01";
        inspection.Status = "Confirmed";
        inspection.ConfirmedAt = new DateTime(2026, 10, 15, 12, 0, 0);
        inspection.ConfirmedBy = "APOTEKER01";
        _inspectionDal.Update(inspection);

        var actual = _inspectionDal.GetData(FakerKey("DO_IN02"), 1);
        actual.Should().NotBeNull();
        actual!.QtyDitolak.Should().Be(2m);
        actual.QtyDiterima.Should().Be(8m);
        actual.AlasanTolak.Should().Be("Kemasan penyok");
        actual.HasJaminanRetur.Should().BeTrue();
        actual.NoSuratJaminan.Should().Be("SURAT-RETUR-01");
        actual.Status.Should().Be("Confirmed");
        actual.ConfirmedBy.Should().Be("APOTEKER01");
    }

    [Fact]
    public void Inspection_Delete_ShouldRemoveAllInspectionsForDeliveryOrder()
    {
        using var trans = TransHelper.NewScope();
        var inspections = new List<DeliveryInspectionDto>
        {
            FakerInspection("DO_IN03", 1),
            FakerInspection("DO_IN03", 2)
        };
        _inspectionDal.Insert(inspections);

        _inspectionDal.Delete(FakerKey("DO_IN03"));

        var list = _inspectionDal.ListData(FakerKey("DO_IN03"));
        list.Should().BeEmpty();
    }
}
