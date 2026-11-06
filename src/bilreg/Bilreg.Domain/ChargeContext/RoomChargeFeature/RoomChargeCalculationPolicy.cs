using Ardalis.GuardClauses;

namespace Bilreg.Domain.ChargeContext.RoomChargeFeature;

public record RoomChargeCandidate(
    string PakaiBedId,
    string BangsalId,
    string KamarId,
    string BedId,
    string PhysicalClassId,
    string PayerClassId,
    string BillingClassId,
    decimal BaseTariff,
    decimal DiscountAmount = 0,
    IEnumerable<RoomChargeKomponenModel>? ListKomponen = null,
    bool IsTitipKelas = false,
    bool IsNaikKelas = false,
    decimal DifferentialAmount = 0,
    bool IsRoomingIn = false,
    string MotherPakaiBedId = ""
);

public class RoomChargeCalculationPolicy
{
    /// <summary>
    /// Evaluates chargeable duration units based on check-in, check-out, and cut-off rules.
    /// Rules:
    /// 1. Daily cut-off running (checkOut is null) -> 1.0 day.
    /// 2. Same-day stay (checkIn.Date == checkOut.Value.Date) -> 1.0 day minimum.
    /// 3. Subsequent-day checkout tiered grace period:
    ///    - Delay &lt;= 2 hours -> 0.0 day
    ///    - Delay &gt; 2 hours and &lt;= 6 hours -> 0.5 day
    ///    - Delay &gt; 6 hours -> 1.0 day
    /// </summary>
    public static decimal EvaluateDuration(DateTime checkIn, DateTime? checkOut, DateTime cutOffTime)
    {
        if (checkOut.HasValue && checkOut.Value < checkIn)
        {
            throw new ArgumentException("Check-out time cannot be earlier than check-in time.", nameof(checkOut));
        }

        // Daily cut-off running: patient actively occupying room
        if (!checkOut.HasValue)
        {
            return 1.0m;
        }

        // Same-day stay: patient admitted and discharged on the same calendar day
        if (checkIn.Date == checkOut.Value.Date)
        {
            return 1.0m;
        }

        // Checkout on a subsequent day: evaluate tiered grace period against cut-off time on checkout day
        var checkoutDate = checkOut.Value.Date;
        var targetCutOff = new DateTime(
            checkoutDate.Year,
            checkoutDate.Month,
            checkoutDate.Day,
            cutOffTime.Hour,
            cutOffTime.Minute,
            cutOffTime.Second,
            checkOut.Value.Kind);

        var delay = checkOut.Value - targetCutOff;
        if (delay.TotalHours <= 2.0)
        {
            return 0.0m;
        }
        if (delay.TotalHours <= 6.0)
        {
            return 0.5m;
        }
        return 1.0m;
    }

    public static decimal EvaluateDuration(DateTime checkIn, DateTime? checkOut, TimeSpan cutOffTimeOfDay)
    {
        var dummy = DateTime.Today.Add(cutOffTimeOfDay);
        return EvaluateDuration(checkIn, checkOut, dummy);
    }

    /// <summary>
    /// Calculates the total room charge amount.
    /// Newborn rooming-in produces 0.0 regardless of base tariff.
    /// </summary>
    public static decimal CalculateTotalAmount(decimal baseTariff, decimal chargedUnits, decimal discountAmount = 0, bool isRoomingIn = false)
    {
        if (isRoomingIn)
        {
            return 0.0m;
        }
        return Math.Max(0m, (baseTariff * chargedUnits) - discountAmount);
    }

    /// <summary>
    /// Evaluates class disparity (Titip Kelas / Naik Kelas / Same Class).
    /// </summary>
    public static (string BillingClassId, bool IsTitipKelas, bool IsNaikKelas, decimal DifferentialAmount)
        EvaluateClassDisparity(string physicalClassId, string payerClassId, decimal physicalTariff, decimal payerTariff, bool isPatientRequestUpgrade)
    {
        if (isPatientRequestUpgrade)
        {
            var diff = Math.Max(0m, physicalTariff - payerTariff);
            return (physicalClassId, false, true, diff);
        }

        if (!string.Equals(physicalClassId, payerClassId, StringComparison.OrdinalIgnoreCase))
        {
            return (payerClassId, true, false, 0m);
        }

        return (payerClassId, false, false, 0m);
    }

    /// <summary>
    /// Consolidates intra-day transfers: selects the highest class rate among beds occupied on the same date,
    /// absorbing multiple bed transfers into exactly 1 charge.
    /// </summary>
    public static RoomChargeCandidate ConsolidateIntraDayTransfers(IEnumerable<RoomChargeCandidate> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        var candidateList = candidates.ToList();
        if (candidateList.Count == 0)
        {
            throw new ArgumentException("Candidates collection cannot be empty.", nameof(candidates));
        }

        if (candidateList.Count == 1)
        {
            return candidateList[0];
        }

        // If all are rooming-in, return the first
        if (candidateList.All(c => c.IsRoomingIn))
        {
            return candidateList[0];
        }

        // Filter out rooming-in and select candidate with highest base tariff
        var eligible = candidateList.Where(c => !c.IsRoomingIn).ToList();
        var winning = eligible
            .OrderByDescending(c => c.BaseTariff)
            .ThenByDescending(c => c.DifferentialAmount)
            .First();

        return winning;
    }

    /// <summary>
    /// Consolidates intra-day transfers from existing RoomChargeModel instances on the same date.
    /// </summary>
    public static RoomChargeModel ConsolidateIntraDayTransfers(IEnumerable<RoomChargeModel> charges)
    {
        ArgumentNullException.ThrowIfNull(charges);
        var chargeList = charges.ToList();
        if (chargeList.Count == 0)
        {
            throw new ArgumentException("Charges collection cannot be empty.", nameof(charges));
        }

        if (chargeList.Count == 1)
        {
            return chargeList[0];
        }

        if (chargeList.All(c => c.IsRoomingIn))
        {
            return chargeList[0];
        }

        var eligible = chargeList.Where(c => !c.IsRoomingIn).ToList();
        var winning = eligible
            .OrderByDescending(c => c.BaseTariff)
            .ThenByDescending(c => c.TotalAmount)
            .First();

        return winning;
    }

    /// <summary>
    /// Consolidates intra-day transfers and creates exactly 1 RoomChargeModel aggregate for the calendar date.
    /// </summary>
    public static RoomChargeModel ConsolidateIntraDayTransfers(
        string regId,
        string pasienId,
        string chargeCycleDate,
        DateTime timeCharge,
        ChargeTriggerTypeEnum triggerType,
        IEnumerable<RoomChargeCandidate> candidates,
        string userId,
        DateTime occurredAt = default)
    {
        var winning = ConsolidateIntraDayTransfers(candidates);
        var chargedUnits = winning.IsRoomingIn ? 0.0m : 1.0m;
        var totalAmount = CalculateTotalAmount(winning.BaseTariff, chargedUnits, winning.DiscountAmount, winning.IsRoomingIn);

        return RoomChargeModel.Create(
            winning.PakaiBedId,
            regId,
            pasienId,
            winning.BangsalId,
            winning.KamarId,
            winning.BedId,
            chargeCycleDate,
            timeCharge,
            triggerType,
            winning.PhysicalClassId,
            winning.PayerClassId,
            winning.BillingClassId,
            winning.IsTitipKelas,
            winning.IsNaikKelas,
            winning.DifferentialAmount,
            winning.IsRoomingIn,
            winning.MotherPakaiBedId,
            chargedUnits,
            winning.BaseTariff,
            winning.DiscountAmount,
            totalAmount,
            winning.ListKomponen ?? [],
            userId,
            occurredAt);
    }
}
