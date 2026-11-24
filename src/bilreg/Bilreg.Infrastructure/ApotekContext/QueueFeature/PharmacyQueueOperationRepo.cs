using System.Data.SqlClient;
using Bilreg.Application.ApotekContext.QueueFeature;
using Bilreg.Domain.ApotekContext.QueueFeature;
using Bilreg.Domain.ApotekContext.Shared;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Infrastructure.ApotekContext.QueueFeature;

public sealed class PharmacyQueueOperationRepo : IPharmacyQueueOperationRepo
{
    private readonly DatabaseOptions _opt;
    private readonly ITrackerPharmacyPort? _tracker;

    public PharmacyQueueOperationRepo(
        IOptions<DatabaseOptions> opt,
        ITrackerPharmacyPort? tracker = null)
    {
        _opt = opt.Value;
        _tracker = tracker;
    }

    private SqlConnection Open() => new(ConnStringHelper.Get(_opt));

    public MayBe<PharmacyLoketSession> GetLoket(string loketKey)
    {
        const string sql = """
            SELECT LoketKey, LoketType, DisplayName, DepoFarmasiId, AntrianId, NoUrut,
                   ClaimState, IsActive, AnnouncementVersion, CalledAt, ServiceStartedAt, ReleasedAt, RowVersion
            FROM BILRG_AptLoketCurrentCall
            WHERE LoketKey = @loketKey;
            """;

        using var c = Open();
        var dto = c.QuerySingleOrDefault<AptLoketCurrentCallDto>(sql, new { loketKey });
        return dto is null
            ? MayBe<PharmacyLoketSession>.None
            : MayBe.From(dto.ToSession());
    }

    public IReadOnlyList<PharmacyLoketSession> ListLoketByDepo(string depoFarmasiId)
    {
        const string sql = """
            SELECT LoketKey, LoketType, DisplayName, DepoFarmasiId, AntrianId, NoUrut,
                   ClaimState, IsActive, AnnouncementVersion, CalledAt, ServiceStartedAt, ReleasedAt, RowVersion
            FROM BILRG_AptLoketCurrentCall
            WHERE DepoFarmasiId = @depoFarmasiId
            ORDER BY LoketType, LoketKey;
            """;

        using var c = Open();
        var dtos = c.Query<AptLoketCurrentCallDto>(sql, new { depoFarmasiId });
        return dtos.Select(d => d.ToSession()).ToList();
    }

    public bool TryCallLoket(
        string loketKey,
        string antrianId,
        int noUrut,
        byte[] expectedRowVersion,
        string userId,
        DateTime at)
    {
        var rowVersionParam = expectedRowVersion is { Length: > 0 } ? expectedRowVersion : null;

        using var trans = TransHelper.NewScope();
        try
        {
            const string sql = """
                SET XACT_ABORT ON;

                -- 1. Validate no other loket has an active claim on this ticket
                IF EXISTS (
                    SELECT 1 FROM BILRG_AptLoketCurrentCall WITH (UPDLOCK, HOLDLOCK)
                    WHERE AntrianId = @antrianId AND NoUrut = @noUrut AND IsActive = 1
                )
                BEGIN
                    SELECT CAST(0 AS BIT);
                    RETURN;
                END

                -- 2. Inspect target loket
                DECLARE @loketType INT = 1;
                DECLARE @loketExists BIT = 0;
                DECLARE @depoFarmasiId VARCHAR(26) = '';

                SELECT @loketType = LoketType, @loketExists = 1, @depoFarmasiId = DepoFarmasiId
                FROM BILRG_AptLoketCurrentCall WITH (UPDLOCK, HOLDLOCK)
                WHERE LoketKey = @loketKey;

                IF @loketExists = 1
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM BILRG_AptLoketCurrentCall WITH (UPDLOCK, HOLDLOCK)
                        WHERE LoketKey = @loketKey
                          AND ClaimState = 0
                          AND (@rowVersionParam IS NULL OR RowVersion = @rowVersionParam)
                    )
                    BEGIN
                        SELECT CAST(0 AS BIT);
                        RETURN;
                    END
                END
                ELSE IF @rowVersionParam IS NOT NULL
                BEGIN
                    SELECT CAST(0 AS BIT);
                    RETURN;
                END

                -- 3. Update BILRG_AptQueueEntry call count & timestamp
                IF @loketType = 2
                BEGIN
                    UPDATE BILRG_AptQueueEntry
                    SET CallCountSerah = CallCountSerah + 1,
                        LastCalledSerahAt = @at,
                        UpdUser = @userId,
                        UpdDate = @at
                    WHERE AntrianId = @antrianId AND NoUrut = @noUrut
                      AND ApotekPhase IN (4, 5, 7)
                      AND NOT EXISTS (
                          SELECT 1 FROM BILRG_AptLoketCurrentCall WITH (UPDLOCK, HOLDLOCK)
                          WHERE AntrianId = @antrianId AND NoUrut = @noUrut AND IsActive = 1
                      );
                END
                ELSE
                BEGIN
                    UPDATE BILRG_AptQueueEntry
                    SET CallCountPenerimaan = CallCountPenerimaan + 1,
                        LastCalledPenerimaanAt = @at,
                        UpdUser = @userId,
                        UpdDate = @at
                    WHERE AntrianId = @antrianId AND NoUrut = @noUrut
                      AND ApotekPhase IN (1, 2, 7)
                      AND NOT EXISTS (
                          SELECT 1 FROM BILRG_AptLoketCurrentCall WITH (UPDLOCK, HOLDLOCK)
                          WHERE AntrianId = @antrianId AND NoUrut = @noUrut AND IsActive = 1
                      );
                END

                IF @@ROWCOUNT <> 1
                BEGIN
                    SELECT CAST(0 AS BIT);
                    RETURN;
                END

                -- 4. Update or insert BILRG_AptLoketCurrentCall
                IF @loketExists = 1
                BEGIN
                    UPDATE BILRG_AptLoketCurrentCall
                    SET AntrianId = @antrianId,
                        NoUrut = @noUrut,
                        ClaimState = 1,
                        IsActive = 1,
                        AnnouncementVersion = AnnouncementVersion + 1,
                        CalledAt = @at,
                        ServiceStartedAt = '3000-01-01',
                        ReleasedAt = '3000-01-01',
                        UpdUser = @userId,
                        UpdDate = @at
                    WHERE LoketKey = @loketKey
                      AND ClaimState = 0
                      AND (@rowVersionParam IS NULL OR RowVersion = @rowVersionParam);

                    IF @@ROWCOUNT <> 1
                    BEGIN
                        SELECT CAST(0 AS BIT);
                        RETURN;
                    END
                END
                ELSE
                BEGIN
                    SELECT @depoFarmasiId = sp.DepoFarmasiId
                    FROM BILRG_AptQueueEntry qe
                    JOIN BILRG_AptServicePoint sp ON qe.ServicePointId = sp.ServicePointId
                    WHERE qe.AntrianId = @antrianId AND qe.NoUrut = @noUrut;

                    INSERT INTO BILRG_AptLoketCurrentCall (
                        LoketKey, LoketType, DisplayName, DepoFarmasiId,
                        AntrianId, NoUrut, ClaimState, IsActive,
                        AnnouncementVersion, CalledAt, ServiceStartedAt, ReleasedAt,
                        CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate
                    )
                    VALUES (
                        @loketKey, 1, @loketKey, ISNULL(@depoFarmasiId, ''),
                        @antrianId, @noUrut, 1, 1,
                        1, @at, '3000-01-01', '3000-01-01',
                        @userId, @at, @userId, @at, '', '3000-01-01'
                    );
                END

                SELECT CAST(1 AS BIT);
                """;

            using var c = Open();
            var success = c.ExecuteScalar<bool>(
                sql,
                new { loketKey, antrianId, noUrut, rowVersionParam, userId = userId ?? "", at });

            if (!success)
                return false;

            trans.Complete();
            return true;
        }
        catch (SqlException e) when (e.Number is 2601 or 2627)
        {
            return false;
        }
    }

    public bool TryRecallLoket(
        string loketKey,
        byte[] expectedRowVersion,
        string userId,
        DateTime at)
    {
        var rowVersionParam = expectedRowVersion is { Length: > 0 } ? expectedRowVersion : null;

        using var trans = TransHelper.NewScope();
        try
        {
            const string sql = """
                SET XACT_ABORT ON;

                DECLARE @antrianId VARCHAR(26), @noUrut INT, @loketType INT;
                SELECT @antrianId = AntrianId, @noUrut = NoUrut, @loketType = LoketType
                FROM BILRG_AptLoketCurrentCall WITH (UPDLOCK, HOLDLOCK)
                WHERE LoketKey = @loketKey
                  AND ClaimState = 1
                  AND IsActive = 1
                  AND (@rowVersionParam IS NULL OR RowVersion = @rowVersionParam);

                IF @antrianId IS NULL OR @antrianId = ''
                BEGIN
                    SELECT CAST(0 AS BIT);
                    RETURN;
                END

                UPDATE BILRG_AptLoketCurrentCall
                SET AnnouncementVersion = AnnouncementVersion + 1,
                    CalledAt = @at,
                    UpdUser = @userId,
                    UpdDate = @at
                WHERE LoketKey = @loketKey
                  AND ClaimState = 1
                  AND IsActive = 1
                  AND (@rowVersionParam IS NULL OR RowVersion = @rowVersionParam);

                IF @@ROWCOUNT <> 1
                BEGIN
                    SELECT CAST(0 AS BIT);
                    RETURN;
                END

                IF @loketType = 2
                BEGIN
                    UPDATE BILRG_AptQueueEntry
                    SET CallCountSerah = CallCountSerah + 1,
                        LastCalledSerahAt = @at,
                        UpdUser = @userId,
                        UpdDate = @at
                    WHERE AntrianId = @antrianId AND NoUrut = @noUrut
                      AND ApotekPhase NOT IN (6, 8, 9);
                END
                ELSE
                BEGIN
                    UPDATE BILRG_AptQueueEntry
                    SET CallCountPenerimaan = CallCountPenerimaan + 1,
                        LastCalledPenerimaanAt = @at,
                        UpdUser = @userId,
                        UpdDate = @at
                    WHERE AntrianId = @antrianId AND NoUrut = @noUrut
                      AND ApotekPhase NOT IN (6, 8, 9);
                END

                IF @@ROWCOUNT <> 1
                BEGIN
                    SELECT CAST(0 AS BIT);
                    RETURN;
                END

                SELECT CAST(1 AS BIT);
                """;

            using var c = Open();
            var success = c.ExecuteScalar<bool>(
                sql,
                new { loketKey, rowVersionParam, userId = userId ?? "", at });

            if (!success)
                return false;

            trans.Complete();
            return true;
        }
        catch (SqlException e) when (e.Number is 2601 or 2627)
        {
            return false;
        }
    }

    public bool TryStartServiceLoket(
        string loketKey,
        byte[] expectedRowVersion,
        string userId,
        DateTime at)
    {
        var rowVersionParam = expectedRowVersion is { Length: > 0 } ? expectedRowVersion : null;

        using var trans = TransHelper.NewScope();
        try
        {
            const string sql = """
                SET XACT_ABORT ON;

                DECLARE @antrianId VARCHAR(26), @noUrut INT, @loketType INT;
                SELECT @antrianId = AntrianId, @noUrut = NoUrut, @loketType = LoketType
                FROM BILRG_AptLoketCurrentCall WITH (UPDLOCK, HOLDLOCK)
                WHERE LoketKey = @loketKey
                  AND ClaimState = 1
                  AND IsActive = 1
                  AND (@rowVersionParam IS NULL OR RowVersion = @rowVersionParam);

                IF @antrianId IS NULL OR @antrianId = ''
                BEGIN
                    SELECT CAST(0 AS BIT) AS Success, '' AS AntrianId, 0 AS NoUrut;
                    RETURN;
                END

                UPDATE BILRG_AptLoketCurrentCall
                SET ClaimState = 2,
                    ServiceStartedAt = @at,
                    UpdUser = @userId,
                    UpdDate = @at
                WHERE LoketKey = @loketKey
                  AND ClaimState = 1
                  AND IsActive = 1
                  AND (@rowVersionParam IS NULL OR RowVersion = @rowVersionParam);

                IF @@ROWCOUNT <> 1
                BEGIN
                    SELECT CAST(0 AS BIT) AS Success, '' AS AntrianId, 0 AS NoUrut;
                    RETURN;
                END

                IF @loketType = 2
                BEGIN
                    UPDATE BILRG_AptQueueEntry
                    SET ApotekPhase = 5,
                        UpdUser = @userId,
                        UpdDate = @at
                    WHERE AntrianId = @antrianId AND NoUrut = @noUrut
                      AND ApotekPhase IN (4, 5, 7);
                END
                ELSE
                BEGIN
                    UPDATE BILRG_AptQueueEntry
                    SET ApotekPhase = 2,
                        ServedAt = @at,
                        UpdUser = @userId,
                        UpdDate = @at
                    WHERE AntrianId = @antrianId AND NoUrut = @noUrut
                      AND ApotekPhase IN (1, 2, 7);
                END

                IF @@ROWCOUNT <> 1
                BEGIN
                    SELECT CAST(0 AS BIT) AS Success, '' AS AntrianId, 0 AS NoUrut;
                    RETURN;
                END

                SELECT CAST(1 AS BIT) AS Success, @antrianId AS AntrianId, @noUrut AS NoUrut;
                """;

            using var c = Open();
            var result = c.QuerySingleOrDefault<StartServiceDbResult>(
                sql,
                new { loketKey, rowVersionParam, userId = userId ?? "", at });

            if (result is null || !result.Success)
                return false;

            if (_tracker is not null)
            {
                _tracker.ServeOnce(result.AntrianId, result.NoUrut, "-", loketKey, at);
            }

            trans.Complete();
            return true;
        }
        catch (ApotekConcurrencyException)
        {
            return false;
        }
        catch (SqlException e) when (e.Number is 2601 or 2627)
        {
            return false;
        }
    }

    public bool TryReleaseLoket(
        string loketKey,
        byte[] expectedRowVersion,
        string userId,
        DateTime at)
    {
        var rowVersionParam = expectedRowVersion is { Length: > 0 } ? expectedRowVersion : null;

        using var trans = TransHelper.NewScope();
        try
        {
            const string sql = """
                SET XACT_ABORT ON;

                UPDATE BILRG_AptLoketCurrentCall
                SET ClaimState = 0,
                    IsActive = 0,
                    ReleasedAt = @at,
                    UpdUser = @userId,
                    UpdDate = @at
                WHERE LoketKey = @loketKey
                  AND IsActive = 1
                  AND (@rowVersionParam IS NULL OR RowVersion = @rowVersionParam);

                SELECT CAST(IIF(@@ROWCOUNT = 1, 1, 0) AS BIT);
                """;

            using var c = Open();
            var success = c.ExecuteScalar<bool>(
                sql,
                new { loketKey, rowVersionParam, userId = userId ?? "", at });

            if (!success)
                return false;

            trans.Complete();
            return true;
        }
        catch (SqlException e) when (e.Number is 2601 or 2627)
        {
            return false;
        }
    }

    public bool TrySkipLoket(
        string loketKey,
        string reason,
        byte[] expectedRowVersion,
        string userId,
        DateTime at)
    {
        var rowVersionParam = expectedRowVersion is { Length: > 0 } ? expectedRowVersion : null;

        using var trans = TransHelper.NewScope();
        try
        {
            const string sql = """
                SET XACT_ABORT ON;

                DECLARE @antrianId VARCHAR(26), @noUrut INT, @loketType INT;
                SELECT @antrianId = AntrianId, @noUrut = NoUrut, @loketType = LoketType
                FROM BILRG_AptLoketCurrentCall WITH (UPDLOCK, HOLDLOCK)
                WHERE LoketKey = @loketKey
                  AND IsActive = 1
                  AND (@rowVersionParam IS NULL OR RowVersion = @rowVersionParam);

                IF @antrianId IS NULL OR @antrianId = ''
                BEGIN
                    SELECT CAST(0 AS BIT);
                    RETURN;
                END

                UPDATE BILRG_AptLoketCurrentCall
                SET ClaimState = 0,
                    IsActive = 0,
                    ReleasedAt = @at,
                    UpdUser = @userId,
                    UpdDate = @at
                WHERE LoketKey = @loketKey
                  AND IsActive = 1
                  AND (@rowVersionParam IS NULL OR RowVersion = @rowVersionParam);

                IF @@ROWCOUNT <> 1
                BEGIN
                    SELECT CAST(0 AS BIT);
                    RETURN;
                END

                UPDATE BILRG_AptQueueEntry
                SET ApotekPhase = 7,
                    LastSkippedAt = @at,
                    LastSkippedLoketType = @loketType,
                    WithdrawalReason = @reason,
                    UpdUser = @userId,
                    UpdDate = @at
                WHERE AntrianId = @antrianId AND NoUrut = @noUrut
                  AND ApotekPhase NOT IN (6, 8, 9);

                IF @@ROWCOUNT <> 1
                BEGIN
                    SELECT CAST(0 AS BIT);
                    RETURN;
                END

                SELECT CAST(1 AS BIT);
                """;

            using var c = Open();
            var success = c.ExecuteScalar<bool>(
                sql,
                new { loketKey, reason = reason ?? "", rowVersionParam, userId = userId ?? "", at });

            if (!success)
                return false;

            trans.Complete();
            return true;
        }
        catch (SqlException e) when (e.Number is 2601 or 2627)
        {
            return false;
        }
    }

    private sealed class AptLoketCurrentCallDto
    {
        public string LoketKey { get; set; } = string.Empty;
        public int LoketType { get; set; } = 1;
        public string DisplayName { get; set; } = string.Empty;
        public string DepoFarmasiId { get; set; } = string.Empty;
        public string AntrianId { get; set; } = string.Empty;
        public int NoUrut { get; set; } = 0;
        public int ClaimState { get; set; } = 0;
        public bool IsActive { get; set; } = false;
        public long AnnouncementVersion { get; set; } = 0;
        public DateTime CalledAt { get; set; } = ApotekDate.Empty;
        public DateTime ServiceStartedAt { get; set; } = ApotekDate.Empty;
        public DateTime ReleasedAt { get; set; } = ApotekDate.Empty;
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public PharmacyLoketSession ToSession() => new(
            LoketKey,
            (LoketTypeEnum)LoketType,
            DisplayName,
            DepoFarmasiId,
            AntrianId,
            NoUrut,
            (LoketClaimStateEnum)ClaimState,
            IsActive,
            AnnouncementVersion,
            CalledAt,
            ServiceStartedAt,
            ReleasedAt,
            RowVersion);
    }

    private sealed record StartServiceDbResult(bool Success, string AntrianId, int NoUrut);
}
