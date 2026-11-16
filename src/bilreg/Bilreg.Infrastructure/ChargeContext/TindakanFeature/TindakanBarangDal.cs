using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.ChargeContext.TindakanFeature;

public interface ITindakanBarangDal :
    IInsert<TindakanBarangDto>,
    IInsertBulk<TindakanBarangDto>,
    IListData<TindakanBarangDto, ITindakanKey>
{
    IEnumerable<TindakanBarangDto> ListByTindakanId(string tindakanId);
    TindakanBarangDto? GetData(string tindakanBarangId);
    void MarkVoid(string tindakanId, string updUser = "");
}

public class TindakanBarangDal : ITindakanBarangDal
{
    private readonly DatabaseOptions _opt;

    public TindakanBarangDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    private static string SafeTruncate(string? val, int maxLen)
    {
        if (string.IsNullOrWhiteSpace(val)) return string.Empty;
        return val.Length > maxLen ? val[..maxLen] : val;
    }

    private static DynamicParameters CreateParameters(TindakanBarangDto dto)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@TindakanBarangId", SafeTruncate(dto.TindakanBarangId, 26), SqlDbType.VarChar);
        dp.AddParam("@TindakanId", SafeTruncate(dto.TindakanId, 26), SqlDbType.VarChar);
        dp.AddParam("@NoUrut", dto.NoUrut, SqlDbType.Int);
        dp.AddParam("@BrgId", SafeTruncate(dto.BrgId, 13), SqlDbType.VarChar);
        dp.AddParam("@SatuanBomId", SafeTruncate(dto.SatuanBomId, 5), SqlDbType.VarChar);
        dp.AddParam("@QtyBom", dto.QtyBom, SqlDbType.Decimal);
        dp.AddParam("@SatuanKecilId", SafeTruncate(dto.SatuanKecilId, 5), SqlDbType.VarChar);
        dp.AddParam("@QtyKecilRequested", dto.QtyKecilRequested, SqlDbType.Decimal);
        dp.AddParam("@QtyKecilDeducted", dto.QtyKecilDeducted, SqlDbType.Decimal);
        dp.AddParam("@QtyKecilPending", dto.QtyKecilPending, SqlDbType.Decimal);
        dp.AddParam("@IsChargeBarang", dto.IsChargeBarang, SqlDbType.Bit);
        dp.AddParam("@HargaJualTotal", dto.HargaJualTotal, SqlDbType.Decimal);
        dp.AddParam("@PakaiBrgId", SafeTruncate(dto.PakaiBrgId, 13), SqlDbType.VarChar);
        dp.AddParam("@TrsBillingId", SafeTruncate(dto.TrsBillingId, 26), SqlDbType.VarChar);
        dp.AddParam("@IsVoid", dto.IsVoid, SqlDbType.Bit);
        dp.AddParam("@CRTTGL", SafeTruncate(dto.CRTTGL, 10), SqlDbType.VarChar);
        dp.AddParam("@CRTJAM", SafeTruncate(dto.CRTJAM, 8), SqlDbType.VarChar);
        dp.AddParam("@CRTIPA", SafeTruncate(dto.CRTIPA, 40), SqlDbType.VarChar);
        dp.AddParam("@CRTVER", SafeTruncate(dto.CRTVER, 12), SqlDbType.VarChar);
        dp.AddParam("@CRTUSR", SafeTruncate(dto.CRTUSR, 50), SqlDbType.VarChar);
        dp.AddParam("@UPDTGL", SafeTruncate(dto.UPDTGL, 10), SqlDbType.VarChar);
        dp.AddParam("@UPDJAM", SafeTruncate(dto.UPDJAM, 8), SqlDbType.VarChar);
        dp.AddParam("@UPDIPA", SafeTruncate(dto.UPDIPA, 40), SqlDbType.VarChar);
        dp.AddParam("@UPDVER", SafeTruncate(dto.UPDVER, 12), SqlDbType.VarChar);
        dp.AddParam("@UPDUSR", SafeTruncate(dto.UPDUSR, 50), SqlDbType.VarChar);
        return dp;
    }

    public void Insert(TindakanBarangDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_TindakanBarang (
                TindakanBarangId, TindakanId, NoUrut, BrgId, SatuanBomId, QtyBom,
                SatuanKecilId, QtyKecilRequested, QtyKecilDeducted, QtyKecilPending,
                IsChargeBarang, HargaJualTotal, PakaiBrgId, TrsBillingId, IsVoid,
                CRTTGL, CRTJAM, CRTIPA, CRTVER, CRTUSR, UPDTGL, UPDJAM, UPDIPA, UPDVER, UPDUSR
            )
            VALUES (
                @TindakanBarangId, @TindakanId, @NoUrut, @BrgId, @SatuanBomId, @QtyBom,
                @SatuanKecilId, @QtyKecilRequested, @QtyKecilDeducted, @QtyKecilPending,
                @IsChargeBarang, @HargaJualTotal, @PakaiBrgId, @TrsBillingId, @IsVoid,
                @CRTTGL, @CRTJAM, @CRTIPA, @CRTVER, @CRTUSR, @UPDTGL, @UPDJAM, @UPDIPA, @UPDVER, @UPDUSR
            )
            """;

        var dp = CreateParameters(dto);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Insert(IEnumerable<TindakanBarangDto> listModel)
    {
        var list = listModel?.ToList() ?? [];
        if (list.Count == 0) return;

        const string sql = """
            INSERT INTO BILRG_TindakanBarang (
                TindakanBarangId, TindakanId, NoUrut, BrgId, SatuanBomId, QtyBom,
                SatuanKecilId, QtyKecilRequested, QtyKecilDeducted, QtyKecilPending,
                IsChargeBarang, HargaJualTotal, PakaiBrgId, TrsBillingId, IsVoid,
                CRTTGL, CRTJAM, CRTIPA, CRTVER, CRTUSR, UPDTGL, UPDJAM, UPDIPA, UPDVER, UPDUSR
            )
            VALUES (
                @TindakanBarangId, @TindakanId, @NoUrut, @BrgId, @SatuanBomId, @QtyBom,
                @SatuanKecilId, @QtyKecilRequested, @QtyKecilDeducted, @QtyKecilPending,
                @IsChargeBarang, @HargaJualTotal, @PakaiBrgId, @TrsBillingId, @IsVoid,
                @CRTTGL, @CRTJAM, @CRTIPA, @CRTVER, @CRTUSR, @UPDTGL, @UPDJAM, @UPDIPA, @UPDVER, @UPDUSR
            )
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Open();
        foreach (var dto in list)
        {
            var dp = CreateParameters(dto);
            conn.Execute(sql, dp);
        }
    }

    public IEnumerable<TindakanBarangDto> ListData(ITindakanKey filter)
    {
        return ListByTindakanId(filter.TindakanId);
    }

    public IEnumerable<TindakanBarangDto> ListByTindakanId(string tindakanId)
    {
        const string sql = """
            SELECT 
                tb.TindakanBarangId, tb.TindakanId, tb.NoUrut, tb.BrgId,
                ISNULL(b.fs_nm_barang, '') AS BrgName,
                tb.SatuanBomId, tb.QtyBom,
                tb.SatuanKecilId, tb.QtyKecilRequested, tb.QtyKecilDeducted, tb.QtyKecilPending,
                ISNULL(sp.QtyPending, tb.QtyKecilPending) AS QtyPending,
                tb.IsChargeBarang, tb.HargaJualTotal,
                ISNULL(NULLIF(tb.PakaiBrgId, ''), ISNULL(sp.PakaiBrgId, '')) AS PakaiBrgId,
                tb.TrsBillingId, tb.IsVoid,
                tb.CRTTGL, tb.CRTJAM, tb.CRTIPA, tb.CRTVER, tb.CRTUSR,
                tb.UPDTGL, tb.UPDJAM, tb.UPDIPA, tb.UPDVER, tb.UPDUSR
            FROM 
                BILRG_TindakanBarang tb
                LEFT JOIN tb_barang b ON tb.BrgId = b.fs_kd_barang
                LEFT JOIN BILRG_TindakanStokPending sp ON tb.TindakanBarangId = sp.TindakanBarangId AND sp.IsVoid = 0
            WHERE 
                tb.TindakanId = @TindakanId
            ORDER BY 
                tb.NoUrut ASC, tb.TindakanBarangId ASC
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@TindakanId", tindakanId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<TindakanBarangDto>(sql, dp) ?? [];
    }

    public TindakanBarangDto? GetData(string tindakanBarangId)
    {
        const string sql = """
            SELECT 
                tb.TindakanBarangId, tb.TindakanId, tb.NoUrut, tb.BrgId,
                ISNULL(b.fs_nm_barang, '') AS BrgName,
                tb.SatuanBomId, tb.QtyBom,
                tb.SatuanKecilId, tb.QtyKecilRequested, tb.QtyKecilDeducted, tb.QtyKecilPending,
                ISNULL(sp.QtyPending, tb.QtyKecilPending) AS QtyPending,
                tb.IsChargeBarang, tb.HargaJualTotal,
                ISNULL(NULLIF(tb.PakaiBrgId, ''), ISNULL(sp.PakaiBrgId, '')) AS PakaiBrgId,
                tb.TrsBillingId, tb.IsVoid,
                tb.CRTTGL, tb.CRTJAM, tb.CRTIPA, tb.CRTVER, tb.CRTUSR,
                tb.UPDTGL, tb.UPDJAM, tb.UPDIPA, tb.UPDVER, tb.UPDUSR
            FROM 
                BILRG_TindakanBarang tb
                LEFT JOIN tb_barang b ON tb.BrgId = b.fs_kd_barang
                LEFT JOIN BILRG_TindakanStokPending sp ON tb.TindakanBarangId = sp.TindakanBarangId AND sp.IsVoid = 0
            WHERE 
                tb.TindakanBarangId = @TindakanBarangId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@TindakanBarangId", tindakanBarangId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<TindakanBarangDto>(sql, dp);
    }

    public void MarkVoid(string tindakanId, string updUser = "")
    {
        const string sql = """
            UPDATE BILRG_TindakanBarang
            SET 
                IsVoid = 1,
                UPDTGL = @UPDTGL,
                UPDJAM = @UPDJAM,
                UPDUSR = @UPDUSR
            WHERE 
                TindakanId = @TindakanId
                AND IsVoid = 0
            """;

        var now = DateTime.Now;
        var dp = new DynamicParameters();
        dp.AddParam("@TindakanId", tindakanId, SqlDbType.VarChar);
        dp.AddParam("@UPDTGL", now.ToString("yyyy-MM-dd"), SqlDbType.VarChar);
        dp.AddParam("@UPDJAM", now.ToString("HH:mm:ss"), SqlDbType.VarChar);
        dp.AddParam("@UPDUSR", updUser, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }
}
