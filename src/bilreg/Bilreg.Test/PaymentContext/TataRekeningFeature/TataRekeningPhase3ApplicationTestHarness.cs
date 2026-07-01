using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.PaymentContext.TataRekeningFeature;
using Bilreg.Application.PaymentContext.TataRekeningFeature.UseCases;
using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Application.Shared;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
using Moq;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.PaymentContext.TataRekeningFeature;

internal sealed class TataRekeningPhase3ApplicationTestHarness
{
    public const string RegId = "REG-TGT-001";
    public const string SourceRegId = "REG-SRC-001";

    public Mock<ITataRekeningRepo> TataRekeningRepo { get; } = new();
    public Mock<IMergeRequestRepo> MergeRequestRepo { get; } = new();
    public Mock<ITrsBillingRepo> TrsBillingRepo { get; } = new();
    public Mock<IUnitOfWork> UnitOfWork { get; } = new();
    public Mock<IUnitOfWorkScope> UnitOfWorkScope { get; } = new();

    public TataRekeningPhase3ApplicationTestHarness()
    {
        UnitOfWork.Setup(u => u.Begin()).Returns(UnitOfWorkScope.Object);
        SetupMergeScenario();
    }

    public MergeBillingHandler CreateMergeBillingHandler(
        ITransferReceivableService transferReceivableService,
        IAuditRepo auditRepo) =>
        new(
            TataRekeningRepo.Object,
            MergeRequestRepo.Object,
            TrsBillingRepo.Object,
            TataRekeningDomainTestHelper.MergeBillingService,
            transferReceivableService,
            auditRepo,
            UnitOfWork.Object,
            new FixedCurrentUserContext());

    private void SetupMergeScenario()
    {
        var sourceBill = TataRekeningTestDataBuilder.CreateBill("BILL-SRC", SourceRegId, 30_000m);
        var targetBill = TataRekeningTestDataBuilder.CreateBill("BILL-TGT", RegId, 20_000m);
        var source = TataRekeningTestDataBuilder.HydrateClosed(SourceRegId, sourceBill);
        var target = TataRekeningTestDataBuilder.HydrateClosed(RegId, targetBill);
        var mergeRequest = MergeRequestModel.Create("MR-MERGE", SourceRegId, RegId);

        MergeRequestRepo.Setup(r => r.LoadEntity(It.Is<IMergeRequestKey>(k => k.MergeRequestId == "MR-MERGE")))
            .Returns(MayBe.From(mergeRequest));
        TataRekeningRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == SourceRegId)))
            .Returns(MayBe.From(source));
        TataRekeningRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(MayBe.From(target));
    }
}
