using Ardalis.GuardClauses;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.ChargeContext.RoomChargeFeature;

public class RoomChargeModel : IRoomChargeKey
{
    private const string ID_PREFIX = "RMC";
    private readonly List<RoomChargeKomponenModel> _listKomponen;

    public RoomChargeModel(
        string roomChargeId,
        string pakaiBedId,
        string regId,
        string pasienId,
        string bangsalId,
        string kamarId,
        string bedId,
        string chargeCycleDate,
        DateTime timeCharge,
        ChargeTriggerTypeEnum chargeTriggerType,
        string physicalClassId,
        string payerClassId,
        string billingClassId,
        bool isTitipKelas,
        bool isNaikKelas,
        decimal differentialAmount,
        bool isRoomingIn,
        string motherPakaiBedId,
        decimal chargedUnits,
        decimal baseTariff,
        decimal discountAmount,
        decimal totalAmount,
        RoomChargeStatusEnum roomChargeStatus,
        string billingItemRefId,
        string reversalOfRoomChargeId,
        string reversalReason,
        AuditTrailType auditTrail,
        IEnumerable<RoomChargeKomponenModel>? listKomponen = null)
    {
        RoomChargeId = roomChargeId;
        PakaiBedId = pakaiBedId;
        RegId = regId;
        PasienId = pasienId;
        BangsalId = bangsalId;
        KamarId = kamarId;
        BedId = bedId;
        ChargeCycleDate = chargeCycleDate;
        TimeCharge = timeCharge;
        ChargeTriggerType = chargeTriggerType;
        PhysicalClassId = physicalClassId;
        PayerClassId = payerClassId;
        BillingClassId = billingClassId;
        IsTitipKelas = isTitipKelas;
        IsNaikKelas = isNaikKelas;
        DifferentialAmount = differentialAmount;
        IsRoomingIn = isRoomingIn;
        MotherPakaiBedId = motherPakaiBedId;
        ChargedUnits = chargedUnits;
        BaseTariff = baseTariff;
        DiscountAmount = discountAmount;
        TotalAmount = totalAmount;
        RoomChargeStatus = roomChargeStatus;
        BillingItemRefId = billingItemRefId;
        ReversalOfRoomChargeId = reversalOfRoomChargeId;
        ReversalReason = reversalReason;
        AuditTrail = auditTrail ?? AuditTrailType.Default;
        _listKomponen = listKomponen?.ToList() ?? [];
    }

    #region PROPERTIES

    public string RoomChargeId { get; private set; }
    public string PakaiBedId { get; private set; }
    public string RegId { get; private set; }
    public string PasienId { get; private set; }
    public string BangsalId { get; private set; }
    public string KamarId { get; private set; }
    public string BedId { get; private set; }
    public string ChargeCycleDate { get; private set; }
    public DateTime TimeCharge { get; private set; }
    public ChargeTriggerTypeEnum ChargeTriggerType { get; private set; }
    public string PhysicalClassId { get; private set; }
    public string PayerClassId { get; private set; }
    public string BillingClassId { get; private set; }
    public bool IsTitipKelas { get; private set; }
    public bool IsNaikKelas { get; private set; }
    public decimal DifferentialAmount { get; private set; }
    public bool IsRoomingIn { get; private set; }
    public string MotherPakaiBedId { get; private set; }
    public decimal ChargedUnits { get; private set; }
    public decimal BaseTariff { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public RoomChargeStatusEnum RoomChargeStatus { get; private set; }
    public string BillingItemRefId { get; private set; }
    public string ReversalOfRoomChargeId { get; private set; }
    public string ReversalReason { get; private set; }
    public AuditTrailType AuditTrail { get; private set; }
    public IReadOnlyList<RoomChargeKomponenModel> ListKomponen => _listKomponen.AsReadOnly();

    #endregion

    #region FACTORY METHODS

    public static RoomChargeModel Create(
        string pakaiBedId,
        string regId,
        string pasienId,
        string bangsalId,
        string kamarId,
        string bedId,
        string chargeCycleDate,
        DateTime timeCharge,
        ChargeTriggerTypeEnum chargeTriggerType,
        string physicalClassId,
        string payerClassId,
        string billingClassId,
        bool isTitipKelas,
        bool isNaikKelas,
        decimal differentialAmount,
        bool isRoomingIn,
        string motherPakaiBedId,
        decimal chargedUnits,
        decimal baseTariff,
        decimal discountAmount,
        decimal totalAmount,
        IEnumerable<RoomChargeKomponenModel> listKomponen,
        string userId,
        DateTime occurredAt = default,
        string roomChargeId = "",
        string billingItemRefId = "")
    {
        Guard.Against.NullOrWhiteSpace(pakaiBedId, nameof(pakaiBedId));
        Guard.Against.NullOrWhiteSpace(regId, nameof(regId));
        Guard.Against.NullOrWhiteSpace(pasienId, nameof(pasienId));
        Guard.Against.NullOrWhiteSpace(bangsalId, nameof(bangsalId));
        Guard.Against.NullOrWhiteSpace(kamarId, nameof(kamarId));
        Guard.Against.NullOrWhiteSpace(bedId, nameof(bedId));
        Guard.Against.NullOrWhiteSpace(chargeCycleDate, nameof(chargeCycleDate));
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));

        var id = string.IsNullOrWhiteSpace(roomChargeId) ? NunaId.New(ID_PREFIX) : roomChargeId;
        var auditTime = occurredAt == default ? (timeCharge == default ? DateTime.Now : timeCharge) : occurredAt;
        var audit = AuditTrailType.Create(userId, auditTime);

        // Newborn rooming-in rule: units = 0 and total = 0
        if (isRoomingIn)
        {
            chargedUnits = 0.0m;
            totalAmount = 0.0m;
            discountAmount = 0.0m;
        }

        return new RoomChargeModel(
            id,
            pakaiBedId,
            regId,
            pasienId,
            bangsalId,
            kamarId,
            bedId,
            chargeCycleDate,
            timeCharge,
            chargeTriggerType,
            physicalClassId ?? string.Empty,
            payerClassId ?? string.Empty,
            billingClassId ?? string.Empty,
            isTitipKelas,
            isNaikKelas,
            differentialAmount,
            isRoomingIn,
            motherPakaiBedId ?? string.Empty,
            chargedUnits,
            baseTariff,
            discountAmount,
            totalAmount,
            RoomChargeStatusEnum.Posted,
            billingItemRefId ?? string.Empty,
            reversalOfRoomChargeId: string.Empty,
            reversalReason: string.Empty,
            audit,
            listKomponen);
    }

    public static RoomChargeModel Create(
        string pakaiBedId,
        string regId,
        string pasienId,
        string bangsalId,
        string kamarId,
        string bedId,
        string chargeCycleDate,
        DateTime timeCharge,
        ChargeTriggerTypeEnum chargeTriggerType,
        string physicalClassId,
        string payerClassId,
        string billingClassId,
        bool isTitipKelas,
        bool isNaikKelas,
        decimal differentialAmount,
        bool isRoomingIn,
        string motherPakaiBedId,
        decimal chargedUnits,
        decimal baseTariff,
        decimal discountAmount,
        IEnumerable<RoomChargeKomponenModel> listKomponen,
        string userId,
        DateTime occurredAt = default,
        string roomChargeId = "",
        string billingItemRefId = "")
    {
        var totalAmount = isRoomingIn ? 0.0m : Math.Max(0m, (chargedUnits * baseTariff) - discountAmount);
        return Create(
            pakaiBedId,
            regId,
            pasienId,
            bangsalId,
            kamarId,
            bedId,
            chargeCycleDate,
            timeCharge,
            chargeTriggerType,
            physicalClassId,
            payerClassId,
            billingClassId,
            isTitipKelas,
            isNaikKelas,
            differentialAmount,
            isRoomingIn,
            motherPakaiBedId,
            chargedUnits,
            baseTariff,
            discountAmount,
            totalAmount,
            listKomponen,
            userId,
            occurredAt,
            roomChargeId,
            billingItemRefId);
    }

    public static IRoomChargeKey Key(string id) => new RoomChargeModel(
        id, "-", "-", "-", "-", "-", "-", "3000-01-01",
        new DateTime(3000, 1, 1), ChargeTriggerTypeEnum.DailyCutOff,
        "-", "-", "-", false, false, 0, false, "-",
        0, 0, 0, 0, RoomChargeStatusEnum.Posted, "-", "-", "-", AuditTrailType.Default);

    #endregion

    #region BEHAVIOUR

    public void Reverse(string userId, string reason, DateTime occurredAt)
    {
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));
        Guard.Against.NullOrWhiteSpace(reason, nameof(reason));

        if (RoomChargeStatus != RoomChargeStatusEnum.Posted)
        {
            throw new InvalidOperationException(
                $"Cannot reverse room charge '{RoomChargeId}' because current status is '{RoomChargeStatus}', expected '{RoomChargeStatusEnum.Posted}'.");
        }

        var modifTime = occurredAt == default ? DateTime.Now : occurredAt;
        RoomChargeStatus = RoomChargeStatusEnum.Reversed;
        ReversalReason = reason;
        AuditTrail.Modif(userId, modifTime);
    }

    public RoomChargeModel CreateReversalRecord(string reversalRoomChargeId, string userId, string reason, DateTime occurredAt)
    {
        Reverse(userId, reason, occurredAt);

        var id = string.IsNullOrWhiteSpace(reversalRoomChargeId) ? NunaId.New(ID_PREFIX) : reversalRoomChargeId;
        var modifTime = occurredAt == default ? DateTime.Now : occurredAt;

        var reversalKomponen = _listKomponen.Select(k => new RoomChargeKomponenModel(
            k.KomponenId,
            k.KomponenName,
            k.KomponenCategory,
            -k.Tarif,
            -k.Diskon,
            -k.Total,
            k.RekeningPendapatan
        )).ToList();

        return new RoomChargeModel(
            id,
            PakaiBedId,
            RegId,
            PasienId,
            BangsalId,
            KamarId,
            BedId,
            ChargeCycleDate,
            modifTime,
            ChargeTriggerType,
            PhysicalClassId,
            PayerClassId,
            BillingClassId,
            IsTitipKelas,
            IsNaikKelas,
            -DifferentialAmount,
            IsRoomingIn,
            MotherPakaiBedId,
            -ChargedUnits,
            BaseTariff,
            -DiscountAmount,
            -TotalAmount,
            RoomChargeStatusEnum.Reversed,
            billingItemRefId: string.Empty,
            reversalOfRoomChargeId: RoomChargeId,
            reversalReason: reason,
            AuditTrailType.Create(userId, modifTime),
            reversalKomponen);
    }

    public void Adjust(
        decimal newChargedUnits,
        decimal newBaseTariff,
        decimal newDiscountAmount,
        decimal newTotalAmount,
        IEnumerable<RoomChargeKomponenModel> newListKomponen,
        string userId,
        DateTime occurredAt)
    {
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));

        if (RoomChargeStatus != RoomChargeStatusEnum.Posted)
        {
            throw new InvalidOperationException(
                $"Cannot adjust room charge '{RoomChargeId}' because current status is '{RoomChargeStatus}', expected '{RoomChargeStatusEnum.Posted}'.");
        }

        ChargedUnits = IsRoomingIn ? 0.0m : newChargedUnits;
        BaseTariff = newBaseTariff;
        DiscountAmount = IsRoomingIn ? 0.0m : newDiscountAmount;
        TotalAmount = IsRoomingIn ? 0.0m : newTotalAmount;

        _listKomponen.Clear();
        if (newListKomponen != null)
        {
            _listKomponen.AddRange(newListKomponen);
        }

        RoomChargeStatus = RoomChargeStatusEnum.Adjusted;
        var modifTime = occurredAt == default ? DateTime.Now : occurredAt;
        AuditTrail.Modif(userId, modifTime);
    }

    public void SetBillingItemRefId(string billingItemRefId)
    {
        BillingItemRefId = billingItemRefId ?? string.Empty;
    }

    public void AddKomponen(RoomChargeKomponenModel komponen)
    {
        ArgumentNullException.ThrowIfNull(komponen);
        _listKomponen.Add(komponen);
    }

    public void AttachKomponen(IEnumerable<RoomChargeKomponenModel> komponenList)
    {
        ArgumentNullException.ThrowIfNull(komponenList);
        _listKomponen.AddRange(komponenList);
    }

    #endregion
}
