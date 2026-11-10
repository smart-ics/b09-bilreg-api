using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.PurchaseContext.DeliveryFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Infrastructure.PurchaseContext.DeliveryFeature;

public interface IDeliveryOrderDal :
    IInsert<DeliveryOrderDto>,
    IUpdate<DeliveryOrderDto>,
    IDelete<IDeliveryOrderKey>,
    IGetData<DeliveryOrderDto, IDeliveryOrderKey>,
    IListData<DeliveryOrderDto, Periode>
{
}

public class DeliveryOrderDal : IDeliveryOrderDal
{
    private const string DefaultVoidDate = "3000-01-01";
    private const string DefaultVoidTime = "00:00:00";
    private readonly DatabaseOptions _opt;

    public DeliveryOrderDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(DeliveryOrderDto dto)
    {
        const string sql = """
            INSERT INTO TB_TRS_DO (
                fs_kd_trs, fd_tgl_trs, fs_jam_trs, fs_kd_petugas,
                fd_tgl_void, fs_jam_void, fs_kd_petugas_void,
                fs_kd_po, fs_kd_tb, fs_kd_do_asli, fs_kd_layanan, fs_kd_iii,
                fs_kd_bap, fn_total, fs_kd_trs_hna, fb_pembelian_bebas,
                fs_keterangan, fn_genkeu, fs_kd_trs_fk, fn_status_bridge,
                CRTTGL, CRTJAM, CRTIPA, CRTVER, CRTUSR,
                UPDTGL, UPDJAM, UPDIPA, UPDVER, UPDUSR
            ) VALUES (
                @fs_kd_trs, @fd_tgl_trs, @fs_jam_trs, @fs_kd_petugas,
                @fd_tgl_void, @fs_jam_void, @fs_kd_petugas_void,
                @fs_kd_po, @fs_kd_tb, @fs_kd_do_asli, @fs_kd_layanan, @fs_kd_iii,
                @fs_kd_bap, @fn_total, @fs_kd_trs_hna, @fb_pembelian_bebas,
                @fs_keterangan, @fn_genkeu, @fs_kd_trs_fk, @fn_status_bridge,
                @CRTTGL, @CRTJAM, @CRTIPA, @CRTVER, @CRTUSR,
                @UPDTGL, @UPDJAM, @UPDIPA, @UPDVER, @UPDUSR
            )
            """;

        var now = DateTime.Now;
        var isVoided = dto.TglVoid.Year < 3000 && dto.TglVoid != default;
        var tglVoidStr = isVoided ? dto.TglVoid.ToString("yyyy-MM-dd") : DefaultVoidDate;
        var jamVoidStr = isVoided ? (string.IsNullOrWhiteSpace(dto.JamVoid) ? dto.TglVoid.ToString("HH:mm:ss") : dto.JamVoid) : DefaultVoidTime;
        var crtTgl = dto.CrtTgl != default && dto.CrtTgl.Year < 3000 ? dto.CrtTgl : (dto.DoDate != default ? dto.DoDate : now);

        var jamTrs = dto.DoDate != default && dto.DoDate.TimeOfDay != TimeSpan.Zero
            ? dto.DoDate.ToString("HH:mm:ss")
            : now.ToString("HH:mm:ss");

        var jamCrt = crtTgl != default && crtTgl.TimeOfDay != TimeSpan.Zero
            ? crtTgl.ToString("HH:mm:ss")
            : now.ToString("HH:mm:ss");

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_trs", dto.DoId ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_trs", dto.DoDate != default ? dto.DoDate.ToString("yyyy-MM-dd") : DateTime.Today.ToString("yyyy-MM-dd"), SqlDbType.VarChar);
        dp.AddParam("@fs_jam_trs", jamTrs, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_petugas", dto.PetugasId ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_void", tglVoidStr, SqlDbType.VarChar);
        dp.AddParam("@fs_jam_void", jamVoidStr, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_petugas_void", isVoided ? (dto.PetugasVoid ?? string.Empty) : string.Empty, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_po", dto.PoId ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_tb", string.Empty, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_do_asli", dto.SuratJalanNo ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_layanan", dto.LayananId ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_iii", dto.SupplierId ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_bap", string.Empty, SqlDbType.VarChar);
        dp.AddParam("@fn_total", dto.Total, SqlDbType.Decimal);
        dp.AddParam("@fs_kd_trs_hna", string.Empty, SqlDbType.VarChar);
        dp.AddParam("@fb_pembelian_bebas", dto.PembelianBebas, SqlDbType.Bit);
        dp.AddParam("@fs_keterangan", dto.Keterangan ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@fn_genkeu", 0m, SqlDbType.Decimal);
        dp.AddParam("@fs_kd_trs_fk", string.Empty, SqlDbType.VarChar);
        dp.AddParam("@fn_status_bridge", 0m, SqlDbType.Decimal);
        dp.AddParam("@CRTTGL", crtTgl.ToString("yyyy-MM-dd"), SqlDbType.VarChar);
        dp.AddParam("@CRTJAM", jamCrt, SqlDbType.VarChar);
        dp.AddParam("@CRTIPA", string.Empty, SqlDbType.VarChar);
        dp.AddParam("@CRTVER", string.Empty, SqlDbType.VarChar);
        dp.AddParam("@CRTUSR", dto.CrtUsr ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@UPDTGL", DefaultVoidDate, SqlDbType.VarChar);
        dp.AddParam("@UPDJAM", DefaultVoidTime, SqlDbType.VarChar);
        dp.AddParam("@UPDIPA", string.Empty, SqlDbType.VarChar);
        dp.AddParam("@UPDVER", string.Empty, SqlDbType.VarChar);
        dp.AddParam("@UPDUSR", string.Empty, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(DeliveryOrderDto dto)
    {
        const string sql = """
            UPDATE TB_TRS_DO SET
                fd_tgl_trs = @fd_tgl_trs,
                fs_jam_trs = @fs_jam_trs,
                fs_kd_petugas = @fs_kd_petugas,
                fd_tgl_void = @fd_tgl_void,
                fs_jam_void = @fs_jam_void,
                fs_kd_petugas_void = @fs_kd_petugas_void,
                fs_kd_po = @fs_kd_po,
                fs_kd_do_asli = @fs_kd_do_asli,
                fs_kd_layanan = @fs_kd_layanan,
                fs_kd_iii = @fs_kd_iii,
                fn_total = @fn_total,
                fb_pembelian_bebas = @fb_pembelian_bebas,
                fs_keterangan = @fs_keterangan,
                UPDTGL = @UPDTGL,
                UPDJAM = @UPDJAM,
                UPDUSR = @UPDUSR
            WHERE
                fs_kd_trs = @fs_kd_trs
            """;

        var isVoided = dto.TglVoid.Year < 3000 && dto.TglVoid != default;
        var tglVoidStr = isVoided ? dto.TglVoid.ToString("yyyy-MM-dd") : DefaultVoidDate;
        var jamVoidStr = isVoided ? (string.IsNullOrWhiteSpace(dto.JamVoid) ? dto.TglVoid.ToString("HH:mm:ss") : dto.JamVoid) : DefaultVoidTime;
        var updTgl = dto.UpdTgl != default && dto.UpdTgl.Year < 3000 ? dto.UpdTgl : DateTime.Now;

        var jamTrs = dto.DoDate != default && dto.DoDate.TimeOfDay != TimeSpan.Zero
            ? dto.DoDate.ToString("HH:mm:ss")
            : updTgl.ToString("HH:mm:ss");

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_trs", dto.DoId ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_trs", dto.DoDate != default ? dto.DoDate.ToString("yyyy-MM-dd") : DateTime.Today.ToString("yyyy-MM-dd"), SqlDbType.VarChar);
        dp.AddParam("@fs_jam_trs", jamTrs, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_petugas", dto.PetugasId ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_void", tglVoidStr, SqlDbType.VarChar);
        dp.AddParam("@fs_jam_void", jamVoidStr, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_petugas_void", isVoided ? (dto.PetugasVoid ?? string.Empty) : string.Empty, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_po", dto.PoId ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_do_asli", dto.SuratJalanNo ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_layanan", dto.LayananId ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_iii", dto.SupplierId ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@fn_total", dto.Total, SqlDbType.Decimal);
        dp.AddParam("@fb_pembelian_bebas", dto.PembelianBebas, SqlDbType.Bit);
        dp.AddParam("@fs_keterangan", dto.Keterangan ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@UPDTGL", updTgl.ToString("yyyy-MM-dd"), SqlDbType.VarChar);
        dp.AddParam("@UPDJAM", updTgl.ToString("HH:mm:ss"), SqlDbType.VarChar);
        dp.AddParam("@UPDUSR", dto.UpdUsr ?? string.Empty, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IDeliveryOrderKey key)
    {
        const string sql = """
            DELETE FROM TB_TRS_DO
            WHERE fs_kd_trs = @fs_kd_trs
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_trs", key.DeliveryOrderId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public DeliveryOrderDto GetData(IDeliveryOrderKey key)
    {
        var sql = $"""
            {SelectClause()}
            WHERE
                aa.fs_kd_trs = @fs_kd_trs
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_trs", key.DeliveryOrderId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<DeliveryOrderDto>(sql, dp);
    }

    public IEnumerable<DeliveryOrderDto> ListData(Periode filter)
    {
        var sql = $"""
            {SelectClause()}
            WHERE
                aa.fd_tgl_trs >= @Tgl1 AND aa.fd_tgl_trs <= @Tgl2
            ORDER BY
                aa.fd_tgl_trs, aa.fs_jam_trs, aa.fs_kd_trs
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@Tgl1", filter.Tgl1.ToString(DateFormatEnum.YMD), SqlDbType.VarChar);
        dp.AddParam("@Tgl2", filter.Tgl2.ToString(DateFormatEnum.YMD), SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<DeliveryOrderDto>(sql, dp) ?? Enumerable.Empty<DeliveryOrderDto>();
    }

    private static string SelectClause() => """
        SELECT
            aa.fs_kd_trs AS DoId,
            CASE WHEN ISDATE(aa.fd_tgl_trs) = 1 THEN CONVERT(DATETIME, aa.fd_tgl_trs + ' ' + aa.fs_jam_trs) ELSE '3000-01-01' END AS DoDate,
            aa.fs_kd_petugas AS PetugasId,
            ISNULL(NULLIF(LTRIM(RTRIM(pegCrt.fs_nm_peg)), ''), aa.fs_kd_petugas) AS PetugasName,
            ISNULL(NULLIF(LTRIM(RTRIM(pegConf.fs_nm_peg)), ''), ISNULL((SELECT TOP 1 ConfirmedBy FROM BILRG_DoInspeksi WHERE DoId = aa.fs_kd_trs AND ConfirmedAt IS NOT NULL), '')) AS ConfirmedByName,
            aa.fs_kd_po AS PoId,
            ISNULL(po.fd_tgl_trs, '') AS PoDate,
            aa.fs_kd_do_asli AS SuratJalanNo,
            aa.fs_kd_layanan AS LayananId,
            ISNULL(bb.fs_nm_layanan, '') AS LayananName,
            aa.fs_kd_iii AS SupplierId,
            ISNULL(cc.fs_nm_iii, '') AS SupplierName,
            aa.fn_total AS Total,
            aa.fs_keterangan AS Keterangan,
            aa.fb_pembelian_bebas AS PembelianBebas,
            ISNULL((SELECT TOP 1 [Status] FROM BILRG_DoInspeksi WHERE DoId = aa.fs_kd_trs), 
                   CASE WHEN ISDATE(aa.fd_tgl_void) = 1 AND CONVERT(DATETIME, aa.fd_tgl_void) < '3000-01-01' THEN 'Voided' ELSE 'Confirmed' END) AS [Status],
            CASE WHEN ISDATE(aa.fd_tgl_void) = 1 THEN CONVERT(DATETIME, aa.fd_tgl_void + ' ' + aa.fs_jam_void) ELSE '3000-01-01' END AS TglVoid,
            aa.fs_jam_void AS JamVoid,
            aa.fs_kd_petugas_void AS PetugasVoid,
            CASE WHEN ISDATE(aa.CRTTGL) = 1 THEN CONVERT(DATETIME, aa.CRTTGL + ' ' + aa.CRTJAM) ELSE '3000-01-01' END AS CrtTgl,
            aa.CRTUSR AS CrtUsr,
            CASE WHEN ISDATE(aa.UPDTGL) = 1 THEN CONVERT(DATETIME, aa.UPDTGL + ' ' + aa.UPDJAM) ELSE '3000-01-01' END AS UpdTgl,
            aa.UPDUSR AS UpdUsr
        FROM
            TB_TRS_DO aa
            LEFT JOIN ta_layanan bb ON aa.fs_kd_layanan = bb.fs_kd_layanan
            LEFT JOIN t_iii cc ON aa.fs_kd_iii = cc.fs_kd_iii
            LEFT JOIN tb_trs_po po ON aa.fs_kd_po = po.fs_kd_trs
            LEFT JOIN td_peg pegCrt ON aa.fs_kd_petugas = pegCrt.fs_kd_peg
            OUTER APPLY (
                SELECT TOP 1 ConfirmedBy
                FROM BILRG_DoInspeksi
                WHERE DoId = aa.fs_kd_trs AND ConfirmedAt IS NOT NULL
            ) insp
            LEFT JOIN td_peg pegConf ON insp.ConfirmedBy = pegConf.fs_kd_peg
        """;
}
