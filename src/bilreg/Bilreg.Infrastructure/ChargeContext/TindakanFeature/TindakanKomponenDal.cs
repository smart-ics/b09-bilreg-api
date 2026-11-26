using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.ChargeContext.TindakanFeature;

public interface ITindakanKomponenDal :
    IInsertBulk<TindakanKomponenDto>,
    IDelete<ITindakanKey>,
    IListData<TindakanKomponenDto, ITindakanKey>
{
    void UpdatePetugas(string tindakanId, string ppaId, string userId);
    string GetPetugasName(string ppaId);
}

public class TindakanKomponenDal : ITindakanKomponenDal
{
    private readonly DatabaseOptions _opt;

    public TindakanKomponenDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    private static string SafeTruncate(string? val, int maxLen)
    {
        if (string.IsNullOrWhiteSpace(val)) return string.Empty;
        return val.Length > maxLen ? val[..maxLen] : val;
    }

    public void Insert(IEnumerable<TindakanKomponenDto> listModel)
    {
        var list = listModel.ToList();
        if (list.Count == 0) return;

        const string sql = """
            INSERT INTO BILRG_TindakanKomponen(
                TindakanId, NoUrut, KomponenTarifId, KomponenTarifName,
                PpaId, PpaName, Nilai, Diskon, Qty, SubTotal
            )
            VALUES(
                @TindakanId, @NoUrut, @KomponenTarifId, @KomponenTarifName,
                @PpaId, @PpaName, @Nilai, @Diskon, @Qty, @SubTotal
            )
            """;

        var entries = list.Select(x => new
        {
            TindakanId = SafeTruncate(x.TindakanId, 26),
            NoUrut = x.NoUrut,
            KomponenTarifId = SafeTruncate(x.KomponenTarifId, 3),
            KomponenTarifName = SafeTruncate(x.KomponenTarifName, 30),
            PpaId = SafeTruncate(x.PpaId, 10),
            PpaName = SafeTruncate(x.PpaName, 60),
            Nilai = x.Nilai,
            Diskon = x.Diskon,
            Qty = x.Qty,
            SubTotal = x.SubTotal
        });

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, entries);
    }

    public void Delete(ITindakanKey key)
    {
        const string sql = """
            DELETE FROM
                BILRG_TindakanKomponen
            WHERE
                TindakanId = @TindakanId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@TindakanId", SafeTruncate(key.TindakanId, 26), SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public IEnumerable<TindakanKomponenDto> ListData(ITindakanKey filter)
    {
        const string sql = """
            SELECT
                TindakanId,
                NoUrut,
                KomponenTarifId,
                KomponenTarifName,
                PpaId,
                PpaName,
                Qty,
                Nilai,
                Diskon,
                SubTotal
            FROM
                BILRG_TindakanKomponen
            WHERE
                TindakanId = @TindakanId
            ORDER BY
                NoUrut, KomponenTarifId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@TindakanId", SafeTruncate(filter.TindakanId, 26), SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<TindakanKomponenDto>(sql, dp) ?? [];
    }

    public void UpdatePetugas(string tindakanId, string ppaId, string userId)
    {
        const string sql = """
            DECLARE @PpaName VARCHAR(60) = ISNULL((SELECT TOP 1 fs_nm_peg FROM td_peg WHERE fs_kd_peg = @PpaId), '');

            -- 1. Update BILRG_TindakanKomponen
            IF EXISTS (
                SELECT 1 FROM BILRG_TindakanKomponen 
                WHERE TindakanId = @TindakanId AND ISNULL(PpaId, '') <> ''
            )
            BEGIN
                UPDATE BILRG_TindakanKomponen
                SET 
                    PpaId = @PpaId,
                    PpaName = @PpaName
                WHERE 
                    TindakanId = @TindakanId
                    AND ISNULL(PpaId, '') <> '';
            END
            ELSE IF EXISTS (
                SELECT 1 FROM BILRG_TindakanKomponen tk
                INNER JOIN ta_detil_tarif2 dt ON tk.KomponenTarifId = dt.fs_kd_detil_tarif
                WHERE tk.TindakanId = @TindakanId AND ISNULL(dt.fs_kd_sat_tugas, '') <> ''
            )
            BEGIN
                UPDATE tk
                SET 
                    tk.PpaId = @PpaId,
                    tk.PpaName = @PpaName
                FROM BILRG_TindakanKomponen tk
                INNER JOIN ta_detil_tarif2 dt ON tk.KomponenTarifId = dt.fs_kd_detil_tarif
                WHERE 
                    tk.TindakanId = @TindakanId
                    AND ISNULL(dt.fs_kd_sat_tugas, '') <> '';
            END
            ELSE
            BEGIN
                UPDATE BILRG_TindakanKomponen
                SET 
                    PpaId = @PpaId,
                    PpaName = @PpaName
                WHERE 
                    TindakanId = @TindakanId;
            END

            -- 2. Update ta_trs_billing2 if matching
            UPDATE ta_trs_billing2
            SET 
                fs_kd_petugas_medis = @PpaId
            WHERE 
                fs_kd_trs = @TindakanId;
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@TindakanId", SafeTruncate(tindakanId, 26), SqlDbType.VarChar);
        dp.AddParam("@PpaId", SafeTruncate(ppaId, 10), SqlDbType.VarChar);
        dp.AddParam("@UserId", SafeTruncate(userId, 20), SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public string GetPetugasName(string ppaId)
    {
        const string sql = "SELECT TOP 1 ISNULL(fs_nm_peg, '') FROM td_peg WHERE fs_kd_peg = @PpaId";
        var dp = new DynamicParameters();
        dp.AddParam("@PpaId", ppaId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ExecuteScalar<string>(sql, dp) ?? string.Empty;
    }
}
