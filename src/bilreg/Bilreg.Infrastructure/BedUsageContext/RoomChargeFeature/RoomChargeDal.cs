using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.PakaiBedFeature;
using Bilreg.Domain.BedUsageContext.RoomChargeFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using System.Data;
using System.Data.SqlClient;

namespace Bilreg.Infrastructure.BedUsageContext.RoomChargeFeature;

public interface IRoomChargeDal :
    IInsert<RoomChargeDto>,
    IUpdate<RoomChargeDto>,
    IDelete<IRoomChargeKey>,
    IGetData<RoomChargeDto, IRoomChargeKey>,
    IListData<RoomChargeDto, IRegKey>,
    IListData<RoomChargeDto, IPakaiBed>
{ }
public class RoomChargeDal : IRoomChargeDal
{
    private readonly DatabaseOptions _opt;

    public RoomChargeDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    public void Insert(RoomChargeDto dto)
    {
        const string sql = """
            INSERT INTO ta_trs_roomcharge (
                fs_kd_trs, fs_kd_pakai_bed,
            	fd_tgl_trs, fs_jam_trs,
            	fs_kd_petugas, fs_kd_reg, 
            	fs_kd_layanan, fs_kd_bed,
            	fn_tarif, fn_diskon,
            	fn_total, fn_qty)
            VALUES (
                @fs_kd_trs, @fs_kd_pakai_bed,
            	@fd_tgl_trs, @fs_jam_trs,
            	@fs_kd_petugas, @fs_kd_reg, 
            	@fs_kd_layanan, @fs_kd_bed,
            	@fn_tarif, @fn_diskon,
            	@fn_total, @fn_qty)
            """;

        var dp = BuildParams(dto);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(RoomChargeDto dto)
    {
        const string sql = """
            UPDATE ta_trs_roomcharge
            SET fs_kd_pakai_bed = @fs_kd_pakai_bed,
            	fd_tgl_trs = @fd_tgl_trs, 
                fs_jam_trs = @fs_jam_trs,
            	fs_kd_petugas = @fs_kd_petugas, 
                fs_kd_reg = @fs_kd_reg, 
            	fs_kd_layanan = @fs_kd_layanan, 
                fs_kd_bed = @fs_kd_bed,
            	fn_tarif = @fn_tarif, 
                fn_diskon = @fn_diskon,
            	fn_total = @fn_total, 
                fn_qty = @fn_qty
            WHERE fs_kd_trs = @fs_kd_trs
            """;

        var dp = BuildParams(dto);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IRoomChargeKey key)
    {
        const string sql = """
            DELETE ta_trs_roomcharge WHERE fs_kd_trs = @RoomChargeId
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@RoomChargeId", key.RoomChargeId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public RoomChargeDto GetData(IRoomChargeKey key)
    {
        var sql = SelectFromClause() + " WHERE aa.fs_kd_trs = @RoomChargeId";
        var dp = new DynamicParameters();
        dp.AddParam("@RoomChargeId", key.RoomChargeId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<RoomChargeDto>(sql, dp);
    }
    

    public IEnumerable<RoomChargeDto> ListData(IRegKey regKey)
    {
        var sql = SelectFromClause() + " WHERE aa.fs_kd_reg = @RegId";
        var dp = new DynamicParameters();
        dp.AddParam("@RegId", regKey.RegId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<RoomChargeDto>(sql, dp);
    }

    public IEnumerable<RoomChargeDto> ListData(IPakaiBed pakaiKey)
    {
        var sql = SelectFromClause() + " WHERE aa.fs_kd_pakai_bed = @PakaiBedId";
        var dp = new DynamicParameters();
        dp.AddParam("@PakaiBedId", pakaiKey.PakaiBedId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<RoomChargeDto>(sql, dp);
    }

    private static string SelectFromClause() => """
        SELECT 
        	aa.fs_kd_trs, aa.fs_kd_pakai_bed,
        	aa.fd_tgl_trs, aa.fs_jam_trs,
        	aa.fs_kd_petugas, aa.fs_kd_reg, 
        	aa.fs_kd_layanan, aa.fs_kd_bed,
        	aa.fn_tarif, aa.fn_diskon,
        	aa.fn_total, aa.fs_ket,
        	aa.fn_qty, aa.fn_nilai_klaim,
        	aa.fs_kd_trs_dx,
        	ISNULL(bb.fs_mr,'-') AS fs_mr,
        	ISNULL(cc.fs_nm_pasien,'')AS fs_nm_pasien, 
        	ISNULL(dd.fs_nm_layanan,'') AS fs_nm_layanan, 
        	ISNULL(ee.fs_nm_bed,'') AS fs_nm_bed,
        	ISNULL(ee.fb_aktif, 0) AS fb_aktif_bed
        FROM 
        	ta_trs_roomcharge aa
        	LEFT JOIN ta_registrasi bb ON aa.fs_kd_reg = bb.fs_kd_reg
        	LEFT JOIN tc_mr cc ON bb.fs_mr = cc.fs_mr 
        	LEFT JOIN ta_layanan dd ON aa.fs_kd_layanan = dd.fs_kd_layanan 
        	LEFT JOIN ta_bed ee ON aa.fs_kd_bed = ee.fs_kd_bed 
        """;

    private static DynamicParameters BuildParams(RoomChargeDto dto)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_trs", dto.fs_kd_trs, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_pakai_bed", dto.fs_kd_pakai_bed, SqlDbType.VarChar);

        dp.AddParam("@fd_tgl_trs", dto.fd_tgl_trs, SqlDbType.VarChar);
        dp.AddParam("@fs_jam_trs", dto.fs_jam_trs, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_petugas", dto.fs_kd_petugas, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_reg", dto.fs_kd_reg, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_layanan", dto.fs_kd_layanan, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_bed", dto.fs_kd_bed, SqlDbType.VarChar);

        dp.AddParam("@fn_tarif", dto.fn_tarif, SqlDbType.Decimal);
        dp.AddParam("@fn_diskon", dto.fn_diskon, SqlDbType.Decimal);
        dp.AddParam("@fn_total", dto.fn_total, SqlDbType.Decimal);
        dp.AddParam("@fn_qty", dto.fn_qty, SqlDbType.Decimal);
        return dp;
    }
}
