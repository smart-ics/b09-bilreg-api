using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;

public interface IScheduleOpPpaDal:
    IInsertBulk<ScheduleOpPpaDto>,
    IDelete<IScheduleOpKey>,
    IListData<ScheduleOpPpaDto, IScheduleOpKey>
{
}

public class ScheduleOpPpaDal : IScheduleOpPpaDal
{
    private readonly DatabaseOptions _opt;

    public ScheduleOpPpaDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(IEnumerable<ScheduleOpPpaDto> listModel)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        using var bcp = new SqlBulkCopy(conn);
        
        conn.Open();
        bcp.AddMap("ScheduleOpId", "ScheduleOpId");
        bcp.AddMap("NoUrut", "NoUrut");
        bcp.AddMap("PpaId", "PpaId");
        bcp.AddMap("ProfesiId", "ProfesiId");
        bcp.AddMap("GroupSpesialisId", "GroupSpesialisId");

        var fetched = listModel.ToList();
        bcp.BatchSize = fetched.Count;
        bcp.DestinationTableName = "BILRG_ScheduleOpPpa";
        bcp.WriteToServer(fetched.AsDataTable());
    }

    public void Delete(IScheduleOpKey key)
    {
        const string sql = """
            DELETE FROM
                BILRG_ScheduleOpPpa
            WHERE
                ScheduleOpId = @ScheduleOpId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@ScheduleOpId", key.ScheduleOpId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);    
    }

    public IEnumerable<ScheduleOpPpaDto> ListData(IScheduleOpKey filter)
    {
        const string sql = """
            SELECT 
                aa.ScheduleOpId, aa.NoUrut, aa.PpaId, aa.ProfesiId, aa.GroupSpesialisId,
                ISNULL(bb.fs_nm_peg, '') AS PpaName,
                ISNULL(cc.ProfesiName, '') AS ProfesiName,
                ISNULL(dd.GroupSpesialisName, '') AS GroupSpesialisName
            FROM 
                BILRG_ScheduleOpPpa aa
                LEFT JOIN td_peg bb ON aa.PpaId = bb.fs_kd_peg
                LEFT JOIN BILRG_Profesi cc ON aa.ProfesiId = cc.ProfesiId
                LEFT JOIN BILRG_GroupSpesialis dd ON aa.GroupSpesialisId = dd.GroupSpesialisId
            WHERE
                aa.ScheduleOpId = @ScheduleOpId
            ORDER BY aa.NoUrut
            """;
        
        var dp = new DynamicParameters();
        dp.AddParam("@ScheduleOpId", filter.ScheduleOpId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<ScheduleOpPpaDto>(sql, dp);
    }
}