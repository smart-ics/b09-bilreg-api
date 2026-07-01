using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.AdmisiContext.JadwalPraktekFeature;

public class JadwalPraktekHarianDal
{
    private readonly DatabaseOptions _opt;

    public JadwalPraktekHarianDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    private const string SelectColumns = """
        aa.JadwalPraktekHarianId, aa.JadwalPraktekId, aa.TglPraktek,
        aa.DokterId, aa.LayananId, aa.RuangId,
        aa.JamMulai, aa.JamSelesai, aa.MaxPasien, aa.AntrianPattern,
        aa.Status, aa.Source, aa.Catatan,
        aa.CrtUser, aa.CrtDate, aa.UpdUser, aa.UpdDate,
        ISNULL(bb.fs_nm_peg, '-') AS DokterName,
        ISNULL(cc.fs_nm_layanan, '-') AS LayananName,
        ISNULL(cc.fs_kd_layanan_dk,'') AS LayananDkId,
        ISNULL(dd.fs_nm_layanan_dk,'') AS LayananDkName,
        ISNULL(ee.GroupSpesialisId,'') AS GroupSpesialisId,
        ISNULL(ff.GroupSpesialisName,'') AS GroupSpesialisName,
        ISNULL(gg.RuangName,'') AS RuangName,
        ISNULL(gg.PrefixAntrian,'') AS PrefixAntrian
        """;

    private const string FromClause = """
        BILRG_JadwalPraktekHarian aa
        LEFT JOIN td_peg bb ON aa.DokterId = bb.fs_kd_peg
        LEFT JOIN ta_layanan cc ON aa.LayananId = cc.fs_kd_layanan
        LEFT JOIN ta_layanan_dk dd ON cc.fs_kd_layanan_dk = dd.fs_kd_layanan_dk
        LEFT JOIN td_peg2 ee ON aa.DokterId = ee.fs_kd_peg
        LEFT JOIN BILRG_GroupSpesialis ff ON ee.GroupSpesialisId = ff.GroupSpesialisId
        LEFT JOIN Hidok_ruang gg ON aa.RuangId = gg.RuangId
        """;

    public void Insert(JadwalPraktekHarianDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_JadwalPraktekHarian(
                JadwalPraktekHarianId, JadwalPraktekId, TglPraktek,
                DokterId, LayananId, RuangId,
                JamMulai, JamSelesai, MaxPasien, AntrianPattern,
                Status, Source, Catatan,
                CrtUser, CrtDate, UpdUser, UpdDate)
            VALUES (
                @JadwalPraktekHarianId, @JadwalPraktekId, @TglPraktek,
                @DokterId, @LayananId, @RuangId,
                @JamMulai, @JamSelesai, @MaxPasien, @AntrianPattern,
                @Status, @Source, @Catatan,
                @CrtUser, @CrtDate, @UpdUser, @UpdDate)
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, BuildParams(dto));
    }

    public void Update(JadwalPraktekHarianDto dto)
    {
        const string sql = """
            UPDATE BILRG_JadwalPraktekHarian SET
                JadwalPraktekId = @JadwalPraktekId,
                TglPraktek = @TglPraktek,
                DokterId = @DokterId,
                LayananId = @LayananId,
                RuangId = @RuangId,
                JamMulai = @JamMulai,
                JamSelesai = @JamSelesai,
                MaxPasien = @MaxPasien,
                AntrianPattern = @AntrianPattern,
                Status = @Status,
                Source = @Source,
                Catatan = @Catatan,
                UpdUser = @UpdUser,
                UpdDate = @UpdDate
            WHERE JadwalPraktekHarianId = @JadwalPraktekHarianId
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, BuildParams(dto));
    }

    public JadwalPraktekHarianDto GetData(IJadwalPraktekHarianKey key)
    {
        var sql = $"""
            SELECT {SelectColumns}
            FROM {FromClause}
            WHERE aa.JadwalPraktekHarianId = @JadwalPraktekHarianId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@JadwalPraktekHarianId", key.JadwalPraktekHarianId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<JadwalPraktekHarianDto>(sql, dp);
    }

    public IEnumerable<JadwalPraktekHarianDto> ListByDate(DateOnly tglPraktek)
    {
        var sql = $"""
            SELECT {SelectColumns}
            FROM {FromClause}
            WHERE aa.TglPraktek = @TglPraktek
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@TglPraktek", tglPraktek.ToDateTime(TimeOnly.MinValue), SqlDbType.DateTime);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<JadwalPraktekHarianDto>(sql, dp);
    }

    public IEnumerable<JadwalPraktekHarianDto> ListByDateAndDokter(DateOnly tglPraktek, IPpaKey dokter)
    {
        var sql = $"""
            SELECT {SelectColumns}
            FROM {FromClause}
            WHERE aa.TglPraktek = @TglPraktek AND aa.DokterId = @DokterId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@TglPraktek", tglPraktek.ToDateTime(TimeOnly.MinValue), SqlDbType.DateTime);
        dp.AddParam("@DokterId", dokter.PpaId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<JadwalPraktekHarianDto>(sql, dp);
    }

    private static DynamicParameters BuildParams(JadwalPraktekHarianDto dto)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@JadwalPraktekHarianId", dto.JadwalPraktekHarianId, SqlDbType.VarChar);
        dp.AddParam("@JadwalPraktekId", dto.JadwalPraktekId, SqlDbType.VarChar);
        dp.AddParam("@TglPraktek", dto.TglPraktek, SqlDbType.DateTime);
        dp.AddParam("@DokterId", dto.DokterId, SqlDbType.VarChar);
        dp.AddParam("@LayananId", dto.LayananId, SqlDbType.VarChar);
        dp.AddParam("@RuangId", dto.RuangId, SqlDbType.VarChar);
        dp.AddParam("@JamMulai", dto.JamMulai, SqlDbType.VarChar);
        dp.AddParam("@JamSelesai", dto.JamSelesai, SqlDbType.VarChar);
        dp.AddParam("@MaxPasien", dto.MaxPasien, SqlDbType.Int);
        dp.AddParam("@AntrianPattern", dto.AntrianPattern, SqlDbType.VarChar);
        dp.AddParam("@Status", dto.Status, SqlDbType.VarChar);
        dp.AddParam("@Source", dto.Source, SqlDbType.VarChar);
        dp.AddParam("@Catatan", dto.Catatan ?? "", SqlDbType.VarChar);
        dp.AddParam("@CrtUser", dto.CrtUser, SqlDbType.VarChar);
        dp.AddParam("@CrtDate", dto.CrtDate, SqlDbType.DateTime);
        dp.AddParam("@UpdUser", dto.UpdUser, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", dto.UpdDate, SqlDbType.DateTime);
        return dp;
    }
}
