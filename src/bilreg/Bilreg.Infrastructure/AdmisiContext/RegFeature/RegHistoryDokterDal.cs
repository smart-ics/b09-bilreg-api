using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.AdmisiContext.RegFeature;

// resharper disable inconsistentnaming
public interface IRegHistoryDokterDal :
    IInsertBulk<RegHistoryDokterDto>,
    IDelete<IRegKey>,
    IListData<RegHistoryDokterDto, IRegKey>
{
}

public record RegHistoryDokterDto(
    string fs_kd_reg,
    string fs_kd_dokter,
    bool fb_primer,
    string fd_tgl_mulai,
    string fd_tgl_selesai,
    bool fb_reg,
    string fs_nm_peg)
{
    public static RegHistoryDokterDto FromModel(string regId, RegDokterType model)
    {
        return new RegHistoryDokterDto(
            regId,
            model.Dokter.PpaId,
            model.IsPrimer,
            model.AssignDate.ToString("yyyy-MM-dd"),
            model.ReleaseDate?.ToString("yyyy-MM-dd") ?? string.Empty,
            true,
            model.Dokter.PpaName);
    }

    public RegDokterType ToModel()
    {
        var releaseDate = string.IsNullOrEmpty(fd_tgl_selesai)
            ? (DateOnly?)null
            : DateOnly.Parse(fd_tgl_selesai);

        return RegDokterType.Rehydrate(
            new PpaReff(fs_kd_dokter, fs_nm_peg),
            DateOnly.Parse(fd_tgl_mulai),
            releaseDate,
            fb_primer);
    }
}

public class RegHistoryDokterDal : IRegHistoryDokterDal
{
    private readonly DatabaseOptions _opt;

    public RegHistoryDokterDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(IEnumerable<RegHistoryDokterDto> listModel)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        using var bcp = new SqlBulkCopy(conn);

        conn.Open();
        bcp.AddMap("fs_kd_reg", "fs_kd_reg");
        bcp.AddMap("fs_kd_dokter", "fs_kd_dokter");
        bcp.AddMap("fb_primer", "fb_primer");
        bcp.AddMap("fd_tgl_mulai", "fd_tgl_mulai");
        bcp.AddMap("fd_tgl_selesai", "fd_tgl_selesai");
        bcp.AddMap("fb_reg", "fb_reg");

        var fetched = listModel.ToList();
        bcp.BatchSize = fetched.Count;
        bcp.DestinationTableName = "ta_reg_history_dokter";
        bcp.WriteToServer(fetched.AsDataTable());
    }

    public void Delete(IRegKey key)
    {
        const string sql = """
           DELETE FROM
               ta_reg_history_dokter
           WHERE
               fs_kd_reg = @fs_kd_reg
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_reg", key.RegId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public IEnumerable<RegHistoryDokterDto> ListData(IRegKey filter)
    {
        const string sql = """
           SELECT
               aa.fs_kd_reg,
               aa.fs_kd_dokter,
               aa.fb_primer,
               aa.fd_tgl_mulai,
               aa.fd_tgl_selesai,
               aa.fb_reg,
               ISNULL(bb.fs_nm_peg, '') AS fs_nm_peg
           FROM
               ta_reg_history_dokter aa
               LEFT JOIN td_peg bb ON aa.fs_kd_dokter = bb.fs_kd_peg
           WHERE
               aa.fs_kd_reg = @fs_kd_reg
           ORDER BY
               aa.fd_tgl_mulai
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_reg", filter.RegId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<RegHistoryDokterDto>(sql, dp);
    }
}
