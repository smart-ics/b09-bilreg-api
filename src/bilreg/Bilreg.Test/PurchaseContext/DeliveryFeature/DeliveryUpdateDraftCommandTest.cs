using Bilreg.Application.PurchaseContext.DeliveryFeature;
using Bilreg.Application.PurchaseContext.DeliveryFeature.UseCases;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.PurchaseContext.DeliveryFeature;
using Bilreg.Domain.PurchaseContext.PurchaseOrderFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.PurchaseContext.DeliveryFeature;

public class DeliveryUpdateDraftCommandTest
{
    private const string DoId = "DM0114279A";
    private const string SuratJalan = "SJ-TEST-12345";
    private const string PoId = "PO26100001";
    private const string SupplierId = "SUP001";
    private const string SupplierName = "PT Kimia Farma";
    private const string GudangId = "GDFAR";
    private const string GudangName = "Gudang Farmasi Utama";
    private const string UserId = "USR01";
    private const string Keterangan = "Draf penerimaan barang logistik";

    private readonly Mock<IDeliveryOrderRepo> _repoMock;
    private readonly Mock<IDeliveryValidationDal> _validationDalMock;
    private readonly Mock<IPoDeliveryTrackingDal> _poTrackingDalMock;
    private readonly Mock<ITglJamProvider> _clockMock;
    private readonly DateTime _now;
    private readonly DeliveryUpdateDraftHandler _sut;

    public DeliveryUpdateDraftCommandTest()
    {
        _repoMock = new Mock<IDeliveryOrderRepo>();
        _validationDalMock = new Mock<IDeliveryValidationDal>();
        _poTrackingDalMock = new Mock<IPoDeliveryTrackingDal>();
        _clockMock = new Mock<ITglJamProvider>();

        _now = new DateTime(2026, 10, 16, 10, 0, 0);
        _clockMock.Setup(c => c.Now).Returns(_now);

        _validationDalMock
            .Setup(v => v.IsSuratJalanUnique(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
            .Returns(true);

        _sut = new DeliveryUpdateDraftHandler(
            _repoMock.Object,
            _validationDalMock.Object,
            _poTrackingDalMock.Object,
            _clockMock.Object);
    }

    private static List<PoQuotaDto> CreateSamplePoQuotas()
    {
        return
        [
            new PoQuotaDto(
                poId: PoId,
                noUrut: 1,
                brgId: "BRG001",
                brgName: "Paracetamol 500mg",
                satuanId: "BOX",
                qtyPo: 10m,
                qtyDo: 0m,
                sisaQty: 10m,
                isClosed: false,
                konversi: 10,
                harga: 50000m,
                diskon: 0m),
            new PoQuotaDto(
                poId: PoId,
                noUrut: 2,
                brgId: "BRG002",
                brgName: "Amoxicillin 500mg",
                satuanId: "BOX",
                qtyPo: 20m,
                qtyDo: 5m,
                sisaQty: 15m,
                isClosed: false,
                konversi: 10,
                harga: 75000m,
                diskon: 0m)
        ];
    }

    private List<DeliveryUpdateDraftItemCommand> CreateSampleItems(DateTime? edDate = null)
    {
        var ed = edDate ?? _now.AddMonths(24);
        return
        [
            new DeliveryUpdateDraftItemCommand(
                BrgId: "BRG001",
                SatuanId: "BOX",
                Konversi: 10,
                Harga: 50000m,
                Diskon: 0m,
                TaxPercentage: 11m,
                NoBatch: "BATCH-001",
                TglEd: ed,
                QtyKirim: 8m,
                QtyDiterima: 8m,
                QtyDitolak: 0m,
                AlasanTolak: "",
                BrgName: "Paracetamol 500mg",
                SatuanName: "BOX",
                IsBonus: false,
                HasJaminanRetur: false,
                NoSuratJaminan: "",
                PoNoUrut: 1)
        ];
    }

    private DeliveryOrderModel CreateSampleDraftModel(DeliveryOrderStatusEnum status = DeliveryOrderStatusEnum.Draft)
    {
        var supplier = new PartnerReff(SupplierId, SupplierName);
        var gudang = new LayananReff(GudangId, GudangName);
        var created = new AuditInfoType(UserId, _now.AddDays(-1));
        var auditTrail = new AuditTrailType(created, AuditInfoType.Default, AuditInfoType.Default);

        var inspection = DeliveryInspectionType.Create(
            qtyKirim: 5m,
            qtyDiterima: 5m,
            qtyDitolak: 0m,
            alasanTolak: "",
            isBonus: false,
            hasJaminanRetur: false,
            noSuratJaminan: "",
            poId: PoId,
            poNoUrut: 1);

        var item = DeliveryOrderItemType.Create(
            noUrut: 1,
            brg: new BrgReff("BRG001", "Paracetamol 500mg"),
            satuan: new SatuanType("BOX", "BOX"),
            konversi: 10,
            harga: 50000m,
            diskon: 0m,
            taxPercentage: 11m,
            noBatch: "OLD-BATCH-001",
            tglEd: _now.AddMonths(20),
            inspection: inspection,
            referenceDate: _now.AddDays(-1));

        return new DeliveryOrderModel(
            id: DoId,
            suratJalanNo: SuratJalan,
            poId: PoId,
            deliveryOrderDate: _now.AddDays(-1),
            supplier: supplier,
            gudang: gudang,
            keterangan: Keterangan,
            status: status,
            subTotal: 250000m,
            taxTotal: 27500m,
            diskonTotal: 0m,
            total: 277500m,
            biayaLain: 0m,
            diskonLain: 0m,
            grandTotal: 277500m,
            confirmed: AuditInfoType.Default,
            auditTrail: auditTrail,
            items: [item]);
    }

    [Fact]
    public async Task Handle_WhenValidDraftWithPo_UpdatesDraftAndSavesModel()
    {
        // Arrange
        var existingModel = CreateSampleDraftModel();
        _repoMock.Setup(r => r.LoadEntity(It.Is<IDeliveryOrderKey>(k => k.DeliveryOrderId == DoId)))
            .Returns(MayBe.From(existingModel));
        _poTrackingDalMock.Setup(p => p.GetRemainingQuota(PoId))
            .Returns(CreateSamplePoQuotas());

        DeliveryOrderModel? capturedSavedModel = null;
        _repoMock.Setup(r => r.SaveChanges(It.IsAny<DeliveryOrderModel>()))
            .Callback<DeliveryOrderModel>(m => capturedSavedModel = m);

        var command = new DeliveryUpdateDraftCommand(
            DeliveryOrderId: DoId,
            SuratJalanNo: "SJ-UPDATED-99",
            PoId: PoId,
            SupplierId: SupplierId,
            GudangId: GudangId,
            Keterangan: "Catatan terupdate",
            UserId: "USR02",
            Items: CreateSampleItems(),
            SupplierName: SupplierName,
            GudangName: GudangName,
            BiayaLain: 15000m,
            DiskonLain: 5000m);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.DeliveryOrderId.Should().Be(DoId);
        result.DoId.Should().Be(DoId);
        result.Status.Should().Be("Draft");

        _validationDalMock.Verify(v => v.IsSuratJalanUnique(SupplierId, "SJ-UPDATED-99", DoId), Times.Once);
        _repoMock.Verify(r => r.SaveChanges(It.IsAny<DeliveryOrderModel>()), Times.Once);

        capturedSavedModel.Should().NotBeNull();
        capturedSavedModel!.DeliveryOrderId.Should().Be(DoId);
        capturedSavedModel.SuratJalanNo.Should().Be("SJ-UPDATED-99");
        capturedSavedModel.Keterangan.Should().Be("Catatan terupdate");
        capturedSavedModel.BiayaLain.Should().Be(15000m);
        capturedSavedModel.DiskonLain.Should().Be(5000m);
        capturedSavedModel.Status.Should().Be(DeliveryOrderStatusEnum.Draft);
        capturedSavedModel.AuditTrail.Modified.Should().NotBeNull();
        capturedSavedModel.AuditTrail.Modified!.UserId.Should().Be("USR02");
        capturedSavedModel.AuditTrail.Modified!.Timestamp.Should().Be(_now);

        capturedSavedModel.ListItem.Should().HaveCount(1);
        var item = capturedSavedModel.ListItem.First();
        item.NoBatch.Should().Be("BATCH-001");
        item.Inspection.QtyDiterima.Should().Be(8m);
    }

    [Fact]
    public async Task Handle_WhenDocumentNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _repoMock.Setup(r => r.LoadEntity(It.IsAny<IDeliveryOrderKey>()))
            .Returns(MayBe<DeliveryOrderModel>.None);

        var command = new DeliveryUpdateDraftCommand(
            DeliveryOrderId: "NONEXISTENT",
            SuratJalanNo: SuratJalan,
            PoId: PoId,
            SupplierId: SupplierId,
            GudangId: GudangId,
            Keterangan: Keterangan,
            UserId: UserId,
            Items: CreateSampleItems());

        // Act
        var act = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*tidak ditemukan*");
    }

    [Fact]
    public async Task Handle_WhenDocumentAlreadyConfirmed_ThrowsInvalidOperationException()
    {
        // Arrange
        var confirmedModel = CreateSampleDraftModel(DeliveryOrderStatusEnum.Confirmed);
        _repoMock.Setup(r => r.LoadEntity(It.IsAny<IDeliveryOrderKey>()))
            .Returns(MayBe.From(confirmedModel));

        var command = new DeliveryUpdateDraftCommand(
            DeliveryOrderId: DoId,
            SuratJalanNo: SuratJalan,
            PoId: PoId,
            SupplierId: SupplierId,
            GudangId: GudangId,
            Keterangan: Keterangan,
            UserId: UserId,
            Items: CreateSampleItems());

        // Act
        var act = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Confirmed*");
    }

    [Fact]
    public async Task Handle_WhenDocumentAlreadyVoided_ThrowsInvalidOperationException()
    {
        // Arrange
        var voidedModel = CreateSampleDraftModel(DeliveryOrderStatusEnum.Voided);
        _repoMock.Setup(r => r.LoadEntity(It.IsAny<IDeliveryOrderKey>()))
            .Returns(MayBe.From(voidedModel));

        var command = new DeliveryUpdateDraftCommand(
            DeliveryOrderId: DoId,
            SuratJalanNo: SuratJalan,
            PoId: PoId,
            SupplierId: SupplierId,
            GudangId: GudangId,
            Keterangan: Keterangan,
            UserId: UserId,
            Items: CreateSampleItems());

        // Act
        var act = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*dibatalkan*");
    }

    [Fact]
    public async Task Handle_WhenSuratJalanDuplicateForOtherDeliveryOrder_ThrowsInvalidOperationException()
    {
        // Arrange
        var existingModel = CreateSampleDraftModel();
        _repoMock.Setup(r => r.LoadEntity(It.IsAny<IDeliveryOrderKey>()))
            .Returns(MayBe.From(existingModel));

        _validationDalMock
            .Setup(v => v.IsSuratJalanUnique(SupplierId, "SJ-DUPLICATE", DoId))
            .Returns(false);

        var command = new DeliveryUpdateDraftCommand(
            DeliveryOrderId: DoId,
            SuratJalanNo: "SJ-DUPLICATE",
            PoId: PoId,
            SupplierId: SupplierId,
            GudangId: GudangId,
            Keterangan: Keterangan,
            UserId: UserId,
            Items: CreateSampleItems());

        // Act
        var act = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*sudah pernah diterima*");
    }

    [Fact]
    public async Task Handle_WhenSuratJalanSameAsSelf_PassesValidation()
    {
        // Arrange
        var existingModel = CreateSampleDraftModel();
        _repoMock.Setup(r => r.LoadEntity(It.IsAny<IDeliveryOrderKey>()))
            .Returns(MayBe.From(existingModel));
        _poTrackingDalMock.Setup(p => p.GetRemainingQuota(PoId))
            .Returns(CreateSamplePoQuotas());

        _validationDalMock
            .Setup(v => v.IsSuratJalanUnique(SupplierId, SuratJalan, DoId))
            .Returns(true);

        var command = new DeliveryUpdateDraftCommand(
            DeliveryOrderId: DoId,
            SuratJalanNo: SuratJalan,
            PoId: PoId,
            SupplierId: SupplierId,
            GudangId: GudangId,
            Keterangan: Keterangan,
            UserId: UserId,
            Items: CreateSampleItems());

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.DeliveryOrderId.Should().Be(DoId);
        _validationDalMock.Verify(v => v.IsSuratJalanUnique(SupplierId, SuratJalan, DoId), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenPoQuotaExceeded_ThrowsInvalidOperationException()
    {
        // Arrange
        var existingModel = CreateSampleDraftModel();
        _repoMock.Setup(r => r.LoadEntity(It.IsAny<IDeliveryOrderKey>()))
            .Returns(MayBe.From(existingModel));
        _poTrackingDalMock.Setup(p => p.GetRemainingQuota(PoId))
            .Returns(CreateSamplePoQuotas());

        var itemsOverQuota = new List<DeliveryUpdateDraftItemCommand>
        {
            new(
                BrgId: "BRG001",
                SatuanId: "BOX",
                Konversi: 10,
                Harga: 50000m,
                Diskon: 0m,
                TaxPercentage: 11m,
                NoBatch: "BATCH-OVER",
                TglEd: _now.AddMonths(24),
                QtyKirim: 15m,
                QtyDiterima: 15m, // Sisa kuota hanya 10m
                QtyDitolak: 0m,
                AlasanTolak: "",
                BrgName: "Paracetamol 500mg",
                SatuanName: "BOX",
                IsBonus: false,
                HasJaminanRetur: false,
                NoSuratJaminan: "",
                PoNoUrut: 1)
        };

        var command = new DeliveryUpdateDraftCommand(
            DeliveryOrderId: DoId,
            SuratJalanNo: SuratJalan,
            PoId: PoId,
            SupplierId: SupplierId,
            GudangId: GudangId,
            Keterangan: Keterangan,
            UserId: UserId,
            Items: itemsOverQuota);

        // Act
        var act = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*melebihi sisa kuota pesanan*");
    }

    [Fact]
    public async Task Handle_WhenPoLineNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var existingModel = CreateSampleDraftModel();
        _repoMock.Setup(r => r.LoadEntity(It.IsAny<IDeliveryOrderKey>()))
            .Returns(MayBe.From(existingModel));
        _poTrackingDalMock.Setup(p => p.GetRemainingQuota(PoId))
            .Returns(CreateSamplePoQuotas());

        var itemsInvalidLine = new List<DeliveryUpdateDraftItemCommand>
        {
            new(
                BrgId: "BRG001",
                SatuanId: "BOX",
                Konversi: 10,
                Harga: 50000m,
                Diskon: 0m,
                TaxPercentage: 11m,
                NoBatch: "BATCH-INV",
                TglEd: _now.AddMonths(24),
                QtyKirim: 5m,
                QtyDiterima: 5m,
                QtyDitolak: 0m,
                AlasanTolak: "",
                BrgName: "Paracetamol 500mg",
                SatuanName: "BOX",
                IsBonus: false,
                HasJaminanRetur: false,
                NoSuratJaminan: "",
                PoNoUrut: 99) // Baris 99 tidak ada di PO
        };

        var command = new DeliveryUpdateDraftCommand(
            DeliveryOrderId: DoId,
            SuratJalanNo: SuratJalan,
            PoId: PoId,
            SupplierId: SupplierId,
            GudangId: GudangId,
            Keterangan: Keterangan,
            UserId: UserId,
            Items: itemsInvalidLine);

        // Act
        var act = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*tidak ditemukan pada Surat Pesanan*");
    }
    [Fact]
    public async Task Handle_WhenWarehouseDifferentFromPo_SuccessfullyUpdatesDraftWithSpecifiedWarehouse()
    {
        // Arrange
        var existingModel = CreateSampleDraftModel();
        _repoMock.Setup(r => r.LoadEntity(It.IsAny<IDeliveryOrderKey>()))
            .Returns(MayBe.From(existingModel));
        _poTrackingDalMock.Setup(p => p.GetRemainingQuota(PoId))
            .Returns(CreateSamplePoQuotas());
        DeliveryOrderModel? capturedSavedModel = null;
        _repoMock.Setup(r => r.SaveChanges(It.IsAny<DeliveryOrderModel>()))
            .Callback<DeliveryOrderModel>(m => capturedSavedModel = m);

        var command = new DeliveryUpdateDraftCommand(
            DeliveryOrderId: DoId,
            SuratJalanNo: SuratJalan,
            PoId: PoId,
            SupplierId: SupplierId,
            GudangId: "GDLOG",
            Keterangan: Keterangan,
            UserId: UserId,
            Items: CreateSampleItems());

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        capturedSavedModel.Should().NotBeNull();
        capturedSavedModel!.Gudang.LayananId.Should().Be("GDLOG");
        _repoMock.Verify(r => r.SaveChanges(It.IsAny<DeliveryOrderModel>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenBonusItem_DoesNotEnforcePoQuota()
    {
        // Arrange
        var existingModel = CreateSampleDraftModel();
        _repoMock.Setup(r => r.LoadEntity(It.IsAny<IDeliveryOrderKey>()))
            .Returns(MayBe.From(existingModel));
        _poTrackingDalMock.Setup(p => p.GetRemainingQuota(PoId))
            .Returns(CreateSamplePoQuotas());

        var bonusItems = new List<DeliveryUpdateDraftItemCommand>
        {
            new(
                BrgId: "BRG001",
                SatuanId: "BOX",
                Konversi: 10,
                Harga: 0m,
                Diskon: 0m,
                TaxPercentage: 0m,
                NoBatch: "BATCH-BONUS",
                TglEd: _now.AddMonths(24),
                QtyKirim: 100m,
                QtyDiterima: 100m, // Jauh melebihi kuota 10, tapi IsBonus = true
                QtyDitolak: 0m,
                AlasanTolak: "",
                BrgName: "Paracetamol 500mg",
                SatuanName: "BOX",
                IsBonus: true,
                HasJaminanRetur: false,
                NoSuratJaminan: "",
                PoNoUrut: 1)
        };

        var command = new DeliveryUpdateDraftCommand(
            DeliveryOrderId: DoId,
            SuratJalanNo: SuratJalan,
            PoId: PoId,
            SupplierId: SupplierId,
            GudangId: GudangId,
            Keterangan: Keterangan,
            UserId: UserId,
            Items: bonusItems);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.DeliveryOrderId.Should().Be(DoId);
        _repoMock.Verify(r => r.SaveChanges(It.IsAny<DeliveryOrderModel>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNonPoDraft_UpdatesDraftSuccessfully()
    {
        // Arrange
        var supplier = new PartnerReff(SupplierId, SupplierName);
        var gudang = new LayananReff(GudangId, GudangName);
        var created = new AuditInfoType(UserId, _now.AddDays(-1));
        var auditTrail = new AuditTrailType(created, AuditInfoType.Default, AuditInfoType.Default);

        var existingNonPoModel = new DeliveryOrderModel(
            id: DoId,
            suratJalanNo: SuratJalan,
            poId: string.Empty, // Non-PO
            deliveryOrderDate: _now.AddDays(-1),
            supplier: supplier,
            gudang: gudang,
            keterangan: Keterangan,
            status: DeliveryOrderStatusEnum.Draft,
            subTotal: 0m,
            taxTotal: 0m,
            diskonTotal: 0m,
            total: 0m,
            biayaLain: 0m,
            diskonLain: 0m,
            grandTotal: 0m,
            confirmed: AuditInfoType.Default,
            auditTrail: auditTrail,
            items: []);

        _repoMock.Setup(r => r.LoadEntity(It.IsAny<IDeliveryOrderKey>()))
            .Returns(MayBe.From(existingNonPoModel));

        var nonPoItems = new List<DeliveryUpdateDraftItemCommand>
        {
            new(
                BrgId: "BRG999",
                SatuanId: "BOX",
                Konversi: 1,
                Harga: 25000m,
                Diskon: 0m,
                TaxPercentage: 11m,
                NoBatch: "BATCH-NONPO",
                TglEd: _now.AddMonths(24),
                QtyKirim: 4m,
                QtyDiterima: 4m,
                QtyDitolak: 0m,
                AlasanTolak: "",
                BrgName: "Vitamin C",
                SatuanName: "BOX",
                IsBonus: false,
                HasJaminanRetur: false,
                NoSuratJaminan: "",
                PoNoUrut: 0)
        };

        var command = new DeliveryUpdateDraftCommand(
            DeliveryOrderId: DoId,
            SuratJalanNo: "SJ-NONPO-01",
            PoId: string.Empty,
            SupplierId: SupplierId,
            GudangId: GudangId,
            Keterangan: "Pembelian bebas",
            UserId: UserId,
            Items: nonPoItems);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.DeliveryOrderId.Should().Be(DoId);
        _poTrackingDalMock.Verify(p => p.GetRemainingQuota(It.IsAny<string>()), Times.Never);
        _repoMock.Verify(r => r.SaveChanges(It.IsAny<DeliveryOrderModel>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenItemsEmpty_ThrowsArgumentException()
    {
        // Arrange
        var command = new DeliveryUpdateDraftCommand(
            DeliveryOrderId: DoId,
            SuratJalanNo: SuratJalan,
            PoId: PoId,
            SupplierId: SupplierId,
            GudangId: GudangId,
            Keterangan: Keterangan,
            UserId: UserId,
            Items: []);

        // Act
        var act = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*tidak boleh kosong*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Handle_WhenDeliveryOrderIdInvalid_ThrowsArgumentException(string? invalidDoId)
    {
        // Arrange
        var command = new DeliveryUpdateDraftCommand(
            DeliveryOrderId: invalidDoId!,
            SuratJalanNo: SuratJalan,
            PoId: PoId,
            SupplierId: SupplierId,
            GudangId: GudangId,
            Keterangan: Keterangan,
            UserId: UserId,
            Items: CreateSampleItems());

        // Act
        var act = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Handle_WhenDeliveryOrderDateHasNoTime_CombinesWithExistingTime()
    {
        // Arrange
        _poTrackingDalMock.Setup(p => p.GetRemainingQuota(PoId)).Returns(CreateSamplePoQuotas());
        DeliveryOrderModel? capturedSavedModel = null;
        var existing = CreateSampleDraftModel();
        _repoMock.Setup(r => r.LoadEntity(It.Is<IDeliveryOrderKey>(k => k.DeliveryOrderId == DoId)))
            .Returns(MayBe.From(existing));
        _repoMock.Setup(r => r.SaveChanges(It.IsAny<DeliveryOrderModel>()))
            .Callback<DeliveryOrderModel>(m => capturedSavedModel = m);

        var dateOnly = new DateTime(2026, 10, 22); // 00:00:00
        var command = new DeliveryUpdateDraftCommand(
            DeliveryOrderId: DoId,
            SuratJalanNo: SuratJalan,
            PoId: PoId,
            SupplierId: SupplierId,
            GudangId: GudangId,
            Keterangan: Keterangan,
            UserId: UserId,
            Items: CreateSampleItems(),
            DeliveryOrderDate: dateOnly);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        capturedSavedModel.Should().NotBeNull();
        capturedSavedModel!.DeliveryOrderDate.Should().Be(new DateTime(2026, 10, 22, 10, 0, 0));
        capturedSavedModel.DeliveryOrderDate.TimeOfDay.Should().Be(_now.TimeOfDay);
    }
}
