using Bilreg.Application.RadContext.RadOrderFeature;
using Bilreg.Application.RadContext.RadOrderFeature.Integration;
using Bilreg.Application.RadContext.RadOrderFeature.UseCases;
using Bilreg.Domain.RadContext.RadOrderFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Test.Shared;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.RadContext.RadOrderFeature;

public class RadOrderCancelCmdTest
{
    private readonly Mock<IRadOrderRepo> _repoMock = new();
    private readonly Mock<IRadBillingIntegration> _billingMock = new();
    private readonly DateTime _now = new(2026, 11, 24, 14, 30, 0);
    private readonly RadOrderCancelHandler _sut;

    public RadOrderCancelCmdTest()
    {
        _sut = new RadOrderCancelHandler(_repoMock.Object, _billingMock.Object, new TestTglJamProvider(_now));
    }

    private static PatientSnapshotType EmrSnapshot() =>
        new("REG001", "MR0001", "Pasien Uji Coba", new DateTime(1990, 1, 15), "L", 36);

    private static RadOrderItemModel CreateItem() =>
        RadOrderItemModel.Create("TRF01", "Thorax AP", RadModalityEnum.CR, RadUrgencyEnum.Rutin);

    private static RadOrderModel CreateTestOrder(
        string orderId = "RDO0000000001",
        RadOrderStatusEnum status = RadOrderStatusEnum.Requested)
    {
        var audit = new AuditInfoType("DOCTOR1", new DateTime(2026, 11, 24, 8, 0, 0));
        var order = RadOrderModel.CreateFromEmr(
            emrOrderId: "EMR-RAD-001",
            patient: EmrSnapshot(),
            modality: RadModalityEnum.CR,
            items: [CreateItem()],
            orderNo: "RAD26110001",
            audit: audit,
            orderId: orderId);

        if (status == RadOrderStatusEnum.Scheduled)
        {
            order.Schedule("TECH1", new DateTime(2026, 11, 24, 9, 0, 0));
        }
        else if (status == RadOrderStatusEnum.Charged)
        {
            order.MarkCharged("TND001", "BILL_USER", new DateTime(2026, 11, 24, 9, 15, 0));
        }
        else if (status == RadOrderStatusEnum.Cancelled)
        {
            order.Cancel("Batal awal", "USER_PREV", new DateTime(2026, 11, 24, 9, 30, 0));
        }

        return order;
    }

    [Fact]
    public async Task Handle_RequestedOrder_CancelsSuccessfully_AndSavesChanges()
    {
        // Arrange
        var order = CreateTestOrder("RDO0000000001", RadOrderStatusEnum.Requested);
        _repoMock.Setup(r => r.LoadEntity(It.Is<IRadOrderKey>(k => k.OrderId == "RDO0000000001")))
            .Returns(MayBe.From(order));

        var cmd = new RadOrderCancelCmd(
            OrderId: "RDO0000000001",
            UserId: "DR_CANCEL",
            Reason: "Pasien menolak tindakan pemeriksaan");

        // Act
        await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        order.RadOrderStatus.Should().Be(RadOrderStatusEnum.Cancelled);
        order.CancelledReason.Should().Be("Pasien menolak tindakan pemeriksaan");
        order.CancelledUserId.Should().Be("DR_CANCEL");
        order.CancelledDate.Should().Be(_now);
        order.IsCancelled.Should().BeTrue();
        order.IsActive.Should().BeFalse();
        order.AuditTrail.Modified.UserId.Should().Be("DR_CANCEL");
        order.AuditTrail.Modified.Timestamp.Should().Be(_now);

        _repoMock.Verify(r => r.SaveChanges(order), Times.Once);
        _billingMock.Verify(b => b.CancelCharge(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ScheduledOrder_CancelsSuccessfully_AndSavesChanges()
    {
        // Arrange
        var order = CreateTestOrder("RDO0000000002", RadOrderStatusEnum.Scheduled);
        _repoMock.Setup(r => r.LoadEntity(It.Is<IRadOrderKey>(k => k.OrderId == "RDO0000000002")))
            .Returns(MayBe.From(order));

        var cmd = new RadOrderCancelCmd(
            OrderId: "RDO0000000002",
            UserId: "TECH_CANCEL",
            Reason: "Pesawat radiologi memerlukan pemeliharaan darurat");

        // Act
        await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        order.RadOrderStatus.Should().Be(RadOrderStatusEnum.Cancelled);
        order.CancelledReason.Should().Be("Pesawat radiologi memerlukan pemeliharaan darurat");
        order.CancelledUserId.Should().Be("TECH_CANCEL");
        order.CancelledDate.Should().Be(_now);
        order.IsCancelled.Should().BeTrue();

        _repoMock.Verify(r => r.SaveChanges(order), Times.Once);
        _billingMock.Verify(b => b.CancelCharge(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ChargedOrder_CallsCancelCharge_AndCancelsSuccessfully()
    {
        // Arrange
        var order = CreateTestOrder("RDO0000000003", RadOrderStatusEnum.Charged);
        _repoMock.Setup(r => r.LoadEntity(It.Is<IRadOrderKey>(k => k.OrderId == "RDO0000000003")))
            .Returns(MayBe.From(order));

        var cmd = new RadOrderCancelCmd(
            OrderId: "RDO0000000003",
            UserId: "DR_CANCEL",
            Reason: "Kondisi pasien tidak memungkinkan dilakukan foto radiologi");

        // Act
        await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        _billingMock.Verify(b => b.CancelCharge(
            "RDO0000000003",
            "REG001",
            "DR_CANCEL",
            "Kondisi pasien tidak memungkinkan dilakukan foto radiologi",
            _now), Times.Once);

        order.RadOrderStatus.Should().Be(RadOrderStatusEnum.Cancelled);
        order.CancelledReason.Should().Be("Kondisi pasien tidak memungkinkan dilakukan foto radiologi");
        order.CancelledUserId.Should().Be("DR_CANCEL");
        order.CancelledDate.Should().Be(_now);
        order.IsCancelled.Should().BeTrue();

        _repoMock.Verify(r => r.SaveChanges(order), Times.Once);
    }

    [Fact]
    public async Task Handle_ChargedOrder_WhenBillingCompensationFails_ThrowsExceptionAndDoesNotCancel()
    {
        // Arrange
        var order = CreateTestOrder("RDO0000000004", RadOrderStatusEnum.Charged);
        _repoMock.Setup(r => r.LoadEntity(It.Is<IRadOrderKey>(k => k.OrderId == "RDO0000000004")))
            .Returns(MayBe.From(order));

        _billingMock.Setup(b => b.CancelCharge(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>()))
            .Throws(new InvalidOperationException("Rekening pasien sudah final atau lunas di kasir"));

        var cmd = new RadOrderCancelCmd(
            OrderId: "RDO0000000004",
            UserId: "DR_CANCEL",
            Reason: "Pasien membatalkan pemeriksaan radiologi");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*kasir*");

        order.RadOrderStatus.Should().Be(RadOrderStatusEnum.Charged);
        order.IsCancelled.Should().BeFalse();
        _repoMock.Verify(r => r.SaveChanges(It.IsAny<RadOrderModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_OrderNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _repoMock.Setup(r => r.LoadEntity(It.IsAny<IRadOrderKey>()))
            .Returns(MayBe<RadOrderModel>.None);

        var cmd = new RadOrderCancelCmd(
            OrderId: "RDO_NON_EXISTENT",
            UserId: "USER1",
            Reason: "Alasan pembatalan");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*RDO_NON_EXISTENT*");

        _repoMock.Verify(r => r.SaveChanges(It.IsAny<RadOrderModel>()), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("a")]
    [InlineData("ab")]
    public async Task Handle_ShortOrEmptyReason_ThrowsArgumentException(string shortReason)
    {
        // Arrange
        var order = CreateTestOrder("RDO0000000001", RadOrderStatusEnum.Requested);
        _repoMock.Setup(r => r.LoadEntity(It.IsAny<IRadOrderKey>()))
            .Returns(MayBe.From(order));

        var cmd = new RadOrderCancelCmd("RDO0000000001", "USER1", shortReason);

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
        _repoMock.Verify(r => r.SaveChanges(It.IsAny<RadOrderModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AlreadyCancelledOrder_ThrowsInvalidOperationException()
    {
        // Arrange (EX-04: sudah dibatalkan sebelumnya)
        var order = CreateTestOrder("RDO0000000001", RadOrderStatusEnum.Cancelled);
        _repoMock.Setup(r => r.LoadEntity(It.IsAny<IRadOrderKey>()))
            .Returns(MayBe.From(order));

        var cmd = new RadOrderCancelCmd("RDO0000000001", "USER2", "Membatalkan kembali order");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*pembatalan hanya diizinkan untuk status Requested, Scheduled, atau Charged*");

        _repoMock.Verify(r => r.SaveChanges(It.IsAny<RadOrderModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_PhysicalExamOrCompletedStatus_ThrowsInvalidOperationException()
    {
        // Arrange (EX-04: status bukan Requested atau Scheduled, misalnya status pemeriksaan fisik / selesai)
        var order = new RadOrderModel(
            orderId: "RDO0000000001",
            emrOrderId: "EMR-RAD-001",
            orderNo: "RAD26110001",
            orderSource: RadOrderSourceEnum.Internal,
            modality: RadModalityEnum.CR,
            radOrderStatus: (RadOrderStatusEnum)99, // Status simulated exam in progress/completed
            urgency: RadUrgencyEnum.Rutin,
            patient: EmrSnapshot(),
            executionRegId: "REG001",
            dokterPengorderId: "DR01",
            dokterPengorderName: "dr. Sp.Rad",
            layananPengorderId: "LY01",
            layananPengorderName: "Poli Paru",
            clinicalNote: "Catatan klinis",
            cancelledReason: string.Empty,
            cancelledDate: RadOrderModel.EmptyDate,
            cancelledUserId: string.Empty,
            auditTrail: AuditTrailType.Create("DR01", DateTime.Now),
            items: [CreateItem()]);

        _repoMock.Setup(r => r.LoadEntity(It.IsAny<IRadOrderKey>()))
            .Returns(MayBe.From(order));

        var cmd = new RadOrderCancelCmd("RDO0000000001", "USER1", "Pembatalan order pemeriksaan");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*pembatalan hanya diizinkan untuk status Requested, Scheduled, atau Charged*");

        _repoMock.Verify(r => r.SaveChanges(It.IsAny<RadOrderModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_VoidedOrder_ThrowsInvalidOperationException()
    {
        // Arrange
        var order = CreateTestOrder("RDO0000000001", RadOrderStatusEnum.Requested);
        order.AuditTrail.Batal("ADMIN", new DateTime(2026, 11, 24, 9, 0, 0));
        _repoMock.Setup(r => r.LoadEntity(It.IsAny<IRadOrderKey>()))
            .Returns(MayBe.From(order));

        var cmd = new RadOrderCancelCmd("RDO0000000001", "USER1", "Alasan pembatalan valid");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*sudah void*");

        _repoMock.Verify(r => r.SaveChanges(It.IsAny<RadOrderModel>()), Times.Never);
    }

    [Theory]
    [InlineData("", "USER1", "Alasan valid")]
    [InlineData("   ", "USER1", "Alasan valid")]
    [InlineData("RDO001", "", "Alasan valid")]
    [InlineData("RDO001", "   ", "Alasan valid")]
    public async Task Handle_EmptyParameters_ThrowsArgumentException(string orderId, string userId, string reason)
    {
        // Arrange
        var cmd = new RadOrderCancelCmd(orderId, userId, reason);

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
        _repoMock.Verify(r => r.SaveChanges(It.IsAny<RadOrderModel>()), Times.Never);
    }
}
