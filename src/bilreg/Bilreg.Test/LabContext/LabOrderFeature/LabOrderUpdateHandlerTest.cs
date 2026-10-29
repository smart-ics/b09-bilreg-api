using Bilreg.Application.LabContext.LabOrderFeature;
using Bilreg.Application.LabContext.LabOrderFeature.UseCases;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Domain.LabContext.LabResultFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.LabContext.LabOrderFeature;

public class LabOrderUpdateHandlerTest
{
    private readonly Mock<ILabOrderRepo> _repo = new();
    private readonly Mock<ILabTestResolutionService> _resolutionService = new();
    private readonly LabOrderUpdateHandler _sut;

    public LabOrderUpdateHandlerTest()
    {
        _sut = new LabOrderUpdateHandler(
            _repo.Object,
            _resolutionService.Object,
            TestTglJamProvider.Instance);
    }

    private static LabOrderModel OrderedOrder(LabOrderUrgencyEnum urgency = LabOrderUrgencyEnum.Rutin)
    {
        var snapshot = new PatientSnapshotType(
            "REG001", "MR0001", "Pasien Tes", new DateTime(1990, 1, 1), "L", 36);
        var item = LabOrderTestSupport.SampleItem();
        return LabOrderTestSupport.CreateEmrOrder(
            "LAB00000001",
            lines: [LabOrderTestSupport.ResolvedLine(item)],
            snapshot: snapshot,
            audit: new AuditInfoType("U1", DateTime.Now),
            urgency: urgency);
    }

    [Fact]
    public async Task Handle_ValidRequest_UpdatesItemsAndUrgency_SavesAndReturnsResponse()
    {
        // Arrange
        var order = OrderedOrder();
        _repo.Setup(r => r.LoadEntity(It.IsAny<ILabOrderKey>()))
            .Returns(MayBe.From(order));

        var item1 = LabOrderTestSupport.SampleItem("LTD001", "HB", "Hemoglobin", "TRF01", "T-HB", "Tarif HB", VacutainerTypeEnum.Edta, "Blood", 1);
        var comp1 = LabOrderTestSupport.SampleComponent("CMP01", "HB", "Hemoglobin", LabResultTypeEnum.Numeric, "g/dL", "12-16", 1);
        var resolvedLine = new ResolvedLabOrderLine(item1, [comp1]);

        _resolutionService
            .Setup(r => r.ResolveByTarifItems(It.IsAny<IEnumerable<LabOrderTarifItemInput>>()))
            .Returns([resolvedLine]);

        LabOrderModel? savedOrder = null;
        _repo.Setup(r => r.SaveChanges(It.IsAny<LabOrderModel>()))
            .Callback<LabOrderModel>(m => savedOrder = m);

        var cmd = new LabOrderUpdateCmd(
            OrderId: order.OrderId,
            UserId: "USER01",
            Items: [new LabOrderTarifItemInput("TRF01", "Tarif HB", 0)],
            Urgency: LabOrderUrgencyEnum.Rutin,
            ClinicalNote: "Pembaruan diagnosa anemia");

        // Act
        var response = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.OrderId.Should().Be(order.OrderId);
        response.ItemCount.Should().Be(1);
        response.ComponentCount.Should().Be(1);

        savedOrder.Should().NotBeNull();
        savedOrder!.Items.Should().HaveCount(1);
        savedOrder.Items[0].TestDefinitionId.Should().Be("LTD001");
        savedOrder.ItemComponents.Should().HaveCount(1);
        savedOrder.ClinicalNote.Should().Be("Pembaruan diagnosa anemia");
        savedOrder.Urgency.Should().Be(LabOrderUrgencyEnum.Rutin);
        savedOrder.AuditTrail.Modified.UserId.Should().Be("USER01");
        _repo.Verify(r => r.SaveChanges(It.IsAny<LabOrderModel>()), Times.Once);
    }

    [Fact]
    public async Task Handle_OrderNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _repo.Setup(r => r.LoadEntity(It.IsAny<ILabOrderKey>()))
            .Returns(MayBe<LabOrderModel>.None);

        var cmd = new LabOrderUpdateCmd(
            OrderId: "LBO-NOTFOUND",
            UserId: "USER01",
            Items: [new LabOrderTarifItemInput("TRF01", "Tarif HB", 0)],
            Urgency: LabOrderUrgencyEnum.Rutin,
            ClinicalNote: "");

        // Act
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*not found*");
        _repo.Verify(r => r.SaveChanges(It.IsAny<LabOrderModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_OrderNotOrdered_ThrowsInvalidOperationException()
    {
        // Arrange
        var order = OrderedOrder();
        order.MarkCharged("TDK001", "USER01"); // Move to Charged
        _repo.Setup(r => r.LoadEntity(It.IsAny<ILabOrderKey>()))
            .Returns(MayBe.From(order));

        var cmd = new LabOrderUpdateCmd(
            OrderId: order.OrderId,
            UserId: "USER01",
            Items: [new LabOrderTarifItemInput("TRF01", "Tarif HB", 0)],
            Urgency: LabOrderUrgencyEnum.Rutin,
            ClinicalNote: "");

        // Act
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"LabOrder {order.OrderId} berstatus Charged; update hanya diperbolehkan dari Ordered.");
        _repo.Verify(r => r.SaveChanges(It.IsAny<LabOrderModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_EmptyItems_ThrowsArgumentException()
    {
        var cmd = new LabOrderUpdateCmd(
            OrderId: "LBO001",
            UserId: "USER01",
            Items: [],
            Urgency: LabOrderUrgencyEnum.Rutin,
            ClinicalNote: "");

        var act = async () => await _sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
        _repo.Verify(r => r.SaveChanges(It.IsAny<LabOrderModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_EmptyOrderId_ThrowsArgumentException()
    {
        var cmd = new LabOrderUpdateCmd(
            OrderId: "",
            UserId: "USER01",
            Items: [new LabOrderTarifItemInput("TRF01", "Tarif HB", 0)],
            Urgency: LabOrderUrgencyEnum.Rutin,
            ClinicalNote: "");

        var act = async () => await _sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Handle_EmptyUserId_ThrowsArgumentException()
    {
        var cmd = new LabOrderUpdateCmd(
            OrderId: "LBO001",
            UserId: "",
            Items: [new LabOrderTarifItemInput("TRF01", "Tarif HB", 0)],
            Urgency: LabOrderUrgencyEnum.Rutin,
            ClinicalNote: "");

        var act = async () => await _sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Handle_WhenDuplicatesInOtherOrdersAndNotConfirmed_ThrowsDuplicateWarning()
    {
        // Arrange
        var order = OrderedOrder();
        _repo.Setup(r => r.LoadEntity(It.IsAny<ILabOrderKey>()))
            .Returns(MayBe.From(order));

        var item1 = LabOrderTestSupport.SampleItem("LTD001", "HB", "Hemoglobin", "TRF01", "T-HB", "Tarif HB", VacutainerTypeEnum.Edta, "Blood", 1);
        var comp1 = LabOrderTestSupport.SampleComponent("CMP01", "HB", "Hemoglobin", LabResultTypeEnum.Numeric, "g/dL", "12-16", 1);
        var resolvedLine = new ResolvedLabOrderLine(item1, [comp1]);

        _resolutionService
            .Setup(r => r.ResolveByTarifItems(It.IsAny<IEnumerable<LabOrderTarifItemInput>>()))
            .Returns([resolvedLine]);

        var dalMock = new Mock<ILabOrderDal>();
        dalMock
            .Setup(d => d.ListActiveTestsByPatientWithinHours(order.Patient.PatientId, 24, It.IsAny<DateTime>()))
            .Returns([
                new LabOrderActiveTestDto(
                    OrderId: "OTHER_ORDER_01",
                    OrderNo: "ORD-001",
                    TestDefinitionId: "LTD001",
                    LabTestCode: "HB",
                    LabTestName: "Hemoglobin",
                    OrderDate: DateTime.Now.AddHours(-2),
                    LabOrderStatus: 1)
            ]);

        var sut = new LabOrderUpdateHandler(_repo.Object, _resolutionService.Object, TestTglJamProvider.Instance, dalMock.Object);

        var cmd = new LabOrderUpdateCmd(
            OrderId: order.OrderId,
            UserId: "USER01",
            Items: [new LabOrderTarifItemInput("TRF01", "Tarif HB", 0)],
            AllowDuplicateConfirmation: false);

        // Act
        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<DuplicateLabOrderWarningException>();
        ex.Which.DuplicateTestNames.Should().Contain("Hemoglobin");
        _repo.Verify(r => r.SaveChanges(It.IsAny<LabOrderModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenDuplicatesInOtherOrdersAndConfirmed_SavesSuccessfully()
    {
        // Arrange
        var order = OrderedOrder();
        _repo.Setup(r => r.LoadEntity(It.IsAny<ILabOrderKey>()))
            .Returns(MayBe.From(order));

        var item1 = LabOrderTestSupport.SampleItem("LTD001", "HB", "Hemoglobin", "TRF01", "T-HB", "Tarif HB", VacutainerTypeEnum.Edta, "Blood", 1);
        var comp1 = LabOrderTestSupport.SampleComponent("CMP01", "HB", "Hemoglobin", LabResultTypeEnum.Numeric, "g/dL", "12-16", 1);
        var resolvedLine = new ResolvedLabOrderLine(item1, [comp1]);

        _resolutionService
            .Setup(r => r.ResolveByTarifItems(It.IsAny<IEnumerable<LabOrderTarifItemInput>>()))
            .Returns([resolvedLine]);

        var dalMock = new Mock<ILabOrderDal>();
        dalMock
            .Setup(d => d.ListActiveTestsByPatientWithinHours(order.Patient.PatientId, 24, It.IsAny<DateTime>()))
            .Returns([
                new LabOrderActiveTestDto(
                    OrderId: "OTHER_ORDER_01",
                    OrderNo: "ORD-001",
                    TestDefinitionId: "LTD001",
                    LabTestCode: "HB",
                    LabTestName: "Hemoglobin",
                    OrderDate: DateTime.Now.AddHours(-2),
                    LabOrderStatus: 1)
            ]);

        var sut = new LabOrderUpdateHandler(_repo.Object, _resolutionService.Object, TestTglJamProvider.Instance, dalMock.Object);

        var cmd = new LabOrderUpdateCmd(
            OrderId: order.OrderId,
            UserId: "USER01",
            Items: [new LabOrderTarifItemInput("TRF01", "Tarif HB", 0)],
            AllowDuplicateConfirmation: true);

        // Act
        var response = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        _repo.Verify(r => r.SaveChanges(It.IsAny<LabOrderModel>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenDuplicatesOnlyBelongToCurrentOrder_DoesNotThrowDuplicateWarning()
    {
        // Arrange
        var order = OrderedOrder();
        _repo.Setup(r => r.LoadEntity(It.IsAny<ILabOrderKey>()))
            .Returns(MayBe.From(order));

        var item1 = LabOrderTestSupport.SampleItem("LTD001", "HB", "Hemoglobin", "TRF01", "T-HB", "Tarif HB", VacutainerTypeEnum.Edta, "Blood", 1);
        var comp1 = LabOrderTestSupport.SampleComponent("CMP01", "HB", "Hemoglobin", LabResultTypeEnum.Numeric, "g/dL", "12-16", 1);
        var resolvedLine = new ResolvedLabOrderLine(item1, [comp1]);

        _resolutionService
            .Setup(r => r.ResolveByTarifItems(It.IsAny<IEnumerable<LabOrderTarifItemInput>>()))
            .Returns([resolvedLine]);

        var dalMock = new Mock<ILabOrderDal>();
        dalMock
            .Setup(d => d.ListActiveTestsByPatientWithinHours(order.Patient.PatientId, 24, It.IsAny<DateTime>()))
            .Returns([
                new LabOrderActiveTestDto(
                    OrderId: order.OrderId,
                    OrderNo: order.OrderNo,
                    TestDefinitionId: "LTD001",
                    LabTestCode: "HB",
                    LabTestName: "Hemoglobin",
                    OrderDate: DateTime.Now.AddHours(-1),
                    LabOrderStatus: 1)
            ]);

        var sut = new LabOrderUpdateHandler(_repo.Object, _resolutionService.Object, TestTglJamProvider.Instance, dalMock.Object);

        var cmd = new LabOrderUpdateCmd(
            OrderId: order.OrderId,
            UserId: "USER01",
            Items: [new LabOrderTarifItemInput("TRF01", "Tarif HB", 0)],
            AllowDuplicateConfirmation: false);

        // Act
        var response = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        _repo.Verify(r => r.SaveChanges(It.IsAny<LabOrderModel>()), Times.Once);
    }
}
