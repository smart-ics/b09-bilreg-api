using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

// ReSharper disable InconsistentNaming

namespace Bilreg.Infrastructure.BedUsageContext.WardFeature;
public interface IKelasDkDal:
    IGetData<KelasDkDto, IKelasDkKey>,
    IListData<KelasDkDto>
{
}

public class KelasDkDal : IKelasDkDal
{
    private readonly DatabaseOptions _opt;

    public KelasDkDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public KelasDkDto GetData(IKelasDkKey key)
    {
        const string sql = """
            SELECT fs_kd_kelas_dk,fs_nm_kelas_dk
            FROM ta_kelas_dk
            WHERE fs_kd_kelas_dk = @fs_kd_kelas_dk
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kelas_dk", key.KelasDkId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.ReadSingle<KelasDkDto>(sql, dp);
        return result;

    }

    public IEnumerable<KelasDkDto> ListData()
    {
        const string sql = """
            SELECT fs_kd_kelas_dk, fs_nm_kelas_dk
            FROM ta_kelas_dk
            """;
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.Read<KelasDkDto>(sql);
        return result;
    }
}