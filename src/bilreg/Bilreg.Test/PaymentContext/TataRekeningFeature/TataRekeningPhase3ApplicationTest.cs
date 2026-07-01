using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.PaymentContext.TataRekeningFeature;
using Bilreg.Application.PaymentContext.TataRekeningFeature.Dtos;
using Bilreg.Application.PaymentContext.TataRekeningFeature.UseCases;
using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Application.Shared;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.PaymentContext.TataRekeningFeature;

public class TataRekeningPhase3ApplicationTest
{
    private const string RegId = "REG-TGT-001";
    private const string SourceRegId = "REG-SRC-001";
    private const string PetugasVerif = TataRekeningDomainTestHelper.DefaultPetugasVerif;
    private static readonly DateTime TestDate = TataRekeningTestDataBuilder.TestDate;

    private readonly Mock<ITataRekeningRepo> _tataRekeningRepo = new();
    private readonly Mock<IMergeRequestRepo> _mergeRequestRepo = new();
    private readonly Mock<ITrsBillingRepo> _trsBillingRepo = new();
    private readonly Mock<IRegRepo> _regRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IUnitOfWorkScope> _unitOfWorkScope = new();
    private readonly Mock<ITransferReceivableService> _transferReceivableService = new();
    private readonly Mock<IAuditRepo> _auditRepo = new();
    private readonly FixedCurrentUserContext _currentUser = new();

    public TataRekeningPhase3ApplicationTest()
    {
        _unitOfWork.Setup(u => u.Begin()).Returns(_unitOfWorkScope.Object);
    }

    #region Open Tata Rekening Query

    [Fact]
    public async Task P3_01_GivenValidReg_WhenOpenTataRekening_ThenReturnsSummaryBillsProjectionAndMerges()
    {
        var bill = TataRekeningTestDataBuilder.CreateBill("BILL-01", RegId, 50_000m);
        var tataRekening = TataRekeningTestDataBuilder.Hydrate(
            RegId, TataRekeningStatusEnum.Opened, bill);
        var mergeRequest = MergeRequestModel.Create("MR-01", SourceRegId, RegId);

        SetupTataRekeningLoad(RegId, tataRekening);
        SetupRegLoad(RegId, RegModel.Default);
        _mergeRequestRepo.Setup(r => r.ListPendingByReg(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns([mergeRequest]);
        _mergeRequestRepo.Setup(r => r.ListPendingByPatient(It.IsAny<string>()))
            .Returns([]);

        var handler = new OpenTataRekeningHandler(
            _tataRekeningRepo.Object, _regRepo.Object, _mergeRequestRepo.Object);

        var result = await handler.Handle(new OpenTataRekeningQuery(RegId), CancellationToken.None);

        result.Summary.RegId.Should().Be(RegId);
        result.Bills.Should().HaveCount(1);
        result.Bills[0].TrsBillingId.Should().Be("BILL-01");
        result.PendingMergeRequests.Should().HaveCount(1);
        result.PendingMergeRequests[0].MergeRequestId.Should().Be("MR-01");
    }

    [Fact]
    public async Task P3_02_GivenMissingTataRekening_WhenOpenTataRekening_ThenThrowsKeyNotFound()
    {
        _tataRekeningRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(MayBe<TataRekeningModel>.None);

        var handler = new OpenTataRekeningHandler(
            _tataRekeningRepo.Object, _regRepo.Object, _mergeRequestRepo.Object);

        var act = () => handler.Handle(new OpenTataRekeningQuery(RegId), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    #endregion

    #region Close Bill

    [Fact]
    public async Task P3_03_GivenOpenedTataRekening_WhenCloseBill_ThenStatusClosed()
    {
        var tataRekening = TataRekeningTestDataBuilder.Hydrate(
            RegId, TataRekeningStatusEnum.Opened,
            TataRekeningTestDataBuilder.CreateBill("BILL-C", RegId, 10_000m));
        SetupTataRekeningLoad(RegId, tataRekening);

        var handler = new CloseBillHandler(_tataRekeningRepo.Object, _unitOfWork.Object);
        var result = await handler.Handle(new CloseBillCommand(RegId), CancellationToken.None);

        result.Summary.Status.Should().Be(TataRekeningStatusEnum.Closed);
        _tataRekeningRepo.Verify(r => r.SaveChanges(It.IsAny<TataRekeningModel>()), Times.Once);
        _unitOfWorkScope.Verify(s => s.Complete(), Times.Once);
    }

    #endregion

    #region Merge Billing

    [Fact]
    public async Task P3_04_GivenValidMerge_WhenMergeBilling_ThenMovesBillsAndSavesAllRepos()
    {
        var sourceBill = TataRekeningTestDataBuilder.CreateBill("BILL-SRC", SourceRegId, 30_000m);
        var targetBill = TataRekeningTestDataBuilder.CreateBill("BILL-TGT", RegId, 20_000m);
        var source = TataRekeningTestDataBuilder.HydrateClosed(SourceRegId, sourceBill);
        var target = TataRekeningTestDataBuilder.HydrateClosed(RegId, targetBill);
        var mergeRequest = MergeRequestModel.Create("MR-MERGE", SourceRegId, RegId);

        _mergeRequestRepo.Setup(r => r.LoadEntity(It.Is<IMergeRequestKey>(k => k.MergeRequestId == "MR-MERGE")))
            .Returns(MayBe.From(mergeRequest));
        SetupTataRekeningLoad(SourceRegId, source);
        SetupTataRekeningLoad(RegId, target);

        var handler = new MergeBillingHandler(
            _tataRekeningRepo.Object,
            _mergeRequestRepo.Object,
            _trsBillingRepo.Object,
            TataRekeningDomainTestHelper.MergeBillingService,
            _transferReceivableService.Object,
            _auditRepo.Object,
            _unitOfWork.Object,
            _currentUser);

        var result = await handler.Handle(new MergeBillingCommand("MR-MERGE"), CancellationToken.None);

        result.MergeRequest.Status.Should().Be(MergeRequestStatusEnum.Executed);
        result.TargetSummary.RegId.Should().Be(RegId);
        source.ListTrsBill.Should().BeEmpty();
        target.ListTrsBill.Should().HaveCount(2);
        _tataRekeningRepo.Verify(r => r.SaveChanges(It.IsAny<TataRekeningModel>()), Times.Exactly(2));
        _mergeRequestRepo.Verify(r => r.SaveChanges(mergeRequest), Times.Once);
        _trsBillingRepo.Verify(r => r.SaveChanges(It.IsAny<TrsBillType>()), Times.Once);
        _transferReceivableService.Verify(s => s.Transfer(SourceRegId, RegId), Times.Once);
        _auditRepo.Verify(r => r.SaveChanges(It.IsAny<Bilreg.Domain.Shared.AuditLogFeature.AuditLog>()), Times.Once);
        _unitOfWorkScope.Verify(s => s.Complete(), Times.Once);
    }

    [Fact]
    public async Task P3_05_GivenOpenedTarget_WhenMergeBilling_ThenThrows()
    {
        var sourceBill = TataRekeningTestDataBuilder.CreateBill("BILL-S", SourceRegId, 30_000m);
        var targetBill = TataRekeningTestDataBuilder.CreateBill("BILL-T", RegId, 20_000m);
        var source = TataRekeningTestDataBuilder.HydrateClosed(SourceRegId, sourceBill);
        var target = TataRekeningTestDataBuilder.Hydrate(RegId, TataRekeningStatusEnum.Opened, targetBill);
        var mergeRequest = MergeRequestModel.Create("MR-FAIL", SourceRegId, RegId);

        _mergeRequestRepo.Setup(r => r.LoadEntity(It.IsAny<IMergeRequestKey>()))
            .Returns(MayBe.From(mergeRequest));
        SetupTataRekeningLoad(SourceRegId, source);
        SetupTataRekeningLoad(RegId, target);

        var handler = new MergeBillingHandler(
            _tataRekeningRepo.Object,
            _mergeRequestRepo.Object,
            _trsBillingRepo.Object,
            TataRekeningDomainTestHelper.MergeBillingService,
            _transferReceivableService.Object,
            _auditRepo.Object,
            _unitOfWork.Object,
            _currentUser);

        var act = () => handler.Handle(new MergeBillingCommand("MR-FAIL"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _tataRekeningRepo.Verify(r => r.SaveChanges(It.IsAny<TataRekeningModel>()), Times.Never);
        _unitOfWorkScope.Verify(s => s.Complete(), Times.Never);
    }

    #endregion

    #region Financial Verification

    [Fact]
    public async Task P3_06_GivenClosedTataRekening_WhenVerify_ThenStatusValid()
    {
        var tataRekening = TataRekeningTestDataBuilder.HydrateClosed(
            RegId, TataRekeningTestDataBuilder.CreateBill("BILL-V", RegId, 40_000m));
        SetupTataRekeningLoad(RegId, tataRekening);
        _mergeRequestRepo.Setup(r => r.ListPendingByReg(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns([]);

        var handler = new FinancialVerificationHandler(
            _tataRekeningRepo.Object,
            _mergeRequestRepo.Object,
            TataRekeningDomainTestHelper.VerificationService,
            _unitOfWork.Object);

        var result = await handler.Handle(
            new FinancialVerificationCommand(
                RegId, FinancialVerificationAction.Verify, PetugasVerif, TestDate),
            CancellationToken.None);

        result.Summary.FinancialVerificationStatus.Should().Be(FinancialVerificationStatusEnum.Valid);
        _unitOfWorkScope.Verify(s => s.Complete(), Times.Once);
    }

    [Fact]
    public async Task P3_07_GivenPendingMerge_WhenVerify_ThenThrows()
    {
        var tataRekening = TataRekeningTestDataBuilder.HydrateClosed(
            RegId, TataRekeningTestDataBuilder.CreateBill("BILL-V2", RegId, 40_000m));
        SetupTataRekeningLoad(RegId, tataRekening);
        _mergeRequestRepo.Setup(r => r.ListPendingByReg(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns([MergeRequestModel.Create("MR-PEND", SourceRegId, RegId)]);

        var handler = new FinancialVerificationHandler(
            _tataRekeningRepo.Object,
            _mergeRequestRepo.Object,
            TataRekeningDomainTestHelper.VerificationService,
            _unitOfWork.Object);

        var act = () => handler.Handle(
            new FinancialVerificationCommand(
                RegId, FinancialVerificationAction.Verify, PetugasVerif, TestDate),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _tataRekeningRepo.Verify(r => r.SaveChanges(It.IsAny<TataRekeningModel>()), Times.Never);
    }

    [Fact]
    public async Task P3_08_GivenClosedTataRekening_WhenRequireAdjustment_ThenStatusRequiresAdjustment()
    {
        var tataRekening = TataRekeningTestDataBuilder.HydrateClosed(
            RegId, TataRekeningTestDataBuilder.CreateBill("BILL-RA", RegId, 40_000m));
        SetupTataRekeningLoad(RegId, tataRekening);

        var handler = new FinancialVerificationHandler(
            _tataRekeningRepo.Object,
            _mergeRequestRepo.Object,
            TataRekeningDomainTestHelper.VerificationService,
            _unitOfWork.Object);

        var result = await handler.Handle(
            new FinancialVerificationCommand(
                RegId, FinancialVerificationAction.RequireAdjustment, PetugasVerif, TestDate),
            CancellationToken.None);

        result.Summary.FinancialVerificationStatus
            .Should().Be(FinancialVerificationStatusEnum.RequiresAdjustment);
    }

    #endregion

    #region Allocation and Finalization

    [Fact]
    public async Task P3_09_GivenVerifiedTataRekening_WhenAllocate_ThenProjectionRegenerated()
    {
        var bill = TataRekeningTestDataBuilder.CreateBill("BILL-AL", RegId, 100_000m);
        var tataRekening = TataRekeningTestDataBuilder.HydrateClosed(RegId, bill);
        tataRekening.CompleteFinancialVerification(PetugasVerif, TestDate);
        SetupTataRekeningLoad(RegId, tataRekening);

        var payments = new List<PaymentAllocationInputDto>
        {
            new("BYKAS", "KAS", false, 100_000m, 0m, "COA-01", "COA")
        };

        var handler = new AllocateFinancialResponsibilityHandler(
            _tataRekeningRepo.Object, _trsBillingRepo.Object, _unitOfWork.Object);

        var result = await handler.Handle(
            new AllocateFinancialResponsibilityCommand(RegId, payments),
            CancellationToken.None);

        result.Summary.IsFinancialResponsibilityAllocated.Should().BeTrue();
        result.Projection.Should().HaveCount(1);
        result.Projection[0].NilaiJasa.Should().Be(100_000m);
    }

    [Fact]
    public async Task P3_10_GivenAllocatedTataRekening_WhenFinalize_ThenStatusFinalized()
    {
        var bill = TataRekeningTestDataBuilder.CreateBill("BILL-FZ", RegId, 75_000m);
        var tataRekening = TataRekeningTestDataBuilder.HydrateClosed(RegId, bill);
        TataRekeningDomainTestHelper.VerifyAndAllocate(
            tataRekening,
            [TataRekeningTestDataBuilder.BuildPayment(PaymentType.ByKas, 75_000m, 0m)]);
        SetupTataRekeningLoad(RegId, tataRekening);

        var handler = new FinalizeFinancialResponsibilityHandler(
            _tataRekeningRepo.Object, _trsBillingRepo.Object, _unitOfWork.Object);

        var result = await handler.Handle(
            new FinalizeFinancialResponsibilityCommand(RegId, PetugasVerif, TestDate),
            CancellationToken.None);

        result.Summary.Status.Should().Be(TataRekeningStatusEnum.Finalized);
    }

    [Fact]
    public async Task P3_11_GivenFinalizedTataRekening_WhenCancelFinalization_ThenStatusClosed()
    {
        var bill = TataRekeningTestDataBuilder.CreateBill("BILL-CF", RegId, 60_000m);
        var tataRekening = TataRekeningTestDataBuilder.Hydrate(
            RegId, TataRekeningStatusEnum.Opened, bill);
        TataRekeningDomainTestHelper.CloseVerifyAllocateFinalize(
            tataRekening,
            [TataRekeningTestDataBuilder.BuildPayment(PaymentType.ByKas, 60_000m, 0m)]);
        SetupTataRekeningLoad(RegId, tataRekening);

        var handler = new CancelFinalizationHandler(
            _tataRekeningRepo.Object, _trsBillingRepo.Object, _auditRepo.Object, _unitOfWork.Object, _currentUser);

        var result = await handler.Handle(new CancelFinalizationCommand(RegId, "Koreksi alokasi"), CancellationToken.None);

        result.Summary.Status.Should().Be(TataRekeningStatusEnum.Closed);
        result.Summary.IsFinancialResponsibilityAllocated.Should().BeFalse();
    }

    #endregion

    #region Financial Adjustment and Reopen

    [Fact]
    public async Task P3_12_GivenRequiresAdjustment_WhenWaive_ThenResetsVerification()
    {
        var bill = TataRekeningTestDataBuilder.CreateBill("BILL-WV", RegId, 50_000m);
        var tataRekening = TataRekeningTestDataBuilder.HydrateClosed(RegId, bill);
        tataRekening.RequireFinancialAdjustment();
        SetupTataRekeningLoad(RegId, tataRekening);

        var handler = new FinancialAdjustmentHandler(
            _tataRekeningRepo.Object,
            _trsBillingRepo.Object,
            TataRekeningDomainTestHelper.AdjustmentService,
            _auditRepo.Object,
            _unitOfWork.Object,
            _currentUser);

        var result = await handler.Handle(
            new FinancialAdjustmentCommand(
                RegId,
                new FinancialAdjustmentInputDto(
                    FinancialAdjustmentTypeEnum.Waive, 10_000m, "Waive test", "BILL-WV"),
                TestDate),
            CancellationToken.None);

        result.RequiresReopen.Should().BeFalse();
        result.Summary.FinancialVerificationStatus.Should().Be(FinancialVerificationStatusEnum.NotVerified);
        _trsBillingRepo.Verify(r => r.SaveChanges(It.IsAny<TrsBillType>()), Times.Once);
    }

    [Fact]
    public async Task P3_13_GivenRequiresChargeSourceChange_WhenAdjust_ThenRequiresReopenWithoutPersist()
    {
        var bill = TataRekeningTestDataBuilder.CreateBill("BILL-RO", RegId, 50_000m);
        var tataRekening = TataRekeningTestDataBuilder.HydrateClosed(RegId, bill);
        tataRekening.RequireFinancialAdjustment();
        SetupTataRekeningLoad(RegId, tataRekening);

        var handler = new FinancialAdjustmentHandler(
            _tataRekeningRepo.Object,
            _trsBillingRepo.Object,
            TataRekeningDomainTestHelper.AdjustmentService,
            _auditRepo.Object,
            _unitOfWork.Object,
            _currentUser);

        var result = await handler.Handle(
            new FinancialAdjustmentCommand(
                RegId,
                new FinancialAdjustmentInputDto(
                    FinancialAdjustmentTypeEnum.BillingCorrection,
                    0m,
                    "Need charge source",
                    "BILL-RO",
                    RequiresChargeSourceChange: true),
                TestDate),
            CancellationToken.None);

        result.RequiresReopen.Should().BeTrue();
        _tataRekeningRepo.Verify(r => r.SaveChanges(It.IsAny<TataRekeningModel>()), Times.Never);
    }

    [Fact]
    public async Task P3_14_GivenClosedTataRekening_WhenReopen_ThenStatusOpened()
    {
        var tataRekening = TataRekeningTestDataBuilder.HydrateClosed(
            RegId, TataRekeningTestDataBuilder.CreateBill("BILL-RP", RegId, 25_000m));
        SetupTataRekeningLoad(RegId, tataRekening);

        var handler = new ReopenBillingHandler(
            _tataRekeningRepo.Object, _auditRepo.Object, _unitOfWork.Object, _currentUser);

        var result = await handler.Handle(
            new ReopenBillingCommand(RegId, "Koreksi charge source"),
            CancellationToken.None);

        result.Summary.Status.Should().Be(TataRekeningStatusEnum.Opened);
    }

    #endregion

    #region Settlement Initiation

    [Fact]
    public async Task P3_15_GivenFinalizedTataRekening_WhenInitiateSettlement_ThenFlagSet()
    {
        var bill = TataRekeningTestDataBuilder.CreateBill("BILL-SI", RegId, 80_000m);
        var tataRekening = TataRekeningTestDataBuilder.Hydrate(
            RegId, TataRekeningStatusEnum.Opened, bill);
        TataRekeningDomainTestHelper.CloseVerifyAllocateFinalize(
            tataRekening,
            [TataRekeningTestDataBuilder.BuildPayment(PaymentType.ByKas, 80_000m, 0m)]);
        SetupTataRekeningLoad(RegId, tataRekening);

        var handler = new SettlementInitiationHandler(
            _tataRekeningRepo.Object, _auditRepo.Object, _unitOfWork.Object);

        var result = await handler.Handle(
            new SettlementInitiationCommand(RegId, PetugasVerif, TestDate),
            CancellationToken.None);

        result.Summary.SettlementInitiated.Should().BeTrue();
    }

    #endregion

    #region Transaction boundary

    [Fact]
    public async Task P3_16_GivenDomainFailure_WhenCloseBill_ThenDoesNotCompleteTransaction()
    {
        var tataRekening = TataRekeningTestDataBuilder.HydrateClosed(
            RegId, TataRekeningTestDataBuilder.CreateBill("BILL-TX", RegId, 10_000m));
        SetupTataRekeningLoad(RegId, tataRekening);

        var handler = new CloseBillHandler(_tataRekeningRepo.Object, _unitOfWork.Object);

        var act = () => handler.Handle(new CloseBillCommand(RegId), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _tataRekeningRepo.Verify(r => r.SaveChanges(It.IsAny<TataRekeningModel>()), Times.Never);
        _unitOfWorkScope.Verify(s => s.Complete(), Times.Never);
    }

    #endregion

    private void SetupTataRekeningLoad(string regId, TataRekeningModel model) =>
        _tataRekeningRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == regId)))
            .Returns(MayBe.From(model));

    private void SetupRegLoad(string regId, RegModel model) =>
        _regRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == regId)))
            .Returns(MayBe.From(model));
}
