using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.RoomChargeFeature;
using Bilreg.Domain.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.PaymentContext.RekapCetakFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Application.ChargeContext.RoomChargeFeature;

public static class RoomChargeBillingMapper
{
    public static TrsBillType ToTrsBill(RoomChargeModel roomCharge)
    {
        var rawId = string.IsNullOrWhiteSpace(roomCharge.BillingItemRefId) 
            ? roomCharge.RoomChargeId 
            : roomCharge.BillingItemRefId;
        var billingId = rawId.Length > 15 ? rawId[..15] : rawId;

        var transEvents = roomCharge.ListKomponen.Select((k, idx) =>
            new TrsBill2TransEventType(
                idx + 1,
                new TrsBill2KomponenType(k.KomponenId, k.KomponenName),
                TrsBillJenisBayarType.Pdp,
                k.Total,
                PpaType.Default.ToReff(),
                new TrsBill2CoaType(
                    new CoaType(k.RekeningPendapatan, ""),
                    new CoaType(k.RekeningPendapatan, ""),
                    CoaType.Default,
                    CoaType.Default,
                    CoaType.Default,
                    CoaType.Default)
            )).ToList();

        var isModified = roomCharge.AuditTrail.Modified != null &&
                         !string.IsNullOrWhiteSpace(roomCharge.AuditTrail.Modified.UserId) &&
                         roomCharge.AuditTrail.Modified.Timestamp != default &&
                         roomCharge.AuditTrail.Modified.Timestamp.Year > 1900 &&
                         roomCharge.AuditTrail.Modified.Timestamp.Year < 3000;

        var userId = isModified
            ? roomCharge.AuditTrail.Modified.UserId
            : (!string.IsNullOrWhiteSpace(roomCharge.AuditTrail.Created.UserId) ? roomCharge.AuditTrail.Created.UserId : "SYSTEM");

        var timestamp = isModified
            ? roomCharge.AuditTrail.Modified.Timestamp
            : (roomCharge.AuditTrail.Created.Timestamp != default ? roomCharge.AuditTrail.Created.Timestamp : roomCharge.TimeCharge);

        var auditInfo = new AuditInfoType(userId, timestamp);

        return new TrsBillType(
            billingId,
            BillModulGroup.Jasa,
            roomCharge.TimeCharge,
            new RegReff(roomCharge.RegId, roomCharge.PasienId, ""),
            new LayananReff(roomCharge.BangsalId, ""),
            new KelasReff(roomCharge.BillingClassId, ""),
            auditInfo,
            RekapCetakType.Default.ToReff(),
            new TrsBillNilaiType(roomCharge.TotalAmount, roomCharge.DiscountAmount, 0m, roomCharge.DifferentialAmount),
            new TrsBillKetType($"Sewa Kamar {roomCharge.BangsalId}", roomCharge.RoomChargeStatus.ToString(), roomCharge.RoomChargeId, roomCharge.ChargedUnits, roomCharge.PakaiBedId),
            transEvents,
            [],
            []
        );
    }
}
