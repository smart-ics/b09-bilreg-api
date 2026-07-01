using System.Data;
using System.Data.SqlClient;
using Dapper;
using Farinv.Domain.BrgContext.KlasifikasiFeature;
using Farinv.Infrastructure.Helpers;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Farinv.Infrastructure.BrgContext.KlasifikasiFeature;

public interface IGenerikDal :
    IInsert<GenerikDto>,
    IUpdate<GenerikDto>,
    IDelete<IGenerikKey>,
    IGetData<GenerikDto, IGenerikKey>,
    IListData<GenerikDto>
{
}

public class GenerikDal : IGenerikDal
{
    private readonly DatabaseOptions _opt;
    public GenerikDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    
    public void Insert(GenerikDto dto)
    {
        const string sql = """
            INSERT INTO tb_generik(
                fs_kd_generik, fs_nm_generik, fs_komposisi)
            VALUES( 
                @fs_kd_generik, @fs_nm_generik, @fs_komposisi)
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_generik", dto.fs_kd_generik, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_generik", dto.fs_nm_generik, SqlDbType.VarChar);
        dp.AddParam("@fs_komposisi", dto.fs_komposisi, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }
    
    public void Update(GenerikDto dto)
    {
        const string sql = """
           UPDATE 
               tb_generik
           SET
               fs_nm_generik = @fs_nm_generik,
               fs_komposisi = @fs_komposisi
           WHERE
               fs_kd_generik = @fs_kd_generik
           """;
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_generik", dto.fs_kd_generik, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_generik", dto.fs_nm_generik, SqlDbType.VarChar);
        dp.AddParam("@fs_komposisi", dto.fs_komposisi, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }
    
    public void Delete(IGenerikKey key)
    {
        const string sql = """
           DELETE FROM 
                tb_generik
           WHERE
               fs_kd_generik = @fs_kd_generik
           """;
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_generik", key.GenerikId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }
    
    public GenerikDto GetData(IGenerikKey key)
    {
        const string sql = """
           SELECT
               fs_kd_generik,
               fs_nm_generik,
               fs_komposisi
           FROM 
               tb_generik
           WHERE
               fs_kd_generik = @fs_kd_generik
           """;
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_generik", key.GenerikId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.ReadSingle<GenerikDto>(sql, dp);
        return result;
    }
    
    public IEnumerable<GenerikDto> ListData()
    {
        const string sql = """
            SELECT
                fs_kd_generik,
                fs_nm_generik,
                fs_komposisi
            FROM 
                tb_generik
            """;
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<GenerikDto>(sql);
    }
}