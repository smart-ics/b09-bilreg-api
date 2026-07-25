using System.Data.SqlClient;
using Bilreg.Domain.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using System.Data;

namespace Bilreg.Infrastructure.Shared.Helpers;

public class Sequencer : ISequencer
{
    private const int SqlSequenceExhausted = 11728;
    private const int SequenceAboveRequestedMaximum = 50012;
    private readonly DatabaseOptions _opt;

    public Sequencer(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void CreateSequence(string sequenceTag)
    {
        sequenceTag = $"sq_{sequenceTag.ToLower()}";
        const string sql = "CREATE SEQUENCE [{0}] START WITH 1 INCREMENT BY 1 NO CACHE";
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.ExecuteAsync(string.Format(sql, sequenceTag));    
    }

    public int GetNextNoUrut(string sequenceTag)
    {
        sequenceTag = $"sq_{sequenceTag.ToLower()}";

        var sql = $"""
           BEGIN TRY
               EXEC ('SELECT NEXT VALUE FOR [dbo].[{sequenceTag}]')
           END TRY
           BEGIN CATCH
               IF ERROR_NUMBER() IN (11716, 208) -- 11716 = sequence not found, 208 = invalid object name
               BEGIN
                   EXEC ('
                       CREATE SEQUENCE [dbo].[{sequenceTag}]
                       START WITH 1
                       INCREMENT BY 1
                       NO CACHE;
                   ')
                   EXEC ('SELECT NEXT VALUE FOR [dbo].[{sequenceTag}]')
               END
               ELSE
                   THROW;
           END CATCH
           """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ExecuteScalar<int>(sql);
    }

    public int GetNextNoUrut(string sequenceTag, int maxValue)
    {
        ValidateBoundedRequest(sequenceTag, maxValue);
        var sequenceName = $"sq_{sequenceTag.ToLowerInvariant()}";

        const string sql = """
            SET XACT_ABORT ON;
            BEGIN TRANSACTION;

            DECLARE @LockResult INT;
            DECLARE @Resource NVARCHAR(255) = N'BILRG_SEQUENCE_' + @SequenceName;

            EXEC @LockResult = sys.sp_getapplock
                @Resource = @Resource,
                @LockMode = 'Exclusive',
                @LockOwner = 'Transaction',
                @LockTimeout = 10000;

            IF @LockResult < 0
                THROW 50010, 'Unable to acquire sequence configuration lock.', 1;

            IF NOT EXISTS (
                SELECT 1
                FROM sys.sequences aa
                INNER JOIN sys.schemas bb ON bb.schema_id = aa.schema_id
                WHERE bb.name = 'dbo'
                  AND aa.name = @SequenceName
            )
            BEGIN
                DECLARE @CreateSql NVARCHAR(MAX) =
                    N'CREATE SEQUENCE [dbo].' + QUOTENAME(@SequenceName) +
                    N' AS INT START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE ' +
                    CONVERT(NVARCHAR(20), @MaxValue) + N' NO CYCLE NO CACHE;';
                EXEC sys.sp_executesql @CreateSql;
            END
            ELSE
            BEGIN
                DECLARE @CurrentValue BIGINT;
                DECLARE @MinimumValue BIGINT;
                DECLARE @MaximumValue BIGINT;
                DECLARE @IncrementValue BIGINT;
                DECLARE @IsCycling BIT;
                DECLARE @CacheSize INT; 

                SELECT
                    @CurrentValue = CONVERT(BIGINT, aa.current_value),
                    @MinimumValue = CONVERT(BIGINT, aa.minimum_value),
                    @MaximumValue = CONVERT(BIGINT, aa.maximum_value),
                    @IncrementValue = CONVERT(BIGINT, aa.increment),
                    @IsCycling = aa.is_cycling,
                    @CacheSize = aa.cache_size
                FROM sys.sequences aa
                INNER JOIN sys.schemas bb ON bb.schema_id = aa.schema_id
                WHERE bb.name = 'dbo'
                  AND aa.name = @SequenceName;

                IF @CurrentValue > @MaxValue
                    THROW 50012, 'Existing sequence value exceeds the requested maximum.', 1;

                IF @MinimumValue <> 1
                   OR @MaximumValue <> @MaxValue
                   OR @IncrementValue <> 1
                   OR @IsCycling <> 0
                   OR ISNULL(@CacheSize, 0) <> 0
                BEGIN
                    DECLARE @AlterSql NVARCHAR(MAX) =
                        N'ALTER SEQUENCE [dbo].' + QUOTENAME(@SequenceName) +
                        N' INCREMENT BY 1 MINVALUE 1 MAXVALUE ' +
                        CONVERT(NVARCHAR(20), @MaxValue) + N' NO CYCLE NO CACHE;';
                    EXEC sys.sp_executesql @AlterSql;
                END
            END

            DECLARE @NextValue INT;
            DECLARE @NextSql NVARCHAR(MAX) =
                N'SELECT @Value = NEXT VALUE FOR [dbo].' + QUOTENAME(@SequenceName) + N';';

            EXEC sys.sp_executesql
                @NextSql,
                N'@Value INT OUTPUT',
                @Value = @NextValue OUTPUT;

            COMMIT TRANSACTION;
            SELECT @NextValue;
            """;

        var dp = new DynamicParameters();
        dp.Add("@SequenceName", sequenceName, DbType.String, size: 128);
        dp.Add("@MaxValue", maxValue, DbType.Int32);

        try
        {
            using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
            return conn.ExecuteScalar<int>(sql, dp);
        }
        catch (SqlException ex) when (
            ex.Number == SqlSequenceExhausted || ex.Number == SequenceAboveRequestedMaximum)
        {
            throw new SequenceExhaustedException(sequenceTag, maxValue, ex);
        }
    }

    private static void ValidateBoundedRequest(string sequenceTag, int maxValue)
    {
        if (string.IsNullOrWhiteSpace(sequenceTag))
            throw new ArgumentException("Sequence tag is required.", nameof(sequenceTag));
        if (sequenceTag.Length > 125)
            throw new ArgumentException("Sequence tag exceeds the SQL identifier limit.", nameof(sequenceTag));
        if (maxValue < 1)
            throw new ArgumentOutOfRangeException(nameof(maxValue));
    }
}
