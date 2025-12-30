using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.ChargeContext.TarifFeature;

public interface INilaiTarifDal :
    IInsert<NilaiTarifDto>,
    IUpdate<NilaiTarifDto>,
    IDelete<INilaiTarifKey>,
    IGetData<NilaiTarifDto, INilaiTarifKey>,
    IListData<NilaiTarifDto, ITarifKey>,
    IListData<NilaiTarifDto, ILayananKey, INilaiTarifVariant>
{
}

public class NilaiTarifDal : INilaiTarifDal
{
    private readonly DatabaseOptions _opt;

    public NilaiTarifDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    
    public void Insert(NilaiTarifDto dto)
    {
        const string sql = """
           INSERT INTO BILRG_NilaiTarif(
               NilaiTarifId, TarifId, TipeTarifId, KelasId, Nilai)
           VALUES( 
               @NilaiTarifId, @TarifId, @TipeTarifId, @KelasId, @Nilai)
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@NilaiTarifId", dto.NilaiTarifId, SqlDbType.VarChar);
        dp.AddParam("@TarifId", dto.TarifId, SqlDbType.VarChar);
        dp.AddParam("@TipeTarifId", dto.TipeTarifId, SqlDbType.VarChar);
        dp.AddParam("@KelasId", dto.KelasId, SqlDbType.VarChar);
        dp.AddParam("@Nilai", dto.Nilai, SqlDbType.Decimal);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(NilaiTarifDto dto)
    {
        const string sql = """
           UPDATE 
               BILRG_NilaiTarif
           SET
               TarifId = @TarifId,
               TipeTarifId = @TipeTarifId,
               KelasId = @KelasId,
               Nilai = @Nilai
           WHERE
               NilaiTarifId = @NilaiTarifId
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@NilaiTarifId", dto.NilaiTarifId, SqlDbType.VarChar);
        dp.AddParam("@TarifId", dto.TarifId, SqlDbType.VarChar);
        dp.AddParam("@TipeTarifId", dto.TipeTarifId, SqlDbType.VarChar);
        dp.AddParam("@KelasId", dto.KelasId, SqlDbType.VarChar);
        dp.AddParam("@Nilai", dto.Nilai, SqlDbType.Decimal);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(INilaiTarifKey key)
    {
        const string sql = """
           DELETE FROM 
                BILRG_NilaiTarif
           WHERE
               NilaiTarifId = @NilaiTarifId
           """;
        
        var dp = new DynamicParameters();
        dp.AddParam("@NilaiTarifId", key.NilaiTarifId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public NilaiTarifDto GetData(INilaiTarifKey key)
    {
        const string sql = """
           SELECT
               aa.NilaiTarifId, aa.TarifId, aa.TipeTarifId, aa.KelasId, aa.Nilai,
               ISNULL(bb.fs_nm_tarif, '') AS TarifName,
               ISNULL(cc.fs_nm_tarif_tipe, '') AS TipeTarifName,
               ISNULL(dd.fs_nm_kelas, '') AS KelasName
           FROM 
               BILRG_NilaiTarif aa
               LEFT JOIN ta_tarif bb ON aa.TarifId = bb.fs_kd_tarif
               LEFT JOIN ta_tarif_tipe cc ON aa.TipeTarifId = cc.fs_kd_tarif_tipe
               LEFT JOIN ta_kelas dd ON aa.KelasId = dd.fs_kd_kelas
           WHERE
               aa.NilaiTarifId = @NilaiTarifId
           """;
        
        var dp = new DynamicParameters();
        dp.AddParam("@NilaiTarifId", key.NilaiTarifId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.ReadSingle<NilaiTarifDto>(sql, dp);
        return result;
    }

    public IEnumerable<NilaiTarifDto> ListData(ITarifKey key)
    {
        const string sql = """
            SELECT
                aa.NilaiTarifId, aa.TarifId, aa.TipeTarifId, aa.KelasId, aa.Nilai,
                ISNULL(bb.fs_nm_tarif, '') AS TarifName,
                ISNULL(cc.fs_nm_tarif_tipe, '') AS TipeTarifName,
                ISNULL(dd.fs_nm_kelas, '') AS KelasName
            FROM 
                BILRG_NilaiTarif aa
                LEFT JOIN ta_tarif bb ON aa.TarifId = bb.fs_kd_tarif
                LEFT JOIN ta_tarif_tipe cc ON aa.TipeTarifId = cc.fs_kd_tarif_tipe
                LEFT JOIN ta_kelas dd ON aa.KelasId = dd.fs_kd_kelas
            WHERE
                aa.TarifId = @TarifId
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@TarifId", key.TarifId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<NilaiTarifDto>(sql, dp);
    }

    public IEnumerable<NilaiTarifDto> ListData(ILayananKey layanan, INilaiTarifVariant variant)
    {
        const string sql = """
           SELECT
               aa.NilaiTarifId, aa.TarifId, aa.TipeTarifId, aa.KelasId, aa.Nilai,
               ISNULL(bb.fs_nm_tarif, '') AS TarifName,
               ISNULL(cc.fs_nm_tarif_tipe, '') AS TipeTarifName,
               ISNULL(dd.fs_nm_kelas, '') AS KelasName
           FROM 
               BILRG_NilaiTarif aa
               LEFT JOIN ta_tarif bb ON aa.TarifId = bb.fs_kd_tarif
               LEFT JOIN ta_tarif_tipe cc ON aa.TipeTarifId = cc.fs_kd_tarif_tipe
               LEFT JOIN ta_kelas dd ON aa.KelasId = dd.fs_kd_kelas
               LEFT JOIN ta_tarif4 ee ON aa.TarifId = ee.fs_kd_tarif
           WHERE
               ee.fs_kd_layanan = @LayananId
               AND aa.TipeTarifId = @TipeTarifId
               AND aa.KelasId = @KelasId
           """;
        var dp = new DynamicParameters();
        dp.AddParam("@LayananId", layanan.LayananId, SqlDbType.VarChar);
        dp.AddParam("@TipeTarifId", variant.TipeTarifId, SqlDbType.VarChar);
        dp.AddParam("@KelasId", variant.KelasId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<NilaiTarifDto>(sql, dp);
    }
}