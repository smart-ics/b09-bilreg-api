using System.Data;
using Bilreg.Application.InventoryContext.MutasiFeature;
using Bilreg.Application.InventoryContext.OpnameFeature.Ports;
using Bilreg.Domain.InventoryContext.OpnameFeature;
using Bilreg.Infrastructure.InventoryContext.MutasiFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Bilreg.Infrastructure.InventoryContext.OpnameFeature.Services;

public record ReconstructedRowDto(
    string BrgId,
    string BatchNo,
    string ExpiredDate,
    decimal CurrentQty,
    decimal InAfterCutoff,
    decimal OutAfterCutoff,
    decimal ReconstructedQty);

public class SystemQuantityReconstructionService : ISystemQuantityReconstructionService
{
    private readonly ISqlDatabase? _sqlDatabase;
    private readonly Func<string, DynamicParameters, CancellationToken, Task<IEnumerable<ReconstructedRowDto>>>? _queryExecutor;

    public SystemQuantityReconstructionService(ISqlDatabase sqlDatabase)
    {
        _sqlDatabase = sqlDatabase ?? throw new ArgumentNullException(nameof(sqlDatabase));
    }

    internal SystemQuantityReconstructionService(IOptions<DatabaseOptions> options)
        : this(new SqlDatabase(options))
    {
    }

    internal SystemQuantityReconstructionService(
        Func<string, DynamicParameters, CancellationToken, Task<IEnumerable<ReconstructedRowDto>>> queryExecutor)
    {
        _queryExecutor = queryExecutor ?? throw new ArgumentNullException(nameof(queryExecutor));
    }

    public static decimal CalculateReconstructedQuantity(decimal currentQty, decimal inAfterCutoff, decimal outAfterCutoff) =>
        currentQty - inAfterCutoff + outAfterCutoff;

    internal static (string Sql, DynamicParameters Parameters) BuildQuery(
        string layananId,
        DateTime cutoffTime,
        IEnumerable<string>? filterBrgIds)
    {
        var brgList = filterBrgIds?
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct()
            .ToList();

        var hasFilter = brgList != null && brgList.Count > 0;

        var filterClauseMutasi = hasFilter ? "AND b.fs_kd_barang IN @BrgIds" : string.Empty;
        var filterClauseStok = hasFilter ? "AND s.fs_kd_barang IN @BrgIds" : string.Empty;

        var sql = $"""
            WITH MutasiAfterCount AS (
                SELECT b.fs_kd_barang, b.fs_no_batch, b.fd_tgl_ed, b.fs_kd_layanan,
                       SUM(b.fn_stok_in) AS TotalIn,
                       SUM(b.fn_stok_out) AS TotalOut
                FROM tb_buku b WITH (NOLOCK)
                WHERE b.fs_kd_layanan = @LayananId
                  AND b.fd_tgl_jam_mutasi > @CutoffTimeStr
                  AND b.fd_tgl_jam_mutasi < '3000-01-01'
                  {filterClauseMutasi}
                GROUP BY b.fs_kd_barang, b.fs_no_batch, b.fd_tgl_ed, b.fs_kd_layanan
            )
            SELECT 
                s.fs_kd_barang AS BrgId,
                s.fs_no_batch AS BatchNo,
                s.fd_tgl_ed AS ExpiredDate,
                SUM(s.fn_qty) AS CurrentQty,
                ISNULL(m.TotalIn, 0) AS InAfterCutoff,
                ISNULL(m.TotalOut, 0) AS OutAfterCutoff,
                (SUM(s.fn_qty) - ISNULL(m.TotalIn, 0) + ISNULL(m.TotalOut, 0)) AS ReconstructedQty
            FROM tb_stok s WITH (NOLOCK)
            LEFT JOIN MutasiAfterCount m 
                ON s.fs_kd_barang = m.fs_kd_barang 
               AND s.fs_no_batch = m.fs_no_batch 
               AND s.fd_tgl_ed = m.fd_tgl_ed 
               AND s.fs_kd_layanan = m.fs_kd_layanan
            WHERE s.fs_kd_layanan = @LayananId
              {filterClauseStok}
            GROUP BY s.fs_kd_barang, s.fs_no_batch, s.fd_tgl_ed, m.TotalIn, m.TotalOut;
            """;

        var parameters = new DynamicParameters();
        parameters.Add("@LayananId", layananId.Trim(), DbType.AnsiString);
        var cutoffTimeStr = cutoffTime.ToString("yyyy-MM-dd HH:mm:ss");
        parameters.Add("@CutoffTimeStr", cutoffTimeStr, DbType.AnsiString);
        if (hasFilter)
        {
            parameters.Add("@BrgIds", brgList);
        }

        return (sql, parameters);
    }

    public async Task<IReadOnlyDictionary<ItemTupleKey, decimal>> ReconstructBalancesAsync(
        string layananId,
        DateTime cutoffTime,
        IEnumerable<string>? filterBrgIds = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(layananId))
            return new Dictionary<ItemTupleKey, decimal>();

        var (sql, parameters) = BuildQuery(layananId, cutoffTime, filterBrgIds);

        IEnumerable<ReconstructedRowDto> rows;
        if (_queryExecutor != null)
        {
            rows = await _queryExecutor(sql, parameters, cancellationToken);
        }
        else
        {
            using var conn = _sqlDatabase!.GetConnection();
            if (conn.State != ConnectionState.Open)
            {
                await conn.OpenAsync(cancellationToken);
            }

            var commandDef = new CommandDefinition(sql, parameters, commandTimeout: 120, cancellationToken: cancellationToken);
            rows = await conn.QueryAsync<ReconstructedRowDto>(commandDef);
        }

        var result = new Dictionary<ItemTupleKey, decimal>();
        foreach (var row in rows)
        {
            var key = new ItemTupleKey(row.BrgId, row.BatchNo, row.ExpiredDate);
            result[key] = row.ReconstructedQty;
        }

        return result;
    }
}
