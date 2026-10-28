using System.Data.SqlClient;
using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;

namespace Bilreg.Infrastructure.AdmisiContext.AntrianFeature;

public sealed class ClinicQueueOperationDal : IClinicQueueOperationDal
{
    private readonly DatabaseOptions _opt;

    public ClinicQueueOperationDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    private SqlConnection Open() => new(ConnStringHelper.Get(_opt));

    public void UpsertCall(ClinicQueueCallDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var now = DateTime.Now;
        var crtDate = dto.CrtDate == default ? now : dto.CrtDate;
        var updDate = dto.UpdDate == default ? now : dto.UpdDate;
        var calledAt = dto.CalledAt == default ? now : dto.CalledAt;

        const string sql = """
            UPDATE BILRG_ClinicQueueCall
            SET RuangId = @RuangId,
                AntrianId = @AntrianId,
                NoUrut = @NoUrut,
                CalledAt = @CalledAt,
                CallCount = @CallCount,
                CallStatus = @CallStatus,
                UserId = @UserId,
                UpdDate = @UpdDate
            WHERE PoliId = @PoliId AND DokterId = @DokterId;

            IF @@ROWCOUNT = 0
            BEGIN
                INSERT INTO BILRG_ClinicQueueCall
                    (PoliId, DokterId, RuangId, AntrianId, NoUrut, CalledAt, CallCount, CallStatus, UserId, CrtDate, UpdDate)
                VALUES
                    (@PoliId, @DokterId, @RuangId, @AntrianId, @NoUrut, @CalledAt, @CallCount, @CallStatus, @UserId, @CrtDate, @UpdDate);
            END
            """;

        using var conn = Open();
        conn.Execute(sql, new
        {
            PoliId = dto.PoliId ?? string.Empty,
            DokterId = dto.DokterId ?? string.Empty,
            RuangId = dto.RuangId ?? string.Empty,
            AntrianId = dto.AntrianId ?? string.Empty,
            NoUrut = dto.NoUrut,
            CalledAt = calledAt,
            CallCount = dto.CallCount,
            CallStatus = dto.CallStatus,
            UserId = dto.UserId ?? string.Empty,
            CrtDate = crtDate,
            UpdDate = updDate
        });
    }

    public bool MarkSkipped(string poliId, string dokterId, string userId)
    {
        const string sql = """
            UPDATE BILRG_ClinicQueueCall
            SET CallStatus = @CallStatus,
                UserId = @UserId,
                UpdDate = @UpdDate
            WHERE PoliId = @PoliId AND DokterId = @DokterId AND CallStatus = 0;
            """;

        using var conn = Open();
        var affected = conn.Execute(sql, new
        {
            PoliId = poliId ?? string.Empty,
            DokterId = dokterId ?? string.Empty,
            CallStatus = ClinicQueueCallStatus.Skipped,
            UserId = userId ?? string.Empty,
            UpdDate = DateTime.Now
        });
        return affected > 0;
    }

    public bool DismissCall(string poliId, string dokterId, string userId)
    {
        const string sql = """
            UPDATE BILRG_ClinicQueueCall
            SET CallStatus = @CallStatus,
                UserId = @UserId,
                UpdDate = @UpdDate
            WHERE PoliId = @PoliId AND DokterId = @DokterId AND CallStatus <> @CallStatus;
            """;

        using var conn = Open();
        var affected = conn.Execute(sql, new
        {
            PoliId = poliId ?? string.Empty,
            DokterId = dokterId ?? string.Empty,
            CallStatus = ClinicQueueCallStatus.Dismissed,
            UserId = userId ?? string.Empty,
            UpdDate = DateTime.Now
        });
        return affected > 0;
    }

    public ClinicQueueCallDto? GetActiveCall(string poliId, string dokterId)
    {
        const string sql = """
            SELECT PoliId, DokterId, RuangId, AntrianId, NoUrut, CalledAt, CallCount, CallStatus, UserId, CrtDate, UpdDate
            FROM BILRG_ClinicQueueCall
            WHERE PoliId = @PoliId AND DokterId = @DokterId AND CallStatus = 0;
            """;

        using var conn = Open();
        return conn.QuerySingleOrDefault<ClinicQueueCallDto>(sql, new
        {
            PoliId = poliId ?? string.Empty,
            DokterId = dokterId ?? string.Empty
        });
    }

    public IReadOnlyList<ClinicQueueCallDto> GetActiveCallsByPoliIds(IEnumerable<string> poliIds)
    {
        var list = poliIds?.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
        if (list is null || list.Count == 0)
            return Array.Empty<ClinicQueueCallDto>();

        const string sql = """
            SELECT PoliId, DokterId, RuangId, AntrianId, NoUrut, CalledAt, CallCount, CallStatus, UserId, CrtDate, UpdDate
            FROM BILRG_ClinicQueueCall
            WHERE PoliId IN @PoliIds AND CallStatus = 0
            ORDER BY CalledAt DESC;
            """;

        using var conn = Open();
        return conn.Query<ClinicQueueCallDto>(sql, new { PoliIds = list }).ToList();
    }

    public IReadOnlyList<ClinicQueueCallDto> GetActiveCallsByPoliId(string poliId)
    {
        if (string.IsNullOrWhiteSpace(poliId))
            return Array.Empty<ClinicQueueCallDto>();

        const string sql = """
            SELECT PoliId, DokterId, RuangId, AntrianId, NoUrut, CalledAt, CallCount, CallStatus, UserId, CrtDate, UpdDate
            FROM BILRG_ClinicQueueCall
            WHERE PoliId = @PoliId AND CallStatus = 0
            ORDER BY CalledAt DESC;
            """;

        using var conn = Open();
        return conn.Query<ClinicQueueCallDto>(sql, new { PoliId = poliId }).ToList();
    }

    public void ReplaceDisplayPolis(string displayId, IEnumerable<string> poliIds)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayId);

        using var conn = Open();
        conn.Open();
        using var tx = conn.BeginTransaction();

        conn.Execute("DELETE FROM BILRG_AdmDisplayPoli WHERE DisplayId = @DisplayId",
            new { DisplayId = displayId }, tx);

        if (poliIds is not null)
        {
            const string insertSql = """
                INSERT INTO BILRG_AdmDisplayPoli (DisplayId, PoliId, SortOrder)
                VALUES (@DisplayId, @PoliId, @SortOrder);
                """;

            var order = 1;
            foreach (var poliId in poliIds.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                conn.Execute(insertSql, new
                {
                    DisplayId = displayId,
                    PoliId = poliId.Trim(),
                    SortOrder = order++
                }, tx);
            }
        }

        tx.Commit();
    }

    public IReadOnlyList<string> GetPolisByDisplayId(string displayId)
    {
        if (string.IsNullOrWhiteSpace(displayId))
            return Array.Empty<string>();

        const string sql = """
            SELECT PoliId
            FROM BILRG_AdmDisplayPoli
            WHERE DisplayId = @DisplayId
            ORDER BY SortOrder, PoliId;
            """;

        using var conn = Open();
        return conn.Query<string>(sql, new { DisplayId = displayId }).ToList();
    }

    public IReadOnlyList<AdmissionDisplayPoliDto> GetDisplayPolis(string displayId)
    {
        if (string.IsNullOrWhiteSpace(displayId))
            return Array.Empty<AdmissionDisplayPoliDto>();

        const string sql = """
            SELECT DisplayId, PoliId, SortOrder
            FROM BILRG_AdmDisplayPoli
            WHERE DisplayId = @DisplayId
            ORDER BY SortOrder, PoliId;
            """;

        using var conn = Open();
        return conn.Query<AdmissionDisplayPoliDto>(sql, new { DisplayId = displayId }).ToList();
    }

    public IReadOnlyList<string> GetDisplaysByPoliId(string poliId)
    {
        if (string.IsNullOrWhiteSpace(poliId))
            return Array.Empty<string>();

        const string sql = """
            SELECT DisplayId
            FROM BILRG_AdmDisplayPoli
            WHERE PoliId = @PoliId
            ORDER BY DisplayId;
            """;

        using var conn = Open();
        return conn.Query<string>(sql, new { PoliId = poliId }).ToList();
    }
}
