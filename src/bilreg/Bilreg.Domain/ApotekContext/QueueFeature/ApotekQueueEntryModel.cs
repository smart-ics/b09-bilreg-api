using Ardalis.GuardClauses;
using Bilreg.Domain.ApotekContext.Shared;

namespace Bilreg.Domain.ApotekContext.QueueFeature;

public class ApotekQueueEntryModel : IApotekQueueEntryKey
{
    private ApotekQueueEntryModel(
        string antrianId,
        int noUrut,
        string servicePointId,
        string queueLabel,
        ApotekPhaseEnum apotekPhase,
        int callCountPenerimaan,
        DateTime lastCalledPenerimaanAt,
        int callCountSerah,
        DateTime lastCalledSerahAt,
        DateTime takenAt,
        DateTime servedAt,
        DateTime preparedAt,
        DateTime doneAt,
        DateTime withdrawnAt,
        DateTime lastSkippedAt,
        LoketTypeEnum lastSkippedLoketType,
        string withdrawalReason,
        string withdrawalUserId,
        bool isPaymentOverridden,
        string paymentOverrideReason,
        string paymentOverridePharmacistId)
    {
        AntrianId = antrianId;
        NoUrut = noUrut;
        ServicePointId = servicePointId;
        QueueLabel = queueLabel;
        ApotekPhase = apotekPhase;
        CallCountPenerimaan = callCountPenerimaan;
        LastCalledPenerimaanAt = lastCalledPenerimaanAt;
        CallCountSerah = callCountSerah;
        LastCalledSerahAt = lastCalledSerahAt;
        TakenAt = takenAt;
        ServedAt = servedAt;
        PreparedAt = preparedAt;
        DoneAt = doneAt;
        WithdrawnAt = withdrawnAt;
        LastSkippedAt = lastSkippedAt;
        LastSkippedLoketType = lastSkippedLoketType;
        WithdrawalReason = withdrawalReason;
        WithdrawalUserId = withdrawalUserId;
        IsPaymentOverridden = isPaymentOverridden;
        PaymentOverrideReason = paymentOverrideReason;
        PaymentOverridePharmacistId = paymentOverridePharmacistId;
    }

    public string AntrianId { get; }
    public int NoUrut { get; }
    public string ServicePointId { get; private set; }
    public string QueueLabel { get; private set; }
    public ApotekPhaseEnum ApotekPhase { get; private set; }
    public int CallCountPenerimaan { get; private set; }
    public DateTime LastCalledPenerimaanAt { get; private set; }
    public int CallCountSerah { get; private set; }
    public DateTime LastCalledSerahAt { get; private set; }
    public DateTime TakenAt { get; private set; }
    public DateTime ServedAt { get; private set; }
    public DateTime PreparedAt { get; private set; }
    public DateTime DoneAt { get; private set; }
    public DateTime WithdrawnAt { get; private set; }
    public DateTime LastSkippedAt { get; private set; }
    public LoketTypeEnum LastSkippedLoketType { get; private set; }
    public string WithdrawalReason { get; private set; }
    public string WithdrawalUserId { get; private set; }
    public bool IsPaymentOverridden { get; private set; }
    public string PaymentOverrideReason { get; private set; }
    public string PaymentOverridePharmacistId { get; private set; }

    public bool IsTerminal => ApotekPhase is ApotekPhaseEnum.SELESAI or ApotekPhaseEnum.BATAL or ApotekPhaseEnum.NO_SHOW;

    public static IApotekQueueEntryKey Key(string antrianId, int noUrut)
        => new QueueEntryKey(antrianId, noUrut);

    public static ApotekQueueEntryModel Create(
        string antrianId,
        int noUrut,
        string servicePointId,
        string queuePrefix)
    {
        return Create(antrianId, noUrut, servicePointId, queuePrefix, DateTime.Now);
    }

    public static ApotekQueueEntryModel Create(
        string antrianId,
        int noUrut,
        string servicePointId,
        string queuePrefix,
        DateTime takenAt)
    {
        Guard.Against.NullOrWhiteSpace(antrianId, nameof(antrianId));
        Guard.Against.NegativeOrZero(noUrut, nameof(noUrut));
        Guard.Against.NullOrWhiteSpace(servicePointId, nameof(servicePointId));
        Guard.Against.NullOrWhiteSpace(queuePrefix, nameof(queuePrefix));
        EnsureValidBusinessTime(takenAt, nameof(takenAt));

        var prefix = queuePrefix.Trim().ToUpperInvariant();
        var queueLabel = $"{prefix}{noUrut:D3}";

        return new ApotekQueueEntryModel(
            antrianId: antrianId.Trim(),
            noUrut: noUrut,
            servicePointId: servicePointId.Trim(),
            queueLabel: queueLabel,
            apotekPhase: ApotekPhaseEnum.TAKEN,
            callCountPenerimaan: 0,
            lastCalledPenerimaanAt: ApotekDate.Empty,
            callCountSerah: 0,
            lastCalledSerahAt: ApotekDate.Empty,
            takenAt: takenAt,
            servedAt: ApotekDate.Empty,
            preparedAt: ApotekDate.Empty,
            doneAt: ApotekDate.Empty,
            withdrawnAt: ApotekDate.Empty,
            lastSkippedAt: ApotekDate.Empty,
            lastSkippedLoketType: LoketTypeEnum.None,
            withdrawalReason: "",
            withdrawalUserId: "",
            isPaymentOverridden: false,
            paymentOverrideReason: "",
            paymentOverridePharmacistId: "");
    }

    public static ApotekQueueEntryModel Rehydrate(
        string antrianId,
        int noUrut,
        string servicePointId,
        string queueLabel,
        ApotekPhaseEnum apotekPhase,
        int callCountPenerimaan,
        DateTime lastCalledPenerimaanAt,
        int callCountSerah,
        DateTime lastCalledSerahAt,
        DateTime takenAt,
        DateTime servedAt,
        DateTime preparedAt,
        DateTime doneAt,
        DateTime withdrawnAt,
        DateTime lastSkippedAt,
        LoketTypeEnum lastSkippedLoketType,
        string withdrawalReason,
        string withdrawalUserId,
        bool isPaymentOverridden,
        string paymentOverrideReason,
        string paymentOverridePharmacistId)
    {
        return new ApotekQueueEntryModel(
            antrianId,
            noUrut,
            servicePointId,
            queueLabel,
            apotekPhase,
            callCountPenerimaan,
            lastCalledPenerimaanAt,
            callCountSerah,
            lastCalledSerahAt,
            takenAt,
            servedAt,
            preparedAt,
            doneAt,
            withdrawnAt,
            lastSkippedAt,
            lastSkippedLoketType,
            withdrawalReason ?? "",
            withdrawalUserId ?? "",
            isPaymentOverridden,
            paymentOverrideReason ?? "",
            paymentOverridePharmacistId ?? "");
    }

    public void RecordCallPenerimaan(DateTime now)
    {
        EnsureNotTerminal("record call at Loket Penerimaan");
        EnsureValidBusinessTime(now, nameof(now));

        if (ApotekPhase is not (ApotekPhaseEnum.TAKEN or ApotekPhaseEnum.PENERIMAAN or ApotekPhaseEnum.DILEWATI))
            throw new ApotekDomainException($"Cannot call at Loket Penerimaan when phase is '{ApotekPhase}'.");

        CallCountPenerimaan++;
        LastCalledPenerimaanAt = now;
    }

    public void RecordCallPenerimaan() => RecordCallPenerimaan(DateTime.Now);

    public void StartServicePenerimaan(DateTime now)
    {
        EnsureNotTerminal("start service at Loket Penerimaan");
        EnsureValidBusinessTime(now, nameof(now));

        if (ApotekPhase is not (ApotekPhaseEnum.TAKEN or ApotekPhaseEnum.PENERIMAAN or ApotekPhaseEnum.DILEWATI))
            throw new ApotekDomainException($"Cannot start service at Loket Penerimaan when phase is '{ApotekPhase}'.");

        ApotekPhase = ApotekPhaseEnum.PENERIMAAN;
        ServedAt = now;
    }

    public void StartServicePenerimaan() => StartServicePenerimaan(DateTime.Now);

    public void TransitionToPenyiapan()
    {
        EnsureNotTerminal("transition to PENYIAPAN");

        if (ApotekPhase is not (ApotekPhaseEnum.TAKEN or ApotekPhaseEnum.PENERIMAAN or ApotekPhaseEnum.DILEWATI))
            throw new ApotekDomainException($"Cannot transition to PENYIAPAN when phase is '{ApotekPhase}'.");

        ApotekPhase = ApotekPhaseEnum.PENYIAPAN;
    }

    public void ConsolidateToSiapSerah(DateTime now)
    {
        EnsureNotTerminal("consolidate to SIAP_SERAH");
        EnsureValidBusinessTime(now, nameof(now));

        if (ApotekPhase != ApotekPhaseEnum.PENYIAPAN)
            throw new ApotekDomainException($"Only queue entries in PENYIAPAN may consolidate to SIAP_SERAH. Current phase is '{ApotekPhase}'.");

        ApotekPhase = ApotekPhaseEnum.SIAP_SERAH;
        PreparedAt = now;
    }

    public void ConsolidateToSiapSerah() => ConsolidateToSiapSerah(DateTime.Now);

    public void RecordCallSerah(DateTime now)
    {
        EnsureNotTerminal("record call at Loket Serah");
        EnsureValidBusinessTime(now, nameof(now));

        if (ApotekPhase is not (ApotekPhaseEnum.SIAP_SERAH or ApotekPhaseEnum.SERAH_OBAT or ApotekPhaseEnum.DILEWATI))
            throw new ApotekDomainException($"Cannot call at Loket Serah when phase is '{ApotekPhase}'.");

        CallCountSerah++;
        LastCalledSerahAt = now;
    }

    public void RecordCallSerah() => RecordCallSerah(DateTime.Now);

    public void StartServiceSerah(DateTime now)
    {
        EnsureNotTerminal("start service at Loket Serah");
        EnsureValidBusinessTime(now, nameof(now));

        if (ApotekPhase is not (ApotekPhaseEnum.SIAP_SERAH or ApotekPhaseEnum.SERAH_OBAT or ApotekPhaseEnum.DILEWATI))
            throw new ApotekDomainException($"Cannot start service at Loket Serah when phase is '{ApotekPhase}'.");

        ApotekPhase = ApotekPhaseEnum.SERAH_OBAT;
    }

    public void StartServiceSerah() => StartServiceSerah(DateTime.Now);

    public void CompleteHandover(DateTime now, bool isPaymentOverridden = false, string reason = "", string pharmacistId = "")
    {
        EnsureNotTerminal("complete handover");
        EnsureValidBusinessTime(now, nameof(now));

        if (ApotekPhase is not (ApotekPhaseEnum.SERAH_OBAT or ApotekPhaseEnum.SIAP_SERAH))
            throw new ApotekDomainException($"Cannot complete handover when phase is '{ApotekPhase}'.");

        if (isPaymentOverridden)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new ApotekDomainException("Payment override reason is required when payment is overridden.");
            if (string.IsNullOrWhiteSpace(pharmacistId))
                throw new ApotekDomainException("Pharmacist ID is required when payment is overridden.");

            IsPaymentOverridden = true;
            PaymentOverrideReason = reason.Trim();
            PaymentOverridePharmacistId = pharmacistId.Trim();
        }
        else
        {
            IsPaymentOverridden = false;
            PaymentOverrideReason = "";
            PaymentOverridePharmacistId = "";
        }

        ApotekPhase = ApotekPhaseEnum.SELESAI;
        DoneAt = now;
    }

    public void Skip(LoketTypeEnum loketType, DateTime now)
    {
        EnsureNotTerminal("skip queue entry");
        EnsureValidBusinessTime(now, nameof(now));

        if (loketType == LoketTypeEnum.None)
            throw new ArgumentException("A valid loket type is required to skip.", nameof(loketType));

        ApotekPhase = ApotekPhaseEnum.DILEWATI;
        LastSkippedAt = now;
        LastSkippedLoketType = loketType;
    }

    public void Skip(LoketTypeEnum loketType) => Skip(loketType, DateTime.Now);

    public void Withdraw(string reason, string userId, DateTime now)
    {
        EnsureNotTerminal("withdraw queue entry");
        EnsureValidBusinessTime(now, nameof(now));
        Guard.Against.NullOrWhiteSpace(reason, nameof(reason));
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));

        ApotekPhase = ApotekPhaseEnum.BATAL;
        WithdrawalReason = reason.Trim();
        WithdrawalUserId = userId.Trim();
        WithdrawnAt = now;
    }

    public void Withdraw(string reason, string userId) => Withdraw(reason, userId, DateTime.Now);

    public void MarkNoShow(DateTime now)
    {
        EnsureNotTerminal("mark no-show");
        EnsureValidBusinessTime(now, nameof(now));

        ApotekPhase = ApotekPhaseEnum.NO_SHOW;
        WithdrawnAt = now;
    }

    public void MarkNoShow() => MarkNoShow(DateTime.Now);

    private void EnsureNotTerminal(string operation)
    {
        if (IsTerminal)
            throw new ApotekDomainException($"Cannot {operation} because queue entry is in terminal state '{ApotekPhase}'.");
    }

    private static void EnsureValidBusinessTime(DateTime value, string paramName)
    {
        if (value == default || value == DateTime.MinValue || ApotekDate.IsEmpty(value))
            throw new ArgumentException("A valid business time is required.", paramName);
    }

    private sealed record QueueEntryKey(string AntrianId, int NoUrut) : IApotekQueueEntryKey;
}
