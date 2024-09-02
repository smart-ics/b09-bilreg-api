using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.AdmisiContext.PetugasMedisSub.PetugasMedisAgg;
using Bilreg.Domain.AdmisiContext.PetugasMedisSub.PetugasAgg;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.AdmisiContext.PetugasMedisSub.PetugasMedisAgg;

public class PetugasMedisSatTugasDal : IPetugasMedisSatTugasDal
{
    private readonly DatabaseOptions _opt;

    public PetugasMedisSatTugasDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(IEnumerable<PetugasMedisSatTugasModel> listModel)
    {
        //  INSERT BULK
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        using var bcp = new SqlBulkCopy(conn);
        
        bcp.AddMap("PetugasMedisId", "fs_kd_peg");
        bcp.AddMap("SatTugasId", "fs_kd_sat_tugas");
        bcp.AddMap("IsUtama", "fn_utama");

        conn.Open();
        var fetched = listModel.ToList();
        bcp.BatchSize = fetched.Count;
        bcp.DestinationTableName = "td_peg_sat_tugas";
        bcp.WriteToServer(fetched.AsDataTable());
    }

    public void Delete(IPetugasMedisKey key)
    {
        const string sql = @"
            DELETE FROM 
                td_peg_sat_tugas
            WHERE 
                fs_kd_peg = @fs_kd_peg";
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_peg", key.PetugasMedisId, SqlDbType.VarChar);
        
        var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public IEnumerable<PetugasMedisSatTugasModel> ListData(IPetugasMedisKey filter)
    {
        const string sql = @"
            SELECT 
                aa.fs_kd_peg, aa.fs_kd_sat_tugas, aa.fn_utama,
                ISNULL(bb.fs_nm_sat_tugas, '') AS bb.fs_nm_sat_tugas
            FROM 
                td_peg_sat_tugas aa
                LEFT JOIN ta_sat_tugas bb ON aa.fs_kd_sat_tugas = bb.fs_kd_sat_tugas
            WHERE 
                aa.fs_kd_peg = @fs_kd_peg";
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_peg", filter.PetugasMedisId, SqlDbType.VarChar);
        
        var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.Query<PetugasMedisSatTugasDto>(sql, dp);
        var response = result.Select(x => x.ToModel());
        return response;
    }
}

public class PetugasMedisSatTugasDto
{
    public string fs_kd_peg { get; set; }
    public string fs_kd_sat_tugas { get; set; }
    public string fs_nm_sat_tugas { get; set; }
    public int fn_utama { get; set; }

    public PetugasMedisSatTugasModel ToModel()
    {
        var result = new PetugasMedisSatTugasModel(fs_kd_peg, fs_kd_sat_tugas, fs_nm_sat_tugas);
        if (fn_utama == 0) result.SetUtama();
        else result.UnsetUtama();
        return result;
    }
}