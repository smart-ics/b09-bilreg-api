using Bilreg.Application.LabContext.LabExternalFeature;
using Bilreg.Application.LabContext.LabOrderFeature;
using Bilreg.Application.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.LabContext.LabExternalFeature;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.Shared.Helpers;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Test.Shared;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.LabContext.LabExternalFeature;

public class LabExternalCancelHandlerTest
{
    private static readonly DateTime FixedNow = new(2026, 10, 15, 14, 0, 0);

    private readonly Mock<ILabExternalRepo> _labExternalRepo = new();
    private readonly Mock<ITataRekeningRepo> _tataRekeningRepo = new();
    private readonly Mock<ILabOrderRepo> _labOrderRepo = new();
    private readonly LabExternalCancelHandler _sut;

    public LabExternalCancelHandlerTest()
    {
        var clock = new TestTglJamProvider(FixedNow);
        _sut = new LabExternalCancelHandler(
            _labExternalRepo.Object,
            _tataRekeningRepo.Object,
            _labOrderRepo.Object,
            clock);
    }

    private static LabExternalModel CreateActiveExternal(string regExternalId = "LEX0001", string billingAccountId = "LEX0001")
    {
        var demographics = new LabExternalDemographicsType(
            "Budi Santoso",
            "3171012345670001",
            new DateTime(1990, 1, 1),
            "L",
            "08123456789",
            "Jl. Sudirman No. 1");

        var referralInfo = new LabExternalReferralInfoType(
            LabReferralTypeEnum.Aps,
            string.Empty,
            string.Empty,
            string.Empty);

        var audit = new AuditInfoType("USER1", new DateTime(2026, 10, 15, 8, 0, 0));

        return LabExternalModel.Create(
            regExternalId,
            "EX-202610-00001",
            demographics,
            referralInfo,
            billingAccountId,
            audit);
    }

    private static TataRekeningModel CreateTataRekeningWithPayment(string regId = "LEX0001")
    {
        var payment = new TataRekeningPaymentType(
            new PaymentType("1", "Cash", false),
            50000,
            0,
            new CoaType("111", "Kas"));

        return new TataRekeningModel(
            regId,
            TataRekeningStatusEnum.Opened,
            TataRekeningFinalizationType.Default,
            [payment],
            []);
    }

    private static TataRekeningModel CreateEmptyOpenedTataRekening(string regId = "LEX0001")
    {
        return new TataRekeningModel(
            regId,
            TataRekeningStatusEnum.Opened,
            TataRekeningFinalizationType.Default,
            [],
            []);
    }

    private static TataRekeningModel CreateClosedTataRekening(string regId = "LEX0001", TataRekeningStatusEnum status = TataRekeningStatusEnum.Closed)
    {
        return new TataRekeningModel(
            regId,
            status,
            TataRekeningFinalizationType.Default,
            [],
            []);
    }

    private static LabOrderModel CreateOrderWithStatus(
        string regId,
        string orderId,
        LabOrderStatusEnum status,
        CollectionInfoType? collectionInfo = null)
    {
        var snapshot = new PatientSnapshotType(regId, "EXT001", "Budi Santoso", new DateTime(1990, 1, 1), "L", 36);
        return LabOrderModel.Load(
            orderId: orderId,
            emrOrderId: "",
            orderNo: "ORD001",
            orderSource: LabOrderSourceEnum.ExternalPatient,
            labOrderStatus: status,
            lastBillingReleaseStatus: BillingReleaseValidationStatusEnum.NotChecked,
            owareStatus: OwareStatusEnum.Pending,
            patient: snapshot,
            executionRegId: "",
            deferredInfo: DeferredInfoType.Default,
            billingTindakanId: "",
            billingLastError: "",
            collectionInfo: collectionInfo ?? CollectionInfoType.Default,
            lastBillingReleaseCheckAt: LabOrderModel.EmptyDate,
            lastBillingReleaseCheckUserId: "",
            lastBillingReleaseMessage: "",
            releasedDate: LabOrderModel.EmptyDate,
            releasedUserId: "",
            releaseNote: "",
            cancelledReason: "",
            cancelledDate: LabOrderModel.EmptyDate,
            cancelledUserId: "",
            terminationReason: "",
            terminationDate: LabOrderModel.EmptyDate,
            terminationUserId: "",
            auditTrail: AuditTrailType.Default,
            items: [],
            itemComponents: []);
    }

    #region Input Validation Tests

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        var act = () => _sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_InvalidRegExternalId_ThrowsArgumentException(string? invalidId)
    {
        var cmd = new LabExternalCancelCmd(invalidId!, "Alasan batal", "USER1");
        var act = () => _sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_InvalidReason_ThrowsArgumentException(string? invalidReason)
    {
        var cmd = new LabExternalCancelCmd("LEX0001", invalidReason!, "USER1");
        var act = () => _sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_InvalidUserId_ThrowsArgumentException(string? invalidUserId)
    {
        var cmd = new LabExternalCancelCmd("LEX0001", "Alasan batal", invalidUserId!);
        var act = () => _sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Handle_LabExternalNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var cmd = new LabExternalCancelCmd("LEX_NOT_FOUND", "Alasan batal", "USER1");
        _labExternalRepo.Setup(r => r.LoadEntity(It.Is<ILabExternalKey>(k => k.RegExternalId == "LEX_NOT_FOUND")))
            .Returns(MayBe<LabExternalModel>.None);

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*LEX_NOT_FOUND*");
    }

    #endregion

    #region Domain Invariant Prior Status Tests

    [Fact]
    public async Task Handle_LabExternalAlreadyCancelled_ThrowsInvalidOperationException()
    {
        // Arrange
        var external = CreateActiveExternal("LEX0001");
        external.Cancel("Pembatalan sebelumnya", "USER_PREV", new DateTime(2026, 10, 15, 9, 0, 0), false, false);

        _labExternalRepo.Setup(r => r.LoadEntity(It.Is<ILabExternalKey>(k => k.RegExternalId == "LEX0001")))
            .Returns(MayBe.From(external));
        _tataRekeningRepo.Setup(r => r.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe.From(CreateEmptyOpenedTataRekening("LEX0001")));
        _labOrderRepo.Setup(r => r.ListByRegId("LEX0001"))
            .Returns([]);

        var cmd = new LabExternalCancelCmd("LEX0001", "Batal lagi", "USER1");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*sudah dalam status dibatalkan*");
    }

    [Fact]
    public async Task Handle_LabExternalAlreadyCompleted_ThrowsInvalidOperationException()
    {
        // Arrange
        var external = CreateActiveExternal("LEX0001");
        external.MarkCompleted("USER_PREV", new DateTime(2026, 10, 15, 9, 0, 0));

        _labExternalRepo.Setup(r => r.LoadEntity(It.Is<ILabExternalKey>(k => k.RegExternalId == "LEX0001")))
            .Returns(MayBe.From(external));
        _tataRekeningRepo.Setup(r => r.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe.From(CreateEmptyOpenedTataRekening("LEX0001")));
        _labOrderRepo.Setup(r => r.ListByRegId("LEX0001"))
            .Returns([]);

        var cmd = new LabExternalCancelCmd("LEX0001", "Batal selesai", "USER1");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*sudah selesai tidak dapat dibatalkan*");
    }

    #endregion

    #region Guard EX-01 (Cashier Payment) Tests

    [Fact]
    public async Task Handle_WhenCashierPaymentExists_ThrowsInvalidOperationExceptionWithEx01()
    {
        // Arrange
        var external = CreateActiveExternal("LEX0001");
        var tataRekeningWithPayment = CreateTataRekeningWithPayment("LEX0001");

        _labExternalRepo.Setup(r => r.LoadEntity(It.Is<ILabExternalKey>(k => k.RegExternalId == "LEX0001")))
            .Returns(MayBe.From(external));
        _tataRekeningRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == "LEX0001")))
            .Returns(MayBe.From(tataRekeningWithPayment));
        _labOrderRepo.Setup(r => r.ListByRegId("LEX0001"))
            .Returns([]);

        var cmd = new LabExternalCancelCmd("LEX0001", "Salah periksa", "USER1");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<InvalidOperationException>();
        ex.WithMessage("*EX-01*");
        ex.WithMessage("*Tagihan telah dibayar di kasir*");

        _labExternalRepo.Verify(r => r.SaveChanges(It.IsAny<LabExternalModel>()), Times.Never);
    }

    [Theory]
    [InlineData(TataRekeningStatusEnum.Closed)]
    [InlineData(TataRekeningStatusEnum.Finalized)]
    [InlineData(TataRekeningStatusEnum.Lunas)]
    public async Task Handle_WhenTataRekeningNotOpened_ThrowsInvalidOperationExceptionWithEx01(TataRekeningStatusEnum closedStatus)
    {
        // Arrange
        var external = CreateActiveExternal("LEX0001");
        var closedTataRekening = CreateClosedTataRekening("LEX0001", closedStatus);

        _labExternalRepo.Setup(r => r.LoadEntity(It.Is<ILabExternalKey>(k => k.RegExternalId == "LEX0001")))
            .Returns(MayBe.From(external));
        _tataRekeningRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == "LEX0001")))
            .Returns(MayBe.From(closedTataRekening));
        _labOrderRepo.Setup(r => r.ListByRegId("LEX0001"))
            .Returns([]);

        var cmd = new LabExternalCancelCmd("LEX0001", "Salah periksa", "USER1");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<InvalidOperationException>();
        ex.WithMessage("*EX-01*");

        _labExternalRepo.Verify(r => r.SaveChanges(It.IsAny<LabExternalModel>()), Times.Never);
    }

    #endregion

    #region Guard EX-02 (Specimen Collected) Tests

    [Fact]
    public async Task Handle_WhenSpecimenCollectedDateIsSet_ThrowsInvalidOperationExceptionWithEx02()
    {
        // Arrange
        var external = CreateActiveExternal("LEX0001");
        var emptyTataRekening = CreateEmptyOpenedTataRekening("LEX0001");
        var collectionInfo = new CollectionInfoType(new DateTime(2026, 10, 15, 11, 0, 0), "USER_PHLEB", "Spesimen darah EDTA");
        var collectedOrder = CreateOrderWithStatus("LEX0001", "LBO0001", LabOrderStatusEnum.Ordered, collectionInfo);

        _labExternalRepo.Setup(r => r.LoadEntity(It.Is<ILabExternalKey>(k => k.RegExternalId == "LEX0001")))
            .Returns(MayBe.From(external));
        _tataRekeningRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == "LEX0001")))
            .Returns(MayBe.From(emptyTataRekening));
        _labOrderRepo.Setup(r => r.ListByRegId("LEX0001"))
            .Returns([collectedOrder]);

        var cmd = new LabExternalCancelCmd("LEX0001", "Pasien membatalkan", "USER1");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<InvalidOperationException>();
        ex.WithMessage("*EX-02*");
        ex.WithMessage("*Spesimen laboratorium telah diambil*");

        _labExternalRepo.Verify(r => r.SaveChanges(It.IsAny<LabExternalModel>()), Times.Never);
    }

    [Theory]
    [InlineData(LabOrderStatusEnum.Collected)]
    [InlineData(LabOrderStatusEnum.Recorded)]
    [InlineData(LabOrderStatusEnum.Verified)]
    [InlineData(LabOrderStatusEnum.Released)]
    [InlineData(LabOrderStatusEnum.Terminated)]
    public async Task Handle_WhenOrderStatusIsCollectedOrHigher_ThrowsInvalidOperationExceptionWithEx02(LabOrderStatusEnum postCollectionStatus)
    {
        // Arrange
        var external = CreateActiveExternal("LEX0001");
        var emptyTataRekening = CreateEmptyOpenedTataRekening("LEX0001");
        var order = CreateOrderWithStatus("LEX0001", "LBO0001", postCollectionStatus);

        _labExternalRepo.Setup(r => r.LoadEntity(It.Is<ILabExternalKey>(k => k.RegExternalId == "LEX0001")))
            .Returns(MayBe.From(external));
        _tataRekeningRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == "LEX0001")))
            .Returns(MayBe.From(emptyTataRekening));
        _labOrderRepo.Setup(r => r.ListByRegId("LEX0001"))
            .Returns([order]);

        var cmd = new LabExternalCancelCmd("LEX0001", "Pasien membatalkan", "USER1");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<InvalidOperationException>();
        ex.WithMessage("*EX-02*");

        _labExternalRepo.Verify(r => r.SaveChanges(It.IsAny<LabExternalModel>()), Times.Never);
    }

    #endregion

    #region Successful Cancellation Tests

    [Fact]
    public async Task Handle_WhenConditionsClear_CancelsSuccessfullyAndSavesExternal()
    {
        // Arrange
        var external = CreateActiveExternal("LEX0001");
        var emptyTataRekening = CreateEmptyOpenedTataRekening("LEX0001");
        LabExternalModel? savedExternal = null;

        _labExternalRepo.Setup(r => r.LoadEntity(It.Is<ILabExternalKey>(k => k.RegExternalId == "LEX0001")))
            .Returns(MayBe.From(external));
        _tataRekeningRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == "LEX0001")))
            .Returns(MayBe.From(emptyTataRekening));
        _labOrderRepo.Setup(r => r.ListByRegId("LEX0001"))
            .Returns([]);
        _labExternalRepo.Setup(r => r.SaveChanges(It.IsAny<LabExternalModel>()))
            .Callback<LabExternalModel>(m => savedExternal = m);

        var cmd = new LabExternalCancelCmd("LEX0001", "Permintaan pasien", "USER_BATAL");

        // Act
        var response = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        response.Success.Should().BeTrue();
        response.Message.Should().Be("Registrasi eksternal berhasil dibatalkan.");

        _labExternalRepo.Verify(r => r.SaveChanges(It.IsAny<LabExternalModel>()), Times.Once);

        savedExternal.Should().NotBeNull();
        savedExternal!.Status.Should().Be(LabExternalStatusEnum.Cancelled);
        savedExternal.CancelInfo.CancelReason.Should().Be("Permintaan pasien");
        savedExternal.CancelInfo.CancelledUserId.Should().Be("USER_BATAL");
        savedExternal.CancelInfo.CancelledDate.Should().Be(FixedNow);
        savedExternal.AuditTrail.Modified.UserId.Should().Be("USER_BATAL");
        savedExternal.AuditTrail.Modified.Timestamp.Should().Be(FixedNow);
    }

    [Fact]
    public async Task Handle_WhenAssociatedUncollectedOrdersExist_CancelsUncollectedOrdersAndSavesThem()
    {
        // Arrange
        var external = CreateActiveExternal("LEX0001");
        var emptyTataRekening = CreateEmptyOpenedTataRekening("LEX0001");

        var order1 = CreateOrderWithStatus("LEX0001", "LBO0001", LabOrderStatusEnum.Ordered);
        var order2 = CreateOrderWithStatus("LEX0001", "LBO0002", LabOrderStatusEnum.Charged);
        var order3 = CreateOrderWithStatus("LEX0001", "LBO0003", LabOrderStatusEnum.Deferred);

        var savedOrders = new List<LabOrderModel>();

        _labExternalRepo.Setup(r => r.LoadEntity(It.Is<ILabExternalKey>(k => k.RegExternalId == "LEX0001")))
            .Returns(MayBe.From(external));
        _tataRekeningRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == "LEX0001")))
            .Returns(MayBe.From(emptyTataRekening));
        _labOrderRepo.Setup(r => r.ListByRegId("LEX0001"))
            .Returns([order1, order2, order3]);
        _labOrderRepo.Setup(r => r.SaveChanges(It.IsAny<LabOrderModel>()))
            .Callback<LabOrderModel>(m => savedOrders.Add(m));

        var cmd = new LabExternalCancelCmd("LEX0001", "Dokter membatalkan rujukan", "USER_BATAL");

        // Act
        var response = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        response.Success.Should().BeTrue();
        _labExternalRepo.Verify(r => r.SaveChanges(It.IsAny<LabExternalModel>()), Times.Once);

        savedOrders.Should().HaveCount(3);
        savedOrders.Should().AllSatisfy(o =>
        {
            o.LabOrderStatus.Should().Be(LabOrderStatusEnum.Cancelled);
            o.CancelledReason.Should().Be("Dokter membatalkan rujukan");
            o.CancelledUserId.Should().Be("USER_BATAL");
            o.CancelledDate.Should().Be(FixedNow);
        });
    }

    [Fact]
    public async Task Handle_WhenOrderIsAlreadyCancelledWithoutCollection_DoesNotBlockExternalCancellation()
    {
        // Arrange
        var external = CreateActiveExternal("LEX0001");
        var emptyTataRekening = CreateEmptyOpenedTataRekening("LEX0001");
        var alreadyCancelledOrder = CreateOrderWithStatus("LEX0001", "LBO0001", LabOrderStatusEnum.Cancelled);

        _labExternalRepo.Setup(r => r.LoadEntity(It.Is<ILabExternalKey>(k => k.RegExternalId == "LEX0001")))
            .Returns(MayBe.From(external));
        _tataRekeningRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == "LEX0001")))
            .Returns(MayBe.From(emptyTataRekening));
        _labOrderRepo.Setup(r => r.ListByRegId("LEX0001"))
            .Returns([alreadyCancelledOrder]);

        var cmd = new LabExternalCancelCmd("LEX0001", "Pembatalan eksternal", "USER_BATAL");

        // Act
        var response = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        response.Success.Should().BeTrue();
        _labExternalRepo.Verify(r => r.SaveChanges(It.IsAny<LabExternalModel>()), Times.Once);
        // Already cancelled order is not re-saved
        _labOrderRepo.Verify(r => r.SaveChanges(It.IsAny<LabOrderModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenTataRekeningNotFound_CancelsSuccessfullyWhenNoSpecimenCollected()
    {
        // Arrange
        var external = CreateActiveExternal("LEX0001");

        _labExternalRepo.Setup(r => r.LoadEntity(It.Is<ILabExternalKey>(k => k.RegExternalId == "LEX0001")))
            .Returns(MayBe.From(external));
        _tataRekeningRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == "LEX0001")))
            .Returns(MayBe<TataRekeningModel>.None);
        _labOrderRepo.Setup(r => r.ListByRegId("LEX0001"))
            .Returns([]);

        var cmd = new LabExternalCancelCmd("LEX0001", "Pembatalan tanpa billing", "USER_BATAL");

        // Act
        var response = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        response.Success.Should().BeTrue();
        _labExternalRepo.Verify(r => r.SaveChanges(It.IsAny<LabExternalModel>()), Times.Once);
    }

    #endregion
}
