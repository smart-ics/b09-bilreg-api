using Bilreg.Domain.ChargeContext.RoomChargeFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Infrastructure.ChargeContext.RoomChargeFeature;

public class RoomChargeLegacyDto
{
    private DateTime? _timeCharge;

    public string RoomChargeId { get; set; } = string.Empty;
    public string PakaiBedId { get; set; } = string.Empty;
    public string RegId { get; set; } = string.Empty;
    public string BangsalId { get; set; } = string.Empty;
    public string BangsalName { get; set; } = string.Empty;
    public string KamarId { get; set; } = string.Empty;
    public string KamarName { get; set; } = string.Empty;
    public string BedId { get; set; } = string.Empty;
    public string BedCode { get; set; } = string.Empty;
    public string ChargeCycleDate { get; set; } = string.Empty;
    public string JamTrs { get; set; } = string.Empty;

    public DateTime TimeCharge
    {
        get
        {
            if (_timeCharge.HasValue) return _timeCharge.Value;
            if (DateTime.TryParse($"{ChargeCycleDate} {JamTrs}", out var dt))
                return dt;
            if (DateTime.TryParse(ChargeCycleDate, out var d))
                return d;
            return DateTime.MinValue;
        }
        set => _timeCharge = value;
    }

    public string PetugasId { get; set; } = string.Empty;
    public decimal BaseTariff { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal ChargedUnits { get; set; }
    public string PhysicalClassId { get; set; } = string.Empty;
    public string PhysicalClassName { get; set; } = string.Empty;
    public string PayerClassId { get; set; } = string.Empty;
    public string PayerClassName { get; set; } = string.Empty;
    public string BillingClassId { get; set; } = string.Empty;
    public string BillingClassName { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string RoomChargeStatus { get; set; } = "Posted";
    public List<RoomChargeKomponenLegacyDto> ListKomponen { get; set; } = [];

    public RoomChargeModel ToModel()
    {
        var status = string.Equals(RoomChargeStatus, "Reversed", StringComparison.OrdinalIgnoreCase)
            ? RoomChargeStatusEnum.Reversed
            : RoomChargeStatusEnum.Posted;

        var kompModels = ListKomponen?.Select(k => k.ToModel()) ?? [];

        var audit = new AuditTrailType(
            new AuditInfoType(PetugasId ?? string.Empty, TimeCharge == default ? DateTime.Now : TimeCharge),
            AuditInfoType.Default,
            AuditInfoType.Default);

        return new RoomChargeModel(
            RoomChargeId,
            PakaiBedId,
            RegId,
            string.Empty,
            BangsalId,
            string.IsNullOrWhiteSpace(KamarId) ? BangsalId : KamarId,
            BedId,
            ChargeCycleDate,
            TimeCharge,
            ChargeTriggerTypeEnum.DailyCutOff,
            string.IsNullOrWhiteSpace(PhysicalClassId) ? BangsalId : PhysicalClassId,
            PayerClassId,
            string.IsNullOrWhiteSpace(BillingClassId) ? PayerClassId : BillingClassId,
            false,
            false,
            0m,
            false,
            string.Empty,
            ChargedUnits,
            BaseTariff,
            DiscountAmount,
            TotalAmount,
            status,
            string.Empty,
            string.Empty,
            Notes ?? string.Empty,
            audit,
            kompModels,
            BedCode,
            KamarName,
            BangsalName,
            PhysicalClassName,
            PayerClassName,
            BillingClassName);
    }
}

public class RoomChargeKomponenLegacyDto
{
    public string RoomChargeId { get; set; } = string.Empty;
    public string KomponenId { get; set; } = string.Empty;
    public string KomponenName { get; set; } = string.Empty;
    public string KomponenCategory { get; set; } = string.Empty;
    public decimal Tarif { get; set; }
    public decimal Diskon { get; set; }
    public decimal Total { get; set; }
    public string RekeningPendapatan { get; set; } = string.Empty;
    public string RekeningDiskon { get; set; } = string.Empty;

    public RoomChargeKomponenModel ToModel()
    {
        return new RoomChargeKomponenModel(
            KomponenId,
            string.IsNullOrWhiteSpace(KomponenName) ? KomponenId : KomponenName,
            string.IsNullOrWhiteSpace(KomponenCategory) ? "Akomodasi" : KomponenCategory,
            Tarif,
            Diskon,
            Total,
            RekeningPendapatan ?? string.Empty);
    }
}
