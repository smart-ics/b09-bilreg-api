using System.Data;
using System.Data.SqlClient;
using Dapper;
using Farinv.Domain.InventoryContext.StokFeature;
using Farinv.Infrastructure.Helpers;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Farinv.Infrastructure.InventoryContext.StokFeature;

public interface IStokDal :
    IInsert<StokDto>,
    IUpdate<StokDto>,
    IDelete<IStokKey>,
    IGetData<StokDto, IStokKey>,
    IListData<StokDto>
{
}

public class StokDal : IStokDal
{
    private readonly DatabaseOptions _opt;
    public StokDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    
    public void Insert(StokDto dto)
    {
        const string sql = """
            INSERT INTO FARIN_Stok(
                BrgId, LayananId, Qty, Satuan)
            VALUES( 
                @BrgId, @LayananId, @Qty, @Satuan)
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@BrgId", dto.BrgId, SqlDbType.VarChar);
        dp.AddParam("@LayananId", dto.LayananId, SqlDbType.VarChar);
        dp.AddParam("@Qty", dto.Qty, SqlDbType.Int);
        dp.AddParam("@Satuan", dto.Satuan, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }
    
    public void Update(StokDto dto)
    {
        const string sql = """
           UPDATE 
               FARIN_Stok
           SET
               Qty = @Qty,
               Satuan = @Satuan
           WHERE
               BrgId = @BrgId 
               AND LayananId = @LayananId
           """;
        var dp = new DynamicParameters();
        dp.AddParam("@BrgId", dto.BrgId, SqlDbType.VarChar);
        dp.AddParam("@LayananId", dto.LayananId, SqlDbType.VarChar);
        dp.AddParam("@Qty", dto.Qty, SqlDbType.Int);
        dp.AddParam("@Satuan", dto.Satuan, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }
    
    public void Delete(IStokKey key)
    {
        const string sql = """
           DELETE FROM 
                FARIN_Stok
           WHERE
               BrgId = @BrgId 
               AND LayananId = @LayananId
           """;
        var dp = new DynamicParameters();
        dp.AddParam("@BrgId", key.BrgId, SqlDbType.VarChar);
        dp.AddParam("@LayananId", key.LayananId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }
    
    public StokDto GetData(IStokKey key)
    {
        const string sql = """
           SELECT
               aa.BrgId, aa.LayananId, aa.Qty, aa.Satuan,
               ISNULL(bb.fs_nm_barang, '') BrgName,
               ISNULL(cc.fs_nm_layanan, '') LayananName
           FROM 
               FARIN_Stok aa
               LEFT JOIN tb_barang bb ON aa.BrgId = bb.fs_kd_barang
               LEFT JOIN ta_layanan cc ON aa.LayananId = cc.fs_kd_layanan
           WHERE
               aa.BrgId = @BrgId AND aa.LayananId = @LayananId
           """;
        var dp = new DynamicParameters();
        dp.AddParam("@BrgId", key.BrgId, SqlDbType.VarChar);
        dp.AddParam("@LayananId", key.LayananId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.ReadSingle<StokDto>(sql, dp);
        return result;
    }
    
    public IEnumerable<StokDto> ListData()
    {
        const string sql = """
            SELECT
                aa.BrgId, aa.LayananId, aa.Qty, aa.Satuan,
                ISNULL(bb.fs_nm_barang, '') BrgName,
                ISNULL(cc.fs_nm_layanan, '') LayananName
            FROM 
                FARIN_Stok aa
                LEFT JOIN tb_barang bb ON aa.BrgId = bb.fs_kd_barang
                LEFT JOIN ta_layanan cc ON aa.LayananId = cc.fs_kd_layanan
            """;
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<StokDto>(sql);
    }
}