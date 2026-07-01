using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.ChargeContext.TarifFeature;

public interface ITipeTarifDal :
    IInsert<TipeTarifDto>,
    IUpdate<TipeTarifDto>,
    IDelete<ITipeTarifKey>,
    IGetData<TipeTarifDto, ITipeTarifKey>,
    IListData<TipeTarifDto>
{
}

public class TipeTarifDal : ITipeTarifDal
{
    private readonly DatabaseOptions _opt;

    public TipeTarifDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    
    public void Insert(TipeTarifDto dto)
    {
        const string sql = """
            INSERT INTO ta_tarif_tipe(
                fs_kd_tarif_tipe, fs_nm_tarif_tipe, fb_aktif, fn_no_urut)
            VALUES( 
                @fs_kd_tarif_tipe, @fs_nm_tarif_tipe, @fb_aktif, @fn_no_urut)
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_tarif_tipe", dto.fs_kd_tarif_tipe, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_tarif_tipe", dto.fs_nm_tarif_tipe, SqlDbType.VarChar);
        dp.AddParam("@fb_aktif", dto.fb_aktif, SqlDbType.Bit);
        dp.AddParam("@fn_no_urut", dto.fn_no_urut, SqlDbType.Decimal);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(TipeTarifDto dto)
    {
        const string sql = @"
           UPDATE 
               ta_tarif_tipe
           SET
               fs_nm_tarif_tipe = @fs_nm_tarif_tipe,
               fb_aktif = @fb_aktif,
               fn_no_urut = @fn_no_urut
           WHERE
               fs_kd_tarif_tipe = @fs_kd_tarif_tipe";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_tarif_tipe", dto.fs_kd_tarif_tipe, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_tarif_tipe", dto.fs_nm_tarif_tipe, SqlDbType.VarChar);
        dp.AddParam("@fb_aktif", dto.fb_aktif, SqlDbType.Bit);
        dp.AddParam("@fn_no_urut", dto.fn_no_urut, SqlDbType.Decimal);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(ITipeTarifKey key)
    {
        const string sql = @"
           DELETE FROM 
                ta_tarif_tipe
           WHERE
               fs_kd_tarif_tipe = @fs_kd_tarif_tipe";
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_tarif_tipe", key.TipeTarifId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public TipeTarifDto GetData(ITipeTarifKey key)
    {
        const string sql = @"
           SELECT
               fs_kd_tarif_tipe,
               fs_nm_tarif_tipe,
               fb_aktif,
               fn_no_urut
           FROM 
               ta_tarif_tipe
           WHERE
               fs_kd_tarif_tipe = @fs_kd_tarif_tipe";
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_tarif_tipe", key.TipeTarifId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.ReadSingle<TipeTarifDto>(sql, dp);
        return result;
    }

    public IEnumerable<TipeTarifDto> ListData()
    {
        const string sql = """
            SELECT
                fs_kd_tarif_tipe,
                fs_nm_tarif_tipe,
                fb_aktif,
                fn_no_urut
            FROM 
                ta_tarif_tipe
            """;
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<TipeTarifDto>(sql);
    }
}

