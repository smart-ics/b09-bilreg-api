using Ardalis.GuardClauses;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.PaymentContext.TataRekeningFeature;

/// <summary>
/// Riwayat pembatalan finalisasi Tata Rekening.
/// Menyimpan bukti supervisor yang membatalkan finalisasi beserta alasan pembatalan.
/// </summary>
public class TataRekeningCancellationLog
{
    public const int MinimumReasonLength = 10;

    private TataRekeningCancellationLog()
    {
    }

    private TataRekeningCancellationLog(
        string logId,
        string regId,
        string supervisorUserId,
        string userId,
        DateTime cancelledAt,
        string reason,
        string statusBefore,
        string statusAfter,
        string clientIpAddress)
    {
        LogId = logId;
        RegId = regId;
        SupervisorUserId = supervisorUserId;
        UserId = userId;
        CancelledAt = cancelledAt;
        Reason = reason;
        StatusBefore = statusBefore;
        StatusAfter = statusAfter;
        ClientIpAddress = clientIpAddress;
    }

    #region CREATION

    public static TataRekeningCancellationLog Create(
        string regId,
        string supervisorUserId,
        string userId,
        DateTime cancelledAt,
        string reason,
        string statusBefore,
        string statusAfter,
        string? clientIpAddress = null)
    {
        Guard.Against.NullOrWhiteSpace(regId);
        Guard.Against.NullOrWhiteSpace(supervisorUserId);
        Guard.Against.NullOrWhiteSpace(userId);

        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < MinimumReasonLength)
            throw new ArgumentException(
                $"Alasan pembatalan finalisasi minimal {MinimumReasonLength} karakter.",
                nameof(reason));

        return new TataRekeningCancellationLog(
            NunaId.New("BIL"),
            regId,
            supervisorUserId,
            userId,
            cancelledAt,
            reason.Trim(),
            statusBefore,
            statusAfter,
            clientIpAddress ?? string.Empty);
    }

    #endregion

    #region PROPERTIES

    public string LogId { get; private set; } = string.Empty;
    public string RegId { get; private set; } = string.Empty;
    public string SupervisorUserId { get; private set; } = string.Empty;
    public string UserId { get; private set; } = string.Empty;
    public DateTime CancelledAt { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public string StatusBefore { get; private set; } = string.Empty;
    public string StatusAfter { get; private set; } = string.Empty;
    public string ClientIpAddress { get; private set; } = string.Empty;

    #endregion
}
