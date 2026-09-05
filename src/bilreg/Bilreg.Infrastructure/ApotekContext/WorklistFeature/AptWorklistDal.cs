using Bilreg.Application.ApotekContext.IntegrationFeature.UseCases;
using Bilreg.Application.ApotekContext.WorklistFeature;
using Bilreg.Domain.ApotekContext.DispensingFeature;
using Bilreg.Domain.ApotekContext.IntegrationFeature;
using Bilreg.Domain.ApotekContext.InvoiceFeature;
using Bilreg.Domain.ApotekContext.QueueFeature;
using Bilreg.Domain.ApotekContext.SalesOrderFeature;
using Bilreg.Domain.ApotekContext.Shared;
using Bilreg.Domain.ApotekContext.TelaahResepFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using System.Data.SqlClient;

namespace Bilreg.Infrastructure.ApotekContext.WorklistFeature;

public class AptWorklistDal : IAptWorklistDal
{
    private readonly DatabaseOptions _opt;
    public AptWorklistDal(IOptions<DatabaseOptions> opt) => _opt = opt.Value;

    public IReadOnlyList<TelaahWorklistItem> ListTelaah()
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<TelaahWorklistItem>("""
            SELECT ISNULL(t.TelaahResepId, '') AS TelaahResepId,
                   r.ResepKerjaId,
                   r.PasienName,
                   ISNULL(t.TelaahStatus, @availableStatus) AS Status,
                   COALESCE(NULLIF(t.StartedAt, '3000-01-01'), r.CrtDate) AS StartedAt
            FROM BILRG_AptResepKerja r
            LEFT JOIN BILRG_AptTelaahResep t
                ON t.ResepKerjaId = r.ResepKerjaId AND t.VodDate = '3000-01-01'
            WHERE r.VodDate = '3000-01-01'
              AND (t.TelaahResepId IS NULL OR t.TelaahStatus IN (@availableStatus, @underReviewStatus))
            ORDER BY StartedAt
            """, new
            {
                availableStatus = (int)TelaahStatusEnum.Available,
                underReviewStatus = (int)TelaahStatusEnum.UnderReview
            }).ToList();
    }

    public IReadOnlyList<PelayananWorklistItem> ListPelayanan(string antrianId, int? noUrut, DateOnly businessDate)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var filterByQueue = !string.IsNullOrWhiteSpace(antrianId);
        var sql = filterByQueue ? PelayananByQueueSql : PelayananWaitingSql;
        return conn.Query<PelayananWorklistRow>(sql, new
        {
            antrianId = antrianId ?? "",
            noUrut,
            businessDate = businessDate.ToDateTime(TimeOnly.MinValue),
            servicePointId = ApotekLocationIds.PharmacyServicePointId,
            waitingStatus = (int)AntrianStatusEnum.Waiting,
            resepDemand = (int)QueueDemandKindEnum.ResepKerja,
            jualBebasDemand = (int)QueueDemandKindEnum.JualBebas
        }).Select(MapPelayanan).ToList();
    }

    private const string PelayananDemandJoinSql = """
            LEFT JOIN BILRG_AptQueueMapping m ON m.AntrianId = e.AntrianId AND m.NoUrut = e.NoUrut
            LEFT JOIN BILRG_AptResepKerja r ON m.DemandKind = @resepDemand AND r.ResepKerjaId = m.DemandId AND r.VodDate = '3000-01-01'
            LEFT JOIN BILRG_AptJualBebas j ON m.DemandKind = @jualBebasDemand AND j.JualBebasId = m.DemandId AND j.VodDate = '3000-01-01'
            LEFT JOIN BILRG_AptSalesOrder s
                ON m.DemandId IS NOT NULL
               AND s.SourceId = m.DemandId
               AND s.SourceKind = m.DemandKind
               AND s.VodDate = '3000-01-01'
            LEFT JOIN BILRG_AptInvoice i ON i.SalesOrderId = s.SalesOrderId AND i.VodDate = '3000-01-01'
        """;

    private readonly string PelayananWaitingSql = $"""
            SELECT e.AntrianId, e.NoUrut,
                   ISNULL(m.DemandKind, @resepDemand) AS DemandKind,
                   ISNULL(m.DemandId, '') AS DemandId,
                   COALESCE(r.PasienName, j.PasienName, '') AS PasienName,
                   ISNULL(s.SalesOrderId, '') AS SalesOrderId,
                   ISNULL(s.PayerPath, 0) AS PayerPath,
                   ISNULL(i.InvoiceId, '') AS InvoiceId,
                   i.InvoiceStatus AS InvoiceStatus
            FROM BILRG_Antrian q
            INNER JOIN BILRG_AntrianEntry e ON e.AntrianId = q.AntrianId
            {PelayananDemandJoinSql}
            WHERE q.ServicePointCode = @servicePointId
              AND q.AntrianDate = @businessDate
              AND e.AntrianStatus = @waitingStatus
            ORDER BY e.Priority DESC, e.CreatedAt, e.NoUrut, m.DemandKind, m.DemandId
            """;

    private readonly string PelayananByQueueSql = $"""
            SELECT e.AntrianId, e.NoUrut,
                   m.DemandKind,
                   m.DemandId,
                   COALESCE(r.PasienName, j.PasienName, '') AS PasienName,
                   ISNULL(s.SalesOrderId, '') AS SalesOrderId,
                   ISNULL(s.PayerPath, 0) AS PayerPath,
                   ISNULL(i.InvoiceId, '') AS InvoiceId,
                   i.InvoiceStatus AS InvoiceStatus
            FROM BILRG_Antrian q
            INNER JOIN BILRG_AntrianEntry e ON e.AntrianId = q.AntrianId
            INNER JOIN BILRG_AptQueueMapping m ON m.AntrianId = e.AntrianId AND m.NoUrut = e.NoUrut
            LEFT JOIN BILRG_AptResepKerja r ON m.DemandKind = @resepDemand AND r.ResepKerjaId = m.DemandId AND r.VodDate = '3000-01-01'
            LEFT JOIN BILRG_AptJualBebas j ON m.DemandKind = @jualBebasDemand AND j.JualBebasId = m.DemandId AND j.VodDate = '3000-01-01'
            LEFT JOIN BILRG_AptSalesOrder s
                ON s.SourceId = m.DemandId AND s.SourceKind = m.DemandKind AND s.VodDate = '3000-01-01'
            LEFT JOIN BILRG_AptInvoice i ON i.SalesOrderId = s.SalesOrderId AND i.VodDate = '3000-01-01'
            WHERE q.ServicePointCode = @servicePointId
              AND q.AntrianDate = @businessDate
              AND e.AntrianId = @antrianId
              AND (@noUrut IS NULL OR e.NoUrut = @noUrut)
            ORDER BY m.DemandKind, m.DemandId
            """;

    public IReadOnlyList<DispensingWorklistItem> ListDispensing()
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<DispensingWorklistItem>("""
            SELECT DispensingId, SalesOrderId, DispensingStatus AS Status, PreparationStartedAt
            FROM BILRG_AptDispensing
            WHERE DispensingStatus IN (@releasedStatus, @preparingStatus) AND VodDate = '3000-01-01'
            """, new
        {
            releasedStatus = (int)DispensingStatusEnum.Released,
            preparingStatus = (int)DispensingStatusEnum.Preparing
        }).ToList();
    }

    public IReadOnlyList<SerahWorklistItem> ListSerah(DateTime asOf, int collectionWindowDays)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var rows = conn.Query<SerahRow>("""
            SELECT DispensingId, SalesOrderId, DispensingStatus, PreparedAt, PickupCalledAt, EducationAt, HandoverAt, OverrideAt
            FROM BILRG_AptDispensing
            WHERE DispensingStatus IN (@preparedStatus, @completedStatus, @expiredStatus) AND VodDate = '3000-01-01'
            """, new
        {
            preparedStatus = (int)DispensingStatusEnum.Prepared,
            completedStatus = (int)DispensingStatusEnum.Completed,
            expiredStatus = (int)DispensingStatusEnum.Expired
        }).ToList();
        return rows.Select(x => new SerahWorklistItem(
            x.DispensingId,
            x.SalesOrderId,
            SerahWorklistProjection.ComputeCategory(
                (DispensingStatusEnum)x.DispensingStatus,
                x.PreparedAt,
                x.PickupCalledAt,
                x.EducationAt,
                x.HandoverAt,
                x.OverrideAt,
                asOf,
                collectionWindowDays),
            x.PreparedAt,
            x.PickupCalledAt)).ToList();
    }

    public JourneyResponse LoadJourney(string antrianId, int noUrut)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var maps = conn.Query<QueueMappingDtoLite>(
            "SELECT DemandKind, DemandId FROM BILRG_AptQueueMapping WHERE AntrianId=@antrianId AND NoUrut=@noUrut",
            new { antrianId, noUrut }).ToList();
        var openTasks = conn.Query<JourneyTaskRow>("""
            SELECT IntegrationTaskId, SourceId, TaskStatus, LastError
            FROM BILRG_AptIntegrationTask
            WHERE TaskStatus IN (@pending, @processing, @failed, @dead)
            """, new
        {
            pending = (int)AptIntegrationTaskStatusEnum.Pending,
            processing = (int)AptIntegrationTaskStatusEnum.Processing,
            failed = (int)AptIntegrationTaskStatusEnum.Failed,
            dead = (int)AptIntegrationTaskStatusEnum.Dead
        }).ToList();
        var demands = new List<JourneyDemand>();
        foreach (var map in maps)
        {
            string telaahResepId = "";
            TelaahStatusEnum? telaahStatus = null;
            if (map.DemandKind == (int)QueueDemandKindEnum.ResepKerja)
            {
                var telaah = conn.QueryFirstOrDefault<TelaahLite>(
                    "SELECT TelaahResepId, TelaahStatus FROM BILRG_AptTelaahResep WHERE ResepKerjaId=@id AND VodDate='3000-01-01'",
                    new { id = map.DemandId });
                if (telaah is not null)
                {
                    telaahResepId = telaah.TelaahResepId;
                    telaahStatus = (TelaahStatusEnum)telaah.TelaahStatus;
                }
            }

            var orderRows = conn.Query<SalesOrderLite>(
                "SELECT SalesOrderId, PayerPath FROM BILRG_AptSalesOrder WHERE SourceKind=@kind AND SourceId=@id AND VodDate='3000-01-01'",
                new { kind = map.DemandKind, id = map.DemandId }).ToList();
            var salesOrders = orderRows
                .Select(x => new JourneySalesOrderRef(x.SalesOrderId, (PayerPathEnum)x.PayerPath))
                .ToList();
            var orderIds = orderRows.Select(x => x.SalesOrderId).ToList();
            var invoices = orderIds.Count == 0 ? [] : conn.Query<string>(
                "SELECT InvoiceId FROM BILRG_AptInvoice WHERE SalesOrderId IN @orders AND VodDate='3000-01-01'", new { orders = orderIds }).ToList();
            var dispensings = orderIds.Count == 0 ? [] : conn.Query<string>(
                "SELECT DispensingId FROM BILRG_AptDispensing WHERE SalesOrderId IN @orders AND VodDate='3000-01-01'", new { orders = orderIds }).ToList();
            var relatedIds = new HashSet<string>([map.DemandId]);
            foreach (var id in orderIds) relatedIds.Add(id);
            foreach (var id in invoices) relatedIds.Add(id);
            foreach (var id in dispensings) relatedIds.Add(id);
            var integrationTasks = openTasks
                .Where(t => relatedIds.Contains(t.SourceId))
                .Select(t => new JourneyIntegrationTaskRef(
                    t.IntegrationTaskId,
                    (AptIntegrationTaskStatusEnum)t.TaskStatus,
                    t.LastError))
                .ToList();
            demands.Add(new JourneyDemand(
                (QueueDemandKindEnum)map.DemandKind,
                map.DemandId,
                telaahResepId,
                telaahStatus,
                salesOrders,
                invoices,
                dispensings,
                integrationTasks));
        }
        return new JourneyResponse(antrianId, noUrut, demands);
    }

    public IReadOnlyList<UnifiedSalesReportItem> ListUnifiedSales(DateTime date1, DateTime date2)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var apt = conn.Query<UnifiedSalesReportItem>("""
            SELECT 'APT' AS SourceKind, i.InvoiceId AS DocumentId, i.EstablishedAt AS DocumentDate,
                   ISNULL(s.PasienName, '') AS PasienName, i.GrandTotal
            FROM BILRG_AptInvoice i
            LEFT JOIN BILRG_AptSalesOrder s
                ON s.SalesOrderId = i.SalesOrderId AND s.VodDate = '3000-01-01'
            WHERE i.EstablishedAt >= @date1 AND i.EstablishedAt < @date2 AND i.VodDate = '3000-01-01'
            """, new { date1, date2 }).ToList();
        var du = conn.Query<UnifiedSalesReportItem>("""
            SELECT 'DU' AS SourceKind, fs_kd_trs AS DocumentId,
                   CONVERT(DATETIME, fd_tgl_trs, 112) AS DocumentDate,
                   ISNULL(fs_nm_pasien,'') AS PasienName, ISNULL(fn_grand_total,0) AS GrandTotal
            FROM tb_trs_dobill_umum
            WHERE CONVERT(DATETIME, fd_tgl_trs, 112) >= @date1
              AND CONVERT(DATETIME, fd_tgl_trs, 112) < @date2
              AND (fd_tgl_void = '' OR fd_tgl_void = '3000-01-01')
            """, new { date1, date2 }).ToList();
        return apt.Concat(du).ToList();
    }

    private PelayananWorklistItem MapPelayanan(PelayananWorklistRow row)
        => new(
            row.AntrianId,
            row.NoUrut,
            (QueueDemandKindEnum)row.DemandKind,
            row.DemandId,
            row.PasienName,
            row.SalesOrderId,
            (PayerPathEnum)row.PayerPath,
            row.InvoiceId,
            row.InvoiceStatus is null ? null : (InvoiceStatusEnum)row.InvoiceStatus,
            AttentionLabel(row));

    private static string AttentionLabel(PelayananWorklistRow row)
    {
        if (string.IsNullOrWhiteSpace(row.DemandId))
            return "NeedMapping";
        if (string.IsNullOrWhiteSpace(row.SalesOrderId))
            return "NeedSalesOrder";
        return row.InvoiceStatus is null ? "NeedInvoiceOrCoverage" : "";
    }

    private sealed record PelayananWorklistRow(
        string AntrianId, int NoUrut, int DemandKind, string DemandId, string PasienName, string SalesOrderId, int PayerPath,
        string InvoiceId, int? InvoiceStatus);
    private sealed record SerahRow(string DispensingId, string SalesOrderId, int DispensingStatus, DateTime PreparedAt, DateTime PickupCalledAt, DateTime EducationAt, DateTime HandoverAt, DateTime OverrideAt);
    private sealed record QueueMappingDtoLite(int DemandKind, string DemandId);
    private sealed record TelaahLite(string TelaahResepId, int TelaahStatus);
    private sealed record SalesOrderLite(string SalesOrderId, int PayerPath);
    private sealed record JourneyTaskRow(string IntegrationTaskId, string SourceId, int TaskStatus, string LastError);
}

public class AptIntegrationOpsDal : IAptIntegrationOpsDal
{
    private readonly DatabaseOptions _opt;
    public AptIntegrationOpsDal(IOptions<DatabaseOptions> opt) => _opt = opt.Value;
    public IReadOnlyList<AptIntegrationFailureItem> List(AptIntegrationFailureQuery filter)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<AptIntegrationFailureItem>("""
            SELECT IntegrationTaskId, TaskType, SourceKind, SourceId, TaskStatus AS Status,
                   LastError, CorrelationId, RetryCount
            FROM BILRG_AptIntegrationTask
            WHERE (@TaskType IS NULL OR TaskType = @TaskType)
              AND (@SourceKind IS NULL OR SourceKind = @SourceKind)
              AND (@Status IS NULL OR TaskStatus = @Status)
              AND (@LastErrorContains IS NULL OR LastError LIKE '%' + @LastErrorContains + '%')
            ORDER BY CrtDate DESC
            """, new
            {
                TaskType = filter.TaskType is null ? (int?)null : (int)filter.TaskType,
                SourceKind = filter.SourceKind is null ? (int?)null : (int)filter.SourceKind,
                Status = filter.Status is null ? (int?)null : (int)filter.Status,
                LastErrorContains = string.IsNullOrWhiteSpace(filter.LastErrorContains)
                    ? null
                    : filter.LastErrorContains.Trim()
            }).ToList();
    }
}
