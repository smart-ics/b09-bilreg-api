using Bilreg.Domain.AccountingContext.CoaFeature;
using Bilreg.Infrastructure.AccountingContext.UnitFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using System.Data;
using System.Data.SqlClient;

namespace Bilreg.Infrastructure.AccountingContext.CoaFeature;

public interface ICoaDal :
    IGetData<CoaDto, ICoaKey>,
    IListData<CoaDto>
{}

public class CoaDal : ICoaDal
{
    private readonly DatabaseOptions _opt;
    public CoaDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    CoaDto IGetData<CoaDto, ICoaKey>.GetData(ICoaKey key)
    {
        const string sql = @"
                SELECT
                    fs_kd_rek, fs_nm_rek, aa.fs_kd_rek_tipe, 
                    ISNULL(bb.fs_nm_rek_tipe, '') AS fs_nm_rek_tipe
                 FROM 
                    t_rek aa
                    LEFT JOIN t_rek_tipe bb ON aa.fs_kd_rek_tipe = bb.fs_kd_rek_tipe
                 WHERE
                    fs_kd_rek = @fs_kd_rek
                 ";
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_rek", key.CoaId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<CoaDto>(sql, dp);
    }

    IEnumerable<CoaDto> IListData<CoaDto>.ListData()
    {
        const string sql = @"
                SELECT
                    fs_kd_rek, fs_nm_rek, aa.fs_kd_rek_tipe, 
                    ISNULL(bb.fs_nm_rek_tipe, '') AS fs_nm_rek_tipe
                 FROM 
                    t_rek aa
                    LEFT JOIN t_rek_tipe bb ON aa.fs_kd_rek_tipe = bb.fs_kd_rek_tipe
                 ";
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<CoaDto>(sql);
    }
}
