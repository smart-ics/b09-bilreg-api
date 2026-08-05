using Dapper;
using Farinv.Domain.BrgContext.KlasifikasiFeature;
using Farinv.Infrastructure.Helpers;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using System.Data;
using System.Data.SqlClient;

namespace Farinv.Infrastructure.BrgContext.KlasifikasiFeature;

public interface IPabrikDal :
    IInsert<PabrikDto>,
    IUpdate<PabrikDto>,
    IDelete<IPabrikKey>,
    IGetData<PabrikDto, IPabrikKey>,
    IListData<PabrikDto>
{
}

public class PabrikDal : IPabrikDal
{
    private readonly DatabaseOptions _opt;

    public PabrikDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(PabrikDto dto)
    {
        const string sql = """
            INSERT INTO tb_pabrik (
                fs_kd_pabrik,
                fs_nm_pabrik,
                fn_koef_formula
            ) VALUES (
                @fs_kd_pabrik,
                @fs_nm_pabrik,
                @fn_koef_formula
            )
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_pabrik", dto.fs_kd_pabrik, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_pabrik", dto.fs_nm_pabrik, SqlDbType.VarChar);
        dp.AddParam("@fn_koef_formula", dto.fn_koef_formula, SqlDbType.Decimal);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(PabrikDto dto)
    {
        const string sql = """
            UPDATE tb_pabrik
            SET
                fs_nm_pabrik = @fs_nm_pabrik,
                fn_koef_formula = @fn_koef_formula
            WHERE
                fs_kd_pabrik = @fs_kd_pabrik
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_pabrik", dto.fs_kd_pabrik, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_pabrik", dto.fs_nm_pabrik, SqlDbType.VarChar);
        dp.AddParam("@fn_koef_formula", dto.fn_koef_formula, SqlDbType.Decimal);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IPabrikKey key)
    {
        const string sql = """
            DELETE FROM tb_pabrik
            WHERE fs_kd_pabrik = @fs_kd_pabrik
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_pabrik", key.PabrikId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public PabrikDto GetData(IPabrikKey key)
    {
        const string sql = """
            SELECT
                fs_kd_pabrik,
                fs_nm_pabrik,
                fn_koef_formula
            FROM tb_pabrik
            WHERE fs_kd_pabrik = @fs_kd_pabrik
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_pabrik", key.PabrikId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<PabrikDto>(sql, dp);
    }

    public IEnumerable<PabrikDto> ListData()
    {
        const string sql = """
            SELECT
                fs_kd_pabrik,
                fs_nm_pabrik,
                fn_koef_formula
            FROM tb_pabrik
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<PabrikDto>(sql);
    }
}
