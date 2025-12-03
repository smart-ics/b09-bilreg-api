using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.ChargeContext.TarifFeature;

public interface IKomponenSatTugasDal:
    IInsertBulk<KomponenSatTugasDto>,
    IDelete<IKomponenKey>,
    IListData<KomponenSatTugasDto, IKomponenKey>
{
}

public class KomponenSatTugasDal : IKomponenSatTugasDal
{
    private readonly DatabaseOptions _opt;

    public KomponenSatTugasDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(IEnumerable<KomponenSatTugasDto> listModel)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        using var bcp = new SqlBulkCopy(conn);
        
        conn.Open();
        bcp.AddMap("fs_kd_detil_tarif", "fs_kd_detil_tarif");
        bcp.AddMap("fs_kd_sat_tugas", "fs_kd_sat_tugas");

        var fetched = listModel.ToList();
        bcp.BatchSize = fetched.Count;
        bcp.DestinationTableName = "ta_detil_tarif2";
        bcp.WriteToServer(fetched.AsDataTable());
    }

    public void Delete(IKomponenKey key)
    {
        const string sql = """
            DELETE FROM
                ta_detil_tarif2
            WHERE
                fs_kd_detil_tarif = @fs_kd_detil_tarif
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_detil_tarif", key.KomponenId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);    }

    public IEnumerable<KomponenSatTugasDto> ListData(IKomponenKey filter)
    {
        const string sql = """
            SELECT 
                aa.fs_kd_detil_tarif, aa.fs_kd_sat_tugas, 
                ISNULL(bb.fs_nm_sat_tugas, '') AS fs_nm_sat_tugas,
                ISNULL(bb.fs_kd_profesi, '') AS fs_kd_profesi,
                ISNULL(cc.ProfesiName, '') AS fs_nm_profesi
            FROM 
                ta_detil_tarif2 aa
                LEFT JOIN td_sat_tugas bb ON aa.fs_kd_sat_tugas = bb.fs_kd_sat_tugas
                LEFT JOIN BILRG_Profesi cc ON bb.fs_kd_profesi = cc.ProfesiId
            WHERE
                aa.fs_kd_detil_tarif = @fs_kd_detil_tarif
            """;
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_detil_tarif", filter.KomponenId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<KomponenSatTugasDto>(sql, dp);
    }
}
