using Bilreg.Application.LabContext.LabOrderFeature;
using Bilreg.Application.LabContext.LabOrderFeature.Integration;
using Bilreg.Application.LabContext.LabOrderFeature.UseCases;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.LabContext.LabOrderFeature;

public class LabOrderCancelHandlerTest
{
    private const string OrderId = "LAB20261015001";
    private const string RegId = "REG-20261015-001";
    private const string TindakanId = "TDK-BILLING-001";
    private const string UserId = "DOKTER01";
    private const string Reason = "Pasien menolak tindakan laboratorium";
    private static readonly DateTime FixedNow = new(2026, 10, 15, 14, 0, 0);

    private readonly Mock<ILabOrderRepo> _labOrderRepo = new();
    private readonly Mock<ILabBillingIntegration> _billingIntegration = new();
    private readonly Mock<ITglJamProvider> _tglJamProvider = new();
    private readonly LabOrderCancelHandler _sut;

    public LabOrderCancelHandlerTest()
    {
        _tglJamProvider.Setup(t => t.Now).Returns(FixedNow);
        _sut = new LabOrderCancelHandler(
            _labOrderRepo.Object,
            _billingIntegration.Object,
            _tglJamProvider.Object);
    }

    private static LabOrderModel CreateOrderedModel()
    {
        var snapshot = new PatientSnapshotType(
            RegId, "MR001", "Budi Santoso", new DateTime(1985, 5, 20), "L", 41);
        var item = LabOrderTestSupport.SampleItem();
        return LabOrderTestSupport.CreateEmrOrder(
            OrderId,
            lines: [LabOrderTestSupport.ResolvedLine(item)],
            snapshot: snapshot,
            audit: new AuditInfoType("SYS", FixedNow.AddHours(-1)));
    }

    private static LabOrderModel CreateChargedModel(string tindakanId = TindakanId)
    {
        var order = CreateOrderedModel();
        order.Charge("BILLING_OFFICER");
        order.MarkCharged(tindakanId, "BILLING_OFFICER", FixedNow.AddMinutes(-30));
        return order;
    }

    private static LabOrderModel CreateCollectedModel()
    {
        var order = CreateChargedModel();
        order.CollectSpecimen(
            "PHLEBOTOMIST",
            new CollectionInfoType(FixedNow.AddMinutes(-10), "PHLEBOTOMIST", "Koleksi berhasil"));
        return order;
    }

    #region Argument Validations

    [Fact]
    public async Task Handle_GivenNullRequest_ThrowsArgumentNullException()
    {
        var act = async () => await _sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_GivenEmptyOrderId_ThrowsArgumentException(string invalidOrderId)
    {
        var cmd = new LabOrderCancelCmd(invalidOrderId, UserId, Reason);
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_GivenEmptyUserId_ThrowsArgumentException(string invalidUserId)
    {
        var cmd = new LabOrderCancelCmd(OrderId, invalidUserId, Reason);
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_GivenEmptyReason_ThrowsArgumentException(string invalidReason)
    {
        var cmd = new LabOrderCancelCmd(OrderId, UserId, invalidReason);
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Handle_GivenOrderNotFound_ThrowsKeyNotFoundException()
    {
        _labOrderRepo.Setup(r => r.LoadEntity(It.Is<ILabOrderKey>(k => k.OrderId == OrderId)))
            .Returns(MayBe<LabOrderModel>.None);

        var cmd = new LabOrderCancelCmd(OrderId, UserId, Reason);
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*LabOrder '{OrderId}' not found*");
    }

    #endregion

    #region Regular Order Cancellation

    [Fact]
    public async Task Handle_GivenRegularOrderedModel_CancelsSuccessfullyWithAuditTrail()
    {
        // Arrange
        var order = CreateOrderedModel();
        order.LabOrderStatus.Should().Be(LabOrderStatusEnum.Ordered);
        _labOrderRepo.Setup(r => r.LoadEntity(It.Is<ILabOrderKey>(k => k.OrderId == OrderId)))
            .Returns(MayBe.From(order));

        var cmd = new LabOrderCancelCmd(OrderId, UserId, Reason);

        // Act
        await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        order.LabOrderStatus.Should().Be(LabOrderStatusEnum.Cancelled);
        order.CancelledReason.Should().Be(Reason);
        order.CancelledUserId.Should().Be(UserId);
        order.CancelledDate.Should().Be(FixedNow);

        _labOrderRepo.Verify(r => r.SaveChanges(order), Times.Once);
        _billingIntegration.Verify(b => b.CancelCharge(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()), Times.Never);
    }

    [Fact]
    public async Task Handle_GivenDeferredModel_CancelsSuccessfully()
    {
        // Arrange
        var order = CreateOrderedModel();
        order.Defer("Reagen habis", FixedNow.AddDays(1), "LAB_USER", FixedNow.AddMinutes(-20));
        order.LabOrderStatus.Should().Be(LabOrderStatusEnum.Deferred);
        _labOrderRepo.Setup(r => r.LoadEntity(It.Is<ILabOrderKey>(k => k.OrderId == OrderId)))
            .Returns(MayBe.From(order));

        var cmd = new LabOrderCancelCmd(OrderId, UserId, Reason);

        // Act
        await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        order.LabOrderStatus.Should().Be(LabOrderStatusEnum.Cancelled);
        _labOrderRepo.Verify(r => r.SaveChanges(order), Times.Once);
        _billingIntegration.Verify(b => b.CancelCharge(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()), Times.Never);
    }

    #endregion

    #region Charged Order Cancellation

    [Fact]
    public async Task Handle_GivenChargedOrder_CallsBillingIntegrationCancelChargeAndCancelsOrder()
    {
        // Arrange
        var order = CreateChargedModel(TindakanId);
        _labOrderRepo.Setup(r => r.LoadEntity(It.Is<ILabOrderKey>(k => k.OrderId == OrderId)))
            .Returns(MayBe.From(order));

        var cmd = new LabOrderCancelCmd(OrderId, UserId, Reason);

        // Act
        await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        order.LabOrderStatus.Should().Be(LabOrderStatusEnum.Cancelled);
        order.CancelledReason.Should().Be(Reason);
        order.CancelledUserId.Should().Be(UserId);
        order.CancelledDate.Should().Be(FixedNow);

        _billingIntegration.Verify(b => b.CancelCharge(order.OrderId, RegId, UserId, Reason, FixedNow), Times.Once);
        _labOrderRepo.Verify(r => r.SaveChanges(order), Times.Once);
    }

    [Fact]
    public async Task Handle_GivenChargedOrder_WhenBillingIntegrationThrows_PropagatesExceptionAndDoesNotCancelOrder()
    {
        // Arrange
        var order = CreateChargedModel(TindakanId);
        _labOrderRepo.Setup(r => r.LoadEntity(It.Is<ILabOrderKey>(k => k.OrderId == OrderId)))
            .Returns(MayBe.From(order));

        _billingIntegration.Setup(b => b.CancelCharge(order.OrderId, RegId, UserId, Reason, FixedNow))
            .Throws(new InvalidOperationException("Order laboratorium tidak dapat dibatalkan karena transaksi penagihan kasir telah Final/Lunas."));

        var cmd = new LabOrderCancelCmd(OrderId, UserId, Reason);

        // Act
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Order laboratorium tidak dapat dibatalkan karena transaksi penagihan kasir telah Final/Lunas.");

        _billingIntegration.Verify(b => b.CancelCharge(order.OrderId, RegId, UserId, Reason, FixedNow), Times.Once);
        _labOrderRepo.Verify(r => r.SaveChanges(It.IsAny<LabOrderModel>()), Times.Never);
        order.LabOrderStatus.Should().Be(LabOrderStatusEnum.Charged);
    }

    #endregion

    #region Specimen Guard (EX-03)

    [Fact]
    public async Task Handle_GivenCollectedOrder_RejectsCancellationWithSpecimenGuard()
    {
        // Arrange
        var order = CreateCollectedModel();
        order.LabOrderStatus.Should().Be(LabOrderStatusEnum.Collected);
        _labOrderRepo.Setup(r => r.LoadEntity(It.Is<ILabOrderKey>(k => k.OrderId == OrderId)))
            .Returns(MayBe.From(order));

        var cmd = new LabOrderCancelCmd(OrderId, UserId, Reason);

        // Act
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"LabOrder {order.OrderId} tidak dapat dibatalkan karena spesimen telah diambil (status Collected).");

        _labOrderRepo.Verify(r => r.SaveChanges(It.IsAny<LabOrderModel>()), Times.Never);
        _billingIntegration.Verify(b => b.CancelCharge(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()), Times.Never);
    }

    [Fact]
    public async Task Handle_GivenRecordedOrder_RejectsCancellationWithSpecimenGuard()
    {
        // Arrange
        var order = CreateCollectedModel();
        order.MarkRecorded("ANALIS01", FixedNow.AddMinutes(-5));
        order.LabOrderStatus.Should().Be(LabOrderStatusEnum.Recorded);
        _labOrderRepo.Setup(r => r.LoadEntity(It.Is<ILabOrderKey>(k => k.OrderId == OrderId)))
            .Returns(MayBe.From(order));

        var cmd = new LabOrderCancelCmd(OrderId, UserId, Reason);

        // Act
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"LabOrder {order.OrderId} tidak dapat dibatalkan karena spesimen telah diambil (status Recorded).");

        _labOrderRepo.Verify(r => r.SaveChanges(It.IsAny<LabOrderModel>()), Times.Never);
        _billingIntegration.Verify(b => b.CancelCharge(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()), Times.Never);
    }

    [Fact]
    public async Task Handle_GivenVerifiedOrder_RejectsCancellationWithSpecimenGuard()
    {
        // Arrange
        var order = CreateCollectedModel();
        order.MarkRecorded("ANALIS01", FixedNow.AddMinutes(-5));
        order.MarkVerified("DOKTER_PATOLOGI", FixedNow.AddMinutes(-2));
        order.LabOrderStatus.Should().Be(LabOrderStatusEnum.Verified);
        _labOrderRepo.Setup(r => r.LoadEntity(It.Is<ILabOrderKey>(k => k.OrderId == OrderId)))
            .Returns(MayBe.From(order));

        var cmd = new LabOrderCancelCmd(OrderId, UserId, Reason);

        // Act
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"LabOrder {order.OrderId} tidak dapat dibatalkan karena spesimen telah diambil (status Verified).");

        _labOrderRepo.Verify(r => r.SaveChanges(It.IsAny<LabOrderModel>()), Times.Never);
        _billingIntegration.Verify(b => b.CancelCharge(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()), Times.Never);
    }

    [Fact]
    public async Task Handle_GivenReleasedOrder_RejectsCancellationWithSpecimenGuard()
    {
        // Arrange
        var order = CreateCollectedModel();
        order.MarkRecorded("ANALIS01", FixedNow.AddMinutes(-5));
        order.MarkVerified("DOKTER_PATOLOGI", FixedNow.AddMinutes(-2));
        order.Release("DOKTER_PATOLOGI", "Hasil valid", FixedNow.AddMinutes(-1));
        order.LabOrderStatus.Should().Be(LabOrderStatusEnum.Released);
        _labOrderRepo.Setup(r => r.LoadEntity(It.Is<ILabOrderKey>(k => k.OrderId == OrderId)))
            .Returns(MayBe.From(order));

        var cmd = new LabOrderCancelCmd(OrderId, UserId, Reason);

        // Act
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"LabOrder {order.OrderId} tidak dapat dibatalkan karena spesimen telah diambil (status Released).");

        _labOrderRepo.Verify(r => r.SaveChanges(It.IsAny<LabOrderModel>()), Times.Never);
        _billingIntegration.Verify(b => b.CancelCharge(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()), Times.Never);
    }

    #endregion
}
