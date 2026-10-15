using Bilreg.Application.LabContext.LabOrderFeature;
using Bilreg.Application.LabContext.LabOrderFeature.UseCases;
using Bilreg.Application.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Test.PaymentContext.TataRekeningFeature;
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
    private readonly Mock<ITataRekeningRepo> _tataRekeningRepo = new();
    private readonly Mock<ITglJamProvider> _tglJamProvider = new();
    private readonly LabOrderCancelHandler _sut;

    public LabOrderCancelHandlerTest()
    {
        _tglJamProvider.Setup(t => t.Now).Returns(FixedNow);
        _sut = new LabOrderCancelHandler(
            _labOrderRepo.Object,
            _tglJamProvider.Object,
            _tataRekeningRepo.Object);
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
        _tataRekeningRepo.Verify(r => r.LoadEntity(It.IsAny<IRegKey>()), Times.Never);
        _tataRekeningRepo.Verify(r => r.SaveChanges(It.IsAny<TataRekeningModel>()), Times.Never);
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
        _tataRekeningRepo.Verify(r => r.SaveChanges(It.IsAny<TataRekeningModel>()), Times.Never);
    }

    #endregion

    #region Charged Order with Open Billing (Success & Bill Voided)

    [Fact]
    public async Task Handle_GivenChargedOrder_WhenBillingIsOpen_DeletesBillAndCancelsOrder()
    {
        // Arrange
        var order = CreateChargedModel(TindakanId);
        _labOrderRepo.Setup(r => r.LoadEntity(It.Is<ILabOrderKey>(k => k.OrderId == OrderId)))
            .Returns(MayBe.From(order));

        var bill = TataRekeningTestDataBuilder.CreateBill(TindakanId, RegId, 150_000m);
        var tataRekening = TataRekeningTestDataBuilder.Hydrate(
            RegId, TataRekeningStatusEnum.Opened, bill);
        tataRekening.ListTrsBill.Should().Contain(b => b.TrsBillingId == TindakanId);

        _tataRekeningRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(MayBe.From(tataRekening));

        var cmd = new LabOrderCancelCmd(OrderId, UserId, Reason);

        // Act
        await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        order.LabOrderStatus.Should().Be(LabOrderStatusEnum.Cancelled);
        order.CancelledReason.Should().Be(Reason);
        order.CancelledUserId.Should().Be(UserId);
        order.CancelledDate.Should().Be(FixedNow);

        tataRekening.ListTrsBill.Should().NotContain(b => b.TrsBillingId == TindakanId);

        _tataRekeningRepo.Verify(r => r.SaveChanges(tataRekening), Times.Once);
        _labOrderRepo.Verify(r => r.SaveChanges(order), Times.Once);
    }

    #endregion

    #region Charged Order Rejection (Finalized / Lunas / Has Payments)

    [Fact]
    public async Task Handle_GivenChargedOrder_WhenBillingIsFinalized_RejectsCancellation()
    {
        // Arrange
        var order = CreateChargedModel(TindakanId);
        _labOrderRepo.Setup(r => r.LoadEntity(It.Is<ILabOrderKey>(k => k.OrderId == OrderId)))
            .Returns(MayBe.From(order));

        var bill = TataRekeningTestDataBuilder.CreateBill(TindakanId, RegId, 150_000m);
        var tataRekening = TataRekeningTestDataBuilder.Hydrate(
            RegId, TataRekeningStatusEnum.Finalized, bill);

        _tataRekeningRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(MayBe.From(tataRekening));

        var cmd = new LabOrderCancelCmd(OrderId, UserId, Reason);

        // Act
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Order laboratorium tidak dapat dibatalkan karena transaksi penagihan kasir telah Final/Lunas.");

        _tataRekeningRepo.Verify(r => r.SaveChanges(It.IsAny<TataRekeningModel>()), Times.Never);
        _labOrderRepo.Verify(r => r.SaveChanges(It.IsAny<LabOrderModel>()), Times.Never);
        order.LabOrderStatus.Should().Be(LabOrderStatusEnum.Charged);
    }

    [Fact]
    public async Task Handle_GivenChargedOrder_WhenBillingIsLunas_RejectsCancellation()
    {
        // Arrange
        var order = CreateChargedModel(TindakanId);
        _labOrderRepo.Setup(r => r.LoadEntity(It.Is<ILabOrderKey>(k => k.OrderId == OrderId)))
            .Returns(MayBe.From(order));

        var bill = TataRekeningTestDataBuilder.CreateBill(TindakanId, RegId, 150_000m);
        var tataRekening = TataRekeningTestDataBuilder.Hydrate(
            RegId, TataRekeningStatusEnum.Lunas, bill);

        _tataRekeningRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(MayBe.From(tataRekening));

        var cmd = new LabOrderCancelCmd(OrderId, UserId, Reason);

        // Act
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Order laboratorium tidak dapat dibatalkan karena transaksi penagihan kasir telah Final/Lunas.");

        _tataRekeningRepo.Verify(r => r.SaveChanges(It.IsAny<TataRekeningModel>()), Times.Never);
        _labOrderRepo.Verify(r => r.SaveChanges(It.IsAny<LabOrderModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_GivenChargedOrder_WhenBillingHasPayments_RejectsCancellation()
    {
        // Arrange
        var order = CreateChargedModel(TindakanId);
        _labOrderRepo.Setup(r => r.LoadEntity(It.Is<ILabOrderKey>(k => k.OrderId == OrderId)))
            .Returns(MayBe.From(order));

        var bill = TataRekeningTestDataBuilder.CreateBill(TindakanId, RegId, 150_000m);
        var payment = TataRekeningTestDataBuilder.BuildPayment(PaymentType.ByKas, 100_000m, 50_000m);
        var tataRekening = new TataRekeningModel(
            RegId,
            TataRekeningStatusEnum.Opened,
            TataRekeningFinalizationType.Default,
            [payment],
            [bill]);

        _tataRekeningRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(MayBe.From(tataRekening));

        var cmd = new LabOrderCancelCmd(OrderId, UserId, Reason);

        // Act
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Order laboratorium tidak dapat dibatalkan karena transaksi penagihan kasir telah Final/Lunas.");

        _tataRekeningRepo.Verify(r => r.SaveChanges(It.IsAny<TataRekeningModel>()), Times.Never);
        _labOrderRepo.Verify(r => r.SaveChanges(It.IsAny<LabOrderModel>()), Times.Never);
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
        _tataRekeningRepo.Verify(r => r.SaveChanges(It.IsAny<TataRekeningModel>()), Times.Never);
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
    }

    #endregion
}
