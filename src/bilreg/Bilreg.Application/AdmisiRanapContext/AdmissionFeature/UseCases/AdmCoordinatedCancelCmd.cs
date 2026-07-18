using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Domain.AdmisiRanapContext.WaitingListFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using MediatR;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Application.AdmisiRanapContext.AdmissionFeature.UseCases;

public record AdmCoordinatedCancelCmd(
    string RegId,
    string Reason,
    string UserId,
    AdmissionStatusEnum ExpectedAdmissionStatus,
    DateTime? ExpectedUpdatedAt,
    string RequestId,
    string? ClientIpAddress,
    string? UserAgent) : IRequest<AdmCoordinatedCancelResponse>, IRegKey;

public record AdmCoordinatedCancelResponse(
    string RegId,
    AdmissionStatusEnum AdmissionStatus,
    bool RegistrationVoided,
    bool AlreadyCancelled,
    string? WaitingListId,
    string? RestoredSource,
    string CorrelationId);

public sealed class AdmCoordinatedCancelHandler : IRequestHandler<AdmCoordinatedCancelCmd, AdmCoordinatedCancelResponse>
{
    private readonly ICoordinatedCancellationRepo _repo;
    private readonly IRegistrationCancellationEligibilityRepo _eligibility;
    private readonly IAuditRepo _auditRepo;

    public AdmCoordinatedCancelHandler(ICoordinatedCancellationRepo repo,
        IRegistrationCancellationEligibilityRepo eligibility, IAuditRepo auditRepo)
        => (_repo, _eligibility, _auditRepo) = (repo, eligibility, auditRepo);

    public Task<AdmCoordinatedCancelResponse> Handle(AdmCoordinatedCancelCmd request, CancellationToken cancellationToken)
    {
        Validate(request);
        var normalized = request with { RegId = request.RegId.Trim(), Reason = request.Reason.Trim(), UserId = request.UserId.Trim(), RequestId = request.RequestId.Trim() };
        var fingerprint = Fingerprint(normalized);
        var now = DateTime.UtcNow;

        using var trans = TransHelper.NewScope();
        var ledger = _repo.LockLedger(normalized.RequestId);
        if (ledger is not null)
        {
            if (!string.Equals(ledger.Fingerprint, fingerprint, StringComparison.Ordinal))
                throw new CoordinatedCancellationException(CoordinatedCancellationErrorCode.RequestIdReused, "Request ID telah digunakan untuk permintaan yang berbeda.");
            if (ledger.Lifecycle == "Completed")
                return Task.FromResult(JsonSerializer.Deserialize<AdmCoordinatedCancelResponse>(ledger.ResponseJson)!
                    with { AlreadyCancelled = true });
            throw new CoordinatedCancellationException(CoordinatedCancellationErrorCode.ConcurrencyConflict, "Permintaan pembatalan sedang diproses.");
        }

        var newLedger = new CoordinatedCancellationLedger(normalized.RequestId, fingerprint, normalized.RegId,
            "InProgress", "", now, new DateTime(3000, 1, 1), normalized.RequestId);
        if (!_repo.TryStartLedger(newLedger))
            throw new CoordinatedCancellationException(CoordinatedCancellationErrorCode.ConcurrencyConflict, "Permintaan pembatalan sedang diproses.");

        var state = _repo.LockState(normalized.RegId);
        if (state.IsCoherentlyCancelled)
        {
            var replay = new AdmCoordinatedCancelResponse(normalized.RegId, AdmissionStatusEnum.Cancelled, true, true,
                state.WaitingListId, state.SourceId, normalized.RequestId);
            Ensure(_repo.CompleteLedger(normalized.RequestId, JsonSerializer.Serialize(replay), now));
            trans.Complete();
            return Task.FromResult(replay);
        }
        ValidateState(normalized, state);
        if (_eligibility.HasBillingItems(normalized.RegId))
            throw new CoordinatedCancellationException(CoordinatedCancellationErrorCode.RegistrationHasBillingItems,
                "Registration cancellation is not eligible.");

        if (state.WaitingListId is not null) Ensure(_repo.CancelWaitingList(state, normalized.UserId, now));
        if (_repo.EndDoctorAssignments(normalized.RegId, DateOnly.FromDateTime(now)) < 1)
            throw new CoordinatedCancellationException(CoordinatedCancellationErrorCode.ConcurrencyConflict, "Penugasan dokter telah berubah.");
        Ensure(_repo.VoidRegistration(normalized.RegId, normalized.UserId, now));
        Ensure(_repo.DeleteRegAktif(normalized.RegId));
        if (state.SourceId is not null) EnsureSource(_repo.RestoreSource(state, normalized.UserId, now));
        Ensure(_repo.CancelAdmission(state, normalized.ExpectedUpdatedAt, normalized.UserId, now));

        WriteAudits(normalized, state, now);
        var response = new AdmCoordinatedCancelResponse(normalized.RegId, AdmissionStatusEnum.Cancelled, true, false,
            state.WaitingListId, state.SourceId, normalized.RequestId);
        Ensure(_repo.CompleteLedger(normalized.RequestId, JsonSerializer.Serialize(response), now));
        trans.Complete();
        return Task.FromResult(response);
    }

    private void WriteAudits(AdmCoordinatedCancelCmd cmd, CoordinatedCancellationState state, DateTime now)
    {
        var info = new AuditInfoType(cmd.UserId, now);
        var snapshots = state.AuditSnapshots;
        Save("VOID", "AdmissionModel", state.RegId, snapshots?.Admission);
        Save("VOID", "RegModel", state.RegId, snapshots?.Registration);
        Save("VOID", "RegInapModel", state.RegId, snapshots?.RegInapAndDoctorHistory);
        Save("DELETE", "RegAktifModel", state.RegId, snapshots?.RegAktif);
        if (state.WaitingListId is not null) Save("VOID", "WaitingListModel", state.WaitingListId, snapshots?.WaitingList);
        if (state.SourceId is not null) Save("RESTORE", state.SourceKind!, state.SourceId, snapshots?.Source);
        void Save(string action, string entity, string id, object? entitySnapshot) => _auditRepo.SaveChanges(AuditLog.Create(info, action, entity, id,
            cmd.Reason, AuditLogSnapshotJson.Serialize(entitySnapshot ?? state.AuditSnapshot), cmd.RequestId, cmd.ClientIpAddress, cmd.UserAgent));
    }

    private static void ValidateState(AdmCoordinatedCancelCmd cmd, CoordinatedCancellationState state)
    {
        if (!state.AdmissionExists || !state.RegistrationExists || !state.RegInapExists)
            throw new KeyNotFoundException($"Admission/Registration '{cmd.RegId}' tidak ditemukan.");
        if (state.RegAktifCount != 1 || state.ActiveDoctorAssignments < 1)
            throw new CoordinatedCancellationException(CoordinatedCancellationErrorCode.StateInconsistent, "State pembatalan terkoordinasi tidak konsisten.");
        if (state.AdmissionStatus != cmd.ExpectedAdmissionStatus || state.AdmissionStatus is AdmissionStatusEnum.Completed or AdmissionStatusEnum.Cancelled ||
            (cmd.ExpectedUpdatedAt.HasValue && state.AdmissionUpdatedAt != cmd.ExpectedUpdatedAt.Value))
            throw new CoordinatedCancellationException(CoordinatedCancellationErrorCode.ConcurrencyConflict, "Admission telah berubah.");
        if (state.WaitingListStatus is not null and not (WaitingListStatusEnum.Waiting or WaitingListStatusEnum.Accepted))
            throw new CoordinatedCancellationException(CoordinatedCancellationErrorCode.ConcurrencyConflict, "Waiting List telah berubah.");
        if (state.SourceId is not null && !state.SourceOwnedAndCancellable)
            throw new CoordinatedCancellationException(CoordinatedCancellationErrorCode.SourceStateMismatch, "Sumber admission tidak sesuai.");
    }
    private static void Ensure(int affected) { if (affected != 1) throw new CoordinatedCancellationException(CoordinatedCancellationErrorCode.ConcurrencyConflict, "Data berubah secara bersamaan."); }
    private static void EnsureSource(int affected) { if (affected != 1) throw new CoordinatedCancellationException(CoordinatedCancellationErrorCode.SourceStateMismatch, "Sumber admission tidak sesuai."); }
    private static void Validate(AdmCoordinatedCancelCmd c)
    {
        Guard.Against.NullOrWhiteSpace(c.RegId); Guard.Against.NullOrWhiteSpace(c.Reason); Guard.Against.NullOrWhiteSpace(c.UserId); Guard.Against.NullOrWhiteSpace(c.RequestId);
        if (c.RegId.Trim().Length > 50 || c.Reason.Trim().Length is < 1 or > 500 || c.UserId.Trim().Length > 50 || c.RequestId.Trim().Length is < 1 or > 50)
            throw new ArgumentException("Data pembatalan tidak valid.");
        if (c.ExpectedUpdatedAt.HasValue && c.ExpectedUpdatedAt.Value.Kind != DateTimeKind.Utc)
            throw new ArgumentException("ExpectedUpdatedAt harus UTC.", nameof(c.ExpectedUpdatedAt));
    }
    private static string Fingerprint(AdmCoordinatedCancelCmd c)
    {
        var text = string.Join("|", c.RegId, c.Reason, c.UserId, (int)c.ExpectedAdmissionStatus,
            c.ExpectedUpdatedAt?.ToUniversalTime().ToString("O") ?? "");
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
    }
}
