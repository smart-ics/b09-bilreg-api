using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.PurchaseContext.DeliveryFeature;
using Bilreg.Domain.PurchaseContext.PurchaseOrderFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Infrastructure.PurchaseContext.DeliveryFeature;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.PurchaseContext.DeliveryFeature;

public class DeliveryOrderRepoTest
{
    private const string DoId = "DM0114280A";
    private const string PoId = "PO26100001";
    private const string SuratJalan = "SJ-VENDOR-9999";
    private const string SupplierId = "SUP001";
    private const string SupplierName = "PT Kimia Farma";
    private const string LayananId = "GDFAR";
    private const string LayananName = "Gudang Farmasi Utama";
    private const string UserId = "USR01";

    private readonly Mock<IDeliveryOrderDal> _orderDalMock;
    private readonly Mock<IDeliveryOrderItemDal> _itemDalMock;
    private readonly Mock<IDeliveryInspectionDal> _inspectionDalMock;
    private readonly DeliveryOrderRepo _sut;

    public DeliveryOrderRepoTest()
    {
        _orderDalMock = new Mock<IDeliveryOrderDal>();
        _itemDalMock = new Mock<IDeliveryOrderItemDal>();
        _inspectionDalMock = new Mock<IDeliveryInspectionDal>();
        _sut = new DeliveryOrderRepo(_orderDalMock.Object, _itemDalMock.Object, _inspectionDalMock.Object);
    }

    private static DeliveryOrderModel BuildModel(int itemCount = 1, DateTime? today = null)
    {
        var refDate = today ?? new DateTime(2026, 10, 15, 10, 0, 0);
        var supplier = new PartnerReff(SupplierId, SupplierName);
        var gudang = new LayananReff(LayananId, LayananName);

        var model = new DeliveryOrderModel(
            DoId,
            SuratJalan,
            PoId,
            refDate,
            supplier,
            gudang,
            "Pengiriman Reguler",
            DeliveryOrderStatusEnum.Draft,
            0m, 0m, 0m, 0m, 0m, 0m, 0m,
            AuditInfoType.Default,
            AuditTrailType.Create(UserId, refDate),
            []);

        for (var i = 1; i <= itemCount; i++)
        {
            var inspection = DeliveryInspectionType.Create(
                qtyKirim: 10m,
                qtyDiterima: 10m,
                qtyDitolak: 0m,
                alasanTolak: string.Empty,
                isBonus: false,
                hasJaminanRetur: false,
                noSuratJaminan: string.Empty,
                poId: PoId,
                poNoUrut: i);

            var item = DeliveryOrderItemType.Create(
                noUrut: i,
                brg: new BrgReff($"BRG000{i}", $"Obat Paten {i}"),
                satuan: new SatuanType("BOX", "Box"),
                konversi: 10,
                harga: 50000m,
                diskon: 5000m,
                taxPercentage: 11m,
                noBatch: $"BATCH-00{i}",
                tglEd: refDate.AddMonths(24),
                inspection: inspection,
                het: 65000m,
                referenceDate: refDate);

            model.AddItem(item, refDate);
        }

        return model;
    }

    private static DeliveryOrderDto BuildOrderDto(DeliveryOrderStatusEnum status = DeliveryOrderStatusEnum.Draft)
    {
        return new DeliveryOrderDto
        {
            DoId = DoId,
            DoDate = new DateTime(2026, 10, 15, 10, 0, 0),
            PetugasId = UserId,
            PoId = PoId,
            SuratJalanNo = SuratJalan,
            LayananId = LayananId,
            LayananName = LayananName,
            SupplierId = SupplierId,
            SupplierName = SupplierName,
            Total = 499500m,
            Keterangan = "Pengiriman Reguler",
            PembelianBebas = false,
            TglVoid = status == DeliveryOrderStatusEnum.Voided ? new DateTime(2026, 10, 15, 12, 0, 0) : new DateTime(3000, 1, 1),
            JamVoid = status == DeliveryOrderStatusEnum.Voided ? "12:00:00" : "00:00:00",
            PetugasVoid = status == DeliveryOrderStatusEnum.Voided ? "USER_VOID" : string.Empty,
            CrtTgl = new DateTime(2026, 10, 15, 10, 0, 0),
            CrtUsr = UserId,
            UpdTgl = new DateTime(3000, 1, 1),
            UpdUsr = string.Empty
        };
    }

    private static List<DeliveryOrderItemDto> BuildItemDtos()
    {
        return
        [
            new DeliveryOrderItemDto
            {
                DoId = DoId,
                NoUrut = 1,
                BrgId = "BRG0001",
                BrgName = "Obat Paten 1",
                Harga = 50000m,
                Diskon = 5000m,
                Qty = 10m,
                SatuanId = "BOX",
                SatuanName = "Box",
                SubTotal = 450000m,
                Tax = 49500m,
                Total = 499500m,
                NoBatch = "BATCH-001",
                TglEd = new DateTime(2028, 10, 15),
                IsVoid = false,
                Konversi = 10,
                Het = 65000m,
                Biaya = 0m
            }
        ];
    }

    private static List<DeliveryInspectionDto> BuildInspectionDtos(string status = "Draft")
    {
        return
        [
            new DeliveryInspectionDto
            {
                DoId = DoId,
                NoUrut = 1,
                BrgId = "BRG0001",
                BrgName = "Obat Paten 1",
                PoId = PoId,
                PoNoUrut = 1,
                QtyKirim = 10m,
                QtyDiterima = 10m,
                QtyDitolak = 0m,
                AlasanTolak = string.Empty,
                IsBonus = false,
                HasJaminanRetur = false,
                NoSuratJaminan = string.Empty,
                Status = status,
                ConfirmedAt = status == "Confirmed" ? new DateTime(2026, 10, 15, 11, 0, 0) : null,
                ConfirmedBy = status == "Confirmed" ? "APOTEKER01" : string.Empty,
                VoidedAt = status == "Voided" ? new DateTime(2026, 10, 15, 12, 0, 0) : null,
                VoidedBy = status == "Voided" ? "USER_VOID" : string.Empty,
                CreatedAt = new DateTime(2026, 10, 15, 10, 0, 0),
                CreatedBy = UserId,
                UpdatedAt = new DateTime(2026, 10, 15, 10, 0, 0),
                UpdatedBy = UserId
            }
        ];
    }

    [Fact]
    public void GivenNewDraftDeliveryOrder_WhenSaveChanges_ThenInsertHeaderAndReplaceItemsAndInspections()
    {
        // Arrange
        var model = BuildModel();
        _orderDalMock
            .Setup(x => x.GetData(It.IsAny<IDeliveryOrderKey>()))
            .Returns((DeliveryOrderDto)null!);

        DeliveryOrderDto? capturedHeader = null;
        IEnumerable<DeliveryOrderItemDto>? capturedItems = null;
        IEnumerable<DeliveryInspectionDto>? capturedInspections = null;

        _orderDalMock
            .Setup(x => x.Insert(It.IsAny<DeliveryOrderDto>()))
            .Callback<DeliveryOrderDto>(dto => capturedHeader = dto);
        _itemDalMock
            .Setup(x => x.Insert(It.IsAny<IEnumerable<DeliveryOrderItemDto>>()))
            .Callback<IEnumerable<DeliveryOrderItemDto>>(items => capturedItems = items);
        _inspectionDalMock
            .Setup(x => x.Insert(It.IsAny<IEnumerable<DeliveryInspectionDto>>()))
            .Callback<IEnumerable<DeliveryInspectionDto>>(inspections => capturedInspections = inspections);

        // Act
        _sut.SaveChanges(model);

        // Assert
        _orderDalMock.Verify(x => x.Insert(It.IsAny<DeliveryOrderDto>()), Times.Once);
        _orderDalMock.Verify(x => x.Update(It.IsAny<DeliveryOrderDto>()), Times.Never);
        _itemDalMock.Verify(x => x.Delete(It.Is<IDeliveryOrderKey>(k => k.DeliveryOrderId == DoId)), Times.Once);
        _itemDalMock.Verify(x => x.Insert(It.IsAny<IEnumerable<DeliveryOrderItemDto>>()), Times.Once);
        _inspectionDalMock.Verify(x => x.Delete(It.Is<IDeliveryOrderKey>(k => k.DeliveryOrderId == DoId)), Times.Once);
        _inspectionDalMock.Verify(x => x.Insert(It.IsAny<IEnumerable<DeliveryInspectionDto>>()), Times.Once);

        capturedHeader.Should().NotBeNull();
        capturedHeader!.DoId.Should().Be(DoId);
        capturedHeader.SuratJalanNo.Should().Be(SuratJalan);
        capturedHeader.PoId.Should().Be(PoId);
        capturedHeader.SupplierId.Should().Be(SupplierId);
        capturedHeader.LayananId.Should().Be(LayananId);
        capturedHeader.Total.Should().Be(model.GrandTotal);
        capturedHeader.TglVoid.Year.Should().Be(3000);

        capturedItems.Should().NotBeNull();
        capturedItems.Should().HaveCount(1);
        capturedItems!.First().BrgId.Should().Be("BRG0001");
        capturedItems!.First().Qty.Should().Be(10m);
        capturedItems!.First().Het.Should().Be(65000m);

        capturedInspections.Should().NotBeNull();
        capturedInspections.Should().HaveCount(1);
        capturedInspections!.First().Status.Should().Be("Draft");
        capturedInspections!.First().QtyKirim.Should().Be(10m);
        capturedInspections!.First().QtyDiterima.Should().Be(10m);
    }

    [Fact]
    public void GivenExistingDeliveryOrder_WhenSaveChanges_ThenUpdateHeaderAndReplaceItemsAndInspections()
    {
        // Arrange
        var model = BuildModel();
        _orderDalMock
            .Setup(x => x.GetData(It.IsAny<IDeliveryOrderKey>()))
            .Returns(BuildOrderDto());

        _itemDalMock
            .Setup(x => x.ListData(It.IsAny<IDeliveryOrderKey>()))
            .Returns(BuildItemDtos());
        _inspectionDalMock
            .Setup(x => x.ListData(It.IsAny<IDeliveryOrderKey>()))
            .Returns(BuildInspectionDtos());

        // Act
        _sut.SaveChanges(model);

        // Assert
        _orderDalMock.Verify(x => x.Update(It.Is<DeliveryOrderDto>(d => d.DoId == DoId)), Times.Once);
        _orderDalMock.Verify(x => x.Insert(It.IsAny<DeliveryOrderDto>()), Times.Never);
        _itemDalMock.Verify(x => x.Delete(It.Is<IDeliveryOrderKey>(k => k.DeliveryOrderId == DoId)), Times.Once);
        _itemDalMock.Verify(x => x.Insert(It.IsAny<IEnumerable<DeliveryOrderItemDto>>()), Times.Once);
        _inspectionDalMock.Verify(x => x.Delete(It.Is<IDeliveryOrderKey>(k => k.DeliveryOrderId == DoId)), Times.Once);
        _inspectionDalMock.Verify(x => x.Insert(It.IsAny<IEnumerable<DeliveryInspectionDto>>()), Times.Once);
    }

    [Fact]
    public void GivenConfirmedDeliveryOrder_WhenSaveChanges_ThenPersistConfirmedStatusAndAudit()
    {
        // Arrange
        var model = BuildModel();
        var confirmDate = new DateTime(2026, 10, 15, 11, 0, 0);
        model.Confirm("APOTEKER01", confirmDate);

        _orderDalMock
            .Setup(x => x.GetData(It.IsAny<IDeliveryOrderKey>()))
            .Returns(BuildOrderDto());
        _itemDalMock
            .Setup(x => x.ListData(It.IsAny<IDeliveryOrderKey>()))
            .Returns(BuildItemDtos());
        _inspectionDalMock
            .Setup(x => x.ListData(It.IsAny<IDeliveryOrderKey>()))
            .Returns(BuildInspectionDtos());

        IEnumerable<DeliveryInspectionDto>? capturedInspections = null;
        _inspectionDalMock
            .Setup(x => x.Insert(It.IsAny<IEnumerable<DeliveryInspectionDto>>()))
            .Callback<IEnumerable<DeliveryInspectionDto>>(rows => capturedInspections = rows);

        // Act
        _sut.SaveChanges(model);

        // Assert
        capturedInspections.Should().NotBeNull();
        var inspection = capturedInspections!.First();
        inspection.Status.Should().Be("Confirmed");
        inspection.ConfirmedBy.Should().Be("APOTEKER01");
        inspection.ConfirmedAt.Should().Be(confirmDate);
    }

    [Fact]
    public void GivenVoidedDeliveryOrder_WhenSaveChanges_ThenPersistVoidedDateAndVoidedStatus()
    {
        // Arrange
        var model = BuildModel();
        var voidDate = new DateTime(2026, 10, 15, 12, 0, 0);
        model.Void("USER_VOID", voidDate);

        _orderDalMock
            .Setup(x => x.GetData(It.IsAny<IDeliveryOrderKey>()))
            .Returns(BuildOrderDto());
        _itemDalMock
            .Setup(x => x.ListData(It.IsAny<IDeliveryOrderKey>()))
            .Returns(BuildItemDtos());
        _inspectionDalMock
            .Setup(x => x.ListData(It.IsAny<IDeliveryOrderKey>()))
            .Returns(BuildInspectionDtos());

        DeliveryOrderDto? capturedHeader = null;
        IEnumerable<DeliveryOrderItemDto>? capturedItems = null;
        IEnumerable<DeliveryInspectionDto>? capturedInspections = null;

        _orderDalMock
            .Setup(x => x.Update(It.IsAny<DeliveryOrderDto>()))
            .Callback<DeliveryOrderDto>(dto => capturedHeader = dto);
        _itemDalMock
            .Setup(x => x.Insert(It.IsAny<IEnumerable<DeliveryOrderItemDto>>()))
            .Callback<IEnumerable<DeliveryOrderItemDto>>(rows => capturedItems = rows);
        _inspectionDalMock
            .Setup(x => x.Insert(It.IsAny<IEnumerable<DeliveryInspectionDto>>()))
            .Callback<IEnumerable<DeliveryInspectionDto>>(rows => capturedInspections = rows);

        // Act
        _sut.SaveChanges(model);

        // Assert
        capturedHeader.Should().NotBeNull();
        capturedHeader!.TglVoid.Should().Be(voidDate);
        capturedHeader.PetugasVoid.Should().Be("USER_VOID");

        capturedItems.Should().NotBeNull();
        capturedItems!.First().IsVoid.Should().BeTrue();

        capturedInspections.Should().NotBeNull();
        var inspection = capturedInspections!.First();
        inspection.Status.Should().Be("Voided");
        inspection.VoidedBy.Should().Be("USER_VOID");
        inspection.VoidedAt.Should().Be(voidDate);
    }

    [Fact]
    public void GivenExistingDeliveryOrderInDb_WhenLoadEntity_ThenReconstituteFullAggregateModel()
    {
        // Arrange
        var key = new DeliveryOrderKey(DoId);
        _orderDalMock
            .Setup(x => x.GetData(key))
            .Returns(BuildOrderDto(DeliveryOrderStatusEnum.Draft));
        _itemDalMock
            .Setup(x => x.ListData(key))
            .Returns(BuildItemDtos());
        _inspectionDalMock
            .Setup(x => x.ListData(key))
            .Returns(BuildInspectionDtos("Draft"));

        // Act
        var result = _sut.LoadEntity(key);

        // Assert
        result.HasValue.Should().BeTrue();
        var actual = result.Value;
        actual.DeliveryOrderId.Should().Be(DoId);
        actual.SuratJalanNo.Should().Be(SuratJalan);
        actual.PoId.Should().Be(PoId);
        actual.Supplier.PartnerId.Should().Be(SupplierId);
        actual.Supplier.PartnerName.Should().Be(SupplierName);
        actual.Gudang.LayananId.Should().Be(LayananId);
        actual.Gudang.LayananName.Should().Be(LayananName);
        actual.Status.Should().Be(DeliveryOrderStatusEnum.Draft);
        actual.Total.Should().Be(499500m);
        actual.GrandTotal.Should().Be(499500m);

        actual.ListItem.Should().HaveCount(1);
        var item = actual.ListItem.First();
        item.NoUrut.Should().Be(1);
        item.Brg.BrgId.Should().Be("BRG0001");
        item.Harga.Should().Be(50000m);
        item.Diskon.Should().Be(5000m);
        item.Qty.Should().Be(10m);
        item.SubTotal.Should().Be(450000m);
        item.Tax.Should().Be(49500m);
        item.Total.Should().Be(499500m);
        item.Het.Should().Be(65000m);

        item.Inspection.Should().NotBeNull();
        item.Inspection.QtyKirim.Should().Be(10m);
        item.Inspection.QtyDiterima.Should().Be(10m);
        item.Inspection.QtyDitolak.Should().Be(0m);
        item.Inspection.PoId.Should().Be(PoId);
    }

    [Fact]
    public void GivenHistoricalLegacyDeliveryOrderWithoutInspeksi_WhenLoadEntity_ThenGracefulFallbackInspectionValues()
    {
        // Arrange
        var key = new DeliveryOrderKey(DoId);
        _orderDalMock
            .Setup(x => x.GetData(key))
            .Returns(BuildOrderDto(DeliveryOrderStatusEnum.Confirmed));
        _itemDalMock
            .Setup(x => x.ListData(key))
            .Returns(BuildItemDtos());
        _inspectionDalMock
            .Setup(x => x.ListData(key))
            .Returns(new List<DeliveryInspectionDto>()); // Empty inspection table (legacy)

        // Act
        var result = _sut.LoadEntity(key);

        // Assert
        result.HasValue.Should().BeTrue();
        var actual = result.Value;
        actual.Status.Should().Be(DeliveryOrderStatusEnum.Confirmed);

        actual.ListItem.Should().HaveCount(1);
        var item = actual.ListItem.First();
        item.Inspection.Should().NotBeNull();
        item.Inspection.QtyKirim.Should().Be(item.Qty);
        item.Inspection.QtyDiterima.Should().Be(item.Qty);
        item.Inspection.QtyDitolak.Should().Be(0m);
        item.Inspection.AlasanTolak.Should().BeEmpty();
    }

    [Fact]
    public void GivenNonExistentDeliveryOrder_WhenLoadEntity_ThenReturnNone()
    {
        // Arrange
        var key = new DeliveryOrderKey("NON_EXISTENT");
        _orderDalMock
            .Setup(x => x.GetData(key))
            .Returns((DeliveryOrderDto)null!);

        // Act
        var result = _sut.LoadEntity(key);

        // Assert
        result.HasValue.Should().BeFalse();
    }

    [Fact]
    public void GivenDeliveryOrderKey_WhenDeleteEntity_ThenDeleteAllChildTablesAndHeader()
    {
        // Arrange
        var key = new DeliveryOrderKey(DoId);

        // Act
        _sut.DeleteEntity(key);

        // Assert
        _itemDalMock.Verify(x => x.Delete(It.Is<IDeliveryOrderKey>(k => k.DeliveryOrderId == DoId)), Times.Once);
        _inspectionDalMock.Verify(x => x.Delete(It.Is<IDeliveryOrderKey>(k => k.DeliveryOrderId == DoId)), Times.Once);
        _orderDalMock.Verify(x => x.Delete(It.Is<IDeliveryOrderKey>(k => k.DeliveryOrderId == DoId)), Times.Once);
    }

    [Fact]
    public void MapToItemDto_WhenItemIsBonus_MapsZeroSubTotalAndTotal()
    {
        // Arrange
        var refDate = new DateTime(2026, 10, 15, 10, 0, 0);
        var supplier = new PartnerReff(SupplierId, SupplierName);
        var gudang = new LayananReff(LayananId, LayananName);

        var model = new DeliveryOrderModel(
            DoId,
            SuratJalan,
            PoId,
            refDate,
            supplier,
            gudang,
            "Pengiriman Reguler",
            DeliveryOrderStatusEnum.Draft,
            0m, 0m, 0m, 0m, 0m, 0m, 0m,
            AuditInfoType.Default,
            AuditTrailType.Create(UserId, refDate),
            []);

        var inspBonus = DeliveryInspectionType.Create(
            qtyKirim: 1m,
            qtyDiterima: 1m,
            qtyDitolak: 0m,
            alasanTolak: string.Empty,
            isBonus: true,
            hasJaminanRetur: false,
            noSuratJaminan: string.Empty,
            poId: PoId,
            poNoUrut: 1);

        var item = DeliveryOrderItemType.Create(
            noUrut: 1,
            brg: new BrgReff("BRG0001", "Barang Bonus"),
            satuan: new SatuanType("BOX", "Box"),
            konversi: 10,
            harga: 272323.92m,
            diskon: 0m,
            taxPercentage: 11m,
            noBatch: "BATCH-BONUS-01",
            tglEd: refDate.AddMonths(24),
            inspection: inspBonus,
            het: 0m,
            referenceDate: refDate);

        model.AddItem(item, refDate);

        _orderDalMock
            .Setup(x => x.GetData(It.IsAny<IDeliveryOrderKey>()))
            .Returns((DeliveryOrderDto)null!);

        IEnumerable<DeliveryOrderItemDto>? capturedItems = null;
        _itemDalMock
            .Setup(x => x.Insert(It.IsAny<IEnumerable<DeliveryOrderItemDto>>()))
            .Callback<IEnumerable<DeliveryOrderItemDto>>(items => capturedItems = items);

        // Act
        _sut.SaveChanges(model);

        // Assert
        capturedItems.Should().NotBeNull();
        var itemDto = capturedItems!.Single();
        itemDto.Harga.Should().Be(272323.92m);
        itemDto.Diskon.Should().Be(272323.92m);
        itemDto.SubTotal.Should().Be(0m);
        itemDto.Tax.Should().Be(0m);
        itemDto.Total.Should().Be(0m);
    }
}
