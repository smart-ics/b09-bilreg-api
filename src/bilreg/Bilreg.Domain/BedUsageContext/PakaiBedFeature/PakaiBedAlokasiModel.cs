using Ardalis.GuardClauses;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.BedUsageContext.PakaiBedFeature;

public record PakaiBedAlokasiModel : IPakaiBedAlokasiKey
{
    private const string ID_PREFIX = "PKB";
    internal static readonly DateTime EmptyDate = new(3000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly List<PakaiBedTransisiModel> _listTransisi;
    private readonly List<PakaiBedKoreksiModel> _listKoreksi;

    public PakaiBedAlokasiModel(
        string pakaiBedId,
        int version,
        string regId,
        string pasienId,
        string bangsalId,
        string kamarId,
        string bedId,
        string waitingListId,
        string requestId,
        PakaiBedPurposeEnum pakaiBedPurpose,
        OccupantRoleEnum occupantRole,
        PakaiBedStatusEnum pakaiBedStatus,
        DateTime proposedAt,
        DateTime? startedAt,
        string assignedBy,
        string bedAssignabilityEvidenceId,
        IEnumerable<PakaiBedTransisiModel> listTransisi,
        IEnumerable<PakaiBedKoreksiModel> listKoreksi,
        string physicalKelasId = "",
        string billingKelasId = "",
        string tipeKamarId = "",
        decimal tarif = 0,
        string motherPakaiBedId = "",
        bool isTitipKelas = false,
        bool isNaikKelas = false)
    {
        PakaiBedId = pakaiBedId;
        Version = version;
        RegId = regId;
        PasienId = pasienId;
        BangsalId = bangsalId;
        KamarId = kamarId;
        BedId = bedId;
        WaitingListId = waitingListId;
        RequestId = requestId;
        PakaiBedPurpose = pakaiBedPurpose;
        OccupantRole = occupantRole;
        PakaiBedStatus = pakaiBedStatus;
        ProposedAt = proposedAt;
        StartedAt = startedAt;
        AssignedBy = assignedBy;
        BedAssignabilityEvidenceId = bedAssignabilityEvidenceId;
        _listTransisi = listTransisi?.ToList() ?? [];
        _listKoreksi = listKoreksi?.ToList() ?? [];
        PhysicalKelasId = physicalKelasId ?? string.Empty;
        BillingKelasId = billingKelasId ?? string.Empty;
        TipeKamarId = tipeKamarId ?? string.Empty;
        Tarif = tarif;
        MotherPakaiBedId = motherPakaiBedId ?? string.Empty;
        IsTitipKelas = isTitipKelas;
        IsNaikKelas = isNaikKelas;
    }

    #region CREATION

    public static PakaiBedAlokasiModel Propose(
        string regId,
        string pasienId,
        string bangsalId,
        string kamarId,
        string bedId,
        string waitingListId,
        string requestId,
        PakaiBedPurposeEnum pakaiBedPurpose,
        OccupantRoleEnum occupantRole,
        string actorId,
        DateTime occurredAt,
        DateTime recordedAt,
        string reason,
        string physicalKelasId = "",
        string billingKelasId = "",
        string tipeKamarId = "",
        decimal tarif = 0,
        string motherPakaiBedId = "",
        bool isTitipKelas = false,
        bool isNaikKelas = false)
    {
        Guard.Against.NullOrWhiteSpace(regId);
        Guard.Against.NullOrWhiteSpace(pasienId);
        Guard.Against.NullOrWhiteSpace(bangsalId);
        Guard.Against.NullOrWhiteSpace(kamarId);
        Guard.Against.NullOrWhiteSpace(bedId);
        Guard.Against.NullOrWhiteSpace(requestId);
        ValidatePurposeAndRole(pakaiBedPurpose, occupantRole, waitingListId, motherPakaiBedId);

        var pakaiBedId = NunaId.New(ID_PREFIX);
        var transition = PakaiBedTransisiModel.Propose(
            pakaiBedId,
            occurredAt,
            recordedAt,
            actorId,
            reason,
            requestId,
            waitingListId);

        return new PakaiBedAlokasiModel(
            pakaiBedId,
            0,
            regId,
            pasienId,
            bangsalId,
            kamarId,
            bedId,
            waitingListId,
            requestId,
            pakaiBedPurpose,
            occupantRole,
            PakaiBedStatusEnum.Proposed,
            occurredAt,
            null,
            string.Empty,
            string.Empty,
            [transition],
            [],
            physicalKelasId,
            billingKelasId,
            tipeKamarId,
            tarif,
            motherPakaiBedId,
            isTitipKelas,
            isNaikKelas);
    }

    public static PakaiBedAlokasiModel Default => new(
        "-",
        0,
        "-",
        "-",
        "-",
        "-",
        "-",
        string.Empty,
        "-",
        PakaiBedPurposeEnum.Clinical,
        OccupantRoleEnum.Primary,
        PakaiBedStatusEnum.Proposed,
        EmptyDate,
        null,
        string.Empty,
        string.Empty,
        [],
        [],
        string.Empty,
        string.Empty,
        string.Empty,
        0,
        string.Empty,
        false,
        false);

    public static IPakaiBedAlokasiKey Key(string id)
    {
        Guard.Against.NullOrWhiteSpace(id);
        return Default with { PakaiBedId = id };
    }

    #endregion

    #region PROPERTIES

    public string PakaiBedId { get; init; }
    public int Version { get; init; }
    public string RegId { get; init; }
    public string PasienId { get; init; }
    public string BangsalId { get; init; }
    public string KamarId { get; init; }
    public string BedId { get; init; }
    public string WaitingListId { get; init; }
    public string RequestId { get; init; }
    public PakaiBedPurposeEnum PakaiBedPurpose { get; init; }
    public OccupantRoleEnum OccupantRole { get; init; }
    public PakaiBedStatusEnum PakaiBedStatus { get; init; }
    public DateTime ProposedAt { get; init; }
    public DateTime? StartedAt { get; init; }
    public string AssignedBy { get; init; }
    public string BedAssignabilityEvidenceId { get; init; }
    public string PhysicalKelasId { get; init; }
    public string BillingKelasId { get; init; }
    public string TipeKamarId { get; init; }
    public decimal Tarif { get; init; }
    public string MotherPakaiBedId { get; init; }
    public bool IsTitipKelas { get; init; }
    public bool IsNaikKelas { get; init; }
    public IEnumerable<PakaiBedTransisiModel> ListTransition => _listTransisi.AsReadOnly();
    public IEnumerable<PakaiBedKoreksiModel> ListCorrection => _listKoreksi.AsReadOnly();

    public bool IsClinicalPakaiBed =>
        PakaiBedStatus == PakaiBedStatusEnum.Active &&
        PakaiBedPurpose == PakaiBedPurposeEnum.Clinical &&
        OccupantRole == OccupantRoleEnum.Primary;

    public bool IsCompanionBed =>
        PakaiBedPurpose == PakaiBedPurposeEnum.Companion &&
        OccupantRole == OccupantRoleEnum.Companion;

    public bool IsRoomingIn =>
        PakaiBedPurpose == PakaiBedPurposeEnum.RoomingIn &&
        !string.IsNullOrWhiteSpace(MotherPakaiBedId);

    #endregion

    #region BEHAVIOUR

    public PakaiBedAlokasiModel Assign(
        string actorId,
        DateTime occurredAt,
        DateTime recordedAt,
        string reason,
        string bedAssignabilityEvidenceId)
    {
        Guard.Against.NullOrWhiteSpace(actorId);
        Guard.Against.NullOrWhiteSpace(reason);
        Guard.Against.NullOrWhiteSpace(bedAssignabilityEvidenceId);

        if (PakaiBedStatus != PakaiBedStatusEnum.Proposed)
            throw new InvalidOperationException(
                $"PakaiBed {PakaiBedId} harus Proposed untuk ditetapkan (status saat ini: {PakaiBedStatus}).");

        EnsureUtc(occurredAt, nameof(occurredAt));
        EnsureUtc(recordedAt, nameof(recordedAt));
        if (occurredAt < ProposedAt)
            throw new ArgumentException(
                $"Waktu assignment ({occurredAt:O}) tidak boleh lebih awal dari proposal ({ProposedAt:O}).",
                nameof(occurredAt));

        var assignment = PakaiBedTransisiModel.Assign(
            PakaiBedId,
            occurredAt,
            recordedAt,
            actorId,
            reason,
            RequestId,
            WaitingListId,
            bedAssignabilityEvidenceId);
        var transitions = _listTransisi.Append(assignment);

        return new PakaiBedAlokasiModel(
            PakaiBedId,
            Version + 1,
            RegId,
            PasienId,
            BangsalId,
            KamarId,
            BedId,
            WaitingListId,
            RequestId,
            PakaiBedPurpose,
            OccupantRole,
            PakaiBedStatusEnum.Active,
            ProposedAt,
            occurredAt,
            actorId,
            bedAssignabilityEvidenceId,
            transitions,
            _listKoreksi,
            PhysicalKelasId,
            BillingKelasId,
            TipeKamarId,
            Tarif,
            MotherPakaiBedId,
            IsTitipKelas,
            IsNaikKelas);
    }

    public PakaiBedAlokasiModel Release(
        DateTime occurredAt,
        string reason,
        string actorId,
        DateTime? recordedAt = null)
    {
        Guard.Against.NullOrWhiteSpace(actorId);
        Guard.Against.NullOrWhiteSpace(reason);

        if (PakaiBedStatus != PakaiBedStatusEnum.Active)
            throw new InvalidOperationException(
                $"PakaiBed {PakaiBedId} harus Active untuk dilepas (status saat ini: {PakaiBedStatus}).");

        EnsureUtc(occurredAt, nameof(occurredAt));
        var recAt = recordedAt ?? occurredAt;
        EnsureUtc(recAt, nameof(recordedAt));

        if (recAt < occurredAt)
            throw new ArgumentException(
                $"RecordedAt ({recAt:O}) tidak boleh lebih awal dari OccurredAt ({occurredAt:O}).",
                nameof(recordedAt));

        if (StartedAt.HasValue && occurredAt < StartedAt.Value)
            throw new ArgumentException(
                $"Waktu release ({occurredAt:O}) tidak boleh lebih awal dari waktu mulai ({StartedAt.Value:O}).",
                nameof(occurredAt));

        var releaseTransition = new PakaiBedTransisiModel(
            NunaId.New("ATR"),
            PakaiBedId,
            PakaiBedTransisiEnum.Released,
            occurredAt,
            recAt,
            actorId,
            reason,
            RequestId,
            WaitingListId,
            BedAssignabilityEvidenceId);
        var transitions = _listTransisi.Append(releaseTransition);

        return new PakaiBedAlokasiModel(
            PakaiBedId,
            Version + 1,
            RegId,
            PasienId,
            BangsalId,
            KamarId,
            BedId,
            WaitingListId,
            RequestId,
            PakaiBedPurpose,
            OccupantRole,
            PakaiBedStatusEnum.Released,
            ProposedAt,
            StartedAt,
            AssignedBy,
            BedAssignabilityEvidenceId,
            transitions,
            _listKoreksi,
            PhysicalKelasId,
            BillingKelasId,
            TipeKamarId,
            Tarif,
            MotherPakaiBedId,
            IsTitipKelas,
            IsNaikKelas);
    }

    public PakaiBedAlokasiModel Release(
        string reason,
        string actorId,
        DateTime occurredAt,
        DateTime? recordedAt = null)
        => Release(occurredAt, reason, actorId, recordedAt);

    public PakaiBedAlokasiModel Void(
        DateTime voidAt,
        string reason,
        string actorId,
        DateTime? recordedAt = null)
    {
        Guard.Against.NullOrWhiteSpace(actorId);
        Guard.Against.NullOrWhiteSpace(reason);

        if (PakaiBedStatus == PakaiBedStatusEnum.Cancelled)
            throw new InvalidOperationException(
                $"PakaiBed {PakaiBedId} sudah dibatalkan (Cancelled).");

        EnsureUtc(voidAt, nameof(voidAt));
        var recAt = recordedAt ?? voidAt;
        EnsureUtc(recAt, nameof(recordedAt));

        if (recAt < voidAt)
            throw new ArgumentException(
                $"RecordedAt ({recAt:O}) tidak boleh lebih awal dari VoidAt ({voidAt:O}).",
                nameof(recordedAt));

        if (voidAt < ProposedAt)
            throw new ArgumentException(
                $"Waktu pembatalan ({voidAt:O}) tidak boleh lebih awal dari waktu proposal ({ProposedAt:O}).",
                nameof(voidAt));

        var voidTransition = new PakaiBedTransisiModel(
            NunaId.New("ATR"),
            PakaiBedId,
            PakaiBedTransisiEnum.Cancelled,
            voidAt,
            recAt,
            actorId,
            reason,
            RequestId,
            WaitingListId,
            BedAssignabilityEvidenceId);
        var transitions = _listTransisi.Append(voidTransition);

        return new PakaiBedAlokasiModel(
            PakaiBedId,
            Version + 1,
            RegId,
            PasienId,
            BangsalId,
            KamarId,
            BedId,
            WaitingListId,
            RequestId,
            PakaiBedPurpose,
            OccupantRole,
            PakaiBedStatusEnum.Cancelled,
            ProposedAt,
            StartedAt,
            AssignedBy,
            BedAssignabilityEvidenceId,
            transitions,
            _listKoreksi,
            PhysicalKelasId,
            BillingKelasId,
            TipeKamarId,
            Tarif,
            MotherPakaiBedId,
            IsTitipKelas,
            IsNaikKelas);
    }

    public PakaiBedAlokasiModel Void(
        string reason,
        string actorId,
        DateTime voidAt,
        DateTime? recordedAt = null)
        => Void(voidAt, reason, actorId, recordedAt);

    #endregion

    #region HELPERS

    internal static void EnsureUtc(DateTime value, string parameterName)
    {
        if (value.Kind != DateTimeKind.Utc)
            throw new ArgumentException(
                $"{parameterName} harus menggunakan DateTimeKind.Utc.",
                parameterName);
    }

    private static void ValidatePurposeAndRole(
        PakaiBedPurposeEnum pakaiBedPurpose,
        OccupantRoleEnum occupantRole,
        string waitingListId,
        string motherPakaiBedId = "")
    {
        var isClinical = pakaiBedPurpose == PakaiBedPurposeEnum.Clinical &&
                         occupantRole == OccupantRoleEnum.Primary;
        var isCompanion = pakaiBedPurpose == PakaiBedPurposeEnum.Companion &&
                          occupantRole == OccupantRoleEnum.Companion;
        var isRoomingIn = pakaiBedPurpose == PakaiBedPurposeEnum.RoomingIn &&
                          (occupantRole == OccupantRoleEnum.Associated || occupantRole == OccupantRoleEnum.Companion);

        if (!isClinical && !isCompanion && !isRoomingIn)
            throw new InvalidOperationException(
                $"P3 hanya mendukung Clinical/Primary, Companion/Companion, atau RoomingIn (Associated/Companion); " +
                $"kombinasi saat ini: {pakaiBedPurpose}/{occupantRole}.");

        if (isClinical)
            Guard.Against.NullOrWhiteSpace(waitingListId);

        if (isCompanion && !string.IsNullOrWhiteSpace(waitingListId))
            throw new InvalidOperationException(
                "Companion PakaiBed tidak boleh menghasilkan konsekuensi Waiting List.");

        if (isRoomingIn && string.IsNullOrWhiteSpace(motherPakaiBedId))
            throw new InvalidOperationException(
                "RoomingIn PakaiBed memerlukan MotherPakaiBedId.");
    }

    #endregion
}

public interface IPakaiBedAlokasiKey
{
    string PakaiBedId { get; }
}
