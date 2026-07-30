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

public sealed class AdmissionQueueDisplayDto
{
    public string DisplayId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool AudioEnabled { get; set; }
    public int PollIntervalMs { get; set; }
    public string LayoutKey { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public long RowVersion { get; set; }
    public string CrtUser { get; set; } = string.Empty;
    public DateTime CrtDate { get; set; }
    public string UpdUser { get; set; } = string.Empty;
    public DateTime UpdDate { get; set; }
}

public sealed class AdmissionDisplayLoketDto
{
    public string DisplayId { get; set; } = string.Empty;
    public string LoketKey { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public interface IAdmissionQueueDisplayDal
{
    AdmissionQueueDisplayDto? GetData(string displayId);
    IReadOnlyList<AdmissionDisplayLoketDto> ListLokets(string displayId);
    IReadOnlyList<AdmissionQueueDisplayDto> ListAll();
    IReadOnlyList<AdmissionDisplayLoketDto> ListAllLokets();
    void Insert(AdmissionQueueDisplayDto dto, IReadOnlyList<AdmissionDisplayLoketDto> lokets);
    int Update(AdmissionQueueDisplayDto dto, long expectedRowVersion, IReadOnlyList<AdmissionDisplayLoketDto> lokets);
}

public sealed class AdmissionQueueDisplayDal : IAdmissionQueueDisplayDal
{
    private readonly DatabaseOptions _opt;
    public AdmissionQueueDisplayDal(IOptions<DatabaseOptions> opt) => _opt = opt.Value;

    public AdmissionQueueDisplayDto? GetData(string displayId)
    {
        const string sql = """
            SELECT DisplayId, DisplayName, LocationName, IsActive, AudioEnabled, PollIntervalMs,
                   LayoutKey, Notes, RowVersion, CrtUser, CrtDate, UpdUser, UpdDate
            FROM BILRG_AdmQueueDisplay WHERE DisplayId = @DisplayId
            """;
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.QuerySingleOrDefault<AdmissionQueueDisplayDto>(sql, new { DisplayId = displayId });
    }

    public IReadOnlyList<AdmissionDisplayLoketDto> ListLokets(string displayId)
    {
        const string sql = """
            SELECT DisplayId, LoketKey, SortOrder
            FROM BILRG_AdmDisplayLoket WHERE DisplayId = @DisplayId
            ORDER BY SortOrder, LoketKey
            """;
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<AdmissionDisplayLoketDto>(sql, new { DisplayId = displayId }).ToList();
    }

    public IReadOnlyList<AdmissionQueueDisplayDto> ListAll()
    {
        const string sql = """
            SELECT DisplayId, DisplayName, LocationName, IsActive, AudioEnabled, PollIntervalMs,
                   LayoutKey, Notes, RowVersion, CrtUser, CrtDate, UpdUser, UpdDate
            FROM BILRG_AdmQueueDisplay ORDER BY DisplayName
            """;
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<AdmissionQueueDisplayDto>(sql).ToList();
    }

    public IReadOnlyList<AdmissionDisplayLoketDto> ListAllLokets()
    {
        const string sql = """
            SELECT DisplayId, LoketKey, SortOrder
            FROM BILRG_AdmDisplayLoket ORDER BY DisplayId, SortOrder, LoketKey
            """;
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<AdmissionDisplayLoketDto>(sql).ToList();
    }

    public void Insert(AdmissionQueueDisplayDto dto, IReadOnlyList<AdmissionDisplayLoketDto> lokets)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Open();
        using var tx = conn.BeginTransaction();
        const string insertDisplay = """
            INSERT INTO BILRG_AdmQueueDisplay
                (DisplayId, DisplayName, LocationName, IsActive, AudioEnabled, PollIntervalMs,
                 LayoutKey, Notes, RowVersion, CrtUser, CrtDate, UpdUser, UpdDate)
            VALUES
                (@DisplayId, @DisplayName, @LocationName, @IsActive, @AudioEnabled, @PollIntervalMs,
                 @LayoutKey, @Notes, @RowVersion, @CrtUser, @CrtDate, @UpdUser, @UpdDate)
            """;
        conn.Execute(insertDisplay, dto, tx);
        ReplaceLokets(conn, tx, dto.DisplayId, lokets);
        tx.Commit();
    }

    public int Update(AdmissionQueueDisplayDto dto, long expectedRowVersion, IReadOnlyList<AdmissionDisplayLoketDto> lokets)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Open();
        using var tx = conn.BeginTransaction();
        const string updateDisplay = """
            UPDATE BILRG_AdmQueueDisplay
            SET DisplayName=@DisplayName, LocationName=@LocationName, IsActive=@IsActive,
                AudioEnabled=@AudioEnabled, PollIntervalMs=@PollIntervalMs, LayoutKey=@LayoutKey,
                Notes=@Notes, RowVersion=@RowVersion, UpdUser=@UpdUser, UpdDate=@UpdDate
            WHERE DisplayId=@DisplayId AND RowVersion=@ExpectedRowVersion
            """;
        var affected = conn.Execute(updateDisplay, new
        {
            dto.DisplayId,
            dto.DisplayName,
            dto.LocationName,
            dto.IsActive,
            dto.AudioEnabled,
            dto.PollIntervalMs,
            dto.LayoutKey,
            dto.Notes,
            dto.RowVersion,
            dto.UpdUser,
            dto.UpdDate,
            ExpectedRowVersion = expectedRowVersion
        }, tx);
        if (affected == 1)
            ReplaceLokets(conn, tx, dto.DisplayId, lokets);
        tx.Commit();
        return affected;
    }

    private static void ReplaceLokets(
        SqlConnection conn,
        SqlTransaction tx,
        string displayId,
        IReadOnlyList<AdmissionDisplayLoketDto> lokets)
    {
        conn.Execute("DELETE FROM BILRG_AdmDisplayLoket WHERE DisplayId=@DisplayId",
            new { DisplayId = displayId }, tx);
        const string insertLoket = """
            INSERT INTO BILRG_AdmDisplayLoket (DisplayId, LoketKey, SortOrder)
            VALUES (@DisplayId, @LoketKey, @SortOrder)
            """;
        foreach (var loket in lokets)
            conn.Execute(insertLoket, loket, tx);
    }
}

public sealed class AdmissionQueueDisplayRepo : IAdmissionQueueDisplayRepo
{
    private readonly IAdmissionQueueDisplayDal _dal;
    private readonly IAuditRepo _auditRepo;

    public AdmissionQueueDisplayRepo(IAdmissionQueueDisplayDal dal, IAuditRepo auditRepo)
    {
        _dal = dal;
        _auditRepo = auditRepo;
    }

    public MayBe<AdmissionQueueDisplayModel> LoadEntity(IAdmissionQueueDisplayKey key)
    {
        var dto = _dal.GetData(key.DisplayId);
        if (dto is null) return MayBe<AdmissionQueueDisplayModel>.None;
        var lokets = _dal.ListLokets(key.DisplayId);
        return MayBe.From(ToModel(dto, lokets));
    }

    public void SaveChanges(AdmissionQueueDisplayModel model)
    {
        var existing = _dal.GetData(model.DisplayId);
        var dto = ToDto(model);
        var loketDtos = model.Lokets
            .Select(x => new AdmissionDisplayLoketDto
            {
                DisplayId = model.DisplayId,
                LoketKey = x.LoketKey,
                SortOrder = x.SortOrder
            })
            .ToList();

        if (existing is null)
        {
            _dal.Insert(dto, loketDtos);
            WriteAudit("CREATE", model.DisplayId, model.UpdatedBy, null, model);
            return;
        }

        var beforeLokets = _dal.ListLokets(model.DisplayId);
        var before = ToModel(existing, beforeLokets);
        var expected = model.RowVersion - 1;
        var affected = _dal.Update(dto, expected, loketDtos);
        if (affected == 0)
            throw new AdmissionQueueConfigurationException(
                AdmissionQueueConfigurationErrorCodes.Concurrency,
                "Display was changed by another user.");

        var action = before.Lokets.Select(x => x.LoketKey).SequenceEqual(model.Lokets.Select(x => x.LoketKey))
            ? (model.Active == before.Active ? "UPDATE" : model.Active ? "ACTIVATE" : "DEACTIVATE")
            : "MAPPING_REPLACE";
        WriteAudit(action, model.DisplayId, model.UpdatedBy, before, model);
    }

    public IReadOnlyList<AdmissionQueueDisplayModel> ListAll()
    {
        var displays = _dal.ListAll();
        var lokets = _dal.ListAllLokets().GroupBy(x => x.DisplayId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<AdmissionDisplayLoketDto>)g.ToList(),
                StringComparer.OrdinalIgnoreCase);
        return displays.Select(d => ToModel(d,
            lokets.TryGetValue(d.DisplayId, out var list) ? list : Array.Empty<AdmissionDisplayLoketDto>()))
            .ToList();
    }

    private void WriteAudit(
        string action,
        string entityId,
        string userId,
        AdmissionQueueDisplayModel? before,
        AdmissionQueueDisplayModel after)
    {
        var payload = JsonSerializer.Serialize(new { before, after });
        var audit = AuditLog.Create(
            userId,
            after.UpdatedAt,
            action,
            nameof(AdmissionQueueDisplayModel),
            entityId,
            reason: null,
            originalDataJson: payload);
        _auditRepo.SaveChanges(audit);
    }

    private static AdmissionQueueDisplayModel ToModel(
        AdmissionQueueDisplayDto dto,
        IReadOnlyList<AdmissionDisplayLoketDto> lokets) =>
        AdmissionQueueDisplayModel.Load(
            dto.DisplayId, dto.DisplayName, dto.LocationName, dto.IsActive, dto.AudioEnabled,
            dto.PollIntervalMs, dto.LayoutKey, dto.Notes,
            lokets.Select(x => new AdmissionDisplayLoketMapping(x.LoketKey, x.SortOrder)),
            dto.RowVersion, dto.CrtUser, dto.CrtDate, dto.UpdUser, dto.UpdDate);

    private static AdmissionQueueDisplayDto ToDto(AdmissionQueueDisplayModel model) => new()
    {
        DisplayId = model.DisplayId,
        DisplayName = model.DisplayName,
        LocationName = model.LocationName,
        IsActive = model.Active,
        AudioEnabled = model.AudioEnabled,
        PollIntervalMs = model.PollIntervalMs,
        LayoutKey = model.LayoutKey,
        Notes = model.Notes,
        RowVersion = model.RowVersion,
        CrtUser = model.CreatedBy,
        CrtDate = model.CreatedAt,
        UpdUser = model.UpdatedBy,
        UpdDate = model.UpdatedAt
    };
}
