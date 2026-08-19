using Bilreg.Application.ApotekContext.IntegrationFeature.UseCases;
using Bilreg.Application.ApotekContext.WorklistFeature;
using Bilreg.Domain.ApotekContext.DispensingFeature;
using Bilreg.Domain.ApotekContext.InvoiceFeature;
using Bilreg.Domain.ApotekContext.QueueFeature;
using Bilreg.Domain.ApotekContext.SalesOrderFeature;
using Bilreg.Domain.ApotekContext.TelaahResepFeature;
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
            SELECT t.TelaahResepId, t.ResepKerjaId, r.PasienName, t.TelaahStatus AS Status, t.StartedAt
            FROM BILRG_AptTelaahResep t
            INNER JOIN BILRG_AptResepKerja r ON r.ResepKerjaId = t.ResepKerjaId
            WHERE t.TelaahStatus IN (0,1) AND t.VodDate = '3000-01-01'
            ORDER BY t.StartedAt
            """).ToList();
    }

    public IReadOnlyList<PelayananWorklistItem> ListPelayanan(string antrianId, int? noUrut)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<PelayananWorklistRow>("""
            SELECT m.AntrianId, m.NoUrut, m.DemandKind, m.DemandId,
                   COALESCE(r.PasienName, j.PasienName, '') AS PasienName,
                   ISNULL(s.SalesOrderId, '') AS SalesOrderId,
                   ISNULL(s.PayerPath, 0) AS PayerPath,
                   ISNULL(i.InvoiceId, '') AS InvoiceId,
                   i.InvoiceStatus AS InvoiceStatus
            FROM BILRG_AptQueueMapping m
            LEFT JOIN BILRG_AptResepKerja r ON m.DemandKind = 0 AND r.ResepKerjaId = m.DemandId
            LEFT JOIN BILRG_AptJualBebas j ON m.DemandKind = 1 AND j.JualBebasId = m.DemandId
            LEFT JOIN BILRG_AptSalesOrder s ON s.SourceId = m.DemandId AND s.SourceKind = m.DemandKind AND s.VodDate = '3000-01-01'
            LEFT JOIN BILRG_AptInvoice i ON i.SalesOrderId = s.SalesOrderId AND i.VodDate = '3000-01-01'
            WHERE (@antrianId = '' OR m.AntrianId = @antrianId)
              AND (@noUrut IS NULL OR m.NoUrut = @noUrut)
            """, new { antrianId = antrianId ?? "", noUrut }).Select(x => new PelayananWorklistItem(
                x.AntrianId, x.NoUrut, (QueueDemandKindEnum)x.DemandKind, x.DemandId, x.PasienName, x.SalesOrderId,
                (PayerPathEnum)x.PayerPath, x.InvoiceId,
                x.InvoiceStatus is null ? null : (InvoiceStatusEnum)x.InvoiceStatus,
                AttentionLabel(x))).ToList();
    }

    public IReadOnlyList<DispensingWorklistItem> ListDispensing()
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<DispensingWorklistItem>("""
            SELECT DispensingId, SalesOrderId, DispensingStatus AS Status, PreparationStartedAt
            FROM BILRG_AptDispensing
            WHERE DispensingStatus IN (2,3) AND VodDate = '3000-01-01'
            """).ToList();
    }

    public IReadOnlyList<SerahWorklistItem> ListSerah(DateTime asOf, int collectionWindowDays)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var rows = conn.Query<SerahRow>("""
            SELECT DispensingId, SalesOrderId, DispensingStatus, PreparedAt, PickupCalledAt, EducationAt, HandoverAt, OverrideAt
            FROM BILRG_AptDispensing
            WHERE DispensingStatus IN (4,5,7) AND VodDate = '3000-01-01'
            """).ToList();
        return rows.Select(x => new SerahWorklistItem(x.DispensingId, x.SalesOrderId, Category(x, asOf, collectionWindowDays), x.PreparedAt, x.PickupCalledAt)).ToList();
    }

    public JourneyResponse LoadJourney(string antrianId, int noUrut)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var maps = conn.Query<QueueMappingDtoLite>(
            "SELECT DemandKind, DemandId FROM BILRG_AptQueueMapping WHERE AntrianId=@antrianId AND NoUrut=@noUrut",
            new { antrianId, noUrut }).ToList();
        var pending = conn.Query<string>("""
            SELECT SourceId FROM BILRG_AptIntegrationTask
            WHERE TaskStatus IN (0,2,4)
            """).ToList();
        var demands = new List<JourneyDemand>();
        foreach (var map in maps)
        {
            var orders = conn.Query<string>(
                "SELECT SalesOrderId FROM BILRG_AptSalesOrder WHERE SourceKind=@kind AND SourceId=@id AND VodDate='3000-01-01'",
                new { kind = map.DemandKind, id = map.DemandId }).ToList();
            var invoices = orders.Count == 0 ? [] : conn.Query<string>(
                "SELECT InvoiceId FROM BILRG_AptInvoice WHERE SalesOrderId IN @orders", new { orders }).ToList();
            var dispensings = orders.Count == 0 ? [] : conn.Query<string>(
                "SELECT DispensingId FROM BILRG_AptDispensing WHERE SalesOrderId IN @orders", new { orders }).ToList();
            demands.Add(new JourneyDemand((QueueDemandKindEnum)map.DemandKind, map.DemandId, orders, invoices, dispensings,
                pending.Where(p => orders.Contains(p) || invoices.Contains(p) || dispensings.Contains(p) || p == map.DemandId).ToList()));
        }
        return new JourneyResponse(antrianId, noUrut, demands);
    }

    public IReadOnlyList<UnifiedSalesReportItem> ListUnifiedSales(DateTime date1, DateTime date2)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var apt = conn.Query<UnifiedSalesReportItem>("""
            SELECT 'APT' AS SourceKind, InvoiceId AS DocumentId, EstablishedAt AS DocumentDate,
                   '' AS PasienName, GrandTotal
            FROM BILRG_AptInvoice
            WHERE EstablishedAt >= @date1 AND EstablishedAt < @date2 AND VodDate = '3000-01-01'
            """, new { date1, date2 }).ToList();
        var du = conn.Query<UnifiedSalesReportItem>("""
            SELECT 'DU' AS SourceKind, fs_kd_trs AS DocumentId, fd_tgl_trs AS DocumentDate,
                   ISNULL(fs_nm_pasien,'') AS PasienName, ISNULL(fn_grandtotal,0) AS GrandTotal
            FROM tb_trs_dobill_umum
            WHERE fd_tgl_trs >= @date1 AND fd_tgl_trs < @date2
            """, new { date1, date2 }).ToList();
        return apt.Concat(du).ToList();
    }

    private static string AttentionLabel(PelayananWorklistRow row)
        => string.IsNullOrWhiteSpace(row.SalesOrderId) ? "NeedSalesOrder" : row.InvoiceStatus is null ? "NeedInvoiceOrCoverage" : "";

    private static string Category(SerahRow x, DateTime asOf, int days)
    {
        if (x.DispensingStatus == (int)DispensingStatusEnum.Completed) return "Completed";
        if (x.HandoverAt < new DateTime(2999, 1, 1)) return "Completed";
        if (x.EducationAt < new DateTime(2999, 1, 1) && x.PickupCalledAt < new DateTime(2999, 1, 1)) return "ReadyForHandover";
        if (x.PickupCalledAt < new DateTime(2999, 1, 1)) return "ReadyForReview";
        if (x.OverrideAt >= new DateTime(2999, 1, 1) && asOf > x.PreparedAt.AddDays(days)) return "PickupExpired";
        return "ReadyForPickup";
    }

    private sealed record PelayananWorklistRow(
        string AntrianId, int NoUrut, int DemandKind, string DemandId, string PasienName, string SalesOrderId, int PayerPath,
        string InvoiceId, int? InvoiceStatus);
    private sealed record SerahRow(string DispensingId, string SalesOrderId, int DispensingStatus, DateTime PreparedAt, DateTime PickupCalledAt, DateTime EducationAt, DateTime HandoverAt, DateTime OverrideAt);
    private sealed record QueueMappingDtoLite(int DemandKind, string DemandId);
}

public class AptIntegrationOpsDal : IAptIntegrationOpsDal
{
    private readonly DatabaseOptions _opt;
    public AptIntegrationOpsDal(IOptions<DatabaseOptions> opt) => _opt = opt.Value;
    public IReadOnlyList<AptIntegrationFailureItem> List(AptIntegrationFailureQuery filter)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<AptIntegrationFailureItem>("""
            SELECT IntegrationTaskId, TaskType, SourceId, TaskStatus AS Status, LastError, RetryCount
            FROM BILRG_AptIntegrationTask
            WHERE (@TaskType IS NULL OR TaskType = @TaskType)
              AND (@SourceKind IS NULL OR SourceKind = @SourceKind)
              AND (@Status IS NULL OR TaskStatus = @Status)
            ORDER BY CrtDate DESC
            """, new { TaskType = filter.TaskType is null ? (int?)null : (int)filter.TaskType,
                SourceKind = filter.SourceKind is null ? (int?)null : (int)filter.SourceKind,
                Status = filter.Status is null ? (int?)null : (int)filter.Status }).ToList();
    }
}
