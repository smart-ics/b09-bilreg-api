using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.ChargeContext.TarifFeature;

public interface IGroupKomponenDal :
    IInsert<GroupKomponenDto>,
    IUpdate<GroupKomponenDto>,
    IDelete<IGroupKomponenKey>,
    IGetData<GroupKomponenDto, IGroupKomponenKey>,
    IListData<GroupKomponenDto>
{
}

public class GroupKomponenDal : IGroupKomponenDal
{
    private readonly DatabaseOptions _opt;

    public GroupKomponenDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    
    public void Insert(GroupKomponenDto dto)
    {
        const string sql = """
            INSERT INTO ta_grup_detil_tarif(
                fs_kd_grup_detil_tarif, fs_nm_grup_detil_tarif)
            VALUES( 
                @fs_kd_grup_detil_tarif, @fs_nm_grup_detil_tarif)
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_grup_detil_tarif", dto.fs_kd_grup_detil_tarif, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_grup_detil_tarif", dto.fs_nm_grup_detil_tarif, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(GroupKomponenDto dto)
    {
        const string sql = @"
           UPDATE 
               ta_grup_detil_tarif
           SET
               fs_nm_grup_detil_tarif = @fs_nm_grup_detil_tarif
           WHERE
               fs_kd_grup_detil_tarif = @fs_kd_grup_detil_tarif";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_grup_detil_tarif", dto.fs_kd_grup_detil_tarif, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_grup_detil_tarif", dto.fs_nm_grup_detil_tarif, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IGroupKomponenKey key)
    {
        const string sql = @"
           DELETE FROM 
                ta_grup_detil_tarif
           WHERE
               fs_kd_grup_detil_tarif = @fs_kd_grup_detil_tarif";
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_grup_detil_tarif", key.GroupKomponenId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public GroupKomponenDto GetData(IGroupKomponenKey key)
    {
        const string sql = @"
           SELECT
               fs_kd_grup_detil_tarif,
               fs_nm_grup_detil_tarif
           FROM 
               ta_grup_detil_tarif
           WHERE
               fs_kd_grup_detil_tarif = @fs_kd_grup_detil_tarif";
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_grup_detil_tarif", key.GroupKomponenId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.ReadSingle<GroupKomponenDto>(sql, dp);
        return result;
    }

    public IEnumerable<GroupKomponenDto> ListData()
    {
        const string sql = """
            SELECT
                fs_kd_grup_detil_tarif,
                fs_nm_grup_detil_tarif
            FROM 
                ta_grup_detil_tarif
            """;
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<GroupKomponenDto>(sql);
    }
}
