using Bilreg.Domain.AccountingContext.UnitFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using System.Data;
using System.Data.SqlClient;

namespace Bilreg.Infrastructure.AccountingContext.UnitFeature;

public interface IUnitGrupDal :
    IGetData<UnitGrupDto, IUnitGrupKey>,
    IListData<UnitGrupDto>
{ }
public class UnitGrupDal : IUnitGrupDal
{
    private readonly DatabaseOptions _opt;
    public UnitGrupDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    public UnitGrupDto GetData(IUnitGrupKey key)
    {
        const string sql = @"
                SELECT
                    fs_kd_unit_grup, fs_nm_unit_grup, 
                    fn_urut, fb_rugi_laba
                 FROM 
                    t_unit_grup
                 WHERE
                    fs_kd_unit_grup = @fs_kd_unit_grup
                 ";
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_unit_grup", key.UnitGrupId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<UnitGrupDto>(sql, dp);
    }

    public IEnumerable<UnitGrupDto> ListData()
    {
        const string sql = @"
                SELECT
                    fs_kd_unit_grup, fs_nm_unit_grup, 
                    fn_urut, fb_rugi_laba
                 FROM 
                    t_unit_grup
                 ";
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<UnitGrupDto>(sql);
    }
}

public record UnitGrupDto(
    string fs_kd_unit_grup,
    string fs_nm_unit_grup,
    int fn_urut,
    bool fb_rugi_laba)
{
    public UnitGrupType ToModel()
    {
        return new UnitGrupType(fs_kd_unit_grup, fs_nm_unit_grup,
                (int)fn_urut, (bool)fb_rugi_laba);
    }
}
