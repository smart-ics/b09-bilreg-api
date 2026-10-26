using Bilreg.Application.PurchaseContext.DeliveryFeature;
using Bilreg.Application.PurchaseContext.DeliveryFeature.UseCases;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.PurchaseContext.DeliveryFeature;
using Bilreg.Domain.PurchaseContext.PurchaseOrderFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.PurchaseContext.DeliveryFeature;

public class DeliveryQueryTest
{
    private readonly Mock<IDeliveryOrderRepo> _deliveryOrderRepoMock = new();
    private readonly Mock<IPoDeliveryTrackingDal> _poDeliveryTrackingDalMock = new();
    private readonly Mock<IDeliveryValidationDal> _deliveryValidationDalMock = new();

    private static DeliveryOrderModel CreateTestDeliveryOrder(string doId = "DM0114280A")
    {
        var supplier = new PartnerReff("SUP001", "PT Kimia Farma");
        var gudang = new LayananReff("GDFAR", "Gudang Farmasi Pusat");
        var refDate = new DateTime(2026, 10, 15, 10, 0, 0);

        var model = DeliveryOrderModel.Create(
            suratJalanNo: "SJ-KF-2026-001",
            poId: "PO26100001",
            supplier: supplier,
            gudang: gudang,
            keterangan: "Penerimaan rutin obat",
            userId: "USR01",
            occuredAt: refDate,
            poDate: "2026-10-10"
        );

        var inspection = DeliveryInspectionType.Create(
            qtyKirim: 10m,
            qtyDiterima: 8m,
            qtyDitolak: 2m,
            alasanTolak: "Kemasan penyok",
            isBonus: false,
            hasJaminanRetur: true,
            noSuratJaminan: "SJ-RET-01",
            poId: "PO26100001",
            poNoUrut: 1
        );

        model.AddItem(
            new BrgReff("BRG001", "Paracetamol 500mg"),
            new SatuanType("BOX", "Box"),
            konversi: 100,
            harga: 50000m,
            diskon: 5000m,
            taxPercentage: 11m,
            noBatch: "BATCH123",
            tglEd: refDate.AddYears(2),
            inspection: inspection,
            het: 55000m,
            referenceDate: refDate
        );

        return model;
    }

    // ==========================================
    // DeliveryGetQuery Tests
    // ==========================================

    [Fact]
    public async Task DeliveryGet_WhenOrderExists_ReturnsComprehensiveDetailsWithInspection()
    {
        // Arrange
        var model = CreateTestDeliveryOrder();
        _deliveryOrderRepoMock.Setup(x => x.LoadEntity(It.Is<IDeliveryOrderKey>(k => k.DeliveryOrderId == model.DeliveryOrderId)))
            .Returns(MayBe.From(model));

        var sut = new DeliveryGetHandler(_deliveryOrderRepoMock.Object);
        var query = new DeliveryGetQuery(model.DeliveryOrderId);

        // Act
        var response = await sut.Handle(query, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.DeliveryOrderId.Should().Be(model.DeliveryOrderId);
        response.SuratJalanNo.Should().Be("SJ-KF-2026-001");
        response.PoId.Should().Be("PO26100001");
        response.Supplier.PartnerId.Should().Be("SUP001");
        response.Supplier.PartnerName.Should().Be("PT Kimia Farma");
        response.Gudang.LayananId.Should().Be("GDFAR");
        response.Gudang.LayananName.Should().Be("Gudang Farmasi Pusat");
        response.Keterangan.Should().Be("Penerimaan rutin obat");
        response.Status.Should().Be("Draft");
        response.GrandTotal.Should().Be(model.GrandTotal);
        response.PoDate.Should().Be("2026-10-10");

        response.Created.Should().NotBeNull();
        response.Created.UserId.Should().Be("USR01");
        response.Created.Timestamp.Should().Be(new DateTime(2026, 10, 15, 10, 0, 0).ToString("yyyy-MM-dd HH:mm:ss"));

        response.ListItem.Should().HaveCount(1);
        var item = response.ListItem[0];
        item.NoUrut.Should().Be(1);
        item.BrgId.Should().Be("BRG001");
        item.BrgName.Should().Be("Paracetamol 500mg");
        item.SatuanId.Should().Be("BOX");
        item.SatuanName.Should().Be("Box");
        item.Konversi.Should().Be(100);
        item.Harga.Should().Be(50000m);
        item.Diskon.Should().Be(5000m);
        item.NoBatch.Should().Be("BATCH123");
        item.TglEd.Should().Be(new DateTime(2028, 10, 15).ToString("yyyy-MM-dd"));

        item.Het.Should().Be(55000m);
        item.Inspection.Should().NotBeNull();
        item.Inspection.QtyKirim.Should().Be(10m);
        item.Inspection.QtyDiterima.Should().Be(8m);
        item.Inspection.QtyDitolak.Should().Be(2m);
        item.Inspection.AlasanTolak.Should().Be("Kemasan penyok");
        item.Inspection.IsBonus.Should().BeFalse();
        item.Inspection.HasJaminanRetur.Should().BeTrue();
        item.Inspection.NoSuratJaminan.Should().Be("SJ-RET-01");
        item.Inspection.PoId.Should().Be("PO26100001");
        item.Inspection.PoNoUrut.Should().Be(1);
    }

    [Fact]
    public async Task DeliveryGet_WhenOrderNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _deliveryOrderRepoMock.Setup(x => x.LoadEntity(It.IsAny<IDeliveryOrderKey>()))
            .Returns(MayBe<DeliveryOrderModel>.None);

        var sut = new DeliveryGetHandler(_deliveryOrderRepoMock.Object);
        var query = new DeliveryGetQuery("DM9999999X");

        // Act
        var act = () => sut.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*DM9999999X*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task DeliveryGet_WhenIdIsNullOrEmpty_ThrowsArgumentException(string? invalidId)
    {
        // Arrange
        var sut = new DeliveryGetHandler(_deliveryOrderRepoMock.Object);
        var query = new DeliveryGetQuery(invalidId!);

        // Act
        var act = () => sut.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    // ==========================================
    // DeliveryListByPeriodeQuery Tests
    // ==========================================

    [Fact]
    public async Task DeliveryListByPeriode_WhenDataExists_ReturnsDeliveryList()
    {
        // Arrange
        var views = new List<DeliveryOrderView>
        {
            new(
                "DM0114280A",
                "SJ-001",
                "PO001",
                new DateTime(2026, 10, 15, 8, 30, 0),
                new PartnerReff("SUP01", "Supplier 1"),
                new LayananReff("GDFAR", "Gudang Farmasi"),
                "Ket 1",
                DeliveryOrderStatusEnum.Draft,
                1500000m
            ),
            new(
                "DM0114281A",
                "SJ-002",
                "PO002",
                new DateTime(2026, 10, 15, 9, 0, 0),
                new PartnerReff("SUP02", "Supplier 2"),
                new LayananReff("GDLOG", "Gudang Logistik"),
                "Ket 2",
                DeliveryOrderStatusEnum.Confirmed,
                2750000m
            )
        };

        _deliveryOrderRepoMock.Setup(x => x.ListData(It.IsAny<Periode>()))
            .Returns(MayBe.From<IEnumerable<DeliveryOrderView>>(views));

        var sut = new DeliveryListByPeriodeHandler(_deliveryOrderRepoMock.Object);
        var query = new DeliveryListByPeriodeQuery("2026-10-01", "2026-10-31");

        // Act
        var result = (await sut.Handle(query, CancellationToken.None)).ToList();

        // Assert
        result.Should().HaveCount(2);
        result[0].DeliveryOrderId.Should().Be("DM0114280A");
        result[0].SuratJalanNo.Should().Be("SJ-001");
        result[0].Status.Should().Be("Draft");
        result[0].Total.Should().Be(1500000m);

        result[1].DeliveryOrderId.Should().Be("DM0114281A");
        result[1].SuratJalanNo.Should().Be("SJ-002");
        result[1].Status.Should().Be("Confirmed");
        result[1].Total.Should().Be(2750000m);
    }

    [Fact]
    public async Task DeliveryListByPeriode_WhenNoDataExists_ReturnsEmpty()
    {
        // Arrange
        _deliveryOrderRepoMock.Setup(x => x.ListData(It.IsAny<Periode>()))
            .Returns(MayBe<IEnumerable<DeliveryOrderView>>.None);

        var sut = new DeliveryListByPeriodeHandler(_deliveryOrderRepoMock.Object);
        var query = new DeliveryListByPeriodeQuery("2026-10-01", "2026-10-31");

        // Act
        var result = await sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("", "2026-10-31")]
    [InlineData("2026-10-01", "")]
    [InlineData(null, "2026-10-31")]
    [InlineData("2026-10-01", null)]
    public async Task DeliveryListByPeriode_WhenDateIsNullOrEmpty_ThrowsArgumentException(string? tglAwal, string? tglAkhir)
    {
        // Arrange
        var sut = new DeliveryListByPeriodeHandler(_deliveryOrderRepoMock.Object);
        var query = new DeliveryListByPeriodeQuery(tglAwal!, tglAkhir!);

        // Act
        var act = () => sut.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    // ==========================================
    // DeliveryListPendingPoQuery Tests
    // ==========================================

    [Fact]
    public async Task DeliveryListPendingPo_WhenPendingPoItemsExist_ReturnsGroupedPendingPosWithItems()
    {
        // Arrange
        var pendingItems = new List<PoPendingItemDto>
        {
            new()
            {
                PoId = "PO26100001",
                PoDate = "2026-10-14",
                SupplierId = "SUP01",
                SupplierName = "PT Kimia Sehat",
                LayananId = "GDFAR",
                LayananName = "Gudang Farmasi",
                Keterangan = "PO Obat A",
                StatusDo = 0m,
                NoUrut = 1,
                BrgId = "BRG001",
                BrgName = "Paracetamol",
                SatuanId = "BOX",
                SatuanName = "Box",
                QtyPo = 100m,
                QtyDo = 20m,
                SisaQty = 80m,
                Konversi = 100m,
                Harga = 45000m,
                Diskon = 0m
            },
            new()
            {
                PoId = "PO26100001",
                PoDate = "2026-10-14",
                SupplierId = "SUP01",
                SupplierName = "PT Kimia Sehat",
                LayananId = "GDFAR",
                LayananName = "Gudang Farmasi",
                Keterangan = "PO Obat A",
                StatusDo = 0m,
                NoUrut = 2,
                BrgId = "BRG002",
                BrgName = "Amoxicillin",
                SatuanId = "BOX",
                SatuanName = "Box",
                QtyPo = 50m,
                QtyDo = 0m,
                SisaQty = 50m,
                Konversi = 50m,
                Harga = 60000m,
                Diskon = 5000m
            },
            new()
            {
                PoId = "PO26100002",
                PoDate = "2026-10-15",
                SupplierId = "SUP02",
                SupplierName = "PT Medika Jaya",
                LayananId = "GDLOG",
                LayananName = "Gudang Logistik",
                Keterangan = "PO Alkes B",
                StatusDo = 1m,
                NoUrut = 1,
                BrgId = "BRG010",
                BrgName = "Spuit 3cc",
                SatuanId = "BOX",
                SatuanName = "Box",
                QtyPo = 200m,
                QtyDo = 100m,
                SisaQty = 100m,
                Konversi = 100m,
                Harga = 120000m,
                Diskon = 0m
            }
        };

        _poDeliveryTrackingDalMock.Setup(x => x.ListPendingPoItems(null, null, null, 100))
            .Returns(pendingItems);

        var sut = new DeliveryListPendingPoHandler(_poDeliveryTrackingDalMock.Object);
        var query = new DeliveryListPendingPoQuery();

        // Act
        var result = (await sut.Handle(query, CancellationToken.None)).ToList();

        // Assert
        result.Should().HaveCount(2);

        var po1 = result.First(x => x.PoId == "PO26100001");
        po1.SupplierId.Should().Be("SUP01");
        po1.SupplierName.Should().Be("PT Kimia Sehat");
        po1.LayananId.Should().Be("GDFAR");
        po1.LayananName.Should().Be("Gudang Farmasi");
        po1.StatusDo.Should().Be(0m);
        po1.Items.Should().HaveCount(2);
        po1.Items[0].BrgId.Should().Be("BRG001");
        po1.Items[0].SisaQty.Should().Be(80m);
        po1.Items[1].BrgId.Should().Be("BRG002");
        po1.Items[1].SisaQty.Should().Be(50m);

        var po2 = result.First(x => x.PoId == "PO26100002");
        po2.SupplierId.Should().Be("SUP02");
        po2.SupplierName.Should().Be("PT Medika Jaya");
        po2.LayananId.Should().Be("GDLOG");
        po2.StatusDo.Should().Be(1m);
        po2.Items.Should().HaveCount(1);
        po2.Items[0].BrgId.Should().Be("BRG010");
        po2.Items[0].SisaQty.Should().Be(100m);
    }

    [Fact]
    public async Task DeliveryListPendingPo_WhenNoPendingPoItems_ReturnsEmpty()
    {
        // Arrange
        _poDeliveryTrackingDalMock.Setup(x => x.ListPendingPoItems(null, null, null, 100))
            .Returns([]);

        var sut = new DeliveryListPendingPoHandler(_poDeliveryTrackingDalMock.Object);
        var query = new DeliveryListPendingPoQuery();

        // Act
        var result = await sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    // ==========================================
    [Fact]
    public async Task DeliveryListPendingPo_GivenFilterParameters_PassesParametersToDal()
    {
        // Arrange
        const string tglAwal = "2026-10-01";
        const string tglAkhir = "2026-10-15";
        const string search = "KIMIA";
        const int limit = 50;

        var pendingItems = new List<PoPendingItemDto>
        {
            new()
            {
                PoId = "PO26100001",
                PoDate = "2026-10-05",
                SupplierId = "SUP01",
                SupplierName = "PT Kimia Sehat",
                LayananId = "GDFAR",
                LayananName = "Gudang Farmasi",
                Keterangan = "PO Filtered",
                StatusDo = 0m,
                NoUrut = 1,
                BrgId = "BRG001",
                BrgName = "Paracetamol",
                SatuanId = "BOX",
                SatuanName = "Box",
                QtyPo = 100m,
                QtyDo = 0m,
                SisaQty = 100m,
                Konversi = 100m,
                Harga = 45000m,
                Diskon = 0m
            }
        };

        _poDeliveryTrackingDalMock.Setup(x => x.ListPendingPoItems(tglAwal, tglAkhir, search, limit))
            .Returns(pendingItems);

        var sut = new DeliveryListPendingPoHandler(_poDeliveryTrackingDalMock.Object);
        var query = new DeliveryListPendingPoQuery(tglAwal, tglAkhir, search, limit);

        // Act
        var result = (await sut.Handle(query, CancellationToken.None)).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].PoId.Should().Be("PO26100001");
        _poDeliveryTrackingDalMock.Verify(x => x.ListPendingPoItems(tglAwal, tglAkhir, search, limit), Times.Once);
    }

    // DeliveryValidateSuratJalanQuery Tests
    // ==========================================

    [Fact]
    public async Task DeliveryValidateSuratJalan_WhenUnique_ReturnsIsUniqueTrueAndEmptyFields()
    {
        // Arrange
        _deliveryValidationDalMock
            .Setup(x => x.CheckDuplicateSuratJalan("SUP001", "SJ-12345", null))
            .Returns((DeliveryDuplicateInfoDto?)null);

        var sut = new DeliveryValidateSuratJalanHandler(_deliveryValidationDalMock.Object);
        var query = new DeliveryValidateSuratJalanQuery("SUP001", "SJ-12345");

        // Act
        var response = await sut.Handle(query, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.IsUnique.Should().BeTrue();
        response.ExistingDeliveryOrderId.Should().BeEmpty();
        response.ExistingDate.Should().BeEmpty();
        response.ExistingPetugasName.Should().BeEmpty();
    }

    [Fact]
    public async Task DeliveryValidateSuratJalan_WhenDuplicateExists_ReturnsIsUniqueFalseAndDuplicateDetails()
    {
        // Arrange
        var duplicateInfo = new DeliveryDuplicateInfoDto("DM0114275A", "2026-10-10", "Bpk. Roso");
        _deliveryValidationDalMock
            .Setup(x => x.CheckDuplicateSuratJalan("SUP001", "SJ-12345", null))
            .Returns(duplicateInfo);

        var sut = new DeliveryValidateSuratJalanHandler(_deliveryValidationDalMock.Object);
        var query = new DeliveryValidateSuratJalanQuery("SUP001", "SJ-12345");

        // Act
        var response = await sut.Handle(query, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.IsUnique.Should().BeFalse();
        response.ExistingDeliveryOrderId.Should().Be("DM0114275A");
        response.ExistingDate.Should().Be("2026-10-10");
        response.ExistingPetugasName.Should().Be("Bpk. Roso");
    }

    [Fact]
    public async Task DeliveryValidateSuratJalan_WhenExcludedDoIdMatches_ReturnsIsUniqueTrue()
    {
        // Arrange
        _deliveryValidationDalMock
            .Setup(x => x.CheckDuplicateSuratJalan("SUP001", "SJ-12345", "DM0114275A"))
            .Returns((DeliveryDuplicateInfoDto?)null);

        var sut = new DeliveryValidateSuratJalanHandler(_deliveryValidationDalMock.Object);
        var query = new DeliveryValidateSuratJalanQuery("SUP001", "SJ-12345", "DM0114275A");

        // Act
        var response = await sut.Handle(query, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.IsUnique.Should().BeTrue();
        response.ExistingDeliveryOrderId.Should().BeEmpty();
    }

    [Theory]
    [InlineData("", "SJ-12345")]
    [InlineData("   ", "SJ-12345")]
    [InlineData(null, "SJ-12345")]
    [InlineData("SUP001", "")]
    [InlineData("SUP001", "   ")]
    [InlineData("SUP001", null)]
    public async Task DeliveryValidateSuratJalan_WhenRequiredParamMissing_ThrowsArgumentException(string? supplierId, string? suratJalanNo)
    {
        // Arrange
        var sut = new DeliveryValidateSuratJalanHandler(_deliveryValidationDalMock.Object);
        var query = new DeliveryValidateSuratJalanQuery(supplierId!, suratJalanNo!);

        // Act
        var act = () => sut.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }
}
