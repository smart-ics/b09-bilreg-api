using Bilreg.Application.PurchaseContext.DeliveryFeature;
using Bilreg.Application.PurchaseContext.DeliveryFeature.UseCases;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.PurchaseContext.DeliveryFeature;
using Bilreg.Domain.PurchaseContext.PurchaseOrderFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Infrastructure.PurchaseContext.DeliveryFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.PurchaseContext.DeliveryFeature;

public class DeliveryVoidDraftCommandTest
{
    private const string DoId = "DM0114280A";
    private const string UserId = "USR01";

    private readonly Mock<IDeliveryOrderRepo> _repoMock;
    private readonly Mock<ITglJamProvider> _clockMock;
    private readonly DateTime _now;
    private readonly DeliveryVoidDraftHandler _sut;

    public DeliveryVoidDraftCommandTest()
    {
        _repoMock = new Mock<IDeliveryOrderRepo>();
        _clockMock = new Mock<ITglJamProvider>();
        _now = new DateTime(2026, 10, 15, 14, 30, 0);
        _clockMock.Setup(c => c.Now).Returns(_now);

        _sut = new DeliveryVoidDraftHandler(_repoMock.Object, _clockMock.Object);
    }

    private static DeliveryOrderModel CreateSampleModel(
        DeliveryOrderStatusEnum status = DeliveryOrderStatusEnum.Draft)
    {
        var refDate = new DateTime(2026, 10, 15, 10, 0, 0);
        var supplier = new PartnerReff("SUP001", "PT Kimia Farma");
        var gudang = new LayananReff("GDFAR", "Gudang Farmasi Utama");

        var model = new DeliveryOrderModel(
            DoId,
            "SJ-TEST-12345",
            "PO26100001",
            refDate,
            supplier,
            gudang,
            "Penerimaan draft",
            status,
            0m, 0m, 0m, 0m, 0m, 0m, 0m,
            AuditInfoType.Default,
            AuditTrailType.Create("USR-ORIGIN", refDate),
            []);

        var inspection = DeliveryInspectionType.Create(
            qtyKirim: 10m,
            qtyDiterima: 10m,
            qtyDitolak: 0m,
            alasanTolak: "",
            isBonus: false,
            noSuratJaminan: "",
            poNoUrut: 1);

        var item = DeliveryOrderItemType.Create(
            noUrut: 1,
            brg: new BrgReff("BRG001", "Paracetamol 500mg"),
            satuan: new SatuanType("BOX", "BOX"),
            konversi: 10,
            harga: 50000m,
            diskon: 0m,
            taxPercentage: 11m,
            noBatch: "LOT-001",
            tglEd: refDate.AddMonths(24),
            inspection: inspection,
            referenceDate: refDate);

        // Add item directly to private collection via reflection or AddItem if status was Draft
        // Since constructor initializes with empty list and AddItem requires Draft:
        // When status is Confirmed, model.Confirm can be called on a draft model
        return model;
    }

    [Fact]
    public async Task Handle_WhenDraftExists_VoidsDraftAndSavesModel()
    {
        // ARRANGE
        var draftModel = CreateSampleModel(DeliveryOrderStatusEnum.Draft);
        _repoMock.Setup(r => r.LoadEntity(It.Is<IDeliveryOrderKey>(k => k.DeliveryOrderId == DoId)))
            .Returns(MayBe.From(draftModel));

        DeliveryOrderModel? savedModel = null;
        _repoMock.Setup(r => r.SaveChanges(It.IsAny<DeliveryOrderModel>()))
            .Callback<DeliveryOrderModel>(m => savedModel = m);

        var command = new DeliveryVoidDraftCommand(DoId, UserId, _now);

        // ACT
        var response = await _sut.Handle(command, CancellationToken.None);

        // ASSERT
        response.Should().NotBeNull();
        response.DeliveryOrderId.Should().Be(DoId);
        response.DoId.Should().Be(DoId);
        response.Status.Should().Be("Voided");

        _repoMock.Verify(r => r.SaveChanges(It.IsAny<DeliveryOrderModel>()), Times.Once);
        savedModel.Should().NotBeNull();
        savedModel!.Status.Should().Be(DeliveryOrderStatusEnum.Voided);
        savedModel.AuditTrail.Voided.UserId.Should().Be(UserId);
        savedModel.AuditTrail.Voided.Timestamp.Should().Be(_now);
    }

    [Fact]
    public async Task Handle_WhenAlreadyConfirmed_ThrowsInvalidOperationException()
    {
        // ARRANGE: create model and confirm it
        var confirmedModel = CreateSampleModel(DeliveryOrderStatusEnum.Draft);
        var inspection = DeliveryInspectionType.Create(10m, 10m, 0m, "", false, "", 1);
        confirmedModel.AddItem(
            new BrgReff("BRG01", "Barang 1"),
            new SatuanType("BOX", "BOX"),
            1, 1000m, 0m, 0m, "BATCH-1",
            _now.AddMonths(24),
            inspection,
            _now);
        confirmedModel.Confirm("APPROVER01", _now.AddHours(-1));

        _repoMock.Setup(r => r.LoadEntity(It.Is<IDeliveryOrderKey>(k => k.DeliveryOrderId == DoId)))
            .Returns(MayBe.From(confirmedModel));

        var command = new DeliveryVoidDraftCommand(DoId, UserId, _now);

        // ACT
        var act = async () => await _sut.Handle(command, CancellationToken.None);

        // ASSERT
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*immutable dan tidak dapat dibatalkan*");

        _repoMock.Verify(r => r.SaveChanges(It.IsAny<DeliveryOrderModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenAlreadyVoided_ThrowsInvalidOperationException()
    {
        // ARRANGE: create draft model and void it
        var voidedModel = CreateSampleModel(DeliveryOrderStatusEnum.Draft);
        voidedModel.Void("PREV_USER", _now.AddHours(-2));

        _repoMock.Setup(r => r.LoadEntity(It.Is<IDeliveryOrderKey>(k => k.DeliveryOrderId == DoId)))
            .Returns(MayBe.From(voidedModel));

        var command = new DeliveryVoidDraftCommand(DoId, UserId, _now);

        // ACT
        var act = async () => await _sut.Handle(command, CancellationToken.None);

        // ASSERT
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*sudah dibatalkan sebelumnya*");

        _repoMock.Verify(r => r.SaveChanges(It.IsAny<DeliveryOrderModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenNotFound_ThrowsKeyNotFoundException()
    {
        // ARRANGE
        _repoMock.Setup(r => r.LoadEntity(It.IsAny<IDeliveryOrderKey>()))
            .Returns(MayBe<DeliveryOrderModel>.None);

        var command = new DeliveryVoidDraftCommand("DM-NONEXIST", UserId, _now);

        // ACT
        var act = async () => await _sut.Handle(command, CancellationToken.None);

        // ASSERT
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*DM-NONEXIST*tidak ditemukan*");

        _repoMock.Verify(r => r.SaveChanges(It.IsAny<DeliveryOrderModel>()), Times.Never);
    }

    [Theory]
    [InlineData("", "USR01")]
    [InlineData("DM0114280A", "")]
    public async Task Handle_WhenRequiredFieldEmpty_ThrowsArgumentException(string doId, string user)
    {
        // ARRANGE
        var command = new DeliveryVoidDraftCommand(doId, user, _now);

        // ACT
        var act = async () => await _sut.Handle(command, CancellationToken.None);

        // ASSERT
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Handle_WhenVoidDraftExecuted_IntegrationWithRepo_UpdatesDalWithVoidedStatusAndPreservesAuditParameters()
    {
        // ARRANGE: Set up real repo with mocked DALs
        var orderDalMock = new Mock<IDeliveryOrderDal>();
        var itemDalMock = new Mock<IDeliveryOrderItemDal>();
        var inspectionDalMock = new Mock<IDeliveryInspectionDal>();

        var refDate = new DateTime(2026, 10, 15, 10, 0, 0);
        var orderDto = new DeliveryOrderDto
        {
            DoId = DoId,
            DoDate = refDate,
            PetugasId = "CREATOR01",
            PoId = "PO26100001",
            SuratJalanNo = "SJ-TEST-12345",
            LayananId = "GDFAR",
            LayananName = "Gudang Farmasi Utama",
            SupplierId = "SUP001",
            SupplierName = "PT Kimia Farma",
            Total = 100000m,
            Keterangan = "Draft penerimaan",
            PembelianBebas = false,
            TglVoid = new DateTime(3000, 1, 1),
            JamVoid = "00:00:00",
            PetugasVoid = string.Empty,
            CrtTgl = refDate,
            CrtUsr = "CREATOR01",
            UpdTgl = new DateTime(3000, 1, 1),
            UpdUsr = string.Empty
        };

        var itemDto = new DeliveryOrderItemDto
        {
            DoId = DoId,
            NoUrut = 1,
            BrgId = "BRG001",
            BrgName = "Paracetamol 500mg",
            SatuanId = "BOX",
            SatuanName = "BOX",
            Konversi = 10,
            Harga = 50000m,
            Diskon = 0m,
            Qty = 10m,
            SubTotal = 500000m,
            Tax = 55000m,
            Total = 555000m,
            NoBatch = "LOT-001",
            TglEd = refDate.AddMonths(24),
            IsVoid = false,
            Het = 60000m
        };

        var inspectionDto = new DeliveryInspectionDto
        {
            DoId = DoId,
            NoUrut = 1,
            BrgId = "BRG001",
            BrgName = "Paracetamol 500mg",
            PoNoUrut = 1,
            QtyKirim = 10m,
            QtyDiterima = 10m,
            QtyDitolak = 0m,
            AlasanTolak = string.Empty,
            IsBonus = false,
            NoSuratJaminan = string.Empty,
            Status = nameof(DeliveryOrderStatusEnum.Draft)
        };

        orderDalMock.Setup(d => d.GetData(It.Is<IDeliveryOrderKey>(k => k.DeliveryOrderId == DoId)))
            .Returns(orderDto);
        itemDalMock.Setup(d => d.ListData(It.Is<IDeliveryOrderKey>(k => k.DeliveryOrderId == DoId)))
            .Returns([itemDto]);
        inspectionDalMock.Setup(d => d.ListData(It.Is<IDeliveryOrderKey>(k => k.DeliveryOrderId == DoId)))
            .Returns([inspectionDto]);

        var repo = new DeliveryOrderRepo(orderDalMock.Object, itemDalMock.Object, inspectionDalMock.Object);
        var sut = new DeliveryVoidDraftHandler(repo, _clockMock.Object);
        var command = new DeliveryVoidDraftCommand(DoId, UserId, _now);

        // ACT
        var response = await sut.Handle(command, CancellationToken.None);

        // ASSERT
        response.Should().NotBeNull();
        response.DeliveryOrderId.Should().Be(DoId);
        response.Status.Should().Be("Voided");

        // Verify DAL update receives Voided status and does not corrupt UpdTgl / UpdUsr
        orderDalMock.Verify(d => d.Update(It.Is<DeliveryOrderDto>(dto =>
            dto.DoId == DoId &&
            dto.PetugasVoid == UserId &&
            dto.TglVoid == _now &&
            dto.JamVoid == _now.ToString("HH:mm:ss") &&
            dto.UpdTgl.Year == 3000 &&
            string.IsNullOrEmpty(dto.UpdUsr)
        )), Times.Once);

        // Verify inspection DAL receives Voided status for items
        inspectionDalMock.Verify(d => d.Insert(It.Is<List<DeliveryInspectionDto>>(list =>
            list.All(i => i.Status == nameof(DeliveryOrderStatusEnum.Voided))
        )), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenPreviouslyModifiedDraftVoided_IntegrationWithRepo_PreservesOriginalModificationAudit()
    {
        // ARRANGE: Draft that was legitimately modified previously
        var orderDalMock = new Mock<IDeliveryOrderDal>();
        var itemDalMock = new Mock<IDeliveryOrderItemDal>();
        var inspectionDalMock = new Mock<IDeliveryInspectionDal>();

        var refDate = new DateTime(2026, 10, 15, 10, 0, 0);
        var modDate = new DateTime(2026, 10, 15, 11, 30, 0);
        var orderDto = new DeliveryOrderDto
        {
            DoId = DoId,
            DoDate = refDate,
            PetugasId = "CREATOR01",
            PoId = "PO26100001",
            SuratJalanNo = "SJ-TEST-12345",
            LayananId = "GDFAR",
            LayananName = "Gudang Farmasi Utama",
            SupplierId = "SUP001",
            SupplierName = "PT Kimia Farma",
            Total = 100000m,
            Keterangan = "Draft penerimaan yang pernah diedit",
            PembelianBebas = false,
            TglVoid = new DateTime(3000, 1, 1),
            JamVoid = "00:00:00",
            PetugasVoid = string.Empty,
            CrtTgl = refDate,
            CrtUsr = "CREATOR01",
            UpdTgl = modDate,
            UpdUsr = "EDITOR01"
        };

        var itemDto = new DeliveryOrderItemDto
        {
            DoId = DoId,
            NoUrut = 1,
            BrgId = "BRG001",
            BrgName = "Paracetamol 500mg",
            SatuanId = "BOX",
            SatuanName = "BOX",
            Konversi = 10,
            Harga = 50000m,
            Diskon = 0m,
            Qty = 10m,
            SubTotal = 500000m,
            Tax = 55000m,
            Total = 555000m,
            NoBatch = "LOT-001",
            TglEd = refDate.AddMonths(24),
            IsVoid = false,
            Het = 60000m
        };

        var inspectionDto = new DeliveryInspectionDto
        {
            DoId = DoId,
            NoUrut = 1,
            BrgId = "BRG001",
            BrgName = "Paracetamol 500mg",
            PoNoUrut = 1,
            QtyKirim = 10m,
            QtyDiterima = 10m,
            QtyDitolak = 0m,
            AlasanTolak = string.Empty,
            IsBonus = false,
            NoSuratJaminan = string.Empty,
            Status = nameof(DeliveryOrderStatusEnum.Draft)
        };

        orderDalMock.Setup(d => d.GetData(It.Is<IDeliveryOrderKey>(k => k.DeliveryOrderId == DoId)))
            .Returns(orderDto);
        itemDalMock.Setup(d => d.ListData(It.Is<IDeliveryOrderKey>(k => k.DeliveryOrderId == DoId)))
            .Returns([itemDto]);
        inspectionDalMock.Setup(d => d.ListData(It.Is<IDeliveryOrderKey>(k => k.DeliveryOrderId == DoId)))
            .Returns([inspectionDto]);

        var repo = new DeliveryOrderRepo(orderDalMock.Object, itemDalMock.Object, inspectionDalMock.Object);
        var sut = new DeliveryVoidDraftHandler(repo, _clockMock.Object);
        var command = new DeliveryVoidDraftCommand(DoId, UserId, _now);

        // ACT
        var response = await sut.Handle(command, CancellationToken.None);

        // ASSERT
        response.Should().NotBeNull();
        response.Status.Should().Be("Voided");

        // Verify DAL update receives Voided status while preserving original modification audit (UpdTgl & UpdUsr)
        orderDalMock.Verify(d => d.Update(It.Is<DeliveryOrderDto>(dto =>
            dto.DoId == DoId &&
            dto.PetugasVoid == UserId &&
            dto.TglVoid == _now &&
            dto.UpdTgl == modDate &&
            dto.UpdUsr == "EDITOR01"
        )), Times.Once);
    }
}
