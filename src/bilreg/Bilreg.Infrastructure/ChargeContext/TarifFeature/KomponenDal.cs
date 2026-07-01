using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.ChargeContext.TarifFeature;

public interface IKomponenDal :
    IInsert<KomponenDto>,
    IUpdate<KomponenDto>,
    IDelete<IKomponenKey>,
    IGetData<KomponenDto, IKomponenKey>,
    IListData<KomponenDto>
{
}

public class KomponenDal : IKomponenDal
{
    private readonly DatabaseOptions _opt;

    public KomponenDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(KomponenDto dto)
    {
        const string sql = """
           INSERT INTO ta_detil_tarif(
               fs_kd_detil_tarif, fs_nm_detil_tarif, fs_kd_grup_detil_tarif, 
               fs_kd_rek, fs_kd_rek_diskon)
           VALUES( 
               @fs_kd_detil_tarif, @fs_nm_detil_tarif, @fs_kd_grup_detil_tarif,
               @fs_kd_rek, @fs_kd_rek_diskon)
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_detil_tarif", dto.fs_kd_detil_tarif, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_detil_tarif", dto.fs_nm_detil_tarif, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_grup_detil_tarif", dto.fs_kd_grup_detil_tarif, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_rek", dto.fs_kd_rek_pdpt, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_rek_diskon", dto.fs_kd_rek_diskon, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(KomponenDto dto)
    {
        const string sql = """
           UPDATE 
               ta_detil_tarif
           SET
               fs_kd_detil_tarif = @fs_kd_detil_tarif, 
               fs_nm_detil_tarif = @fs_nm_detil_tarif, 
               fs_kd_grup_detil_tarif = @fs_kd_grup_detil_tarif, 
               fs_kd_rek = @fs_kd_rek,
               fs_kd_rek_diskon = @fs_kd_rek_diskon
           WHERE 
              fs_kd_detil_tarif = @fs_kd_detil_tarif
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_detil_tarif", dto.fs_kd_detil_tarif, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_detil_tarif", dto.fs_nm_detil_tarif, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_grup_detil_tarif", dto.fs_kd_grup_detil_tarif, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_rek", dto.fs_kd_rek_pdpt, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_rek_diskon", dto.fs_kd_rek_diskon, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IKomponenKey key)
    {
        const string sql = """
           DELETE FROM 
               ta_detil_tarif
           WHERE 
              fs_kd_detil_tarif = @fs_kd_detil_tarif
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_detil_tarif", key.KomponenId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public KomponenDto GetData(IKomponenKey key)
    {
        const string sql = """
            SELECT
                aa.fs_kd_detil_tarif, aa.fs_nm_detil_tarif, aa.fs_kd_grup_detil_tarif,
                aa.fs_kd_rek AS fs_kd_rek_pdpt, aa.fs_kd_rek_diskon,
                ISNULL(bb.fs_nm_grup_detil_tarif, '') fs_nm_grup_detil_tarif,
                ISNULL(cc.fs_nm_rek, '') fs_nm_rek_pdpt,
                ISNULL(dd.fs_nm_rek, '') fs_nm_rek_diskon
            FROM
                ta_detil_tarif aa
                LEFT JOIN ta_grup_detil_tarif bb ON aa.fs_kd_grup_detil_tarif = bb.fs_kd_grup_detil_tarif
                LEFT JOIN t_rek cc ON aa.fs_kd_rek = cc.fs_kd_rek
                LEFT JOIN t_rek dd ON aa.fs_kd_rek_diskon = dd.fs_kd_rek
            WHERE
                aa.fs_kd_detil_tarif = @fs_kd_detil_tarif
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_detil_tarif", key.KomponenId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.ReadSingle<KomponenDto>(sql, dp);
        return result;
    }

    public IEnumerable<KomponenDto> ListData()
    {
        const string sql = """
           SELECT
               aa.fs_kd_detil_tarif, aa.fs_nm_detil_tarif, aa.fs_kd_grup_detil_tarif,
               aa.fs_kd_rek AS fs_kd_rek_pdpt, aa.fs_kd_rek_diskon,
               ISNULL(bb.fs_nm_grup_detil_tarif, '') fs_nm_grup_detil_tarif,
               ISNULL(cc.fs_nm_rek, '') fs_nm_rek_pdpt,
               ISNULL(dd.fs_nm_rek, '') fs_nm_rek_diskon
           FROM
               ta_detil_tarif aa
               LEFT JOIN ta_grup_detil_tarif bb ON aa.fs_kd_grup_detil_tarif = bb.fs_kd_grup_detil_tarif
               LEFT JOIN t_rek cc ON aa.fs_kd_rek = cc.fs_kd_rek
               LEFT JOIN t_rek dd ON aa.fs_kd_rek_diskon = dd.fs_kd_rek
           """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.Read<KomponenDto>(sql);
        return result;
    }
}
