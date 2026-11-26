using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.ChargeContext.TindakanFeature;

public interface ITindakanDal :
    IInsert<TindakanDto>,
    IUpdate<TindakanDto>,
    IDelete<ITindakanKey>,
    IGetData<TindakanDto, ITindakanKey>,
    IListData<TindakanDto, IRegKey>
{
    IEnumerable<TindakanJualDto> ListTdkJual(IRegKey regKey);
}

public class TindakanDal : ITindakanDal
{
    private readonly DatabaseOptions _opt;

    public TindakanDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    private static string SafeTruncate(string? val, int maxLen)
    {
        if (string.IsNullOrWhiteSpace(val)) return string.Empty;
        return val.Length > maxLen ? val[..maxLen] : val;
    }

    public void Insert(TindakanDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_Tindakan(
                TindakanId, TindakanDate, OrderTdkId, RegId, PakaiBedId, PasienId, PasienName,
                LayananId, LayananName, KelasId, KelasName, TipeTarifId, TipeTarifName,
                TarifId, TarifName, Qty, Total, IsDiskonDokter, DiskonDokter,
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
            VALUES(
                @TindakanId, @TindakanDate, @OrderTdkId, @RegId, @PakaiBedId, @PasienId, @PasienName,
                @LayananId, @LayananName, @KelasId, @KelasName, @TipeTarifId, @TipeTarifName,
                @TarifId, @TarifName, @Qty, @Total, @IsDiskonDokter, @DiskonDokter,
                @CrtUser, @CrtDate, @UpdUser, @UpdDate, @VodUser, @VodDate)
            """;

        var tglTrs = dto.TindakanDate == default ? DateTime.Now : dto.TindakanDate;
        var crtDate = dto.CrtDate == default ? DateTime.Now : dto.CrtDate;
        var updDate = dto.UpdDate == default ? new DateTime(3000, 1, 1) : dto.UpdDate;
        var vodDate = dto.VodDate == default ? new DateTime(3000, 1, 1) : dto.VodDate;

        var dp = new DynamicParameters();
        dp.AddParam("@TindakanId", SafeTruncate(dto.TindakanId, 26), SqlDbType.VarChar);
        dp.AddParam("@TindakanDate", tglTrs, SqlDbType.DateTime);
        dp.AddParam("@OrderTdkId", SafeTruncate(dto.OrderTdkId, 26), SqlDbType.VarChar);
        dp.AddParam("@RegId", SafeTruncate(dto.RegId, 10), SqlDbType.VarChar);
        dp.AddParam("@PakaiBedId", SafeTruncate(dto.PakaiBedId, 30), SqlDbType.VarChar);
        dp.AddParam("@PasienId", SafeTruncate(dto.PasienId, 15), SqlDbType.VarChar);
        dp.AddParam("@PasienName", SafeTruncate(dto.PasienName, 60), SqlDbType.VarChar);
        dp.AddParam("@LayananId", SafeTruncate(dto.LayananId, 5), SqlDbType.VarChar);
        dp.AddParam("@LayananName", SafeTruncate(dto.LayananName, 40), SqlDbType.VarChar);
        dp.AddParam("@KelasId", SafeTruncate(dto.KelasId, 3), SqlDbType.VarChar);
        dp.AddParam("@KelasName", SafeTruncate(dto.KelasName, 30), SqlDbType.VarChar);
        dp.AddParam("@TipeTarifId", SafeTruncate(dto.TipeTarifId, 2), SqlDbType.VarChar);
        dp.AddParam("@TipeTarifName", SafeTruncate(dto.TipeTarifName, 30), SqlDbType.VarChar);
        dp.AddParam("@TarifId", SafeTruncate(dto.TarifId, 10), SqlDbType.VarChar);
        dp.AddParam("@TarifName", SafeTruncate(dto.TarifName, 55), SqlDbType.VarChar);
        dp.AddParam("@Qty", dto.Qty <= 0 ? 1 : dto.Qty, SqlDbType.Int);
        dp.AddParam("@Total", dto.Total, SqlDbType.Decimal);
        dp.AddParam("@IsDiskonDokter", dto.IsDiskonDokter, SqlDbType.Bit);
        dp.AddParam("@DiskonDokter", dto.DiskonDokter, SqlDbType.Decimal);
        dp.AddParam("@CrtUser", SafeTruncate(dto.CrtUser, 50), SqlDbType.VarChar);
        dp.AddParam("@CrtDate", crtDate, SqlDbType.DateTime);
        dp.AddParam("@UpdUser", SafeTruncate(dto.UpdUser, 50), SqlDbType.VarChar);
        dp.AddParam("@UpdDate", updDate, SqlDbType.DateTime);
        dp.AddParam("@VodUser", SafeTruncate(dto.VodUser, 50), SqlDbType.VarChar);
        dp.AddParam("@VodDate", vodDate, SqlDbType.DateTime);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(TindakanDto dto)
    {
        const string sql = """
            UPDATE 
                BILRG_Tindakan
            SET
                TindakanDate = @TindakanDate,
                OrderTdkId = @OrderTdkId,
                RegId = @RegId,
                PakaiBedId = @PakaiBedId,
                PasienId = @PasienId,
                PasienName = @PasienName,
                LayananId = @LayananId,
                LayananName = @LayananName,
                KelasId = @KelasId,
                KelasName = @KelasName,
                TipeTarifId = @TipeTarifId,
                TipeTarifName = @TipeTarifName,
                TarifId = @TarifId,
                TarifName = @TarifName,
                Qty = @Qty,
                Total = @Total,
                IsDiskonDokter = @IsDiskonDokter,
                DiskonDokter = @DiskonDokter,
                UpdUser = @UpdUser,
                UpdDate = @UpdDate,
                VodUser = @VodUser,
                VodDate = @VodDate
            WHERE
                TindakanId = @TindakanId
            """;

        var tglTrs = dto.TindakanDate == default ? DateTime.Now : dto.TindakanDate;
        var updDate = dto.UpdDate == default ? DateTime.Now : dto.UpdDate;
        var vodDate = dto.VodDate == default ? new DateTime(3000, 1, 1) : dto.VodDate;

        var dp = new DynamicParameters();
        dp.AddParam("@TindakanId", SafeTruncate(dto.TindakanId, 26), SqlDbType.VarChar);
        dp.AddParam("@TindakanDate", tglTrs, SqlDbType.DateTime);
        dp.AddParam("@OrderTdkId", SafeTruncate(dto.OrderTdkId, 26), SqlDbType.VarChar);
        dp.AddParam("@RegId", SafeTruncate(dto.RegId, 10), SqlDbType.VarChar);
        dp.AddParam("@PakaiBedId", SafeTruncate(dto.PakaiBedId, 30), SqlDbType.VarChar);
        dp.AddParam("@PasienId", SafeTruncate(dto.PasienId, 15), SqlDbType.VarChar);
        dp.AddParam("@PasienName", SafeTruncate(dto.PasienName, 60), SqlDbType.VarChar);
        dp.AddParam("@LayananId", SafeTruncate(dto.LayananId, 5), SqlDbType.VarChar);
        dp.AddParam("@LayananName", SafeTruncate(dto.LayananName, 40), SqlDbType.VarChar);
        dp.AddParam("@KelasId", SafeTruncate(dto.KelasId, 3), SqlDbType.VarChar);
        dp.AddParam("@KelasName", SafeTruncate(dto.KelasName, 30), SqlDbType.VarChar);
        dp.AddParam("@TipeTarifId", SafeTruncate(dto.TipeTarifId, 2), SqlDbType.VarChar);
        dp.AddParam("@TipeTarifName", SafeTruncate(dto.TipeTarifName, 30), SqlDbType.VarChar);
        dp.AddParam("@TarifId", SafeTruncate(dto.TarifId, 10), SqlDbType.VarChar);
        dp.AddParam("@TarifName", SafeTruncate(dto.TarifName, 55), SqlDbType.VarChar);
        dp.AddParam("@Qty", dto.Qty <= 0 ? 1 : dto.Qty, SqlDbType.Int);
        dp.AddParam("@Total", dto.Total, SqlDbType.Decimal);
        dp.AddParam("@IsDiskonDokter", dto.IsDiskonDokter, SqlDbType.Bit);
        dp.AddParam("@DiskonDokter", dto.DiskonDokter, SqlDbType.Decimal);
        dp.AddParam("@UpdUser", SafeTruncate(dto.UpdUser, 50), SqlDbType.VarChar);
        dp.AddParam("@UpdDate", updDate, SqlDbType.DateTime);
        dp.AddParam("@VodUser", SafeTruncate(dto.VodUser, 50), SqlDbType.VarChar);
        dp.AddParam("@VodDate", vodDate, SqlDbType.DateTime);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(ITindakanKey key)
    {
        const string sql = """
            DELETE FROM
                BILRG_Tindakan
            WHERE
                TindakanId = @TindakanId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@TindakanId", key.TindakanId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public TindakanDto GetData(ITindakanKey key)
    {
        const string sql = """
            SELECT
                aa.TindakanId,
                aa.TindakanDate,
                aa.OrderTdkId,
                aa.RegId,
                aa.PakaiBedId,
                aa.PasienId,
                aa.PasienName,
                aa.LayananId,
                aa.LayananName,
                aa.KelasId,
                aa.KelasName,
                aa.TipeTarifId,
                aa.TipeTarifName,
                aa.TarifId,
                aa.TarifName,
                aa.Qty,
                aa.Total,
                aa.IsDiskonDokter,
                aa.DiskonDokter,
                aa.CrtUser,
                aa.CrtDate,
                aa.UpdUser,
                aa.UpdDate,
                aa.VodUser,
                aa.VodDate
            FROM
                BILRG_Tindakan aa
            WHERE
                aa.TindakanId = @TindakanId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@TindakanId", key.TindakanId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<TindakanDto>(sql, dp);
    }

    public IEnumerable<TindakanDto> ListData(IRegKey filter)
    {
        const string sql = """
            SELECT
                aa.TindakanId,
                aa.TindakanDate,
                aa.OrderTdkId,
                aa.RegId,
                aa.PakaiBedId,
                aa.PasienId,
                aa.PasienName,
                aa.LayananId,
                aa.LayananName,
                aa.KelasId,
                aa.KelasName,
                aa.TipeTarifId,
                aa.TipeTarifName,
                aa.TarifId,
                aa.TarifName,
                aa.Qty,
                aa.Total,
                aa.IsDiskonDokter,
                aa.DiskonDokter,
                aa.CrtUser,
                aa.CrtDate,
                aa.UpdUser,
                aa.UpdDate,
                aa.VodUser,
                aa.VodDate
            FROM
                BILRG_Tindakan aa
            WHERE
                aa.RegId = @RegId
                AND (aa.VodDate = @VodDate OR aa.VodDate >= '3000-01-01')
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@RegId", filter.RegId, SqlDbType.VarChar);
        dp.AddParam("@VodDate", new DateTime(3000, 1, 1), SqlDbType.DateTime);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<TindakanDto>(sql, dp);
    }

    public IEnumerable<TindakanJualDto> ListTdkJual(IRegKey regKey)
    {
        const string sql = """
            SELECT
                aa.TindakanId AS TransaksiId,
                aa.TindakanDate AS TransaksiDate, 
                aa.OrderTdkId AS OrderTransaksiId,
                aa.RegId AS RegId,
                aa.PasienId AS PasienId,
                aa.PasienName AS PasienName, 
                aa.LayananId AS LayananId,
                aa.LayananName AS LayananName, 
                aa.TarifId AS DiskripsiId,
                aa.TarifName AS DiskripsiName, 
                aa.Qty AS Qty,
                aa.Total AS Total,
                'TINDAKAN' AS Tipe,
                ISNULL(
                    NULLIF((SELECT TOP 1 tk.PpaName FROM BILRG_TindakanKomponen tk WHERE tk.TindakanId = aa.TindakanId AND ISNULL(tk.PpaName, '') <> '' ORDER BY tk.NoUrut), ''),
                    ISNULL(peg.fs_nm_peg, aa.CrtUser)
                ) AS CrtUser,
                aa.CrtDate AS CrtDate,
                aa.UpdUser AS UpdUser,
                aa.UpdDate AS UpdDate,
                aa.VodUser AS VodUser,
                aa.VodDate AS VodDate,
                aa.PakaiBedId
            FROM
                BILRG_Tindakan aa
                LEFT JOIN td_peg peg ON aa.CrtUser = peg.fs_kd_peg
            WHERE
                aa.RegId = @RegId
                AND (aa.VodDate = @VodDate OR aa.VodDate >= '3000-01-01')

            UNION ALL

            SELECT
                bb.fs_kd_trs2 AS TransaksiId,
                CONVERT(DATETIME, aa.fd_tgl_trs + ' ' + aa.fs_jam_trs) AS TransaksiDate,
                LTRIM(RTRIM(aa.fs_kd_resep)) AS OrderTransaksiId,
                aa.fs_kd_reg AS RegId,
                ISNULL(cc.fs_mr, '') AS PasienId,
                aa.fs_nm_pasien AS PasienName,
                aa.fs_kd_layanan AS LayananId,
                ISNULL(dd.fs_nm_layanan, '') AS LayananName,
                bb.fs_kd_barang AS DiskripsiId,
                bb.fs_nm_barang AS DiskripsiName,
                CAST(bb.fn_qty_barang AS INT) AS Qty,
                bb.fn_total AS Total,
                'BARANG' AS Tipe,
                aa.fs_kd_petugas AS CrtUser,
                CONVERT(DATETIME, aa.fd_tgl_trs + ' ' + aa.fs_jam_trs) AS CrtDate,
                '' AS UpdUser,
                CONVERT(DATETIME, '3000-01-01') AS UpdDate,
                '' AS VodUser,
                CONVERT(DATETIME, '3000-01-01') AS VodDate,
                '' AS PakaiBedId
            FROM
                tb_trs_dobill_umum aa
                INNER JOIN tb_trs_dobill_umum2 bb ON aa.fs_kd_trs = bb.fs_kd_trs
                LEFT JOIN ta_registrasi cc ON aa.fs_kd_reg = cc.fs_kd_reg
                LEFT JOIN ta_layanan dd ON aa.fs_kd_layanan = dd.fs_kd_layanan
            WHERE
                aa.fs_kd_reg = @RegId
                AND (aa.fd_tgl_void = '3000-01-01' OR LTRIM(RTRIM(aa.fs_kd_petugas_void)) = '')
                AND bb.fb_void = 0
                AND bb.fb_komponen = 0

            UNION ALL

            SELECT
                bb.BhpRanapId AS TransaksiId,
                bb.CrtDate AS TransaksiDate,
                '' AS OrderTransaksiId,
                bb.RegId,
                ISNULL(rg.FS_MR, '') AS PasienId,
                ISNULL(pmr.FS_NM_PASIEN, '') AS PasienName,
                bb.LayananId,
                ISNULL(ly.FS_NM_LAYANAN, '') AS LayananName,
                bb.BhpItemId AS DiskripsiId,
                bb.BhpItemName AS DiskripsiName,
                bb.Qty,
                (bb.Qty * bb.Price) AS Total,
                'BHP' AS Tipe,
                ISNULL(peg.fs_nm_peg, bb.CrtUser) AS CrtUser,
                bb.CrtDate,
                '' AS UpdUser,
                '3000-01-01' AS UpdDate,
                bb.VodUser,
                bb.VodDate,
                bb.PakaiBedId
            FROM
                BILRG_BhpRanap bb
                LEFT JOIN td_peg peg ON bb.CrtUser = peg.fs_kd_peg
                LEFT JOIN ta_registrasi rg ON bb.RegId = rg.FS_KD_REG
                LEFT JOIN tc_mr pmr ON rg.FS_MR = pmr.FS_MR
                LEFT JOIN ta_layanan ly ON bb.LayananId = ly.FS_KD_LAYANAN
            WHERE
                bb.RegId = @RegId
                AND bb.VodDate = @VodDate

            UNION ALL

            SELECT
                ISNULL(NULLIF(tb.TrsBillingId, ''), tb.TindakanBarangId) AS TransaksiId,
                aa.TindakanDate AS TransaksiDate,
                aa.OrderTdkId AS OrderTransaksiId,
                aa.RegId AS RegId,
                aa.PasienId AS PasienId,
                aa.PasienName AS PasienName,
                aa.LayananId AS LayananId,
                aa.LayananName AS LayananName,
                tb.BrgId AS DiskripsiId,
                ISNULL(b.fs_nm_barang, '') AS DiskripsiName,
                CAST(tb.QtyBom AS INT) AS Qty,
                tb.HargaJualTotal AS Total,
                'BARANG' AS Tipe,
                ISNULL(peg.fs_nm_peg, tb.CRTUSR) AS CrtUser,
                ISNULL(TRY_CONVERT(DATETIME, tb.CRTTGL + ' ' + tb.CRTJAM), '2000-01-01') AS CrtDate,
                tb.UPDUSR AS UpdUser,
                ISNULL(TRY_CONVERT(DATETIME, tb.UPDTGL + ' ' + tb.UPDJAM), '3000-01-01') AS UpdDate,
                '' AS VodUser,
                CONVERT(DATETIME, '3000-01-01') AS VodDate,
                '' AS PakaiBedId
            FROM
                BILRG_TindakanBarang tb
                INNER JOIN BILRG_Tindakan aa ON tb.TindakanId = aa.TindakanId
                LEFT JOIN tb_barang b ON tb.BrgId = b.fs_kd_barang
                LEFT JOIN td_peg peg ON tb.CRTUSR = peg.fs_kd_peg
            WHERE
                aa.RegId = @RegId
                AND (aa.VodDate = @VodDate OR aa.VodDate >= '3000-01-01')
                AND tb.IsVoid = 0
                AND tb.IsChargeBarang = 1

            ORDER BY
                TransaksiDate DESC, TransaksiId DESC
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@RegId", regKey.RegId, SqlDbType.VarChar);
        dp.AddParam("@VodDate", new DateTime(3000, 1, 1), SqlDbType.DateTime);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<TindakanJualDto>(sql, dp) ?? [];
    }
}
