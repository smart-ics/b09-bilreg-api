using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.ChargeContext.TindakanFeature;
using Bilreg.Application.ChargeContext.TindakanFeature.UseCases;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.Shared;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Infrastructure.ChargeContext.TindakanFeature;
using Bilreg.Test.Shared;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.ChargeContext.TindakanFeature;

//  M03-F01 P2-S04 — use-case and repository verification:
//  create/send/confirm/cancel delegate state rules to the aggregate,
//  cancel rejects received, confirm is idempotent with a single audit,
//  outstanding query covers single-visit and cross-visit scopes,
//  concurrent cancellation loses against a confirmed reception.
public class TindakanLanjutUseCaseTest
{
    private readonly Mock<IRegRepo> _regRepo = new();
    private readonly Mock<ITujuanLanjutRepo> _tujuanRepo = new();
    private readonly Mock<ITindakanLanjutRepo> _orderRepo = new();
    private readonly Mock<IAuditRepo> _auditRepo = new();

    private static readonly DateTime Now = TestTglJamProvider.Instance.Now;

    #region CREATE

    [Fact]
    public async Task Create_WhenValid_ThenPersistsProposedAndWritesOneAudit()
    {
        // Arrange
        var reg = CreateReg();
        _regRepo.Setup(x => x.LoadEntity(It.IsAny<IRegKey>())).Returns(MayBe.From(reg));
        _tujuanRepo.Setup(x => x.ListData()).Returns([ActiveTujuan("Kontrol")]);
        TindakanLanjutModel? saved = null;
        _orderRepo.Setup(x => x.SaveChanges(It.IsAny<TindakanLanjutModel>()))
            .Callback<TindakanLanjutModel>(m => saved = m);
        var sut = new TdkCreateTindakanLanjutHandler(
            _regRepo.Object, _tujuanRepo.Object, _orderRepo.Object,
            _auditRepo.Object, TestTglJamProvider.Instance);

        // Act
        var response = await sut.Handle(
            new TdkCreateTindakanLanjutCmd("RG1", "Kontrol", "U1",
                [new TdkCreateTindakanLanjutItemCmd(1, "LAB1", "Lab Test", 1)]),
            CancellationToken.None);

        // Assert
        response.TindakanLanjutId.Should().NotBeNullOrWhiteSpace();
        saved.Should().NotBeNull();
        saved!.OrderState.Should().Be(TindakanLanjutStateEnum.Proposed);
        saved.ListItem.Should().HaveCount(1);
        _auditRepo.Verify(x => x.SaveChanges(It.IsAny<AuditLog>()), Times.Once);
    }

    [Fact]
    public async Task Create_WhenOrderTypeUnknown_ThenRejectedWithoutSaveOrAudit()
    {
        // Arrange
        _regRepo.Setup(x => x.LoadEntity(It.IsAny<IRegKey>())).Returns(MayBe.From(CreateReg()));
        _tujuanRepo.Setup(x => x.ListData()).Returns([ActiveTujuan("Kontrol")]);
        var sut = new TdkCreateTindakanLanjutHandler(
            _regRepo.Object, _tujuanRepo.Object, _orderRepo.Object,
            _auditRepo.Object, TestTglJamProvider.Instance);

        // Act
        var act = () => sut.Handle(
            new TdkCreateTindakanLanjutCmd("RG1", "Unknown", "U1", []),
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
        _orderRepo.Verify(x => x.SaveChanges(It.IsAny<TindakanLanjutModel>()), Times.Never);
        _auditRepo.Verify(x => x.SaveChanges(It.IsAny<AuditLog>()), Times.Never);
    }

    #endregion

    #region SEND

    [Fact]
    public async Task Send_WhenProposed_ThenTransitionsToSentAndWritesOneAudit()
    {
        // Arrange
        var order = CreateOrder(TindakanLanjutStateEnum.Proposed);
        _orderRepo.Setup(x => x.LoadEntity(It.IsAny<ITindakanLanjutKey>()))
            .Returns(MayBe.From(order));
        var sut = new TdkSendTindakanLanjutHandler(
            _orderRepo.Object, _auditRepo.Object, TestTglJamProvider.Instance);

        // Act
        await sut.Handle(new TdkSendTindakanLanjutCmd(order.TindakanLanjutId, "U1"),
            CancellationToken.None);

        // Assert
        order.OrderState.Should().Be(TindakanLanjutStateEnum.Sent);
        _orderRepo.Verify(x => x.SaveChanges(order), Times.Once);
        _auditRepo.Verify(x => x.SaveChanges(It.IsAny<AuditLog>()), Times.Once);
    }

    #endregion

    #region CONFIRM

    [Fact]
    public async Task Confirm_WhenSent_ThenFirstConfirmationWritesOneAudit()
    {
        // Arrange
        var order = CreateOrder(TindakanLanjutStateEnum.Sent);
        _orderRepo.Setup(x => x.LoadEntity(It.IsAny<ITindakanLanjutKey>()))
            .Returns(MayBe.From(order));
        var sut = new TdkConfirmTindakanLanjutHandler(
            _orderRepo.Object, _auditRepo.Object, TestTglJamProvider.Instance);

        // Act
        var response = await sut.Handle(
            new TdkConfirmTindakanLanjutCmd(order.TindakanLanjutId, "Unit X", "U1"),
            CancellationToken.None);

        // Assert
        response.IsNewConfirmation.Should().BeTrue();
        order.OrderState.Should().Be(TindakanLanjutStateEnum.Received);
        _auditRepo.Verify(x => x.SaveChanges(It.IsAny<AuditLog>()), Times.Once);
    }

    [Fact]
    public async Task Confirm_WhenAlreadyReceived_ThenNoSecondTransitionOrAudit()
    {
        // Arrange
        var order = CreateOrder(TindakanLanjutStateEnum.Received);
        _orderRepo.Setup(x => x.LoadEntity(It.IsAny<ITindakanLanjutKey>()))
            .Returns(MayBe.From(order));
        var sut = new TdkConfirmTindakanLanjutHandler(
            _orderRepo.Object, _auditRepo.Object, TestTglJamProvider.Instance);

        // Act
        var response = await sut.Handle(
            new TdkConfirmTindakanLanjutCmd(order.TindakanLanjutId, "Unit X", "U1"),
            CancellationToken.None);

        // Assert
        response.IsNewConfirmation.Should().BeFalse();
        order.OrderState.Should().Be(TindakanLanjutStateEnum.Received);
        _orderRepo.Verify(x => x.SaveChanges(It.IsAny<TindakanLanjutModel>()), Times.Never);
        _auditRepo.Verify(x => x.SaveChanges(It.IsAny<AuditLog>()), Times.Never);
    }

    #endregion

    #region CANCEL

    [Fact]
    public async Task Cancel_WhenReceived_ThenRejected()
    {
        // Arrange
        var order = CreateOrder(TindakanLanjutStateEnum.Received);
        _orderRepo.Setup(x => x.LoadEntity(It.IsAny<ITindakanLanjutKey>()))
            .Returns(MayBe.From(order));
        var sut = new TdkBatalTindakanLanjutHandler(
            _orderRepo.Object, _auditRepo.Object, TestTglJamProvider.Instance);

        // Act
        var act = () => sut.Handle(
            new TdkBatalTindakanLanjutCmd(order.TindakanLanjutId, "U1", "batal"),
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        _orderRepo.Verify(x => x.SaveChanges(It.IsAny<TindakanLanjutModel>()), Times.Never);
        _auditRepo.Verify(x => x.SaveChanges(It.IsAny<AuditLog>()), Times.Never);
    }

    [Fact]
    public async Task Cancel_WhenSent_ThenCancelledAndWritesOneAudit()
    {
        // Arrange
        var order = CreateOrder(TindakanLanjutStateEnum.Sent);
        _orderRepo.Setup(x => x.LoadEntity(It.IsAny<ITindakanLanjutKey>()))
            .Returns(MayBe.From(order));
        var sut = new TdkBatalTindakanLanjutHandler(
            _orderRepo.Object, _auditRepo.Object, TestTglJamProvider.Instance);

        // Act
        await sut.Handle(
            new TdkBatalTindakanLanjutCmd(order.TindakanLanjutId, "U1", "batal"),
            CancellationToken.None);

        // Assert
        order.OrderState.Should().Be(TindakanLanjutStateEnum.Cancelled);
        _orderRepo.Verify(x => x.SaveChanges(order), Times.Once);
        _auditRepo.Verify(x => x.SaveChanges(It.IsAny<AuditLog>()), Times.Once);
    }

    #endregion

    #region OUTSTANDING QUERY

    [Fact]
    public async Task Outstanding_WhenRegIdGiven_ThenScopesToSingleVisit()
    {
        // Arrange
        var order = CreateOrder(TindakanLanjutStateEnum.Sent);
        _orderRepo.Setup(x => x.ListOutstanding(It.IsAny<IRegKey>()))
            .Returns([order]);
        var sut = new TdkListTindakanLanjutOutstandingHandler(_orderRepo.Object);

        // Act
        var result = await sut.Handle(
            new TdkListTindakanLanjutOutstandingCmd("RG1"), CancellationToken.None);

        // Assert
        var list = result.ToList();
        list.Should().HaveCount(1);
        list[0].TindakanLanjutId.Should().Be(order.TindakanLanjutId);
        list[0].ListItem.Should().HaveCount(1);
        _orderRepo.Verify(x => x.ListOutstandingAll(), Times.Never);
    }

    [Fact]
    public async Task Outstanding_WhenNoRegId_ThenListsAcrossVisits()
    {
        // Arrange
        _orderRepo.Setup(x => x.ListOutstandingAll())
            .Returns([
                CreateOrder(TindakanLanjutStateEnum.Proposed),
                CreateOrder(TindakanLanjutStateEnum.Sent)]);
        var sut = new TdkListTindakanLanjutOutstandingHandler(_orderRepo.Object);

        // Act
        var result = await sut.Handle(
            new TdkListTindakanLanjutOutstandingCmd(), CancellationToken.None);

        // Assert
        result.ToList().Should().HaveCount(2);
        _orderRepo.Verify(x => x.ListOutstanding(It.IsAny<IRegKey>()), Times.Never);
    }

    #endregion

    #region CONCURRENCY

    [Fact]
    public void SaveChanges_WhenRowVersionStale_ThenConcurrentCancelLoses()
    {
        // Arrange
        var dalMock = new Mock<ITindakanLanjutDal>();
        var itemDalMock = new Mock<ITindakanLanjutItemDal>();
        var existing = CreateOrder(TindakanLanjutStateEnum.Sent);
        existing.Cancel("late cancel", "U1", Now);
        var staleDto = TindakanLanjutDto.FromModel(existing);
        dalMock.Setup(x => x.GetData(It.IsAny<ITindakanLanjutKey>())).Returns(staleDto);
        dalMock.Setup(x => x.UpdateState(It.IsAny<TindakanLanjutDto>(), It.IsAny<int>()))
            .Returns(0); // another writer (confirmed reception) moved RowVersion first
        var sut = new TindakanLanjutRepo(dalMock.Object, itemDalMock.Object);

        // Act
        var act = () => sut.SaveChanges(existing);

        // Assert
        act.Should().Throw<TindakanLanjutConcurrencyException>();
        itemDalMock.Verify(x => x.Delete(It.IsAny<ITindakanLanjutKey>()), Times.Never);
    }

    [Fact]
    public void SaveChanges_WhenNewOrder_ThenInsertsHeaderAndItems()
    {
        // Arrange
        var dalMock = new Mock<ITindakanLanjutDal>();
        var itemDalMock = new Mock<ITindakanLanjutItemDal>();
        dalMock.Setup(x => x.GetData(It.IsAny<ITindakanLanjutKey>()))
            .Returns((TindakanLanjutDto)null!);
        var sut = new TindakanLanjutRepo(dalMock.Object, itemDalMock.Object);
        var order = CreateOrder(TindakanLanjutStateEnum.Proposed);

        // Act
        sut.SaveChanges(order);

        // Assert
        dalMock.Verify(x => x.Insert(It.IsAny<TindakanLanjutDto>()), Times.Once);
        itemDalMock.Verify(x => x.Insert(It.IsAny<IEnumerable<TindakanLanjutItemDto>>()), Times.Once);
    }

    #endregion

    #region HELPERS

    private static TujuanLanjutType ActiveTujuan(string orderType)
        => new(orderType, LayananType.Default.ToReff(), true);

    private static TindakanLanjutModel CreateOrder(TindakanLanjutStateEnum targetState)
    {
        var order = TindakanLanjutModel.Create(
            CreateReg(), ActiveTujuan("Kontrol"),
            [TindakanLanjutItemModel.Create(1, "LAB1", "Lab Test", 1)],
            "U1", Now);
        if (targetState is TindakanLanjutStateEnum.Sent
            or TindakanLanjutStateEnum.Received
            or TindakanLanjutStateEnum.Cancelled)
            order.Send("U1", Now);
        if (targetState == TindakanLanjutStateEnum.Received)
            order.ConfirmReceived("Unit X", "U1", Now);
        if (targetState == TindakanLanjutStateEnum.Cancelled)
            order.Cancel("batal", "U1", Now);
        return order;
    }

    private static RegModel CreateReg()
    {
        return new RegModel(
            "RG00000001",
            new DateOnly(2025, 11, 1),
            new AuditInfoType("tester", new DateOnly(2025, 11, 1).ToDateTime(TimeOnly.MinValue)),
            AuditInfoType.Default,
            AuditInfoType.Default,
            AuditInfoType.Default,
            JenisRegEnum.RegJalan,
            PasienModel.Default.ToReff(),
            TipeJaminanType.Default.ToReff(),
            PolisModel.Default.ToReff(),
            KelasType.Default.ToReff(),
            CaraMasukDkType.Default,
            RujukanType.Default.ToReff(),
            PpaType.Default.ToReff(),
            LayananType.Default.ToReff(),
            KarcisType.Default.ToReff(),
            RegEligibilityType.Default,
            []);
    }

    #endregion
}
