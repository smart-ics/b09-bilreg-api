using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.AdmisiContext.RegFeature;

public sealed class RegistrationHistoryReader : IRegistrationHistoryReader
{
    private const string SentinelDate = "3000-01-01";
    private readonly DatabaseOptions _options;

    public RegistrationHistoryReader(IOptions<DatabaseOptions> options)
    {
        _options = options.Value;
    }

    public RegistrationSearchView? GetById(string registrationId)
    {
        var sql = SelectClause + """
            WHERE reg.fs_kd_reg = @registrationId
              AND reg.fd_tgl_void = '3000-01-01'
            """;
        var parameters = new DynamicParameters();
        parameters.AddParam("@registrationId", registrationId, SqlDbType.VarChar);
        using var connection = new SqlConnection(ConnStringHelper.Get(_options));
        return Map(connection.ReadSingle<RegistrationSearchDto>(sql, parameters));
    }

    public IReadOnlyList<RegistrationSearchView> Search(string keyword, DateOnly admissionDate)
    {
        var sql = SelectClause + """
            WHERE reg.fd_tgl_masuk = @admissionDate
              AND reg.fd_tgl_void = '3000-01-01'
              AND (
                    reg.fs_kd_reg = @keyword
                 OR reg.fs_mr = @keyword
                 OR pasien.fs_nm_pasien LIKE @containsKeyword
              )
            """;
        var parameters = new DynamicParameters();
        parameters.AddParam("@admissionDate", admissionDate.ToString("yyyy-MM-dd"), SqlDbType.VarChar);
        parameters.AddParam("@keyword", keyword, SqlDbType.VarChar);
        parameters.AddParam("@containsKeyword", $"%{EscapeLike(keyword)}%", SqlDbType.VarChar);
        using var connection = new SqlConnection(ConnStringHelper.Get(_options));
        return (connection.Read<RegistrationSearchDto>(sql, parameters) ?? [])
            .Select(Map)
            .Where(x => x is not null)
            .Cast<RegistrationSearchView>()
            .ToList();
    }

    public IReadOnlyList<RegistrationSearchView> FindRelated(
        DateOnly admissionDate,
        IReadOnlyCollection<string> registrationIds,
        IReadOnlyCollection<string> patientIds)
    {
        var normalizedRegistrationIds = registrationIds.Where(Real).Distinct().ToArray();
        var normalizedPatientIds = patientIds.Where(Real).Distinct().ToArray();
        if (normalizedRegistrationIds.Length == 0 && normalizedPatientIds.Length == 0) return [];

        var sql = SelectClause + """
            WHERE reg.fd_tgl_masuk = @admissionDate
              AND reg.fd_tgl_void = '3000-01-01'
              AND (reg.fs_kd_reg IN @registrationIds OR reg.fs_mr IN @patientIds)
            """;
        var parameters = new
        {
            admissionDate = admissionDate.ToString("yyyy-MM-dd"),
            registrationIds = normalizedRegistrationIds.Length == 0 ? ["\0"] : normalizedRegistrationIds,
            patientIds = normalizedPatientIds.Length == 0 ? ["\0"] : normalizedPatientIds
        };
        using var connection = new SqlConnection(ConnStringHelper.Get(_options));
        return (connection.Query<RegistrationSearchDto>(sql, parameters) ?? [])
            .Select(Map)
            .Where(x => x is not null)
            .Cast<RegistrationSearchView>()
            .ToList();
    }

    private static RegistrationSearchView? Map(RegistrationSearchDto? row)
    {
        if (row is null) return null;
        return new RegistrationSearchView(
            row.RegistrationId,
            DateOnly.Parse(row.AdmissionDate),
            ParseTime(row.AdmissionTime),
            row.PatientId,
            row.PatientName,
            ParseDate(row.BirthDate),
            Real(row.Gender) ? row.Gender : null,
            Real(row.ServiceId) ? row.ServiceId : null,
            Real(row.ServiceName) ? row.ServiceName : null,
            Real(row.DoctorId) ? row.DoctorId : null,
            Real(row.DoctorName) ? row.DoctorName : null,
            Real(row.GuaranteeName) ? row.GuaranteeName : null,
            ParseAuditDate(row.ExitDate),
            ParseAuditDate(row.VoidDate));
    }

    private static DateOnly? ParseDate(string? value) =>
        DateOnly.TryParse(value, out var date) ? date : null;

    private static DateOnly? ParseAuditDate(string? value)
    {
        var date = ParseDate(value);
        return date?.ToString("yyyy-MM-dd") == SentinelDate ? null : date;
    }

    private static TimeOnly? ParseTime(string? value) =>
        TimeOnly.TryParse(value, out var time) ? time : null;

    private static bool Real(string? value) =>
        !string.IsNullOrWhiteSpace(value) && value.Trim() != "-";

    private static string EscapeLike(string value) =>
        value.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");

    private const string SelectClause = """
        SELECT
            reg.fs_kd_reg AS RegistrationId,
            reg.fd_tgl_masuk AS AdmissionDate,
            reg.fs_jam_masuk AS AdmissionTime,
            reg.fs_mr AS PatientId,
            ISNULL(pasien.fs_nm_pasien, '-') AS PatientName,
            pasien.fd_tgl_lahir AS BirthDate,
            pasien.fs_jns_kelamin AS Gender,
            reg.fs_kd_layanan AS ServiceId,
            layanan.fs_nm_layanan AS ServiceName,
            reg.fs_kd_medis AS DoctorId,
            dokter.fs_nm_peg AS DoctorName,
            jaminan.fs_nm_tipe_jaminan AS GuaranteeName,
            reg.fd_tgl_keluar AS ExitDate,
            reg.fd_tgl_void AS VoidDate
        FROM ta_registrasi reg
        LEFT JOIN tc_mr pasien ON pasien.fs_mr = reg.fs_mr
        LEFT JOIN ta_layanan layanan ON layanan.fs_kd_layanan = reg.fs_kd_layanan
        LEFT JOIN td_peg dokter ON dokter.fs_kd_peg = reg.fs_kd_medis
        LEFT JOIN ta_tipe_jaminan jaminan
            ON jaminan.fs_kd_tipe_jaminan = reg.fs_kd_tipe_jaminan
        """;

    private sealed record RegistrationSearchDto(
        string RegistrationId,
        string AdmissionDate,
        string? AdmissionTime,
        string PatientId,
        string PatientName,
        string? BirthDate,
        string? Gender,
        string? ServiceId,
        string? ServiceName,
        string? DoctorId,
        string? DoctorName,
        string? GuaranteeName,
        string? ExitDate,
        string? VoidDate);
}
