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

public class RadOrderChargeCmdTest
{
    private readonly Mock<IRadOrderRepo> _repoMock = new();
    private readonly Mock<IRadBillingIntegration> _billingMock = new();
    private readonly DateTime _now = new(2026, 11, 24, 14, 30, 0);
    private readonly RadOrderChargeHandler _sut;

    public RadOrderChargeCmdTest()
    {
        _sut = new RadOrderChargeHandler(_repoMock.Object, _billingMock.Object, new TestTglJamProvider(_now));
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
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        var act = () => _sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_InvalidOrderId_ThrowsArgumentException(string orderId)
    {
        var cmd = new RadOrderChargeCmd(orderId, "USER1");
        var act = () => _sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_InvalidUserId_ThrowsArgumentException(string userId)
    {
        var cmd = new RadOrderChargeCmd("RDO0000000001", userId);
        var act = () => _sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Handle_OrderNotFound_ThrowsKeyNotFoundException()
    {
        var cmd = new RadOrderChargeCmd("RDO_NOT_FOUND", "USER1");
        _repoMock.Setup(x => x.LoadEntity(It.Is<IRadOrderKey>(k => k.OrderId == "RDO_NOT_FOUND")))
            .Returns(MayBe<RadOrderModel>.None);

        var act = () => _sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Order radiologi 'RDO_NOT_FOUND' tidak ditemukan.*");
    }

    [Fact]
    public async Task Handle_ChargeSuccess_MarksChargedAndSaves()
    {
        var order = CreateTestOrder("RDO0000000001", RadOrderStatusEnum.Requested);
        var cmd = new RadOrderChargeCmd("RDO0000000001", "USER_CHARGE");

        _repoMock.Setup(x => x.LoadEntity(It.Is<IRadOrderKey>(k => k.OrderId == "RDO0000000001")))
            .Returns(MayBe<RadOrderModel>.Some(order));

        _billingMock.Setup(x => x.ChargeOrder(order, "USER_CHARGE", _now))
            .Returns("TND_RAD_999");

        var response = await _sut.Handle(cmd, CancellationToken.None);

        response.Should().NotBeNull();
        response.Success.Should().BeTrue();
        response.BillingTindakanId.Should().Be("TND_RAD_999");
        response.BillingLastError.Should().BeNull();

        order.RadOrderStatus.Should().Be(RadOrderStatusEnum.Charged);
        order.BillingTindakanId.Should().Be("TND_RAD_999");
        order.BillingLastError.Should().BeEmpty();

        _repoMock.Verify(x => x.SaveChanges(order), Times.Once);
    }

    [Fact]
    public async Task Handle_ScheduledOrder_ChargeSuccess_MarksCharged()
    {
        var order = CreateTestOrder("RDO0000000001", RadOrderStatusEnum.Scheduled);
        var cmd = new RadOrderChargeCmd("RDO0000000001", "USER_CHARGE");

        _repoMock.Setup(x => x.LoadEntity(It.Is<IRadOrderKey>(k => k.OrderId == "RDO0000000001")))
            .Returns(MayBe<RadOrderModel>.Some(order));

        _billingMock.Setup(x => x.ChargeOrder(order, "USER_CHARGE", _now))
            .Returns("TND_RAD_888");

        var response = await _sut.Handle(cmd, CancellationToken.None);

        response.Success.Should().BeTrue();
        response.BillingTindakanId.Should().Be("TND_RAD_888");
        order.RadOrderStatus.Should().Be(RadOrderStatusEnum.Charged);
        _repoMock.Verify(x => x.SaveChanges(order), Times.Once);
    }

    [Fact]
    public async Task Handle_ChargeException_RecordsBillingErrorAndSaves()
    {
        var order = CreateTestOrder("RDO0000000001", RadOrderStatusEnum.Requested);
        var cmd = new RadOrderChargeCmd("RDO0000000001", "USER_CHARGE");

        _repoMock.Setup(x => x.LoadEntity(It.Is<IRadOrderKey>(k => k.OrderId == "RDO0000000001")))
            .Returns(MayBe<RadOrderModel>.Some(order));

        _billingMock.Setup(x => x.ChargeOrder(order, "USER_CHARGE", _now))
            .Throws(new RadBillingChargeException("Penjamin tidak aktif"));

        var response = await _sut.Handle(cmd, CancellationToken.None);

        response.Should().NotBeNull();
        response.Success.Should().BeFalse();
        response.BillingTindakanId.Should().BeNull();
        response.BillingLastError.Should().Be("Penjamin tidak aktif");

        order.RadOrderStatus.Should().Be(RadOrderStatusEnum.Requested);
        order.BillingLastError.Should().Be("Penjamin tidak aktif");

        _repoMock.Verify(x => x.SaveChanges(order), Times.Once);
    }

    [Fact]
    public async Task Handle_AlreadyChargedOrder_ThrowsInvalidOperationException()
    {
        var order = CreateTestOrder("RDO0000000001", RadOrderStatusEnum.Charged);
        var cmd = new RadOrderChargeCmd("RDO0000000001", "USER_CHARGE");

        _repoMock.Setup(x => x.LoadEntity(It.Is<IRadOrderKey>(k => k.OrderId == "RDO0000000001")))
            .Returns(MayBe<RadOrderModel>.Some(order));

        var act = () => _sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*charge hanya diizinkan untuk status Requested atau Scheduled*");

        _billingMock.Verify(x => x.ChargeOrder(It.IsAny<RadOrderModel>(), It.IsAny<string>(), It.IsAny<DateTime>()), Times.Never);
        _repoMock.Verify(x => x.SaveChanges(It.IsAny<RadOrderModel>()), Times.Never);
    }
}
