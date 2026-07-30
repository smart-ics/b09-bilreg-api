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

public sealed class AdmissionWorkstationDto
{
    public string WorkstationKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public string LoketKey { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string Notes { get; set; } = string.Empty;
    public long RowVersion { get; set; }
    public string CrtUser { get; set; } = string.Empty;
    public DateTime CrtDate { get; set; }
    public string UpdUser { get; set; } = string.Empty;
    public DateTime UpdDate { get; set; }
}

public interface IAdmissionWorkstationDal
{
    AdmissionWorkstationDto? GetData(string workstationKey);
    void Insert(AdmissionWorkstationDto dto);
    int Update(AdmissionWorkstationDto dto, long expectedRowVersion);
    IReadOnlyList<AdmissionWorkstationDto> ListAll();
    AdmissionWorkstationDto? FindActiveByLoketKey(string loketKey, string? excludeWorkstationKey);
}

public sealed class AdmissionWorkstationDal : IAdmissionWorkstationDal
{
    private readonly DatabaseOptions _opt;
    public AdmissionWorkstationDal(IOptions<DatabaseOptions> opt) => _opt = opt.Value;

    public AdmissionWorkstationDto? GetData(string workstationKey)
    {
        const string sql = """
            SELECT WorkstationKey, DisplayName, LocationName, LoketKey, IsActive, Notes, RowVersion,
                   CrtUser, CrtDate, UpdUser, UpdDate
            FROM BILRG_AdmWorkstation WHERE WorkstationKey = @WorkstationKey
            """;
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.QuerySingleOrDefault<AdmissionWorkstationDto>(sql, new { WorkstationKey = workstationKey });
    }

    public void Insert(AdmissionWorkstationDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_AdmWorkstation
                (WorkstationKey, DisplayName, LocationName, LoketKey, IsActive, Notes, RowVersion,
                 CrtUser, CrtDate, UpdUser, UpdDate)
            VALUES
                (@WorkstationKey, @DisplayName, @LocationName, @LoketKey, @IsActive, @Notes, @RowVersion,
                 @CrtUser, @CrtDate, @UpdUser, @UpdDate)
            """;
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dto);
    }

    public int Update(AdmissionWorkstationDto dto, long expectedRowVersion)
    {
        const string sql = """
            UPDATE BILRG_AdmWorkstation
            SET DisplayName=@DisplayName, LocationName=@LocationName, LoketKey=@LoketKey,
                IsActive=@IsActive, Notes=@Notes, RowVersion=@RowVersion,
                UpdUser=@UpdUser, UpdDate=@UpdDate
            WHERE WorkstationKey=@WorkstationKey AND RowVersion=@ExpectedRowVersion
            """;
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Execute(sql, new
        {
            dto.WorkstationKey,
            dto.DisplayName,
            dto.LocationName,
            dto.LoketKey,
            dto.IsActive,
            dto.Notes,
            dto.RowVersion,
            dto.UpdUser,
            dto.UpdDate,
            ExpectedRowVersion = expectedRowVersion
        });
    }

    public IReadOnlyList<AdmissionWorkstationDto> ListAll()
    {
        const string sql = """
            SELECT WorkstationKey, DisplayName, LocationName, LoketKey, IsActive, Notes, RowVersion,
                   CrtUser, CrtDate, UpdUser, UpdDate
            FROM BILRG_AdmWorkstation ORDER BY DisplayName
            """;
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<AdmissionWorkstationDto>(sql).ToList();
    }

    public AdmissionWorkstationDto? FindActiveByLoketKey(string loketKey, string? excludeWorkstationKey)
    {
        const string sql = """
            SELECT TOP 1 WorkstationKey, DisplayName, LocationName, LoketKey, IsActive, Notes, RowVersion,
                   CrtUser, CrtDate, UpdUser, UpdDate
            FROM BILRG_AdmWorkstation
            WHERE IsActive = 1 AND LoketKey = @LoketKey
              AND (@ExcludeWorkstationKey IS NULL OR WorkstationKey <> @ExcludeWorkstationKey)
            """;
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.QuerySingleOrDefault<AdmissionWorkstationDto>(sql,
            new { LoketKey = loketKey, ExcludeWorkstationKey = excludeWorkstationKey });
    }
}

public sealed class AdmissionWorkstationRepo : IAdmissionWorkstationRepo
{
    private readonly IAdmissionWorkstationDal _dal;
    private readonly IAuditRepo _auditRepo;

    public AdmissionWorkstationRepo(IAdmissionWorkstationDal dal, IAuditRepo auditRepo)
    {
        _dal = dal;
        _auditRepo = auditRepo;
    }

    public MayBe<AdmissionWorkstationModel> LoadEntity(IAdmissionWorkstationKey key)
    {
        var dto = _dal.GetData(key.WorkstationKey);
        return dto is null ? MayBe<AdmissionWorkstationModel>.None : MayBe.From(ToModel(dto));
    }

    public void SaveChanges(AdmissionWorkstationModel model)
    {
        var existing = _dal.GetData(model.WorkstationKey);
        var dto = ToDto(model);
        if (existing is null)
        {
            _dal.Insert(dto);
            WriteAudit("CREATE", model.WorkstationKey, model.UpdatedBy, null, model);
            return;
        }

        var before = ToModel(existing);
        var expected = model.RowVersion - 1;
        var affected = _dal.Update(dto, expected);
        if (affected == 0)
            throw new AdmissionQueueConfigurationException(
                AdmissionQueueConfigurationErrorCodes.Concurrency,
                "Workstation was changed by another user.");
        WriteAudit(model.Active == before.Active ? "UPDATE" : model.Active ? "ACTIVATE" : "DEACTIVATE",
            model.WorkstationKey, model.UpdatedBy, before, model);
    }

    public IReadOnlyList<AdmissionWorkstationModel> ListAll() =>
        _dal.ListAll().Select(ToModel).ToList();

    public AdmissionWorkstationModel? FindActiveByLoketKey(string loketKey, string? excludeWorkstationKey = null)
    {
        var dto = _dal.FindActiveByLoketKey(loketKey, excludeWorkstationKey);
        return dto is null ? null : ToModel(dto);
    }

    private void WriteAudit(
        string action,
        string entityId,
        string userId,
        AdmissionWorkstationModel? before,
        AdmissionWorkstationModel after)
    {
        var payload = JsonSerializer.Serialize(new { before, after });
        var audit = AuditLog.Create(
            userId,
            after.UpdatedAt,
            action,
            nameof(AdmissionWorkstationModel),
            entityId,
            reason: null,
            originalDataJson: payload);
        _auditRepo.SaveChanges(audit);
    }

    private static AdmissionWorkstationModel ToModel(AdmissionWorkstationDto dto) =>
        AdmissionWorkstationModel.Load(
            dto.WorkstationKey, dto.DisplayName, dto.LocationName, dto.LoketKey, dto.IsActive,
            dto.Notes, dto.RowVersion, dto.CrtUser, dto.CrtDate, dto.UpdUser, dto.UpdDate);

    private static AdmissionWorkstationDto ToDto(AdmissionWorkstationModel model) => new()
    {
        WorkstationKey = model.WorkstationKey,
        DisplayName = model.DisplayName,
        LocationName = model.LocationName,
        LoketKey = model.LoketKey,
        IsActive = model.Active,
        Notes = model.Notes,
        RowVersion = model.RowVersion,
        CrtUser = model.CreatedBy,
        CrtDate = model.CreatedAt,
        UpdUser = model.UpdatedBy,
        UpdDate = model.UpdatedAt
    };
}
