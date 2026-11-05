using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.PaymentContext.TataRekeningFeature;
using Bilreg.Application.PaymentContext.TataRekeningFeature.UseCases;
using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Application.Shared;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using Bilreg.Test.Shared;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.PaymentContext.TataRekeningFeature;

/// <summary>
/// P2-S04 — Pembatalan finalisasi Tata Rekening hanya boleh dilakukan oleh supervisor
/// dengan alasan pembatalan, serta diblokir bila alokasi pembayaran atau transaksi kasir
/// hilir sudah diproses.
/// </summary>
public class CancelFinalizationSupervisorGovernedTest
{
    private const string RegId = "REG-CF-001";
    private const string SupervisorUserId = "SPV-01";
    private const string Reason = "Koreksi alokasi pembayaran";

    private readonly Mock<ITataRekeningRepo> _tataRekeningRepo = new();
    private readonly Mock<ITrsBillingRepo> _trsBillingRepo = new();
    private readonly Mock<IAuditRepo> _auditRepo = new();
    private readonly Mock<ITataRekeningCancellationLogRepo> _cancellationLogRepo = new();
    private readonly Mock<IDownstreamSettlementInspector> _settlementInspector = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IUnitOfWorkScope> _unitOfWorkScope = new();

    public CancelFinalizationSupervisorGovernedTest()
    {
        _unitOfWork.Setup(u => u.Begin()).Returns(_unitOfWorkScope.Object);
    }

    [Fact]
    public async Task S04_01_GivenSupervisorRole_WhenCancelFinalization_ThenStatusClosedAndCancellationLogged()
    {
        var tataRekening = CreateFinalizedTataRekening();
        SetupTataRekeningLoad(tataRekening);
        var currentUser = new FixedCurrentUserContext("verif-01", TataRekeningSupervisorRole.VerifikatorSupervisor);

        var handler = CreateHandler(currentUser);

        var result = await handler.Handle(
            new CancelFinalizationCommand(RegId, "verif-01", SupervisorUserId, Reason),
            CancellationToken.None);

        result.Summary.Status.Should().Be(TataRekeningStatusEnum.Closed);
        result.Summary.IsFinancialResponsibilityAllocated.Should().BeFalse();

        _cancellationLogRepo.Verify(
            r => r.SaveChanges(It.Is<TataRekeningCancellationLog>(x =>
                x.RegId == RegId
                && x.SupervisorUserId == SupervisorUserId
                && x.UserId == "verif-01"
                && x.Reason == Reason
                && x.StatusBefore == nameof(TataRekeningStatusEnum.Finalized)
                && x.StatusAfter == nameof(TataRekeningStatusEnum.Closed))),
            Times.Once);

        _auditRepo.Verify(
            r => r.SaveChanges(It.Is<AuditLog>(x =>
                x.ActionType == "TATA_REKENING_CANCEL_FINALIZATION" && x.EntityId == RegId)),
            Times.Once);

        _unitOfWorkScope.Verify(s => s.Complete(), Times.Once);
    }

    [Fact]
    public async Task S04_02_GivenApprovedAllocation_WhenCancelFinalization_ThenThrowsDownstreamBarrier()
    {
        var tataRekening = CreateFinalizedTataRekening();
        SetupTataRekeningLoad(tataRekening);
        _settlementInspector
            .Setup(s => s.HasApprovedAllocation(It.IsAny<string>()))
            .Returns(true);

        var handler = CreateHandler(new FixedCurrentUserContext("verif-01", TataRekeningSupervisorRole.TataRekeningSupervisor));

        var act = () => handler.Handle(
            new CancelFinalizationCommand(RegId, "verif-01", SupervisorUserId, Reason),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Alokasi Pembayaran atau Transaksi Kasir telah diproses*");

        _cancellationLogRepo.Verify(r => r.SaveChanges(It.IsAny<TataRekeningCancellationLog>()), Times.Never);
        _tataRekeningRepo.Verify(r => r.SaveChanges(It.IsAny<TataRekeningModel>()), Times.Never);
        tataRekening.Status.Should().Be(TataRekeningStatusEnum.Finalized);
    }

    [Fact]
    public async Task S04_03_GivenCashierSettlement_WhenCancelFinalization_ThenThrowsDownstreamBarrier()
    {
        var tataRekening = CreateFinalizedTataRekening();
        SetupTataRekeningLoad(tataRekening);
        _settlementInspector
            .Setup(s => s.HasCashierSettlement(It.IsAny<string>()))
            .Returns(true);

        var handler = CreateHandler(new FixedCurrentUserContext("verif-01", TataRekeningSupervisorRole.AdminKeuangan));

        var act = () => handler.Handle(
            new CancelFinalizationCommand(RegId, "verif-01", SupervisorUserId, Reason),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Alokasi Pembayaran atau Transaksi Kasir telah diproses*");

        _cancellationLogRepo.Verify(r => r.SaveChanges(It.IsAny<TataRekeningCancellationLog>()), Times.Never);
        _tataRekeningRepo.Verify(r => r.SaveChanges(It.IsAny<TataRekeningModel>()), Times.Never);
    }

    [Fact]
    public async Task S04_04_GivenMissingSupervisorRole_WhenCancelFinalization_ThenThrowsUnauthorized()
    {
        var tataRekening = CreateFinalizedTataRekening();
        SetupTataRekeningLoad(tataRekening);

        var handler = CreateHandler(new FixedCurrentUserContext("verif-01"));

        var act = () => handler.Handle(
            new CancelFinalizationCommand(RegId, "verif-01", SupervisorUserId, Reason),
            CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*Supervisor Tata Rekening*");

        _cancellationLogRepo.Verify(r => r.SaveChanges(It.IsAny<TataRekeningCancellationLog>()), Times.Never);
        _tataRekeningRepo.Verify(r => r.SaveChanges(It.IsAny<TataRekeningModel>()), Times.Never);
    }

    [Fact]
    public async Task S04_05_GivenShortReason_WhenCancelFinalization_ThenThrowsArgumentException()
    {
        var tataRekening = CreateFinalizedTataRekening();
        SetupTataRekeningLoad(tataRekening);

        var handler = CreateHandler(new FixedCurrentUserContext("verif-01", TataRekeningSupervisorRole.VerifikatorSupervisor));

        var act = () => handler.Handle(
            new CancelFinalizationCommand(RegId, "verif-01", SupervisorUserId, "koreksi"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*minimal 10 karakter*");

        _cancellationLogRepo.Verify(r => r.SaveChanges(It.IsAny<TataRekeningCancellationLog>()), Times.Never);
    }

    [Fact]
    public async Task S04_06_GivenMissingSupervisorUserId_WhenCancelFinalization_ThenThrowsArgumentException()
    {
        var tataRekening = CreateFinalizedTataRekening();
        SetupTataRekeningLoad(tataRekening);

        var handler = CreateHandler(new FixedCurrentUserContext("verif-01", TataRekeningSupervisorRole.VerifikatorSupervisor));

        var act = () => handler.Handle(
            new CancelFinalizationCommand(RegId, "verif-01", " ", Reason),
            CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();

        _cancellationLogRepo.Verify(r => r.SaveChanges(It.IsAny<TataRekeningCancellationLog>()), Times.Never);
    }

    [Fact]
    public void S04_07_GivenDownstreamProcessed_WhenAggregateCancelFinalization_ThenThrowsAndKeepsFinalized()
    {
        var tataRekening = CreateFinalizedTataRekening();

        var act = () => tataRekening.CancelFinalization(new FakeDownstreamSettlementInspector(hasApprovedAllocation: true));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Batalkan transaksi kasir/alokasi pembayaran terlebih dahulu sebelum membuka kunci tagihan*");

        tataRekening.Status.Should().Be(TataRekeningStatusEnum.Finalized);
    }

    private static TataRekeningModel CreateFinalizedTataRekening()
    {
        var bill = TataRekeningTestDataBuilder.CreateBill("BILL-CF", RegId, 60_000m);
        var tataRekening = TataRekeningTestDataBuilder.Hydrate(RegId, TataRekeningStatusEnum.Opened, bill);
        TataRekeningDomainTestHelper.CloseVerifyAllocateFinalize(
            tataRekening,
            [TataRekeningTestDataBuilder.BuildPayment(PaymentType.ByKas, 60_000m, 0m)]);
        return tataRekening;
    }

    private void SetupTataRekeningLoad(TataRekeningModel tataRekening) =>
        _tataRekeningRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(MayBe.From(tataRekening));

    private CancelFinalizationHandler CreateHandler(ICurrentUserContext currentUser) =>
        new(
            _tataRekeningRepo.Object,
            _trsBillingRepo.Object,
            _auditRepo.Object,
            _cancellationLogRepo.Object,
            _settlementInspector.Object,
            _unitOfWork.Object,
            currentUser,
            TestTglJamProvider.Instance);
}
