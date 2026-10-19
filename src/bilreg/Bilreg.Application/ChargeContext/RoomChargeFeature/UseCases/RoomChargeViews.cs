namespace Bilreg.Application.ChargeContext.RoomChargeFeature.UseCases;

public record RoomChargeKomponenView(
    string KomponenId,
    string KomponenName,
    string KomponenCategory,
    decimal Tarif,
    decimal Diskon,
    decimal Total,
    string RekeningPendapatan
);

public record RoomChargeView(
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
    string ReversalReason
);

public record RoomChargeDetailView(
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
    IEnumerable<RoomChargeKomponenView> ListKomponen
);

public record RoomChargeSummaryView(
    decimal TotalChargedUnits,
    decimal TotalAmount,
    decimal DifferentialAmount,
    decimal TotalSubsidi,
    int ActiveDaysCount
);
