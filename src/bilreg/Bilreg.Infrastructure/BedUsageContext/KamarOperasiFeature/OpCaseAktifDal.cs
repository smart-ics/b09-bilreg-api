using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;

public interface IOpCaseAktifDal :
    IInsert<OpCaseAktifDto>,
    IUpdate<OpCaseAktifDto>,
    IDelete<IOrderOpKey>,
    IGetData<OpCaseAktifDto, IOrderOpKey>,
    IListData<OpCaseAktifDto>,
    IListData<OpCaseAktifDto, OpCaseAktifFilter>
{
}

public class OpCaseAktifDal : IOpCaseAktifDal
{
    private readonly DatabaseOptions _opt;

    public OpCaseAktifDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    
    public void Insert(OpCaseAktifDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_OpCaseAktif(
                OrderOpId, OrderOpDate, PasienId, OpCaseState)
            VALUES( 
                @OrderOpId, @OrderOpDate, @PasienId, @OpCaseState)
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@OrderOpId", dto.OrderOpId, SqlDbType.VarChar);
        dp.AddParam("@OrderOpDate", dto.OrderOpDate, SqlDbType.DateTime);
        dp.AddParam("@PasienId", dto.PasienId, SqlDbType.VarChar);
        dp.AddParam("@OpCaseState", dto.OpCaseState, SqlDbType.Int);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(OpCaseAktifDto dto)
    {
        const string sql = @"
           UPDATE 
               BILRG_OpCaseAktif
           SET
               OrderOpDate = @OrderOpDate,
               PasienId = @PasienId,
               OpCaseState = @OpCaseState
           WHERE
               OrderOpId = @OrderOpId";

        var dp = new DynamicParameters();
        dp.AddParam("@OrderOpId", dto.OrderOpId, SqlDbType.VarChar);
        dp.AddParam("@OrderOpDate", dto.OrderOpDate, SqlDbType.DateTime);
        dp.AddParam("@PasienId", dto.PasienId, SqlDbType.VarChar);
        dp.AddParam("@OpCaseState", dto.OpCaseState, SqlDbType.Int);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IOrderOpKey key)
    {
        const string sql = @"
           DELETE FROM 
                BILRG_OpCaseAktif
           WHERE
               OrderOpId = @OrderOpId";
        
        var dp = new DynamicParameters();
        dp.AddParam("@OrderOpId", key.OrderOpId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public OpCaseAktifDto GetData(IOrderOpKey key)
    {
        const string sql = """
            SELECT
               aa.OrderOpId,
               aa.OrderOpDate,
               aa.PasienId,
               ISNULL(bb.fs_nm_pasien, '') AS PasienName,
               ISNULL(dd.DokterPengorderId, '') AS DokterPengorderId,
               ISNULL(ff.fs_nm_peg, '') AS DokterPengorderName,
               ISNULL(dd.DokterOperatorId, '') AS DokterOperatorId,
               ISNULL(hh.fs_nm_peg, '') AS DokterOperatorName,
               ISNULL(dd.NamaOperasi, ISNULL(cc.NamaOperasi, '')) AS NamaOperasi,
               ISNULL(dd.ProsedurTambahan, '') AS ProsedurTambahan,
               ISNULL(dd.KlasifikasiOperasi, 1) AS KlasifikasiOperasi,
               ISNULL(dd.PreferedDate, aa.OrderOpDate) AS PreferedDate,
               ISNULL(dd.EstimasiDurasi, 0) AS EstimasiDurasi,
               ISNULL(dd.UrgencyLevel, 0) AS UrgencyLevel,
               aa.OpCaseState,
               ISNULL(bb.fd_tgl_lahir, '3000-01-01') AS TglLahir,
               ISNULL(bb.fs_jns_kelamin, '') AS Gender
            FROM 
               BILRG_OpCaseAktif aa
               LEFT JOIN tc_mr bb ON aa.PasienId = bb.fs_mr
               LEFT JOIN BILRG_OpCase cc ON aa.OrderOpId = cc.OrderOpId
               LEFT JOIN BILRG_OrderOp dd ON aa.OrderOpId = dd.OrderOpId
               LEFT JOIN td_peg ff ON dd.DokterPengorderId = ff.fs_kd_peg
               LEFT JOIN td_peg hh ON dd.DokterOperatorId = hh.fs_kd_peg
            WHERE
               aa.OrderOpId = @OrderOpId
            """;
        
        var dp = new DynamicParameters();
        dp.AddParam("@OrderOpId", key.OrderOpId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.ReadSingle<OpCaseAktifDto>(sql, dp);
        return result;
    }

    public IEnumerable<OpCaseAktifDto> ListData()
    {
        return ListData(new OpCaseAktifFilter());
    }

    public IEnumerable<OpCaseAktifDto> ListData(OpCaseAktifFilter filter)
    {
        var topClause = filter.Limit.HasValue && filter.Limit.Value > 0
            ? "TOP (@Limit)"
            : string.Empty;

        var sql = $"""
            SELECT {topClause}
                aa.OrderOpId,
                aa.OrderOpDate,
                aa.PasienId,
                ISNULL(bb.fs_nm_pasien, '') AS PasienName,
                ISNULL(dd.DokterPengorderId, '') AS DokterPengorderId,
                ISNULL(ff.fs_nm_peg, '') AS DokterPengorderName,
                ISNULL(dd.DokterOperatorId, '') AS DokterOperatorId,
                ISNULL(hh.fs_nm_peg, '') AS DokterOperatorName,
                ISNULL(dd.NamaOperasi, ISNULL(cc.NamaOperasi, '')) AS NamaOperasi,
                ISNULL(dd.ProsedurTambahan, '') AS ProsedurTambahan,
                ISNULL(dd.KlasifikasiOperasi, 1) AS KlasifikasiOperasi,
                ISNULL(dd.PreferedDate, aa.OrderOpDate) AS PreferedDate,
                ISNULL(dd.EstimasiDurasi, 0) AS EstimasiDurasi,
                ISNULL(dd.UrgencyLevel, 0) AS UrgencyLevel,
                aa.OpCaseState,
                ISNULL(bb.fd_tgl_lahir, '3000-01-01') AS TglLahir,
                ISNULL(bb.fs_jns_kelamin, '') AS Gender
            FROM BILRG_OpCaseAktif aa
            LEFT JOIN tc_mr bb ON aa.PasienId = bb.fs_mr
            LEFT JOIN BILRG_OpCase cc ON aa.OrderOpId = cc.OrderOpId
            LEFT JOIN BILRG_OrderOp dd ON aa.OrderOpId = dd.OrderOpId
            LEFT JOIN td_peg ff ON dd.DokterPengorderId = ff.fs_kd_peg
            LEFT JOIN td_peg hh ON dd.DokterOperatorId = hh.fs_kd_peg
            WHERE (@OpCaseState IS NULL OR aa.OpCaseState = @OpCaseState)
              AND (@TglAwal IS NULL OR dd.PreferedDate >= @TglAwal)
              AND (@TglAkhir IS NULL OR dd.PreferedDate <= @TglAkhir)
            ORDER BY 
              CASE WHEN dd.UrgencyLevel = 1 THEN 0 ELSE 1 END,
              aa.OrderOpDate ASC
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@OpCaseState", filter.OpCaseState, SqlDbType.Int);
        dp.AddParam("@TglAwal", filter.TglAwal, SqlDbType.DateTime);
        dp.AddParam("@TglAkhir", filter.TglAkhir, SqlDbType.DateTime);
        if (filter.Limit.HasValue && filter.Limit.Value > 0)
        {
            dp.AddParam("@Limit", filter.Limit.Value, SqlDbType.Int);
        }

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<OpCaseAktifDto>(sql, dp);
    }
}
