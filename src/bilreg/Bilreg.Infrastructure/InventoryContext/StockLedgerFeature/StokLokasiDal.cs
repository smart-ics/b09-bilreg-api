using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;

public interface IStokLokasiDal :
    IInsert<StokLokasiDto>,
    IGetData<StokLokasiDto, IStokLokasiKey>
{
    int UpdateConditional(StokLokasiDto dto, long expectedVersion);
    IEnumerable<StokLokasiDto> ListByStokBatchId(string stokBatchId);
    IEnumerable<StokLokasiDto> ListAllocationCandidates(string brgId, string layananId);
}

public class StokLokasiDal : IStokLokasiDal, Bilreg.Application.InventoryContext.MutasiFeature.IStokLokasiDal
{
    IEnumerable<Bilreg.Application.InventoryContext.MutasiFeature.IStokLokasiCandidate>
        Bilreg.Application.InventoryContext.MutasiFeature.IStokLokasiDal.ListAllocationCandidates(string brgId, string layananId)
    {
        const string sql = """
            SELECT fn_qty AS QtySisa
            FROM tb_stok
            WHERE fs_kd_barang = @BrgId
              AND fs_kd_layanan = @LayananId
              AND fn_qty > 0
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@BrgId", brgId, SqlDbType.VarChar);
        dp.AddParam("@LayananId", layananId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<Bilreg.Application.InventoryContext.MutasiFeature.StokLokasiCandidate>(sql, dp);
    }

    IEnumerable<Bilreg.Application.InventoryContext.MutasiFeature.MutasiBrgStokLokasiView>
        Bilreg.Application.InventoryContext.MutasiFeature.IStokLokasiDal.ListStokLokasi(string layananId)
    {
        const string sql = """
            SELECT
                aa.fs_kd_barang,
                bb.fs_nm_barang,
                aa.fs_kd_satuan,
                ISNULL(dd.fs_nm_satuan, '') AS fs_nm_satuan,
                ISNULL(cc.fs_kd_grup_rek_dk, '') AS fs_kd_grup_rek_dk,
                aa.fs_no_batch,
                aa.fd_tgl_ed,
                aa.fn_hpp,
                aa.fn_qty
            FROM tb_stok aa
            INNER JOIN tb_barang bb ON aa.fs_kd_barang = bb.fs_kd_barang
            LEFT JOIN tb_satuan dd ON aa.fs_kd_satuan = dd.fs_kd_satuan
            LEFT JOIN tb_grup_rek cc ON bb.fs_kd_grup_rek = cc.fs_kd_grup_rek
            WHERE aa.fs_kd_layanan = @fs_kd_layanan
              AND aa.fn_qty > 0
              AND bb.fb_aktif = 1
            ORDER BY bb.fs_nm_barang ASC, aa.fd_tgl_ed ASC, aa.fs_kd_trs ASC
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_layanan", layananId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var rows = conn.Read<MutasiBrgStokLokasiRowDto>(sql, dp) ?? [];
        var grouped = rows.GroupBy(r => r.fs_kd_barang);
        var result = new List<Bilreg.Application.InventoryContext.MutasiFeature.MutasiBrgStokLokasiView>();

        foreach (var group in grouped)
        {
            var first = group.First();
            var totalStok = group.Sum(x => x.fn_qty);
            var batches = group.Select(x => new Bilreg.Application.InventoryContext.MutasiFeature.MutasiBrgStokBatchView(
                NoBatch: x.fs_no_batch?.Trim() ?? string.Empty,
                TglEd: LegacyStockDateHelper.ParseLegacyDate(x.fd_tgl_ed),
                QtySisa: x.fn_qty,
                Hpp: x.fn_hpp
            )).ToList();

            var defaultHpp = batches.FirstOrDefault(b => b.Hpp > 0)?.Hpp ?? first.fn_hpp;

            result.Add(new Bilreg.Application.InventoryContext.MutasiFeature.MutasiBrgStokLokasiView(
                BrgId: first.fs_kd_barang.Trim(),
                BrgName: first.fs_nm_barang.Trim(),
                TotalStok: totalStok,
                SatuanId: first.fs_kd_satuan.Trim(),
                SatuanName: first.fs_nm_satuan.Trim(),
                GroupRekDkId: first.fs_kd_grup_rek_dk.Trim(),
                HppDefault: defaultHpp,
                Batches: batches
            ));
        }

        return result;
    }

    private const string SelectFrom = """
        SELECT
            aa.StokLokasiId, aa.StokBatchId, aa.BrgId, aa.BrgMasukReffId,
            aa.LayananId, aa.TglEd, aa.NoBatch, aa.QtySisa, aa.Version, aa.TglMasuk,
            aa.CrtUser, aa.CrtDate, aa.UpdUser, aa.UpdDate
        FROM BILRG_StokLokasi aa
        """;

    private readonly DatabaseOptions _opt;

    public StokLokasiDal(IOptions<DatabaseOptions> opt) => _opt = opt.Value;

    public void Insert(StokLokasiDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_StokLokasi(
                StokLokasiId, StokBatchId, BrgId, BrgMasukReffId,
                LayananId, TglEd, NoBatch, QtySisa, Version, TglMasuk,
                CrtUser, CrtDate, UpdUser, UpdDate)
            VALUES(
                @StokLokasiId, @StokBatchId, @BrgId, @BrgMasukReffId,
                @LayananId, @TglEd, @NoBatch, @QtySisa, @Version, @TglMasuk,
                @CrtUser, @CrtDate, @UpdUser, @UpdDate)
            """;
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, BuildParams(dto));
    }

    public int UpdateConditional(StokLokasiDto dto, long expectedVersion)
    {
        // Preserve denormalized TglMasuk on update; never DELETE depleted rows.
        const string sql = """
            UPDATE BILRG_StokLokasi SET
                QtySisa = @QtySisa,
                NoBatch = @NoBatch,
                TglMasuk = @TglMasuk,
                Version = @Version,
                UpdUser = @UpdUser,
                UpdDate = @UpdDate
            WHERE StokLokasiId = @StokLokasiId
              AND Version = @ExpectedVersion
            """;
        var dp = BuildParams(dto);
        dp.AddParam("@ExpectedVersion", expectedVersion, SqlDbType.BigInt);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Execute(sql, dp);
    }

    public StokLokasiDto GetData(IStokLokasiKey key)
    {
        var sql = $"{SelectFrom}\nWHERE aa.StokLokasiId = @StokLokasiId";
        var dp = new DynamicParameters();
        dp.AddParam("@StokLokasiId", key.StokLokasiId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<StokLokasiDto>(sql, dp);
    }

    public IEnumerable<StokLokasiDto> ListByStokBatchId(string stokBatchId)
    {
        var sql = $"{SelectFrom}\nWHERE aa.StokBatchId = @StokBatchId";
        var dp = new DynamicParameters();
        dp.AddParam("@StokBatchId", stokBatchId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<StokLokasiDto>(sql, dp);
    }

    public IEnumerable<StokLokasiDto> ListAllocationCandidates(string brgId, string layananId)
    {
        var sql = $"""
            {SelectFrom}
            WHERE aa.BrgId = @BrgId
              AND aa.LayananId = @LayananId
              AND aa.QtySisa > 0
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@BrgId", brgId, SqlDbType.VarChar);
        dp.AddParam("@LayananId", layananId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<StokLokasiDto>(sql, dp);
    }

    private static DynamicParameters BuildParams(StokLokasiDto dto)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@StokLokasiId", dto.StokLokasiId, SqlDbType.VarChar);
        dp.AddParam("@StokBatchId", dto.StokBatchId, SqlDbType.VarChar);
        dp.AddParam("@BrgId", dto.BrgId, SqlDbType.VarChar);
        dp.AddParam("@BrgMasukReffId", dto.BrgMasukReffId, SqlDbType.VarChar);
        dp.AddParam("@LayananId", dto.LayananId, SqlDbType.VarChar);
        dp.AddParam("@TglEd", dto.TglEd, SqlDbType.DateTime);
        dp.AddParam("@NoBatch", dto.NoBatch, SqlDbType.VarChar);
        dp.AddParam("@QtySisa", dto.QtySisa, SqlDbType.Decimal);
        dp.AddParam("@Version", dto.Version, SqlDbType.BigInt);
        dp.AddParam("@TglMasuk", dto.TglMasuk, SqlDbType.DateTime);
        dp.AddParam("@CrtUser", dto.CrtUser, SqlDbType.VarChar);
        dp.AddParam("@CrtDate", dto.CrtDate, SqlDbType.DateTime);
        dp.AddParam("@UpdUser", dto.UpdUser, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", dto.UpdDate, SqlDbType.DateTime);
        return dp;
    }
}

public class MutasiBrgStokLokasiRowDto
{
    public string fs_kd_barang { get; set; } = string.Empty;
    public string fs_nm_barang { get; set; } = string.Empty;
    public string fs_kd_satuan { get; set; } = string.Empty;
    public string fs_nm_satuan { get; set; } = string.Empty;
    public string fs_kd_grup_rek_dk { get; set; } = string.Empty;
    public string fs_no_batch { get; set; } = string.Empty;
    public string fd_tgl_ed { get; set; } = string.Empty;
    public decimal fn_hpp { get; set; } = 0;
    public decimal fn_qty { get; set; } = 0;
}
