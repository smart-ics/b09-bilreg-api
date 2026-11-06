using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.ChargeContext.TindakanFeature;

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

    public void Insert(TindakanDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_Tindakan(
                TindakanId, TindakanDate, OrderTdkId, RegId, PasienId, PasienName, LayananId, LayananName, 
                KelasId, KelasName, TipeTarifId, TipeTarifName, TarifId, TarifName, Total, 
                IsDiskonDokter, DiskonDokter,
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
            VALUES(
                @TindakanId, @TindakanDate, @OrderTdkId, @RegId, @PasienId, @PasienName, @LayananId, @LayananName, 
                @KelasId, @KelasName, @TipeTarifId, @TipeTarifName, @TarifId, @TarifName, @Total, 
                @IsDiskonDokter, @DiskonDokter,
                @CrtUser, @CrtDate, @UpdUser, @UpdDate, @VodUser, @VodDate)
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@TindakanId", dto.TindakanId, SqlDbType.VarChar);
        dp.AddParam("@TindakanDate", dto.TindakanDate, SqlDbType.DateTime);
        dp.AddParam("@OrderTdkId", dto.OrderTdkId, SqlDbType.VarChar);

        dp.AddParam("@RegId", dto.RegId, SqlDbType.VarChar);
        dp.AddParam("@PasienId", dto.PasienId, SqlDbType.VarChar);
        dp.AddParam("@PasienName", dto.PasienName, SqlDbType.VarChar);
        dp.AddParam("@LayananId", dto.LayananId, SqlDbType.VarChar);
        dp.AddParam("@LayananName", dto.LayananName, SqlDbType.VarChar);
        
        dp.AddParam("@KelasId", dto.KelasId, SqlDbType.VarChar);
        dp.AddParam("@KelasName", dto.KelasName, SqlDbType.VarChar);
        dp.AddParam("@TipeTarifId", dto.TipeTarifId, SqlDbType.VarChar);
        dp.AddParam("@TipeTarifName", dto.TipeTarifName, SqlDbType.VarChar);
        dp.AddParam("@TarifId", dto.TarifId, SqlDbType.VarChar);
        dp.AddParam("@TarifName", dto.TarifName, SqlDbType.VarChar);
        dp.AddParam("@Total", dto.Total, SqlDbType.Decimal);
        dp.AddParam("@IsDiskonDokter", dto.IsDiskonDokter, SqlDbType.Bit);
        dp.AddParam("@DiskonDokter", dto.DiskonDokter, SqlDbType.Decimal);
        
        dp.AddParam("@CrtUser", dto.CrtUser, SqlDbType.VarChar);
        dp.AddParam("@CrtDate", dto.CrtDate, SqlDbType.DateTime);
        dp.AddParam("@UpdUser", dto.UpdUser, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", dto.UpdDate, SqlDbType.DateTime);
        dp.AddParam("@VodUser", dto.VodUser, SqlDbType.VarChar);
        dp.AddParam("@VodDate", dto.VodDate, SqlDbType.DateTime);

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
              Total = @Total, 
              IsDiskonDokter = @IsDiskonDokter,
              DiskonDokter = @DiskonDokter,
              CrtUser = @CrtUser, 
              CrtDate = @CrtDate,
              UpdUser = @UpdUser, 
              UpdDate = @UpdDate,
              VodUser = @VodUser, 
              VodDate = @VodDate
           WHERE
              TindakanId = @TindakanId
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@TindakanId", dto.TindakanId, SqlDbType.VarChar);
        dp.AddParam("@TindakanDate", dto.TindakanDate, SqlDbType.DateTime);
        dp.AddParam("@OrderTdkId", dto.OrderTdkId, SqlDbType.VarChar);

        dp.AddParam("@RegId", dto.RegId, SqlDbType.VarChar);
        dp.AddParam("@PasienId", dto.PasienId, SqlDbType.VarChar);
        dp.AddParam("@PasienName", dto.PasienName, SqlDbType.VarChar);
        dp.AddParam("@LayananId", dto.LayananId, SqlDbType.VarChar);
        dp.AddParam("@LayananName", dto.LayananName, SqlDbType.VarChar);

        dp.AddParam("@KelasId", dto.KelasId, SqlDbType.VarChar);
        dp.AddParam("@KelasName", dto.KelasName, SqlDbType.VarChar);
        dp.AddParam("@TipeTarifId", dto.TipeTarifId, SqlDbType.VarChar);
        dp.AddParam("@TipeTarifName", dto.TipeTarifName, SqlDbType.VarChar);
        dp.AddParam("@TarifId", dto.TarifId, SqlDbType.VarChar);
        dp.AddParam("@TarifName", dto.TarifName, SqlDbType.VarChar);
        dp.AddParam("@Total", dto.Total, SqlDbType.Decimal);
        dp.AddParam("@IsDiskonDokter", dto.IsDiskonDokter, SqlDbType.Bit);
        dp.AddParam("@DiskonDokter", dto.DiskonDokter, SqlDbType.Decimal);
        
        dp.AddParam("@CrtUser", dto.CrtUser, SqlDbType.VarChar);
        dp.AddParam("@CrtDate", dto.CrtDate, SqlDbType.DateTime);
        dp.AddParam("@UpdUser", dto.UpdUser, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", dto.UpdDate, SqlDbType.DateTime);
        dp.AddParam("@VodUser", dto.VodUser, SqlDbType.VarChar);
        dp.AddParam("@VodDate", dto.VodDate, SqlDbType.DateTime);

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
               aa.TindakanId, aa.TindakanDate, aa.OrderTdkId,  
               aa.RegId, aa.PasienId, aa.PasienName, aa.LayananId, aa.LayananName, 
               aa.KelasId, aa.KelasName, aa.TipeTarifId, aa.TipeTarifName, aa.TarifId, aa.TarifName, 
               ISNULL((SELECT TOP 1 CAST(tk.Qty AS INT) FROM BILRG_TindakanKomponen tk WHERE tk.TindakanId = aa.TindakanId), 1) AS Qty,
               aa.Total, 
               aa.IsDiskonDokter, aa.DiskonDokter,
               aa.CrtUser, aa.CrtDate, aa.UpdUser, aa.UpdDate,  aa.VodUser, aa.VodDate
           FROM
               BILRG_Tindakan aa
           	   LEFT JOIN BILRG_OrderTdk bb ON aa.OrderTdkId = bb.OrderTdkId
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
               aa.TindakanId, aa.TindakanDate, aa.OrderTdkId,  
               aa.RegId, aa.PasienId, aa.PasienName, aa.LayananId, aa.LayananName, 
               aa.KelasId, aa.KelasName, aa.TipeTarifId, aa.TipeTarifName, aa.TarifId, aa.TarifName, 
               ISNULL((SELECT TOP 1 CAST(tk.Qty AS INT) FROM BILRG_TindakanKomponen tk WHERE tk.TindakanId = aa.TindakanId), 1) AS Qty,
               aa.Total, 
               aa.IsDiskonDokter, aa.DiskonDokter,
               aa.CrtUser, aa.CrtDate, aa.UpdUser, aa.UpdDate,  aa.VodUser, aa.VodDate
           FROM
               BILRG_Tindakan aa
           WHERE
               aa.RegId = @RegId
               AND aa.VodDate = @VodDate
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
                aa.TindakanId AS TransaksiId, aa.TindakanDate AS TransaksiDate, 
           	    aa.OrderTdkId AS OrderTransaksiId, aa.RegId, aa.PasienId, aa.PasienName, 
           	    aa.LayananId, aa.LayananName, 
                aa.TarifId AS DiskripsiId, aa.TarifName AS DiskripsiName, 
           	    ISNULL((SELECT TOP 1 CAST(tk.Qty AS INT) FROM BILRG_TindakanKomponen tk WHERE tk.TindakanId = aa.TindakanId), 1) AS Qty,
                aa.Total, 'TINDAKAN' AS Tipe,
                aa.CrtUser, aa.CrtDate, aa.UpdUser, aa.UpdDate,  aa.VodUser, aa.VodDate
           FROM
                BILRG_Tindakan aa
           WHERE
                aa.RegId = @RegId
                AND aa.VodDate = @VodDate

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
                CONVERT(DATETIME, '3000-01-01') AS VodDate
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

           ORDER BY
                TransaksiDate DESC, TransaksiId DESC
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@RegId", regKey.RegId, SqlDbType.VarChar);
        dp.AddParam("@VodDate", new DateTime(3000, 1, 1), SqlDbType.DateTime);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<TindakanJualDto>(sql, dp);
    }
}