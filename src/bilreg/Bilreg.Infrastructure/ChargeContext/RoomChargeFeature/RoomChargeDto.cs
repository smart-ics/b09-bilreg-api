using Bilreg.Domain.ChargeContext.RoomChargeFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Infrastructure.ChargeContext.RoomChargeFeature;

public record RoomChargeDto(
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
    DateTime UpdatedAt)
{
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
            model.AuditTrail.Modified.Timestamp);
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
            listKomponen);
    }
}
