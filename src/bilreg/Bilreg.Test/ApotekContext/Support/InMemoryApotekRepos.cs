using Bilreg.Application.ApotekContext.CopyResepFeature;
using Bilreg.Application.ApotekContext.DispensingFeature;
using Bilreg.Application.ApotekContext.IntegrationFeature;
using Bilreg.Application.ApotekContext.InvoiceFeature;
using Bilreg.Application.ApotekContext.JualBebasFeature;
using Bilreg.Application.ApotekContext.QueueFeature;
using Bilreg.Application.ApotekContext.ResepKerjaFeature;
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
using Bilreg.Domain.ApotekContext.ResepKerjaFeature;
using Bilreg.Domain.ApotekContext.SalesOrderFeature;
using Bilreg.Domain.ApotekContext.TelaahResepFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.ApotekContext.Support;

public class InMemoryResepKerjaRepo : IResepKerjaRepo
{
    public Dictionary<string, ResepKerjaModel> Store { get; } = new();
    public void SaveChanges(ResepKerjaModel model) => Store[model.ResepKerjaId] = model;
    public MayBe<ResepKerjaModel> LoadEntity(IResepKerjaKey key)
        => Store.TryGetValue(key.ResepKerjaId, out var m) ? MayBe.From(m) : MayBe<ResepKerjaModel>.None;
    public MayBe<ResepKerjaModel> LoadBySource(ResepKerjaSourceKindEnum sourceKind, string sourceResepId)
    {
        var m = Store.Values.FirstOrDefault(x => x.SourceKind == sourceKind && x.SourceResepId == sourceResepId);
        return m is null ? MayBe<ResepKerjaModel>.None : MayBe.From(m);
    }
}

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
    public void SaveChanges(TelaahResepModel model) => Store[model.TelaahResepId] = model;
    public MayBe<TelaahResepModel> LoadEntity(ITelaahResepKey key)
        => Store.TryGetValue(key.TelaahResepId, out var m) ? MayBe.From(m) : MayBe<TelaahResepModel>.None;
    public MayBe<TelaahResepModel> LoadByResepKerja(string resepKerjaId)
    {
        var m = Store.Values.FirstOrDefault(x => x.ResepKerjaId == resepKerjaId);
        return m is null ? MayBe<TelaahResepModel>.None : MayBe.From(m);
    }
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

public class InMemoryQueueMappingRepo : IQueueMappingRepo
{
    public Dictionary<string, QueueMappingModel> Store { get; } = new();
    private static string Id(IQueueMappingKey key) => $"{(int)key.DemandKind}:{key.DemandId}";
    public void SaveChanges(QueueMappingModel model) => Store[Id(model)] = model;
    public MayBe<QueueMappingModel> LoadEntity(IQueueMappingKey key)
        => Store.TryGetValue(Id(key), out var m) ? MayBe.From(m) : MayBe<QueueMappingModel>.None;
    public IReadOnlyList<QueueMappingModel> ListByQueue(string antrianId, int noUrut)
        => Store.Values.Where(x => x.AntrianId == antrianId && x.NoUrut == noUrut).ToList();
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
        => Store.Values.Where(x => x.TaskStatus is AptIntegrationTaskStatusEnum.Pending or AptIntegrationTaskStatusEnum.Failed).Take(batchSize);
    public bool ClaimPending(IAptIntegrationTaskKey key) => true;
}

public class FakePrescriptionPort : IPrescriptionContractPort
{
    public PrescriptionContract Contract { get; set; } = default!;
    public PrescriptionContract Load(ResepKerjaSourceKindEnum sourceKind, string sourceResepId) => Contract;
}

public class AllowAllAuth : IAptAuthorizationPolicy
{
    public void AssertCommandAllowed(string commandName, string actorUserId) { }
}

public class FakePaymentPort : IPaymentClearancePort
{
    public PaymentClearanceEvidence? Evidence { get; set; }
    public PaymentClearanceEvidence? Load(string paymentClearanceReff) => Evidence;
}

public class FakePricePort : IMedicationPricePort
{
    public MedicationPrice PriceAt(string brgId, DateTime snapshotAt) => new(1000, 0);
}

public class FakeSepPort : ISepFornasPort
{
    public SepFornasEvidence Evaluate(string regId, string brgId) => new("SEP-1", FornasCoverageEnum.Covered);
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
