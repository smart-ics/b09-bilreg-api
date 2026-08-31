using Bilreg.Domain.ApotekContext.DispensingFeature;
using Bilreg.Domain.ApotekContext.IntegrationFeature;
using Bilreg.Domain.ApotekContext.InvoiceFeature;
using Bilreg.Domain.ApotekContext.QueueFeature;
using Bilreg.Domain.ApotekContext.SalesOrderFeature;
using Bilreg.Domain.ApotekContext.TelaahResepFeature;
using MediatR;

namespace Bilreg.Application.ApotekContext.WorklistFeature;

public record TelaahWorklistQuery : IRequest<IReadOnlyList<TelaahWorklistItem>>;

public record TelaahWorklistItem(
    string TelaahResepId,
    string ResepKerjaId,
    string PasienName,
    TelaahStatusEnum Status,
    DateTime StartedAt);

public record PelayananWorklistQuery(string AntrianId, int? NoUrut, DateOnly BusinessDate)
    : IRequest<IReadOnlyList<PelayananWorklistItem>>;

public record PelayananWorklistItem(
    string AntrianId,
    int NoUrut,
    QueueDemandKindEnum DemandKind,
    string DemandId,
    string PasienName,
    string SalesOrderId,
    PayerPathEnum PayerPath,
    string InvoiceId,
    InvoiceStatusEnum? InvoiceStatus,
    string AttentionLabel);

public record DispensingWorklistQuery : IRequest<IReadOnlyList<DispensingWorklistItem>>;

public record DispensingWorklistItem(
    string DispensingId,
    string SalesOrderId,
    DispensingStatusEnum Status,
    DateTime PreparationStartedAt);

public record SerahWorklistQuery(DateTime AsOf) : IRequest<IReadOnlyList<SerahWorklistItem>>;

public record SerahWorklistItem(
    string DispensingId,
    string SalesOrderId,
    string Category,
    DateTime PreparedAt,
    DateTime PickupCalledAt);

public record JourneyQuery(string AntrianId, int NoUrut) : IRequest<JourneyResponse>;

public record JourneySalesOrderRef(string SalesOrderId, PayerPathEnum PayerPath);

public record JourneyIntegrationTaskRef(
    string IntegrationTaskId,
    AptIntegrationTaskStatusEnum Status,
    string LastError);

public record JourneyDemand(
    QueueDemandKindEnum DemandKind,
    string DemandId,
    string TelaahResepId,
    TelaahStatusEnum? TelaahStatus,
    IReadOnlyList<JourneySalesOrderRef> SalesOrders,
    IReadOnlyList<string> InvoiceIds,
    IReadOnlyList<string> DispensingIds,
    IReadOnlyList<JourneyIntegrationTaskRef> IntegrationTasks);

public record JourneyResponse(string AntrianId, int NoUrut, IReadOnlyList<JourneyDemand> Demands);

public record UnifiedSalesReportQuery(DateTime Date1, DateTime Date2) : IRequest<IReadOnlyList<UnifiedSalesReportItem>>;

public record UnifiedSalesReportItem(
    string SourceKind,
    string DocumentId,
    DateTime DocumentDate,
    string PasienName,
    decimal GrandTotal);

public interface IAptWorklistDal
{
    IReadOnlyList<TelaahWorklistItem> ListTelaah();
    IReadOnlyList<PelayananWorklistItem> ListPelayanan(string antrianId, int? noUrut, DateOnly businessDate);
    IReadOnlyList<DispensingWorklistItem> ListDispensing();
    IReadOnlyList<SerahWorklistItem> ListSerah(DateTime asOf, int collectionWindowDays);
    JourneyResponse LoadJourney(string antrianId, int noUrut);
    IReadOnlyList<UnifiedSalesReportItem> ListUnifiedSales(DateTime date1, DateTime date2);
}

public class TelaahWorklistHandler : IRequestHandler<TelaahWorklistQuery, IReadOnlyList<TelaahWorklistItem>>
{
    private readonly IAptWorklistDal _dal;
    public TelaahWorklistHandler(IAptWorklistDal dal) => _dal = dal;
    public Task<IReadOnlyList<TelaahWorklistItem>> Handle(TelaahWorklistQuery request, CancellationToken cancellationToken)
        => Task.FromResult(_dal.ListTelaah());
}

public class PelayananWorklistHandler : IRequestHandler<PelayananWorklistQuery, IReadOnlyList<PelayananWorklistItem>>
{
    private readonly IAptWorklistDal _dal;
    public PelayananWorklistHandler(IAptWorklistDal dal) => _dal = dal;
    public Task<IReadOnlyList<PelayananWorklistItem>> Handle(PelayananWorklistQuery request, CancellationToken cancellationToken)
        => Task.FromResult(_dal.ListPelayanan(request.AntrianId, request.NoUrut, request.BusinessDate));
}

public class DispensingWorklistHandler : IRequestHandler<DispensingWorklistQuery, IReadOnlyList<DispensingWorklistItem>>
{
    private readonly IAptWorklistDal _dal;
    public DispensingWorklistHandler(IAptWorklistDal dal) => _dal = dal;
    public Task<IReadOnlyList<DispensingWorklistItem>> Handle(DispensingWorklistQuery request, CancellationToken cancellationToken)
        => Task.FromResult(_dal.ListDispensing());
}

public class SerahWorklistHandler : IRequestHandler<SerahWorklistQuery, IReadOnlyList<SerahWorklistItem>>
{
    private readonly IAptWorklistDal _dal;
    private readonly Shared.ICollectionWindowDaysProvider _window;
    public SerahWorklistHandler(IAptWorklistDal dal, Shared.ICollectionWindowDaysProvider window)
    {
        _dal = dal;
        _window = window;
    }

    public Task<IReadOnlyList<SerahWorklistItem>> Handle(SerahWorklistQuery request, CancellationToken cancellationToken)
        => Task.FromResult(_dal.ListSerah(request.AsOf, _window.GetDays()));
}

public class JourneyHandler : IRequestHandler<JourneyQuery, JourneyResponse>
{
    private readonly IAptWorklistDal _dal;
    public JourneyHandler(IAptWorklistDal dal) => _dal = dal;
    public Task<JourneyResponse> Handle(JourneyQuery request, CancellationToken cancellationToken)
        => Task.FromResult(_dal.LoadJourney(request.AntrianId, request.NoUrut));
}

public class UnifiedSalesReportHandler : IRequestHandler<UnifiedSalesReportQuery, IReadOnlyList<UnifiedSalesReportItem>>
{
    private readonly IAptWorklistDal _dal;
    public UnifiedSalesReportHandler(IAptWorklistDal dal) => _dal = dal;
    public Task<IReadOnlyList<UnifiedSalesReportItem>> Handle(UnifiedSalesReportQuery request, CancellationToken cancellationToken)
        => Task.FromResult(_dal.ListUnifiedSales(request.Date1, request.Date2));
}
