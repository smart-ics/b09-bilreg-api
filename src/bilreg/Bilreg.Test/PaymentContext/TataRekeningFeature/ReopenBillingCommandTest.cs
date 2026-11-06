using Bilreg.Application.PaymentContext.TataRekeningFeature;
using Bilreg.Application.PaymentContext.TataRekeningFeature.UseCases;
using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Application.Shared;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
using Bilreg.Domain.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.PaymentContext.TataRekeningFeature;

public class ReopenBillingCommandTest
{
    private const string RegId = "REG-REOPEN-001";
    private const string UserId = "USER-TEST-01";
    private const string Reason = "Koreksi kesalahan input tagihan";

    private readonly Mock<ITataRekeningRepo> _tataRekeningRepo = new();
    private readonly Mock<ITrsBillingRepo> _trsBillingRepo = new();
    private readonly Mock<IAuditRepo> _auditRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IUnitOfWorkScope> _unitOfWorkScope = new();
    private readonly FixedCurrentUserContext _currentUser = new(UserId);

    public ReopenBillingCommandTest()
    {
        _unitOfWork.Setup(u => u.Begin()).Returns(_unitOfWorkScope.Object);
    }

    [Fact]
    public async Task GivenClosedTataRekeningWithBills_WhenHandleReopen_ThenSavesTataRekeningAndEachBillAndAuditLog()
    {
        // Arrange
        var bill1 = CreateBill("BILL-01", BillModulGroup.Jasa, 60_000m);
        var bill2 = CreateBill("BILL-02", BillModulGroup.Obat, 40_000m);
        var tataRekening = new TataRekeningModel(
            RegId,
            TataRekeningStatusEnum.Opened,
            TataRekeningFinalizationType.Default,
            [],
            [bill1, bill2]);
        tataRekening.Close();
        tataRekening.Status.Should().Be(TataRekeningStatusEnum.Closed);

        _tataRekeningRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(MayBe.From(tataRekening));

        var handler = new ReopenBillingHandler(
            _tataRekeningRepo.Object,
            _trsBillingRepo.Object,
            _auditRepo.Object,
            _unitOfWork.Object,
            _currentUser,
            TestTglJamProvider.Instance);

        var command = new ReopenBillingCommand(RegId, Reason, UserId);

        // Act
        var response = await handler.Handle(command, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.Summary.RegId.Should().Be(RegId);
        response.Summary.Status.Should().Be(TataRekeningStatusEnum.Opened);
        response.Summary.IsFinancialResponsibilityAllocated.Should().BeFalse();
        response.Summary.FinancialVerificationStatus.Should().Be(FinancialVerificationStatusEnum.NotVerified);

        _tataRekeningRepo.Verify(r => r.SaveChanges(tataRekening), Times.Once);
        _trsBillingRepo.Verify(r => r.SaveChanges(bill1), Times.Once);
        _trsBillingRepo.Verify(r => r.SaveChanges(bill2), Times.Once);
        _trsBillingRepo.Verify(r => r.SaveChanges(It.IsAny<TrsBillType>()), Times.Exactly(2));

        _auditRepo.Verify(r => r.SaveChanges(It.Is<AuditLog>(a =>
            a.UserId == UserId &&
            a.ActionType == "TATA_REKENING_REOPEN_BILLING" &&
            a.EntityName == nameof(TataRekeningModel) &&
            a.EntityId == RegId &&
            a.Reason == Reason)), Times.Once);

        _unitOfWorkScope.Verify(s => s.Complete(), Times.Once);
    }

    [Fact]
    public async Task GivenNonExistentTataRekening_WhenHandleReopen_ThenThrowsKeyNotFoundException()
    {
        // Arrange
        _tataRekeningRepo.Setup(r => r.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe<TataRekeningModel>.None);

        var handler = new ReopenBillingHandler(
            _tataRekeningRepo.Object,
            _trsBillingRepo.Object,
            _auditRepo.Object,
            _unitOfWork.Object,
            _currentUser,
            TestTglJamProvider.Instance);

        var command = new ReopenBillingCommand("NON-EXISTENT", Reason, UserId);

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*tidak ditemukan*");

        _tataRekeningRepo.Verify(r => r.SaveChanges(It.IsAny<TataRekeningModel>()), Times.Never);
        _trsBillingRepo.Verify(r => r.SaveChanges(It.IsAny<TrsBillType>()), Times.Never);
    }

    [Theory]
    [InlineData("", "Valid reason", "user-01")]
    [InlineData("   ", "Valid reason", "user-01")]
    [InlineData("REG-01", "", "user-01")]
    [InlineData("REG-01", "   ", "user-01")]
    [InlineData("REG-01", "Valid reason", "")]
    [InlineData("REG-01", "Valid reason", "   ")]
    public async Task GivenInvalidInputs_WhenHandleReopen_ThenThrowsArgumentException(
        string regId, string reason, string userId)
    {
        // Arrange
        var handler = new ReopenBillingHandler(
            _tataRekeningRepo.Object,
            _trsBillingRepo.Object,
            _auditRepo.Object,
            _unitOfWork.Object,
            _currentUser,
            TestTglJamProvider.Instance);

        var command = new ReopenBillingCommand(regId, reason, userId);

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    private static TrsBillType CreateBill(string billId, BillModulGroup modulGroup, decimal amount) =>
        TataRekeningTestDataBuilder.CreateBill(billId, RegId, amount) with { ModulGroup = modulGroup };
}
