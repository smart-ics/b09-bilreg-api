using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.AdmisiContext.PetugasMedisSub.PetugasMedisAgg;
using Bilreg.Domain.AdmisiContext.PetugasMedisSub.PetugasAgg;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.AdmisiContext.PetugasMedisSub.PetugasMedisAgg;

public class PetugasMedisLayananDal : IPetugasMedisLayananDal
{
    private readonly DatabaseOptions _opt;

    public PetugasMedisLayananDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(IEnumerable<PetugasMedisLayananModel> listModel)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        using var bcp = new SqlBulkCopy(conn);

        conn.Open();
        bcp.AddMap("PetugasMedisId", "fs_kd_peg");
        bcp.AddMap("LayananId", "fs_kd_layanan");
        bcp.AddMap("IsUtama", "fb_utama");

        var fetched = listModel.ToList();
        bcp.BatchSize = fetched.Count;
        bcp.DestinationTableName = "td_peg_layanan";
        bcp.WriteToServer(fetched.AsDataTable());
    }

    public void Delete(IPetugasMedisKey key)
    {
        const string sql = @"
            DELETE FROM 
                td_peg_layanan
            WHERE 
                fs_kd_peg = @fs_kd_peg";
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_peg", key.PetugasMedisId, SqlDbType.VarChar);
        
        var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public IEnumerable<PetugasMedisLayananModel> ListData(IPetugasMedisKey filter)
    {
        const string sql = @"
            SELECT 
                aa.fs_kd_peg, aa.fs_kd_layanan, aa.fb_utama,
                ISNULL(bb.fs_nm_layanan, '') AS bb.fs_nm_layanan
            FROM 
                td_peg_layanan aa
                LEFT JOIN ta_layanan bb ON aa.fs_kd_layanan = bb.fs_kd_layanan
            WHERE 
                aa.fs_kd_peg = @fs_kd_peg ";
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_peg", filter.PetugasMedisId, SqlDbType.VarChar);
        
        var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.Query<PetugasMedisLayananDto>(sql, dp);
        var response = result.Select(x => x.ToModel());
        return response;
    }
}

public class PetugasMedisLayananDto
{
    public string fs_kd_peg { get; set; }
    public string fs_kd_layanan { get; set; }
    public string fs_nm_layanan { get; set; }
    public bool fb_utama { get; set; }

    public PetugasMedisLayananModel ToModel()
    {
        var result = new PetugasMedisLayananModel(fs_kd_peg, fs_kd_layanan, fs_nm_layanan);
        if (fb_utama) result.SetUtama();
        else result.UnsetUtama();
        
        return result;
    }
}