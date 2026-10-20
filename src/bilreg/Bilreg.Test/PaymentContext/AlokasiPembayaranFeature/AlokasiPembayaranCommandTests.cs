using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.PaymentContext.AlokasiPembayaranFeature;
using Bilreg.Application.PaymentContext.DepositFeature;
using Bilreg.Application.PaymentContext.TataRekeningFeature;
using Bilreg.Application.PaymentContext.TataRekeningFeature.Dtos;
using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Application.Shared;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Test.PaymentContext.TataRekeningFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.PaymentContext.AlokasiPembayaranFeature;

public class AlokasiPembayaranCommandTests
{
    private const string RegId = "REG-ALOKASI-001";
    private const string PetugasVerif = TataRekeningDomainTestHelper.DefaultPetugasVerif;
    private static readonly DateTime TestDate = TataRekeningTestDataBuilder.TestDate;

    private readonly Mock<ITataRekeningRepo> _tataRekeningRepo = new();
    private readonly Mock<ITrsBillingRepo> _trsBillingRepo = new();
    private readonly Mock<IDepositRepo> _depositRepo = new();
    private readonly Mock<IRegJaminanPolicy> _regJaminanPolicy = new();
    private readonly Mock<IRegRepo> _regRepo = new();
    private readonly Mock<IAuditRepo> _auditRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IUnitOfWorkScope> _unitOfWorkScope = new();
    private readonly FixedCurrentUserContext _currentUser = new(PetugasVerif);

    public AlokasiPembayaranCommandTests()
    {
        _unitOfWork.Setup(u => u.Begin()).Returns(_unitOfWorkScope.Object);
    }

    [Fact]
    public async Task GivenOpenedTataRekening_WhenAllocate_ThenThrowsInvalidOperationExceptionBecauseNotClosed()
    {
        var bill = TataRekeningTestDataBuilder.CreateBill("BILL-01", RegId, 100_000m);
        var tataRekening = TataRekeningTestDataBuilder.Hydrate(RegId, TataRekeningStatusEnum.Opened, bill);
        _tataRekeningRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(MayBe.From(tataRekening));

        var payments = new List<PaymentAllocationInputDto>
        {
            new("BYKAS", "KAS", false, 100_000m, 0m, "COA-01", "COA")
        };

        var handler = new AllocateFinancialResponsibilityHandler(
            _tataRekeningRepo.Object, _trsBillingRepo.Object, _depositRepo.Object, _regJaminanPolicy.Object, _unitOfWork.Object);

        var act = () => handler.Handle(
            new AllocateFinancialResponsibilityCommand(RegId, payments),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*CLOSED*");
    }

    [Fact]
    public async Task GivenClosedAndVerifiedTataRekening_WhenAllocate_ThenSucceedsAndGeneratesProjection()
    {
        var bill = TataRekeningTestDataBuilder.CreateBill("BILL-01", RegId, 100_000m);
        var tataRekening = TataRekeningTestDataBuilder.HydrateClosed(RegId, bill);
        tataRekening.CompleteFinancialVerification(PetugasVerif, TestDate);
        _tataRekeningRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(MayBe.From(tataRekening));

        var payments = new List<PaymentAllocationInputDto>
        {
            new("BYKAS", "KAS", false, 100_000m, 0m, "COA-01", "COA")
        };

        var handler = new AllocateFinancialResponsibilityHandler(
            _tataRekeningRepo.Object, _trsBillingRepo.Object, _depositRepo.Object, _regJaminanPolicy.Object, _unitOfWork.Object);

        var result = await handler.Handle(
            new AllocateFinancialResponsibilityCommand(RegId, payments),
            CancellationToken.None);

        result.Summary.IsFinancialResponsibilityAllocated.Should().BeTrue();
        result.Projection.Should().HaveCount(1);
        _tataRekeningRepo.Verify(r => r.SaveChanges(tataRekening), Times.Once);
        _trsBillingRepo.Verify(r => r.SaveChanges(bill), Times.Once);
    }

    [Fact]
    public async Task GivenOpenedTataRekening_WhenFinalize_ThenThrowsInvalidOperationExceptionWithStillOpenMessage()
    {
        var bill = TataRekeningTestDataBuilder.CreateBill("BILL-01", RegId, 100_000m);
        var tataRekening = TataRekeningTestDataBuilder.Hydrate(RegId, TataRekeningStatusEnum.Opened, bill);
        _tataRekeningRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(MayBe.From(tataRekening));

        var handler = new FinalizeFinancialResponsibilityHandler(
            _tataRekeningRepo.Object, _trsBillingRepo.Object, _regRepo.Object, _unitOfWork.Object, _currentUser, TestTglJamProvider.Instance);

        var act = () => handler.Handle(
            new FinalizeFinancialResponsibilityCommand(RegId, "USER-01", TestDate),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*OPEN*");
    }

    [Fact]
    public async Task GivenFinalizedTataRekening_WhenFinalize_ThenThrowsInvalidOperationExceptionWithAlreadyFinalizedMessage()
    {
        var bill = TataRekeningTestDataBuilder.CreateBill("BILL-01", RegId, 100_000m);
        var tataRekening = TataRekeningTestDataBuilder.Hydrate(RegId, TataRekeningStatusEnum.Opened, bill);
        TataRekeningDomainTestHelper.CloseVerifyAllocateFinalize(
            tataRekening,
            [new TataRekeningPaymentType(PaymentType.ByKas, 100_000m, 0m, CoaType.Default)],
            PetugasVerif);
        _tataRekeningRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(MayBe.From(tataRekening));

        var handler = new FinalizeFinancialResponsibilityHandler(
            _tataRekeningRepo.Object, _trsBillingRepo.Object, _regRepo.Object, _unitOfWork.Object, _currentUser, TestTglJamProvider.Instance);

        var act = () => handler.Handle(
            new FinalizeFinancialResponsibilityCommand(RegId, "USER-01", TestDate),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*sudah difinalisasi*");
    }

    [Fact]
    public async Task GivenLunasTataRekening_WhenFinalize_ThenThrowsInvalidOperationExceptionWithLunasMessage()
    {
        var bill = TataRekeningTestDataBuilder.CreateBill("BILL-01", RegId, 100_000m);
        var tataRekening = TataRekeningTestDataBuilder.Hydrate(RegId, TataRekeningStatusEnum.Lunas, bill);
        _tataRekeningRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(MayBe.From(tataRekening));

        var handler = new FinalizeFinancialResponsibilityHandler(
            _tataRekeningRepo.Object, _trsBillingRepo.Object, _regRepo.Object, _unitOfWork.Object, _currentUser, TestTglJamProvider.Instance);

        var act = () => handler.Handle(
            new FinalizeFinancialResponsibilityCommand(RegId, "USER-01", TestDate),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*LUNAS*");
    }

    [Fact]
    public async Task GivenClosedAndVerifiedTataRekening_WhenFinalize_ThenSucceedsAndUpdatesRegistrationDischarge()
    {
        var bill = TataRekeningTestDataBuilder.CreateBill("BILL-01", RegId, 100_000m);
        var tataRekening = TataRekeningTestDataBuilder.HydrateClosed(RegId, bill);
        TataRekeningDomainTestHelper.VerifyAndAllocate(
            tataRekening,
            [new TataRekeningPaymentType(PaymentType.ByKas, 100_000m, 0m, CoaType.Default)],
            PetugasVerif);
        _tataRekeningRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(MayBe.From(tataRekening));

        var reg = RegModel.Default;
        _regRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(MayBe.From(reg));

        var handler = new FinalizeFinancialResponsibilityHandler(
            _tataRekeningRepo.Object, _trsBillingRepo.Object, _regRepo.Object, _unitOfWork.Object, _currentUser, TestTglJamProvider.Instance);

        var result = await handler.Handle(
            new FinalizeFinancialResponsibilityCommand(RegId, "USER-01", TestDate),
            CancellationToken.None);

        result.Summary.Status.Should().Be(TataRekeningStatusEnum.Finalized);
        _tataRekeningRepo.Verify(r => r.SaveChanges(tataRekening), Times.Once);
        _regRepo.Verify(r => r.SaveChanges(reg), Times.Once);
        reg.IsAktif.Should().BeFalse();
        reg.RegKeluarAudit.UserId.Should().Be("USER-01");
        reg.RegKeluarAudit.Timestamp.Should().Be(TestDate);
    }

    [Fact]
    public async Task GivenFinalizedTataRekening_WhenCancelFinalization_ThenSucceedsAndCancelsRegistrationDischarge()
    {
        var bill = TataRekeningTestDataBuilder.CreateBill("BILL-01", RegId, 100_000m);
        var tataRekening = TataRekeningTestDataBuilder.Hydrate(RegId, TataRekeningStatusEnum.Opened, bill);
        TataRekeningDomainTestHelper.CloseVerifyAllocateFinalize(
            tataRekening,
            [new TataRekeningPaymentType(PaymentType.ByKas, 100_000m, 0m, CoaType.Default)],
            PetugasVerif);
        _tataRekeningRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(MayBe.From(tataRekening));

        var reg = RegModel.Default;
        reg.SetKeluar(PetugasVerif, TestDate);
        _regRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(MayBe.From(reg));

        var handler = new CancelFinalizationHandler(
            _tataRekeningRepo.Object, _trsBillingRepo.Object, _regRepo.Object, _auditRepo.Object, _unitOfWork.Object, _currentUser, TestTglJamProvider.Instance);

        var result = await handler.Handle(
            new CancelFinalizationCommand(RegId, "USER-01", "Koreksi alokasi"),
            CancellationToken.None);

        result.Summary.Status.Should().Be(TataRekeningStatusEnum.Closed);
        result.Summary.IsFinancialResponsibilityAllocated.Should().BeFalse();
        _auditRepo.Verify(r => r.SaveChanges(It.IsAny<AuditLog>()), Times.Once);
        _regRepo.Verify(r => r.SaveChanges(reg), Times.Once);
        reg.IsAktif.Should().BeTrue();
        reg.RegKeluarAudit.Should().Be(AuditInfoType.Default);
        reg.RegCancelOutAudit.UserId.Should().Be("USER-01");
        reg.RegCancelOutAudit.Timestamp.Should().Be(TestTglJamProvider.Instance.Now);
    }

    [Fact]
    public async Task GivenFinalizedTataRekening_WhenInitiateSettlement_ThenSettlementInitiatedIsTrue()
    {
        var bill = TataRekeningTestDataBuilder.CreateBill("BILL-01", RegId, 100_000m);
        var tataRekening = TataRekeningTestDataBuilder.Hydrate(RegId, TataRekeningStatusEnum.Opened, bill);
        TataRekeningDomainTestHelper.CloseVerifyAllocateFinalize(
            tataRekening,
            [new TataRekeningPaymentType(PaymentType.ByKas, 100_000m, 0m, CoaType.Default)],
            PetugasVerif);
        _tataRekeningRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(MayBe.From(tataRekening));

        var handler = new SettlementInitiationHandler(
            _tataRekeningRepo.Object, _auditRepo.Object, _unitOfWork.Object, _currentUser, TestTglJamProvider.Instance);

        var result = await handler.Handle(
            new SettlementInitiationCommand(RegId, "USER-01", TestDate),
            CancellationToken.None);

        result.Summary.SettlementInitiated.Should().BeTrue();
        _auditRepo.Verify(r => r.SaveChanges(It.IsAny<AuditLog>()), Times.Once);
    }
}
