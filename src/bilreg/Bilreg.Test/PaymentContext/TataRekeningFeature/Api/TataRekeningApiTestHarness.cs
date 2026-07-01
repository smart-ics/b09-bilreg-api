using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.PaymentContext.TataRekeningFeature;
using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Application.Shared;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.PaymentContext.TataRekeningFeature.Api;

public sealed class TataRekeningApiTestHarness
{
    public const string RegId = TataRekeningPhase3ApplicationTestHarness.RegId;
    public const string SourceRegId = TataRekeningPhase3ApplicationTestHarness.SourceRegId;

    public Mock<ITataRekeningRepo> TataRekeningRepo { get; } = new();
    public Mock<IMergeRequestRepo> MergeRequestRepo { get; } = new();
    public Mock<ITrsBillingRepo> TrsBillingRepo { get; } = new();
    public Mock<IRegRepo> RegRepo { get; } = new();
    public Mock<IUnitOfWork> UnitOfWork { get; } = new();
    public Mock<IUnitOfWorkScope> UnitOfWorkScope { get; } = new();
    public Mock<ITransferReceivableService> TransferReceivableService { get; } = new();
    public Mock<IAuditRepo> AuditRepo { get; } = new();

    public TataRekeningApiTestHarness()
    {
        UnitOfWork.Setup(u => u.Begin()).Returns(UnitOfWorkScope.Object);
    }

    public void Reset()
    {
        TataRekeningRepo.Reset();
        MergeRequestRepo.Reset();
        TrsBillingRepo.Reset();
        RegRepo.Reset();
        UnitOfWork.Reset();
        UnitOfWorkScope.Reset();
        TransferReceivableService.Reset();
        AuditRepo.Reset();
        UnitOfWork.Setup(u => u.Begin()).Returns(UnitOfWorkScope.Object);
    }

    public void ConfigureServices(IServiceCollection services)
    {
        var harness = this;
        RemoveAndRegister(services, () => harness.TataRekeningRepo.Object);
        RemoveAndRegister(services, () => harness.MergeRequestRepo.Object);
        RemoveAndRegister(services, () => harness.TrsBillingRepo.Object);
        RemoveAndRegister(services, () => harness.RegRepo.Object);
        RemoveAndRegister(services, () => harness.UnitOfWork.Object);
        RemoveAndRegister(services, () => harness.TransferReceivableService.Object);
        RemoveAndRegister(services, () => harness.AuditRepo.Object);
        RemoveAndRegister(services, () => (ICurrentUserContext)new FixedCurrentUserContext(TestAuthHandler.UserId));
    }

    public void SetupOpenScenario()
    {
        var bill = TataRekeningTestDataBuilder.CreateBill("BILL-API-01", RegId, 50_000m);
        var tataRekening = TataRekeningTestDataBuilder.Hydrate(RegId, TataRekeningStatusEnum.Opened, bill);
        SetupTataRekeningLoad(RegId, tataRekening);
        SetupRegLoad(RegId, RegModel.Default);
        MergeRequestRepo.Setup(r => r.ListPendingByReg(It.IsAny<IRegKey>())).Returns([]);
        MergeRequestRepo.Setup(r => r.ListPendingByPatient(It.IsAny<string>())).Returns([]);
    }

    public void SetupCloseScenario()
    {
        var bill = TataRekeningTestDataBuilder.CreateBill("BILL-CLOSE", RegId, 40_000m);
        var tataRekening = TataRekeningTestDataBuilder.Hydrate(RegId, TataRekeningStatusEnum.Opened, bill);
        SetupTataRekeningLoad(RegId, tataRekening);
    }

    public void SetupMergeScenario()
    {
        var sourceBill = TataRekeningTestDataBuilder.CreateBill("BILL-SRC", SourceRegId, 30_000m);
        var targetBill = TataRekeningTestDataBuilder.CreateBill("BILL-TGT", RegId, 20_000m);
        var source = TataRekeningTestDataBuilder.HydrateClosed(SourceRegId, sourceBill);
        var target = TataRekeningTestDataBuilder.HydrateClosed(RegId, targetBill);
        var mergeRequest = MergeRequestModel.Create("MR-API", SourceRegId, RegId);

        MergeRequestRepo.Setup(r => r.LoadEntity(It.Is<IMergeRequestKey>(k => k.MergeRequestId == "MR-API")))
            .Returns(MayBe.From(mergeRequest));
        SetupTataRekeningLoad(SourceRegId, source);
        SetupTataRekeningLoad(RegId, target);
    }

    public void SetupVerifyScenario()
    {
        var tataRekening = TataRekeningTestDataBuilder.HydrateClosed(
            RegId, TataRekeningTestDataBuilder.CreateBill("BILL-VER", RegId, 40_000m));
        SetupTataRekeningLoad(RegId, tataRekening);
        MergeRequestRepo.Setup(r => r.ListPendingByReg(It.IsAny<IRegKey>())).Returns([]);
    }

    public void SetupNotFound()
    {
        TataRekeningRepo.Setup(r => r.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe<TataRekeningModel>.None);
    }

    public void SetupConcurrencyConflict()
    {
        var bill = TataRekeningTestDataBuilder.CreateBill("BILL-STALE", RegId, 10_000m);
        var tataRekening = TataRekeningTestDataBuilder.Hydrate(RegId, TataRekeningStatusEnum.Opened, bill);
        SetupTataRekeningLoad(RegId, tataRekening);
        TataRekeningRepo.Setup(r => r.SaveChanges(It.IsAny<TataRekeningModel>()))
            .Throws(new InvalidOperationException(
                $"Tata Rekening '{RegId}' stale; please reload (expected version: 1)."));
    }

    public void SetupTataRekeningLoad(string regId, TataRekeningModel model) =>
        TataRekeningRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == regId)))
            .Returns(MayBe.From(model));

    private void SetupRegLoad(string regId, RegModel model) =>
        RegRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == regId)))
            .Returns(MayBe.From(model));

    private static void RemoveAndRegister<T>(IServiceCollection services, Func<T> factory) where T : class
    {
        services.RemoveAll<T>();
        services.AddScoped(_ => factory());
    }
}
