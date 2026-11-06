using Bilreg.Domain.ChargeContext.RoomChargeFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Infrastructure.ChargeContext.RoomChargeFeature;

public record RoomChargeDto
{
    public RoomChargeDto()
    {
    }

    public RoomChargeDto(
        string RoomChargeId,
        string PakaiBedId,
        string RegId,
        string PasienId,
        string BangsalId,
        string KamarId,
        string BedId,
        string ChargeCycleDate,
        DateTime TimeCharge,
        string ChargeTriggerType,
        string PhysicalClassId,
        string PayerClassId,
        string BillingClassId,
        bool IsTitipKelas,
        bool IsNaikKelas,
        decimal DifferentialAmount,
        bool IsRoomingIn,
        string MotherPakaiBedId,
        decimal ChargedUnits,
        decimal BaseTariff,
        decimal DiscountAmount,
        decimal TotalAmount,
        string RoomChargeStatus,
        string BillingItemRefId,
        string ReversalOfRoomChargeId,
        string ReversalReason,
        string CreatedBy,
        DateTime CreatedAt,
        string UpdatedBy,
        DateTime UpdatedAt,
        string BedCode = "",
        string KamarName = "",
        string BangsalName = "",
        string PhysicalClassName = "",
        string PayerClassName = "",
        string BillingClassName = "")
    {
        this.RoomChargeId = RoomChargeId ?? string.Empty;
        this.PakaiBedId = PakaiBedId ?? string.Empty;
        this.RegId = RegId ?? string.Empty;
        this.PasienId = PasienId ?? string.Empty;
        this.BangsalId = BangsalId ?? string.Empty;
        this.KamarId = KamarId ?? string.Empty;
        this.BedId = BedId ?? string.Empty;
        this.ChargeCycleDate = ChargeCycleDate ?? string.Empty;
        this.TimeCharge = TimeCharge;
        this.ChargeTriggerType = ChargeTriggerType ?? string.Empty;
        this.PhysicalClassId = PhysicalClassId ?? string.Empty;
        this.PayerClassId = PayerClassId ?? string.Empty;
        this.BillingClassId = BillingClassId ?? string.Empty;
        this.IsTitipKelas = IsTitipKelas;
        this.IsNaikKelas = IsNaikKelas;
        this.DifferentialAmount = DifferentialAmount;
        this.IsRoomingIn = IsRoomingIn;
        this.MotherPakaiBedId = MotherPakaiBedId ?? string.Empty;
        this.ChargedUnits = ChargedUnits;
        this.BaseTariff = BaseTariff;
        this.DiscountAmount = DiscountAmount;
        this.TotalAmount = TotalAmount;
        this.RoomChargeStatus = RoomChargeStatus ?? "Posted";
        this.BillingItemRefId = BillingItemRefId ?? string.Empty;
        this.ReversalOfRoomChargeId = ReversalOfRoomChargeId ?? string.Empty;
        this.ReversalReason = ReversalReason ?? string.Empty;
        this.CreatedBy = CreatedBy ?? string.Empty;
        this.CreatedAt = CreatedAt;
        this.UpdatedBy = UpdatedBy ?? string.Empty;
        this.UpdatedAt = UpdatedAt;
        this.BedCode = BedCode ?? string.Empty;
        this.KamarName = KamarName ?? string.Empty;
        this.BangsalName = BangsalName ?? string.Empty;
        this.PhysicalClassName = PhysicalClassName ?? string.Empty;
        this.PayerClassName = PayerClassName ?? string.Empty;
        this.BillingClassName = BillingClassName ?? string.Empty;
    }

    public string RoomChargeId { get; set; } = string.Empty;
    public string PakaiBedId { get; set; } = string.Empty;
    public string RegId { get; set; } = string.Empty;
    public string PasienId { get; set; } = string.Empty;
    public string BangsalId { get; set; } = string.Empty;
    public string BangsalName { get; set; } = string.Empty;
    public string KamarId { get; set; } = string.Empty;
    public string KamarName { get; set; } = string.Empty;
    public string BedId { get; set; } = string.Empty;
    public string BedCode { get; set; } = string.Empty;
    public string ChargeCycleDate { get; set; } = string.Empty;
    public DateTime TimeCharge { get; set; }
    public string ChargeTriggerType { get; set; } = string.Empty;
    public string PhysicalClassId { get; set; } = string.Empty;
    public string PhysicalClassName { get; set; } = string.Empty;
    public string PayerClassId { get; set; } = string.Empty;
    public string PayerClassName { get; set; } = string.Empty;
    public string BillingClassId { get; set; } = string.Empty;
    public string BillingClassName { get; set; } = string.Empty;
    public bool IsTitipKelas { get; set; }
    public bool IsNaikKelas { get; set; }
    public decimal DifferentialAmount { get; set; }
    public bool IsRoomingIn { get; set; }
    public string MotherPakaiBedId { get; set; } = string.Empty;
    public decimal ChargedUnits { get; set; }
    public decimal BaseTariff { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string RoomChargeStatus { get; set; } = "Posted";
    public string BillingItemRefId { get; set; } = string.Empty;
    public string ReversalOfRoomChargeId { get; set; } = string.Empty;
    public string ReversalReason { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }

    public static RoomChargeDto FromModel(RoomChargeModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        return new RoomChargeDto(
            model.RoomChargeId,
            model.PakaiBedId,
            model.RegId,
            model.PasienId,
            model.BangsalId,
            model.KamarId,
            model.BedId,
            model.ChargeCycleDate,
            model.TimeCharge,
            model.ChargeTriggerType.ToString(),
            model.PhysicalClassId,
            model.PayerClassId,
            model.BillingClassId,
            model.IsTitipKelas,
            model.IsNaikKelas,
            model.DifferentialAmount,
            model.IsRoomingIn,
            model.MotherPakaiBedId,
            model.ChargedUnits,
            model.BaseTariff,
            model.DiscountAmount,
            model.TotalAmount,
            model.RoomChargeStatus.ToString(),
            model.BillingItemRefId,
            model.ReversalOfRoomChargeId,
            model.ReversalReason,
            model.AuditTrail.Created.UserId,
            model.AuditTrail.Created.Timestamp,
            model.AuditTrail.Modified.UserId,
            model.AuditTrail.Modified.Timestamp,
            model.BedCode,
            model.KamarName,
            model.BangsalName,
            model.PhysicalClassName,
            model.PayerClassName,
            model.BillingClassName);
    }

    public RoomChargeModel ToModel(IEnumerable<RoomChargeKomponenModel>? listKomponen = null)
    {
        var triggerType = Enum.TryParse<ChargeTriggerTypeEnum>(ChargeTriggerType, true, out var parsedTrigger)
            ? parsedTrigger
            : ChargeTriggerTypeEnum.DailyCutOff;

        var status = Enum.TryParse<RoomChargeStatusEnum>(RoomChargeStatus, true, out var parsedStatus)
            ? parsedStatus
            : RoomChargeStatusEnum.Posted;

        var audit = new AuditTrailType(
            new AuditInfoType(CreatedBy ?? string.Empty, CreatedAt == default ? DateTime.Now : CreatedAt),
            new AuditInfoType(UpdatedBy ?? string.Empty, UpdatedAt == default ? DateTime.Now : UpdatedAt),
            AuditInfoType.Default);

        return new RoomChargeModel(
            RoomChargeId,
            PakaiBedId,
            RegId,
            PasienId,
            BangsalId,
            KamarId,
            BedId,
            ChargeCycleDate,
            TimeCharge,
            triggerType,
            PhysicalClassId ?? string.Empty,
            PayerClassId ?? string.Empty,
            BillingClassId ?? string.Empty,
            IsTitipKelas,
            IsNaikKelas,
            DifferentialAmount,
            IsRoomingIn,
            MotherPakaiBedId ?? string.Empty,
            ChargedUnits,
            BaseTariff,
            DiscountAmount,
            TotalAmount,
            status,
            BillingItemRefId ?? string.Empty,
            ReversalOfRoomChargeId ?? string.Empty,
            ReversalReason ?? string.Empty,
            audit,
            listKomponen,
            BedCode ?? string.Empty,
            KamarName ?? string.Empty,
            BangsalName ?? string.Empty,
            PhysicalClassName ?? string.Empty,
            PayerClassName ?? string.Empty,
            BillingClassName ?? string.Empty);
    }
}
