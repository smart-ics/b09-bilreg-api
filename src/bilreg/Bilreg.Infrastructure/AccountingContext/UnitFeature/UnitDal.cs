using Bilreg.Domain.AccountingContext.UnitFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using System.Data;
using System.Data.SqlClient;

namespace Bilreg.Infrastructure.AccountingContext.UnitFeature;

public interface IUnitDal :
    IGetData<UnitDto, IUnitKey>,
    IListData<UnitDto>
{}
public class UnitDal : IUnitDal
{
    private readonly DatabaseOptions _opt;
    public UnitDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    public UnitDto GetData(IUnitKey key)
    {
        const string sql = @"
                SELECT
                    aa.fs_kd_unit, aa.fs_nm_unit, aa.fn_urut, 
                    aa.fb_rugi_laba, aa.fs_kd_unit_grup, 
                    ISNULL(fs_nm_unit_grup, '') AS fs_nm_unit_grup
                 FROM 
                    t_unit aa
                    LEFT JOIN t_unit_grup bb 
                        ON aa.fs_kd_unit_grup = bb.fs_kd_unit_grup
                 WHERE
                    aa.fs_kd_unit = @fs_kd_unit
                 ";
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_unit", key.UnitId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<UnitDto>(sql, dp);
    }

    public IEnumerable<UnitDto> ListData()
    {
        const string sql = @"
                SELECT
                    aa.fs_kd_unit, aa.fs_nm_unit, aa.fn_urut, 
                    aa.fb_rugi_laba, aa.fs_kd_unit_grup, 
                    ISNULL(fs_nm_unit_grup, '') AS fs_nm_unit_grup
                 FROM 
                    t_unit aa
                    LEFT JOIN t_unit_grup bb 
                        ON aa.fs_kd_unit_grup = bb.fs_kd_unit_grup
                 ";
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<UnitDto>(sql);
    }
}

public record UnitDto(
    string fs_kd_unit, 
    string fs_nm_unit,
    int fn_urut,
    bool fb_rugi_laba,
    string fs_kd_unit_grup,
    string fs_nm_unit_grup)
{
    public UnitType ToModel()
    {
        return new UnitType(fs_kd_unit, fs_nm_unit, 
            (int)fn_urut, (bool)fb_rugi_laba,
            new UnitGrupReff(fs_kd_unit_grup, fs_nm_unit_grup));
    }
}
