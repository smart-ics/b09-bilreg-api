using Bilreg.Application.ApotekContext.CopyResepFeature;
using Bilreg.Application.ApotekContext.DispensingFeature;
using Bilreg.Application.ApotekContext.IntegrationFeature;
using Bilreg.Application.ApotekContext.InvoiceFeature;
using Bilreg.Application.ApotekContext.JualBebasFeature;
using Bilreg.Application.ApotekContext.QueueFeature;
using Bilreg.Application.ApotekContext.SalesOrderFeature;
using Bilreg.Application.ApotekContext.Shared;
using Bilreg.Application.ApotekContext.StockPlanningFeature;
using Bilreg.Application.ApotekContext.TelaahResepFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.ApotekContext.CopyResepFeature;
using Bilreg.Domain.ApotekContext.DispensingFeature;
using Bilreg.Domain.ApotekContext.IntegrationFeature;
using Bilreg.Domain.ApotekContext.InvoiceFeature;
using Bilreg.Domain.ApotekContext.JualBebasFeature;
using Bilreg.Domain.ApotekContext.QueueFeature;
using Bilreg.Domain.ApotekContext.SalesOrderFeature;
using Bilreg.Domain.ApotekContext.TelaahResepFeature;
using Bilreg.Application.SalesContext.ResepFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.BrgContext.PricingPolicyFeature;
using Bilreg.Domain.SalesContext.ResepFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.ApotekContext.Support;

public class InMemoryJualBebasRepo : IJualBebasRepo
{
    public Dictionary<string, JualBebasModel> Store { get; } = new();
    public void SaveChanges(JualBebasModel model) => Store[model.JualBebasId] = model;
    public MayBe<JualBebasModel> LoadEntity(IJualBebasKey key)
        => Store.TryGetValue(key.JualBebasId, out var m) ? MayBe.From(m) : MayBe<JualBebasModel>.None;
}

public class InMemoryTelaahRepo : ITelaahResepRepo
{
    public Dictionary<string, TelaahResepModel> Store { get; } = new();
    public Dictionary<string, List<TelaahResepKajianModel>> KajianStore { get; } = new();

    public void SaveChanges(TelaahResepModel model)
    {
        Store[model.TelaahResepId] = model;
        KajianStore[model.TelaahResepId] = model.KajianItems.ToList();
    }

    public MayBe<TelaahResepModel> LoadEntity(ITelaahResepKey key)
    {
        if (!Store.TryGetValue(key.TelaahResepId, out var m))
            return MayBe<TelaahResepModel>.None;

        var kajian = KajianStore.TryGetValue(key.TelaahResepId, out var k) && k.Count > 0
            ? k
            : (m.KajianItems.Count > 0 ? m.KajianItems : TelaahResepKajianModel.DefaultCatalog());

        var rehydrated = TelaahResepModel.Rehydrate(
            m.TelaahResepId, m.ResepId, m.RegId, m.TelaahStatus, m.PharmacistId,
            m.StartedAt, m.CompletedAt, m.Version, m.Items, kajian);

        return MayBe.From(rehydrated);
    }

    public MayBe<TelaahResepModel> LoadByResep(string resepId)
    {
        var m = Store.Values.FirstOrDefault(x => x.ResepId == resepId);
        if (m is null) return MayBe<TelaahResepModel>.None;
        return LoadEntity(TelaahResepModel.Key(m.TelaahResepId));
    }

    public MayBe<TelaahResepModel> LoadByResepKerja(string resepKerjaId) => LoadByResep(resepKerjaId);
}

public class InMemoryTelaahResepRepo : InMemoryTelaahRepo
{
}

public class InMemoryResepRepo : IResepRepo, Bilreg.Application.SalesContext.ResepFeature.IResepDal, Bilreg.Application.SalesContext.ResepFeature.UseCases.IResepVoidDal
{
    public Dictionary<string, ResepModel> Store { get; } = new();
    public HashSet<string> LockedPrescriptions { get; } = new();
    public void SaveChanges(ResepModel model) => Store[model.ResepId] = model;
    public MayBe<ResepModel> LoadEntity(IResepKey key)
    {
        if (Store.TryGetValue(key.ResepId, out var m))
            return MayBe.From(m);
        return MayBe<ResepModel>.None;
    }
    public void DeleteEntity(IResepKey key) => Store.Remove(key.ResepId);
    public IEnumerable<ResepModel> ListData(Bilreg.Domain.AdmisiContext.RegFeature.IRegKey filter) =>
        Store.Values.Where(x => x.Register.RegId == filter.RegId);
    public void LockPrescription(string resepId) => LockedPrescriptions.Add(resepId);
    public bool HasBeenProcessed(string resepId) => LockedPrescriptions.Contains(resepId);
    public IEnumerable<ResepModel> SearchActiveUnbilled(string query, DateOnly date)
    {
        var q = query?.Trim() ?? string.Empty;
        return Store.Values
            .Where(x => !x.IsVoid)
            .Where(x => string.IsNullOrEmpty(q) ||
                        x.ResepId.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                        x.Register.RegId.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                        x.Register.PasienId.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                        x.Register.PasienName.Contains(q, StringComparison.OrdinalIgnoreCase));
    }
}

public class FakeResepDal : Bilreg.Application.SalesContext.ResepFeature.IResepDal
{
    public HashSet<string> LockedPrescriptions { get; } = new();
    public void LockPrescription(string resepId) => LockedPrescriptions.Add(resepId);
}

public class FakeKemkesKajianResepCatalogPort : IKemkesKajianResepCatalogPort
{
    public IReadOnlyList<TelaahResepKajianModel> GetAll() => TelaahResepKajianModel.DefaultCatalog();
}

public class InMemorySalesOrderRepo : ISalesOrderRepo
{
    public Dictionary<string, SalesOrderModel> Store { get; } = new();
    public void SaveChanges(SalesOrderModel model) => Store[model.SalesOrderId] = model;
    public MayBe<SalesOrderModel> LoadEntity(ISalesOrderKey key)
        => Store.TryGetValue(key.SalesOrderId, out var m) ? MayBe.From(m) : MayBe<SalesOrderModel>.None;
    public MayBe<SalesOrderModel> LoadActive(SalesOrderSourceKindEnum sourceKind, string sourceId, string regId, PayerPathEnum payerPath)
    {
        var m = Store.Values.FirstOrDefault(x => x.SourceKind == sourceKind && x.SourceId == sourceId && x.RegId == regId && x.PayerPath == payerPath && x.IsActiveKey);
        return m is null ? MayBe<SalesOrderModel>.None : MayBe.From(m);
    }
    public IReadOnlyList<SalesOrderModel> ListBySource(SalesOrderSourceKindEnum sourceKind, string sourceId)
        => Store.Values.Where(x => x.SourceKind == sourceKind && x.SourceId == sourceId).ToList();
}

public class InMemoryCopyResepRepo : ICopyResepRepo
{
    public Dictionary<string, CopyResepModel> Store { get; } = new();
    public void SaveChanges(CopyResepModel model) => Store[model.CopyResepId] = model;
    public MayBe<CopyResepModel> LoadEntity(ICopyResepKey key)
        => Store.TryGetValue(key.CopyResepId, out var m) ? MayBe.From(m) : MayBe<CopyResepModel>.None;
}

public class InMemoryInvoiceRepo : IInvoiceRepo
{
    public Dictionary<string, InvoiceModel> Store { get; } = new();
    public void SaveChanges(InvoiceModel model) => Store[model.InvoiceId] = model;
    public MayBe<InvoiceModel> LoadEntity(IInvoiceKey key)
        => Store.TryGetValue(key.InvoiceId, out var m) ? MayBe.From(m) : MayBe<InvoiceModel>.None;
    public IReadOnlyList<InvoiceModel> ListBySalesOrder(string salesOrderId)
        => Store.Values.Where(x => x.SalesOrderId == salesOrderId).ToList();
    public MayBe<InvoiceModel> LoadActiveBySalesOrder(string salesOrderId)
    {
        var m = Store.Values.LastOrDefault(x => x.SalesOrderId == salesOrderId && x.InvoiceStatus != InvoiceStatusEnum.Cancelled);
        return m is null ? MayBe<InvoiceModel>.None : MayBe.From(m);
    }
}

public class InMemoryDispensingRepo : IDispensingRepo
{
    public Dictionary<string, DispensingModel> Store { get; } = new();
    public void SaveChanges(DispensingModel model) => Store[model.DispensingId] = model;
    public MayBe<DispensingModel> LoadEntity(IDispensingKey key)
        => Store.TryGetValue(key.DispensingId, out var m) ? MayBe.From(m) : MayBe<DispensingModel>.None;
    public IReadOnlyList<DispensingModel> ListBySalesOrder(string salesOrderId)
        => Store.Values.Where(x => x.SalesOrderId == salesOrderId).ToList();
}

public class InMemoryQueueMappingRepo : IQueueMappingRepo, IPharmacyQueueMappingRepo
{
    public Dictionary<string, QueueMappingModel> Store { get; } = new();
    private static string Id(IQueueMappingKey key) => $"{(int)key.DemandKind}:{key.DemandId}";
    public void SaveChanges(QueueMappingModel model) => Store[Id(model)] = model;
    public MayBe<QueueMappingModel> LoadEntity(IQueueMappingKey key)
        => Store.TryGetValue(Id(key), out var m) ? MayBe.From(m) : MayBe<QueueMappingModel>.None;
    public IReadOnlyList<QueueMappingModel> ListByQueue(string antrianId, int noUrut)
        => Store.Values.Where(x => x.AntrianId == antrianId && x.NoUrut == noUrut).ToList();
    public MayBe<QueueMappingModel> GetByDemand(QueueDemandKindEnum demandKind, string demandId)
        => LoadEntity(QueueMappingModel.Key(demandKind, demandId));
    public bool SetPasienHadir(QueueDemandKindEnum demandKind, string demandId, string userId, DateTime at)
    {
        var key = QueueMappingModel.Key(demandKind, demandId);
        if (Store.TryGetValue(Id(key), out var m))
        {
            m.MarkPasienHadir(at);
            return true;
        }
        return false;
    }
}

public class InMemoryQueueCloseRepo : IQueueCloseRepo
{
    public Dictionary<string, QueueCloseModel> Store { get; } = new();
    public void SaveChanges(QueueCloseModel model) => Store[model.QueueCloseId] = model;
    public MayBe<QueueCloseModel> LoadEntity(IQueueCloseKey key)
        => Store.TryGetValue(key.QueueCloseId, out var m) ? MayBe.From(m) : MayBe<QueueCloseModel>.None;
    public MayBe<QueueCloseModel> LoadByQueue(string antrianId, int noUrut)
    {
        var m = Store.Values.FirstOrDefault(x => x.AntrianId == antrianId && x.NoUrut == noUrut);
        return m is null ? MayBe<QueueCloseModel>.None : MayBe.From(m);
    }
}

public class InMemoryIntegrationTaskRepo : IAptIntegrationTaskRepo
{
    public Dictionary<string, AptIntegrationTaskModel> Store { get; } = new();
    public void SaveChanges(AptIntegrationTaskModel model) => Store[model.IntegrationTaskId] = model;
    public MayBe<AptIntegrationTaskModel> LoadEntity(IAptIntegrationTaskKey key)
        => Store.TryGetValue(key.IntegrationTaskId, out var m) ? MayBe.From(m) : MayBe<AptIntegrationTaskModel>.None;
    public MayBe<AptIntegrationTaskModel> LoadByIdempotencyKey(string idempotencyKey)
    {
        var m = Store.Values.FirstOrDefault(x => x.IdempotencyKey == idempotencyKey);
        return m is null ? MayBe<AptIntegrationTaskModel>.None : MayBe.From(m);
    }
    public IEnumerable<AptIntegrationTaskModel> ListPending(int batchSize)
        => Store.Values.Where(x => x.TaskStatus == AptIntegrationTaskStatusEnum.Pending).Take(batchSize);
    public bool ClaimPending(IAptIntegrationTaskKey key)
    {
        if (!Store.TryGetValue(key.IntegrationTaskId, out var m))
            return false;
        if (m.TaskStatus == AptIntegrationTaskStatusEnum.Processing)
            return true;
        if (m.TaskStatus != AptIntegrationTaskStatusEnum.Pending)
            return false;
        m.ClaimPending();
        return true;
    }
}

public class AllowAllAuth : IAptAuthorizationPolicy
{
    public void AssertCommandAllowed(string commandName, string actorUserId) { }
}

public class FakePaymentPort : Bilreg.Application.ApotekContext.InvoiceFeature.IPaymentClearancePort
{
    public PaymentClearanceEvidence? Evidence { get; set; }
    public PaymentClearanceEvidence? Load(string paymentClearanceReff) => Evidence;
}

public class FakeTataRekeningChargePort : ITataRekeningChargePort
{
    public int IssueCount { get; private set; }

    public string IssueCharge(InvoiceModel invoice)
    {
        IssueCount++;
        return "TR-CHG-1";
    }
}

public class FakePricePort : IMedicationPricePort
{
    public MedicationPrice PriceAt(string brgId, DateTime snapshotAt) => new(1000, 0);
}

public class FakeSepPort : ISepFornasPort
{
    public SepFornasEvidence Evaluate(string regId, string brgId) => new("SEP-1", FornasCoverageEnum.Covered);
}

public class ConfigurableSepPort : ISepFornasPort
{
    public string SepNo { get; set; } = "SEP-1";
    public Dictionary<string, FornasCoverageEnum> CoverageByBrgId { get; } = new();

    public SepFornasEvidence Evaluate(string regId, string brgId)
        => new(SepNo, CoverageByBrgId.TryGetValue(brgId, out var coverage) ? coverage : FornasCoverageEnum.Covered);
}

public class FakeTrackerPort : ITrackerPharmacyPort
{
    public AntrianStatusEnum Status { get; set; } = AntrianStatusEnum.Waiting;
    public int ServeCount { get; private set; }
    public int DoneCount { get; private set; }
    public AntrianStatusEnum GetStatus(string antrianId, int noUrut) => Status;
    public TrackerPharmacyCommandResult ServeOnce(string antrianId, int noUrut, string pasienTrackerId, string reffId, DateTime at)
    {
        if (Status is AntrianStatusEnum.InService or AntrianStatusEnum.Done)
            return new TrackerPharmacyCommandResult(false, "idempotent");
        Status = AntrianStatusEnum.InService;
        ServeCount++;
        return new TrackerPharmacyCommandResult(true, "served");
    }
    public TrackerPharmacyCommandResult DoneOnce(string antrianId, int noUrut, string pasienTrackerId, string reffId, DateTime at)
    {
        if (Status == AntrianStatusEnum.Done)
            return new TrackerPharmacyCommandResult(false, "idempotent");
        Status = AntrianStatusEnum.Done;
        DoneCount++;
        return new TrackerPharmacyCommandResult(true, "done");
    }
    public TrackerPharmacyCommandResult WithdrawFromWaiting(string antrianId, int noUrut, string reason, string userId, DateTime at)
    {
        if (Status != AntrianStatusEnum.Waiting)
            return new TrackerPharmacyCommandResult(false, "");
        Status = AntrianStatusEnum.Withdrawn;
        return new TrackerPharmacyCommandResult(true, "wdn");
    }
}

public class FakeWindow : ICollectionWindowDaysProvider
{
    public int GetDays() => 7;
}

public class AllowTrPermission : ITataRekeningInvoicePermissionPort
{
    public bool AllowsModification(string tataRekeningChargeId) => true;
}

public class DenyTrPermission : ITataRekeningInvoicePermissionPort
{
    public bool AllowsModification(string tataRekeningChargeId) => false;
}

public class RecordingIntegrationHandler : IAptIntegrationHandler
{
    public AptIntegrationTaskTypeEnum TaskType => AptIntegrationTaskTypeEnum.TrackerServedAt;
    public int Calls { get; private set; }
    public bool Fail { get; set; }

    public AptIntegrationHandleResult Handle(AptIntegrationTaskModel task)
    {
        Calls++;
        return Fail
            ? new AptIntegrationHandleResult(false, "", "boom")
            : new AptIntegrationHandleResult(true, "corr-1", null);
    }
}
