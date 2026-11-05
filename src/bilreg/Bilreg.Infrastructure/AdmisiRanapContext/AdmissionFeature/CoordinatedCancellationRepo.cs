using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Domain.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Domain.AdmisiRanapContext.WaitingListFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;

namespace Bilreg.Infrastructure.AdmisiRanapContext.AdmissionFeature;

/// <summary>All statements deliberately use ambient TransactionScope connections.</summary>
public sealed class CoordinatedCancellationRepo : ICoordinatedCancellationRepo
{
    private readonly DatabaseOptions _opt;
    public CoordinatedCancellationRepo(IOptions<DatabaseOptions> opt) => _opt = opt.Value;
    private SqlConnection Open() => new(ConnStringHelper.Get(_opt));

    public CoordinatedCancellationLedger? LockLedger(string requestId)
    {
        const string sql = "SELECT RequestId, Fingerprint, RegId, Lifecycle, ResponseJson, CreatedAt, CompletedAt, CorrelationId FROM BILRG_AdmCoordinatedCancellationRequest WITH (UPDLOCK, HOLDLOCK) WHERE RequestId=@requestId";
        using var c = Open(); return c.QuerySingleOrDefault<CoordinatedCancellationLedger>(sql, new { requestId });
    }
    public bool TryStartLedger(CoordinatedCancellationLedger ledger)
    {
        const string sql = "INSERT INTO BILRG_AdmCoordinatedCancellationRequest (RequestId,Fingerprint,RegId,Lifecycle,ResponseJson,CreatedAt,CompletedAt,CorrelationId) VALUES (@RequestId,@Fingerprint,@RegId,@Lifecycle,@ResponseJson,@CreatedAt,@CompletedAt,@CorrelationId)";
        using var c = Open();
        try { return c.Execute(sql, ledger) == 1; } catch (SqlException ex) when (ex.Number is 2601 or 2627) { return false; }
    }

    public CoordinatedCancellationState LockState(string regId)
    {
        using var c = Open();
        var reg = c.QuerySingleOrDefault<RegRow>("SELECT fs_kd_reg RegId, fs_kd_jenis_reg JenisReg, fd_tgl_void VoidDate, fd_tgl_keluar ExitDate FROM ta_registrasi WITH (UPDLOCK,HOLDLOCK) WHERE fs_kd_reg=@regId", new { regId });
        var admission = c.QuerySingleOrDefault<AdmissionRow>("SELECT RegId,AdmissionStatus,AdmissionSource,ISNULL(InpatientOrderId,'-') InpatientOrderId,UpdDate FROM BILRG_AdmAdmission WITH (UPDLOCK,HOLDLOCK) WHERE RegId=@regId", new { regId });
        var inap = c.QuerySingleOrDefault<int?>("SELECT 1 FROM ta_reg_inap WITH (UPDLOCK,HOLDLOCK) WHERE fs_kd_reg=@regId", new { regId }) == 1;
        var doctors = c.Query<int>("SELECT 1 FROM ta_reg_history_dokter WITH (UPDLOCK,HOLDLOCK) WHERE fs_kd_reg=@regId AND ISNULL(LTRIM(RTRIM(fd_tgl_selesai)),'')=''", new { regId }).Count();
        var waiting = c.QuerySingleOrDefault<WaitingRow>("SELECT TOP 1 WaitingListId,WaitingListStatus FROM BILRG_BedWaitingList WITH (UPDLOCK,HOLDLOCK) WHERE RegId=@regId AND WaitingListStatus IN (0,1) ORDER BY CrtDate DESC", new { regId });
        var activeCount = c.Query<int>("SELECT 1 FROM BILRG_RegAktif WITH (UPDLOCK,HOLDLOCK) WHERE RegId=@regId", new { regId }).Count();
        var regInapSnapshot = c.QuerySingleOrDefault("SELECT * FROM ta_reg_inap WITH (UPDLOCK,HOLDLOCK) WHERE fs_kd_reg=@regId", new { regId });
        var doctorHistorySnapshot = c.Query("SELECT * FROM ta_reg_history_dokter WITH (UPDLOCK,HOLDLOCK) WHERE fs_kd_reg=@regId AND ISNULL(LTRIM(RTRIM(fd_tgl_selesai)),'')=''", new { regId }).ToArray();
        var regAktifSnapshot = c.Query("SELECT * FROM BILRG_RegAktif WITH (UPDLOCK,HOLDLOCK) WHERE RegId=@regId", new { regId }).ToArray();
        if (admission is null)
            return new CoordinatedCancellationState(regId, AdmissionStatusEnum.Cancelled, default, AdmissionSourceEnum.Admission, "", "", reg is not null, inap, doctors, waiting?.WaitingListId, waiting is null ? null : (WaitingListStatusEnum)waiting.WaitingListStatus, activeCount, null, null, false, false, new { Reg = reg }, AdmissionExists: false,
                AuditSnapshots: new CoordinatedCancellationAuditSnapshots(null, reg, new { RegInap = regInapSnapshot, DoctorHistory = doctorHistorySnapshot }, regAktifSnapshot, waiting, null));

        string? sourceId = null, sourceKind = null; var sourceOk = true; var sourceRestored = true; object? sourceSnapshot = null;
        var orderId = !string.IsNullOrWhiteSpace(admission.InpatientOrderId) && admission.InpatientOrderId != "-"
            ? admission.InpatientOrderId
            : null;

        if (admission.AdmissionSource == (int)AdmissionSourceEnum.Admission && !string.IsNullOrWhiteSpace(orderId))
        {
            var source = c.QuerySingleOrDefault<InpatientOrderRow>(
                "SELECT InpatientOrderId,OrderStatus,FulfilledRegId FROM BILRG_AdmInpatientOrder WITH (UPDLOCK,HOLDLOCK) WHERE InpatientOrderId=@id",
                new { id = orderId });

            if (source is not null)
            {
                sourceId = source.InpatientOrderId;
                sourceKind = "InpatientOrderModel";
                sourceOk = source.OrderStatus == 2 && source.FulfilledRegId == regId;
                sourceRestored = (source.OrderStatus is 0 or 1 && source.FulfilledRegId == "-") || source.OrderStatus == 3;
                sourceSnapshot = source;
            }
        }
        var coherent = admission.AdmissionStatus == (int)AdmissionStatusEnum.Cancelled && reg is not null && reg.VoidDate != "3000-01-01" && activeCount == 0 && doctors == 0 && waiting is null && sourceRestored;
        return new CoordinatedCancellationState(regId, (AdmissionStatusEnum)admission.AdmissionStatus, admission.UpdDate,
            (AdmissionSourceEnum)admission.AdmissionSource, "-", "-",
            reg is not null && reg.JenisReg == "1" && reg.VoidDate == "3000-01-01" && reg.ExitDate == "3000-01-01", inap, doctors,
            waiting?.WaitingListId, waiting is null ? null : (WaitingListStatusEnum)waiting.WaitingListStatus, activeCount, sourceId, sourceKind, sourceOk, coherent,
            new { Registration = reg, Admission = admission, WaitingList = waiting, SourceId = sourceId, SourceKind = sourceKind },
            AuditSnapshots: new CoordinatedCancellationAuditSnapshots(admission, reg, new { RegInap = regInapSnapshot, DoctorHistory = doctorHistorySnapshot }, regAktifSnapshot, waiting, sourceSnapshot),
            InpatientOrderId: admission.InpatientOrderId ?? "-");
    }
    public int CancelWaitingList(CoordinatedCancellationState state, string userId, DateTime now) => Execute("UPDATE BILRG_BedWaitingList SET WaitingListStatus=3, VodUser=@userId, VodDate=@now, UpdUser=@userId, UpdDate=@now WHERE WaitingListId=@id AND RegId=@regId AND WaitingListStatus IN (0,1)", new { id = state.WaitingListId, regId = state.RegId, userId, now });
    public int EndDoctorAssignments(string regId, DateOnly effectiveDate) => Execute("UPDATE ta_reg_history_dokter SET fd_tgl_selesai=@date WHERE fs_kd_reg=@regId AND ISNULL(LTRIM(RTRIM(fd_tgl_selesai)),'')=''", new { regId, date = effectiveDate.ToString("yyyy-MM-dd") });
    public int VoidRegistration(string regId, string userId, DateTime now) => Execute("UPDATE ta_registrasi SET fd_tgl_void=@date, fs_jam_void=@time, fs_kd_petugas_void=@userId WHERE fs_kd_reg=@regId AND fs_kd_jenis_reg='1' AND fd_tgl_void='3000-01-01' AND fd_tgl_keluar='3000-01-01'", new { regId, userId, date = now.ToString("yyyy-MM-dd"), time = now.ToString("HH:mm:ss") });
    public int DeleteRegAktif(string regId) => Execute("DELETE FROM BILRG_RegAktif WHERE RegId=@regId", new { regId });
    public int RestoreSource(CoordinatedCancellationState state, string userId, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(state.SourceId)) return 1;

        return Execute("UPDATE BILRG_AdmInpatientOrder SET OrderStatus=0,FulfilledRegId='-',UpdUser=@userId,UpdDate=@now WHERE InpatientOrderId=@id AND OrderStatus=2 AND FulfilledRegId=@regId", new { id = state.SourceId, regId = state.RegId, userId, now });
    }
    public int VoidSource(CoordinatedCancellationState state, string reason, string userId, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(state.SourceId)) return 1;

        return Execute("UPDATE BILRG_AdmInpatientOrder SET OrderStatus=3,VodUser=@userId,VodDate=@now,UpdUser=@userId,UpdDate=@now WHERE InpatientOrderId=@id AND OrderStatus=2 AND FulfilledRegId=@regId", new { id = state.SourceId, regId = state.RegId, userId, now });
    }
    public int CancelAdmission(CoordinatedCancellationState state, DateTime? expectedUpdatedAt, string userId, DateTime now) => Execute("UPDATE BILRG_AdmAdmission SET AdmissionStatus=4,VodUser=@userId,VodDate=@now,UpdUser=@userId,UpdDate=@now WHERE RegId=@regId AND AdmissionStatus=@status AND (@expected IS NULL OR UpdDate=@expected)", new { regId = state.RegId, status = (int)state.AdmissionStatus, expected = expectedUpdatedAt, userId, now });
    public int CompleteLedger(string requestId, string responseJson, DateTime now) => Execute("UPDATE BILRG_AdmCoordinatedCancellationRequest SET Lifecycle='Completed',ResponseJson=@responseJson,CompletedAt=@now WHERE RequestId=@requestId AND Lifecycle='InProgress'", new { requestId, responseJson, now });
    private int Execute(string sql, object args) { using var c = Open(); return c.Execute(sql, args); }
    private sealed record RegRow(string RegId, string JenisReg, string VoidDate, string ExitDate);
    private sealed record AdmissionRow(string RegId, int AdmissionStatus, int AdmissionSource, string InpatientOrderId, DateTime UpdDate);
    private sealed record WaitingRow(string WaitingListId, int WaitingListStatus);
    private sealed record InpatientOrderRow(string InpatientOrderId, int OrderStatus, string FulfilledRegId);
}
