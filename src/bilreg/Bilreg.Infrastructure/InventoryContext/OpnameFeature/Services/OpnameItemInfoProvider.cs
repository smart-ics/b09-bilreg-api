using System.Data;
using Bilreg.Application.InventoryContext.MutasiFeature;
using Bilreg.Application.InventoryContext.OpnameFeature.UseCases.Commands;
using Bilreg.Infrastructure.InventoryContext.MutasiFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;

namespace Bilreg.Infrastructure.InventoryContext.OpnameFeature.Services;

public class OpnameItemInfoProvider : IOpnameItemInfoProvider
{
    private readonly ISqlDatabase? _sqlDatabase;
    private readonly Func<string, object?, decimal?>? _hppExecutor;
    private readonly Func<string, object?, string?>? _satuanExecutor;

    public OpnameItemInfoProvider(ISqlDatabase sqlDatabase)
    {
        _sqlDatabase = sqlDatabase ?? throw new ArgumentNullException(nameof(sqlDatabase));
    }

    internal OpnameItemInfoProvider(IOptions<DatabaseOptions> options)
        : this(new SqlDatabase(options))
    {
    }

    internal OpnameItemInfoProvider(
        Func<string, object?, decimal?> hppExecutor,
        Func<string, object?, string?> satuanExecutor)
    {
        _hppExecutor = hppExecutor;
        _satuanExecutor = satuanExecutor;
    }

    public decimal GetHpp(string brgId)
    {
        if (string.IsNullOrWhiteSpace(brgId))
            return 0m;

        if (_hppExecutor != null)
        {
            var executedHpp = _hppExecutor(brgId, null);
            return executedHpp.GetValueOrDefault(0m);
        }

        using var conn = _sqlDatabase!.GetConnection();
        const string sqlTbStok = """
            SELECT TOP 1 fn_hpp
            FROM tb_stok
            WHERE fs_kd_barang = @BrgId AND fn_hpp > 0
            ORDER BY fd_tgl_mutasi DESC, fs_jam_mutasi DESC
            """;

        var hpp = conn.QueryFirstOrDefault<decimal?>(sqlTbStok, new { BrgId = brgId.Trim() });
        if (hpp.HasValue && hpp.Value > 0)
            return hpp.Value;

        const string sqlBatch = """
            SELECT TOP 1 Hpp
            FROM BILRG_StokBatch
            WHERE BrgId = @BrgId AND Hpp > 0
            ORDER BY CrtDate DESC
            """;

        var batchHpp = conn.QueryFirstOrDefault<decimal?>(sqlBatch, new { BrgId = brgId.Trim() });
        return batchHpp.GetValueOrDefault(0m);
    }

    public string GetBaseSatuan(string brgId)
    {
        if (string.IsNullOrWhiteSpace(brgId))
            return string.Empty;

        if (_satuanExecutor != null)
        {
            return _satuanExecutor(brgId, null) ?? string.Empty;
        }

        using var conn = _sqlDatabase!.GetConnection();
        const string sqlSatuan = """
            SELECT TOP 1 fs_kd_satuan
            FROM tb_barang_satuan
            WHERE fs_kd_barang = @BrgId
            ORDER BY CASE WHEN fn_nilai = 1 THEN 0 ELSE 1 END, fn_nilai ASC
            """;

        var satuan = conn.QueryFirstOrDefault<string>(sqlSatuan, new { BrgId = brgId.Trim() });
        if (!string.IsNullOrWhiteSpace(satuan))
            return satuan.Trim();

        const string sqlFallback = """
            SELECT TOP 1 fs_kd_satuan
            FROM tb_stok
            WHERE fs_kd_barang = @BrgId AND fs_kd_satuan <> ''
            """;

        var fallbackSatuan = conn.QueryFirstOrDefault<string>(sqlFallback, new { BrgId = brgId.Trim() });
        return fallbackSatuan?.Trim() ?? string.Empty;
    }
}
