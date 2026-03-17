using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using System.Data;
using System.Data.SqlClient;

namespace Bilreg.Infrastructure.PaymentContext.RegOutFeature;

public interface IRegPembayaranDal :
    IInsert<RegPembayaranDto>,
    IUpdate<RegPembayaranDto>,
    IDelete<RegPembayaranDto>,
    IListData<RegPembayaranDto, IRegKey>
{ }

public class RegPembayaranDal : IRegPembayaranDal
{
    private readonly DatabaseOptions _opt;
    public RegPembayaranDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(RegPembayaranDto dto)
    {
        const string sql = """
            INSERT INTO ta_registrasi3(
                fs_kd_reg, fs_kd_bayar, fn_jasa, fn_obat)
            VALUES(
                @RegId, @CaraBayarId, @NilaiJasa, @NilaiObat)
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@RegId", dto.fs_kd_reg, SqlDbType.VarChar);
        dp.AddParam("@CaraBayarId", dto.fs_kd_bayar, SqlDbType.VarChar);
        dp.AddParam("@NilaiJasa", dto.fn_jasa, SqlDbType.Decimal);
        dp.AddParam("@NilaiObat", dto.fn_obat, SqlDbType.Decimal);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(RegPembayaranDto dto)
    {
        const string sql = """
            UPDATE ta_registrasi3
            SET 
                fn_jasa = @NilaiJasa,
                fn_obat = @NilaiObat
            WHERE
                fs_kd_reg = @RegId
                AND fs_kd_bayar = @CaraBayarId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@RegId", dto.fs_kd_reg, SqlDbType.VarChar);
        dp.AddParam("@CaraBayarId", dto.fs_kd_bayar, SqlDbType.VarChar);
        dp.AddParam("@NilaiJasa", dto.fn_jasa, SqlDbType.Decimal);
        dp.AddParam("@NilaiObat", dto.fn_obat, SqlDbType.Decimal);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(RegPembayaranDto key)
    {
        const string sql = """
            DELETE FROM
                ta_registrasi3
            WHERE
               fs_kd_reg = @RegId 
               AND fs_kd_bayar = @CaraBayarId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@RegId", key.fs_kd_reg, SqlDbType.VarChar);
        dp.AddParam("@CaraBayarId", key.fs_kd_bayar, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public IEnumerable<RegPembayaranDto> ListData(IRegKey key)
    {
        const string sql = """
            SELECT 
                ISNULL(a.fs_kd_reg, @RegId) AS fs_kd_reg,
                'BYDPK' AS fs_kd_bayar, 
                'Bayar Di Muka' AS fs_nm_bayar, 
                ISNULL(a.fn_jasa,0) AS fn_jasa, 
                ISNULL(a.fn_obat,0) AS fn_obat
            FROM (SELECT 1 x) d
            LEFT JOIN ta_registrasi3 a ON a.fs_kd_reg = @RegId AND a.fs_kd_bayar = 'BYDPK'

            UNION ALL

            SELECT 
                ISNULL(a.fs_kd_reg, @RegId) AS fs_kd_reg,
                'BYDPU' AS fs_kd_bayar,
                'Deposit' AS fs_nm_bayar,
                ISNULL(a.fn_jasa,0) AS fn_jasa, 
                ISNULL(a.fn_obat,0) AS fn_obat
            FROM (SELECT 1 x) d
            LEFT JOIN ta_registrasi3 a ON a.fs_kd_reg = @RegId AND a.fs_kd_bayar = 'BYDPU'

            UNION ALL

            SELECT 
                ISNULL(a.fs_kd_reg, @RegId) AS fs_kd_reg,
                'BYPRI' AS fs_kd_bayar,
                'Hutang Pribadi' AS fs_nm_bayar,
                ISNULL(a.fn_jasa,0) AS fn_jasa, 
                ISNULL(a.fn_obat,0) AS fn_obat
            FROM (SELECT 1 x) d
            LEFT JOIN ta_registrasi3 a ON a.fs_kd_reg = @RegId AND a.fs_kd_bayar = 'BYPRI'

            UNION ALL

            SELECT   
                ISNULL(bb.fs_kd_reg, @RegId) AS fs_kd_reg,
                aa.fs_kd_tipe_jaminan AS fs_kd_bayar, 
                aa.fs_nm_tipe_jaminan AS fs_nm_bayar, 
                ISNULL(bb.fn_jasa,0) AS fn_jasa, 
                ISNULL(bb.fn_obat,0) AS fn_obat
            FROM ta_tipe_jaminan aa 
            LEFT JOIN ta_registrasi3 bb ON bb.fs_kd_reg = @RegId AND aa.fs_kd_tipe_jaminan = bb.fs_kd_bayar 
            WHERE aa.fb_subsidi = 1

            UNION ALL

            SELECT   
                ISNULL(aa.fs_kd_reg, @RegId) AS fs_kd_reg,
                aa.fs_kd_bayar AS fs_kd_bayar, 
                bb.fs_nm_tipe_jaminan AS fs_nm_bayar, 
                aa.fn_jasa AS fn_jasa, 
                aa.fn_obat AS fn_obat
            FROM ta_registrasi3 aa 
            INNER JOIN ta_tipe_jaminan bb ON aa.fs_kd_bayar = bb.fs_kd_tipe_jaminan 
            WHERE aa.fs_kd_reg = @RegId AND bb.fb_subsidi = 0

            UNION ALL

            SELECT 
                ISNULL(a.fs_kd_reg, @RegId) AS fs_kd_reg,
                'BYKAS' AS fs_kd_bayar,
                'Bayar Pribadi' AS fs_nm_bayar,
                ISNULL(a.fn_jasa,0) AS fn_jasa, 
                ISNULL(a.fn_obat,0) AS fn_obat
            FROM (SELECT 1 x) d
            LEFT JOIN ta_registrasi3 a ON a.fs_kd_reg = @RegId AND a.fs_kd_bayar = 'BYKAS'
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@RegId", key.RegId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<RegPembayaranDto>(sql, dp);
    }
}
