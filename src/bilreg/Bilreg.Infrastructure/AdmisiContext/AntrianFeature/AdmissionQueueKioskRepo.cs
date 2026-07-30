using System.Data.SqlClient;
using System.Text.Json;
using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.AdmisiContext.AntrianFeature;

public sealed class AdmissionQueueKioskDto
{
    public string StationId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int PrinterProxyPort { get; set; }
    public string Notes { get; set; } = string.Empty;
    public long RowVersion { get; set; }
    public string CrtUser { get; set; } = string.Empty;
    public DateTime CrtDate { get; set; }
    public string UpdUser { get; set; } = string.Empty;
    public DateTime UpdDate { get; set; }
}

public sealed class AdmissionKioskServicePointDto
{
    public string StationId { get; set; } = string.Empty;
    public string ServicePointId { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public interface IAdmissionQueueKioskDal
{
    AdmissionQueueKioskDto? GetData(string stationId);
    IReadOnlyList<AdmissionKioskServicePointDto> ListServicePoints(string stationId);
    IReadOnlyList<AdmissionQueueKioskDto> ListAll();
    IReadOnlyList<AdmissionKioskServicePointDto> ListAllServicePoints();
    void Insert(AdmissionQueueKioskDto dto, IReadOnlyList<AdmissionKioskServicePointDto> servicePoints);
    int Update(AdmissionQueueKioskDto dto, long expectedRowVersion, IReadOnlyList<AdmissionKioskServicePointDto> servicePoints);
}

public sealed class AdmissionQueueKioskDal : IAdmissionQueueKioskDal
{
    private readonly DatabaseOptions _opt;
    public AdmissionQueueKioskDal(IOptions<DatabaseOptions> opt) => _opt = opt.Value;

    public AdmissionQueueKioskDto? GetData(string stationId)
    {
        const string sql = """
            SELECT StationId, DisplayName, LocationName, IsActive, PrinterProxyPort,
                   Notes, RowVersion, CrtUser, CrtDate, UpdUser, UpdDate
            FROM BILRG_AdmQueueKiosk WHERE StationId = @StationId
            """;
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.QuerySingleOrDefault<AdmissionQueueKioskDto>(sql, new { StationId = stationId });
    }

    public IReadOnlyList<AdmissionKioskServicePointDto> ListServicePoints(string stationId)
    {
        const string sql = """
            SELECT StationId, ServicePointId, SortOrder
            FROM BILRG_AdmKioskServicePoint
            WHERE StationId = @StationId
            ORDER BY SortOrder, ServicePointId
            """;
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<AdmissionKioskServicePointDto>(sql, new { StationId = stationId }).ToList();
    }

    public IReadOnlyList<AdmissionQueueKioskDto> ListAll()
    {
        const string sql = """
            SELECT StationId, DisplayName, LocationName, IsActive, PrinterProxyPort,
                   Notes, RowVersion, CrtUser, CrtDate, UpdUser, UpdDate
            FROM BILRG_AdmQueueKiosk
            ORDER BY DisplayName
            """;
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<AdmissionQueueKioskDto>(sql).ToList();
    }

    public IReadOnlyList<AdmissionKioskServicePointDto> ListAllServicePoints()
    {
        const string sql = """
            SELECT StationId, ServicePointId, SortOrder
            FROM BILRG_AdmKioskServicePoint
            ORDER BY StationId, SortOrder, ServicePointId
            """;
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<AdmissionKioskServicePointDto>(sql).ToList();
    }

    public void Insert(AdmissionQueueKioskDto dto, IReadOnlyList<AdmissionKioskServicePointDto> servicePoints)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Open();
        using var tx = conn.BeginTransaction();
        const string sql = """
            INSERT INTO BILRG_AdmQueueKiosk
                (StationId, DisplayName, LocationName, IsActive, PrinterProxyPort,
                 Notes, RowVersion, CrtUser, CrtDate, UpdUser, UpdDate)
            VALUES
                (@StationId, @DisplayName, @LocationName, @IsActive, @PrinterProxyPort,
                 @Notes, @RowVersion, @CrtUser, @CrtDate, @UpdUser, @UpdDate)
            """;
        conn.Execute(sql, dto, tx);
        ReplaceServicePoints(conn, tx, dto.StationId, servicePoints);
        tx.Commit();
    }

    public int Update(
        AdmissionQueueKioskDto dto,
        long expectedRowVersion,
        IReadOnlyList<AdmissionKioskServicePointDto> servicePoints)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Open();
        using var tx = conn.BeginTransaction();
        const string sql = """
            UPDATE BILRG_AdmQueueKiosk
            SET DisplayName=@DisplayName, LocationName=@LocationName, IsActive=@IsActive,
                PrinterProxyPort=@PrinterProxyPort, Notes=@Notes, RowVersion=@RowVersion,
                UpdUser=@UpdUser, UpdDate=@UpdDate
            WHERE StationId=@StationId AND RowVersion=@ExpectedRowVersion
            """;
        var affected = conn.Execute(sql, new
        {
            dto.StationId,
            dto.DisplayName,
            dto.LocationName,
            dto.IsActive,
            dto.PrinterProxyPort,
            dto.Notes,
            dto.RowVersion,
            dto.UpdUser,
            dto.UpdDate,
            ExpectedRowVersion = expectedRowVersion
        }, tx);
        if (affected == 1)
            ReplaceServicePoints(conn, tx, dto.StationId, servicePoints);
        tx.Commit();
        return affected;
    }

    private static void ReplaceServicePoints(
        SqlConnection conn,
        SqlTransaction tx,
        string stationId,
        IReadOnlyList<AdmissionKioskServicePointDto> servicePoints)
    {
        conn.Execute(
            "DELETE FROM BILRG_AdmKioskServicePoint WHERE StationId=@StationId",
            new { StationId = stationId },
            tx);
        const string sql = """
            INSERT INTO BILRG_AdmKioskServicePoint (StationId, ServicePointId, SortOrder)
            VALUES (@StationId, @ServicePointId, @SortOrder)
            """;
        foreach (var servicePoint in servicePoints)
            conn.Execute(sql, servicePoint, tx);
    }
}

public sealed class AdmissionQueueKioskRepo : IAdmissionQueueKioskRepo
{
    private readonly IAdmissionQueueKioskDal _dal;
    private readonly IAuditRepo _auditRepo;

    public AdmissionQueueKioskRepo(IAdmissionQueueKioskDal dal, IAuditRepo auditRepo)
    {
        _dal = dal;
        _auditRepo = auditRepo;
    }

    public MayBe<AdmissionQueueKioskModel> LoadEntity(IAdmissionQueueKioskKey key)
    {
        var dto = _dal.GetData(key.StationId);
        if (dto is null) return MayBe<AdmissionQueueKioskModel>.None;
        return MayBe.From(ToModel(dto, _dal.ListServicePoints(key.StationId)));
    }

    public void SaveChanges(AdmissionQueueKioskModel model)
    {
        var existing = _dal.GetData(model.StationId);
        var dto = ToDto(model);
        var mappings = model.ServicePoints.Select(x => new AdmissionKioskServicePointDto
        {
            StationId = model.StationId,
            ServicePointId = x.ServicePointId,
            SortOrder = x.SortOrder
        }).ToList();

        if (existing is null)
        {
            _dal.Insert(dto, mappings);
            WriteAudit("CREATE", model.StationId, model.UpdatedBy, null, model);
            return;
        }

        var before = ToModel(existing, _dal.ListServicePoints(model.StationId));
        var affected = _dal.Update(dto, model.RowVersion - 1, mappings);
        if (affected == 0)
            throw new AdmissionQueueConfigurationException(
                AdmissionQueueConfigurationErrorCodes.Concurrency,
                "Kiosk was changed by another user.");

        var mappingChanged = !before.ServicePoints.Select(x => x.ServicePointId)
            .SequenceEqual(model.ServicePoints.Select(x => x.ServicePointId));
        var action = mappingChanged
            ? "MAPPING_REPLACE"
            : model.Active == before.Active ? "UPDATE" : model.Active ? "ACTIVATE" : "DEACTIVATE";
        WriteAudit(action, model.StationId, model.UpdatedBy, before, model);
    }

    public IReadOnlyList<AdmissionQueueKioskModel> ListAll()
    {
        var mappings = _dal.ListAllServicePoints()
            .GroupBy(x => x.StationId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<AdmissionKioskServicePointDto>)g.ToList(),
                StringComparer.OrdinalIgnoreCase);
        return _dal.ListAll().Select(x => ToModel(
            x,
            mappings.TryGetValue(x.StationId, out var list)
                ? list
                : Array.Empty<AdmissionKioskServicePointDto>())).ToList();
    }

    private void WriteAudit(
        string action,
        string entityId,
        string userId,
        AdmissionQueueKioskModel? before,
        AdmissionQueueKioskModel after)
    {
        var audit = AuditLog.Create(
            userId,
            after.UpdatedAt,
            action,
            nameof(AdmissionQueueKioskModel),
            entityId,
            reason: null,
            originalDataJson: JsonSerializer.Serialize(new { before, after }));
        _auditRepo.SaveChanges(audit);
    }

    private static AdmissionQueueKioskModel ToModel(
        AdmissionQueueKioskDto dto,
        IReadOnlyList<AdmissionKioskServicePointDto> servicePoints) =>
        AdmissionQueueKioskModel.Load(
            dto.StationId,
            dto.DisplayName,
            dto.LocationName,
            dto.IsActive,
            dto.PrinterProxyPort,
            dto.Notes,
            servicePoints.Select(x => new AdmissionKioskServicePointMapping(x.ServicePointId, x.SortOrder)),
            dto.RowVersion,
            dto.CrtUser,
            dto.CrtDate,
            dto.UpdUser,
            dto.UpdDate);

    private static AdmissionQueueKioskDto ToDto(AdmissionQueueKioskModel model) => new()
    {
        StationId = model.StationId,
        DisplayName = model.DisplayName,
        LocationName = model.LocationName,
        IsActive = model.Active,
        PrinterProxyPort = model.PrinterProxyPort,
        Notes = model.Notes,
        RowVersion = model.RowVersion,
        CrtUser = model.CreatedBy,
        CrtDate = model.CreatedAt,
        UpdUser = model.UpdatedBy,
        UpdDate = model.UpdatedAt
    };
}
