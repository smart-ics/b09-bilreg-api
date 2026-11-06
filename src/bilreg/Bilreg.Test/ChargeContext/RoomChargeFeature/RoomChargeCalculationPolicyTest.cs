using Bilreg.Domain.ChargeContext.RoomChargeFeature;
using FluentAssertions;

namespace Bilreg.Test.ChargeContext.RoomChargeFeature;

public class RoomChargeCalculationPolicyTest
{
    private static readonly DateTime StandardCutOff = new(2026, 10, 18, 12, 0, 0);

    #region EVALUATE DURATION TESTS

    [Theory]
    [InlineData("2026-10-18 08:00:00", "2026-10-18 11:30:00")] // Morning discharge, before 12:00 cutoff
    [InlineData("2026-10-18 08:00:00", "2026-10-18 15:00:00")] // Afternoon discharge, after 12:00 cutoff
    [InlineData("2026-10-18 20:00:00", "2026-10-18 23:45:00")] // Late night admission and discharge
    public void GivenSameDayStay_WhenEvaluateDuration_ThenReturnsOneDayMinimum(string checkInStr, string checkOutStr)
    {
        var checkIn = DateTime.Parse(checkInStr);
        var checkOut = DateTime.Parse(checkOutStr);

        var units = RoomChargeCalculationPolicy.EvaluateDuration(checkIn, checkOut, StandardCutOff);

        units.Should().Be(1.0m, "Same-day stay must result in at least 1.0 day charge");
    }

    [Theory]
    [InlineData("2026-10-16 14:00:00")]
    [InlineData("2026-10-17 09:30:00")]
    [InlineData("2026-10-18 06:00:00")]
    public void GivenDailyCutOffRunning_WhenEvaluateDuration_ThenReturnsOneDay(string checkInStr)
    {
        var checkIn = DateTime.Parse(checkInStr);

        var units = RoomChargeCalculationPolicy.EvaluateDuration(checkIn, checkOut: null, StandardCutOff);

        units.Should().Be(1.0m, "Daily cut-off running with active patient in bed must charge 1.0 day");
    }

    [Theory]
    [InlineData("2026-10-18 10:00:00")] // 2 hours before cutoff (negative delay)
    [InlineData("2026-10-18 12:00:00")] // Exactly on cutoff (0 delay)
    [InlineData("2026-10-18 13:00:00")] // 1 hour after cutoff
    [InlineData("2026-10-18 14:00:00")] // Exactly 2 hours after cutoff (boundary)
    public void GivenCheckoutDelayUpToTwoHours_WhenEvaluateDuration_ThenReturnsZeroAdditionalDay(string checkOutStr)
    {
        var checkIn = new DateTime(2026, 10, 17, 10, 0, 0);
        var checkOut = DateTime.Parse(checkOutStr);

        var units = RoomChargeCalculationPolicy.EvaluateDuration(checkIn, checkOut, StandardCutOff);

        units.Should().Be(0.0m, "Checkout grace period within 2 hours delay incurs 0.0 additional day");
    }

    [Theory]
    [InlineData("2026-10-18 14:00:01")] // 1 second over 2 hours
    [InlineData("2026-10-18 15:30:00")] // 3.5 hours delay
    [InlineData("2026-10-18 18:00:00")] // Exactly 6 hours delay (boundary)
    public void GivenCheckoutDelayBetweenTwoAndSixHours_WhenEvaluateDuration_ThenReturnsHalfDay(string checkOutStr)
    {
        var checkIn = new DateTime(2026, 10, 17, 10, 0, 0);
        var checkOut = DateTime.Parse(checkOutStr);

        var units = RoomChargeCalculationPolicy.EvaluateDuration(checkIn, checkOut, StandardCutOff);

        units.Should().Be(0.5m, "Checkout grace period between > 2h and <= 6h incurs 0.5 additional day");
    }

    [Theory]
    [InlineData("2026-10-18 18:00:01")] // 1 second over 6 hours
    [InlineData("2026-10-18 20:00:00")] // 8 hours delay
    [InlineData("2026-10-18 23:59:59")] // End of day checkout
    public void GivenCheckoutDelayGreaterThanSixHours_WhenEvaluateDuration_ThenReturnsFullDay(string checkOutStr)
    {
        var checkIn = new DateTime(2026, 10, 17, 10, 0, 0);
        var checkOut = DateTime.Parse(checkOutStr);

        var units = RoomChargeCalculationPolicy.EvaluateDuration(checkIn, checkOut, StandardCutOff);

        units.Should().Be(1.0m, "Checkout delay exceeding 6 hours incurs 1.0 additional day");
    }

    [Fact]
    public void GivenCheckOutEarlierThanCheckIn_WhenEvaluateDuration_ThenThrowsArgumentException()
    {
        var checkIn = new DateTime(2026, 10, 18, 14, 0, 0);
        var checkOut = new DateTime(2026, 10, 18, 10, 0, 0);

        var act = () => RoomChargeCalculationPolicy.EvaluateDuration(checkIn, checkOut, StandardCutOff);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Check-out time cannot be earlier than check-in time*");
    }

    #endregion

    #region NEWBORN ROOMING-IN TESTS

    [Fact]
    public void GivenNewbornRoomingIn_WhenCalculateTotalAmount_ThenReturnsZero()
    {
        var total = RoomChargeCalculationPolicy.CalculateTotalAmount(
            baseTariff: 450_000m,
            chargedUnits: 1.0m,
            discountAmount: 0m,
            isRoomingIn: true);

        total.Should().Be(0.0m, "Rooming-in baby must have 0.0 total amount");
    }

    [Fact]
    public void GivenRegularPatient_WhenCalculateTotalAmount_ThenCalculatesUnitsTimesTariffMinusDiscount()
    {
        var total = RoomChargeCalculationPolicy.CalculateTotalAmount(
            baseTariff: 400_000m,
            chargedUnits: 1.5m,
            discountAmount: 50_000m,
            isRoomingIn: false);

        total.Should().Be((400_000m * 1.5m) - 50_000m);
    }

    #endregion

    #region INTRA-DAY TRANSFER CONSOLIDATION TESTS

    [Fact]
    public void GivenMultipleTransfersOnSameDate_WhenConsolidate_ThenPicksHighestClassRate()
    {
        // Patient started in Kelas 3, moved to Kelas 1, then moved to Kelas 2 on the same calendar day
        var candidates = new List<RoomChargeCandidate>
        {
            new("PKB-01", "W-REG", "K-301", "B-01", "KLS-3", "KLS-3", "KLS-3", BaseTariff: 150_000m),
            new("PKB-02", "W-VIP", "K-101", "B-01", "KLS-1", "KLS-3", "KLS-1", BaseTariff: 500_000m, IsNaikKelas: true, DifferentialAmount: 350_000m),
            new("PKB-03", "W-REG", "K-201", "B-01", "KLS-2", "KLS-3", "KLS-2", BaseTariff: 300_000m, IsNaikKelas: true, DifferentialAmount: 150_000m)
        };

        var winning = RoomChargeCalculationPolicy.ConsolidateIntraDayTransfers(candidates);

        winning.PakaiBedId.Should().Be("PKB-02");
        winning.BillingClassId.Should().Be("KLS-1");
        winning.BaseTariff.Should().Be(500_000m);
        winning.DifferentialAmount.Should().Be(350_000m);
    }

    [Fact]
    public void GivenMultipleTransfersOnSameDate_WhenConsolidateToModel_ThenProducesExactlyOneChargeForDate()
    {
        var candidates = new List<RoomChargeCandidate>
        {
            new("PKB-01", "W-REG", "K-301", "B-01", "KLS-3", "KLS-3", "KLS-3", BaseTariff: 150_000m),
            new("PKB-02", "W-VIP", "K-101", "B-01", "KLS-1", "KLS-3", "KLS-1", BaseTariff: 500_000m, IsNaikKelas: true, DifferentialAmount: 350_000m)
        };

        var charge = RoomChargeCalculationPolicy.ConsolidateIntraDayTransfers(
            regId: "REG-TRANS-01",
            pasienId: "PAS-01",
            chargeCycleDate: "2026-10-18",
            timeCharge: new DateTime(2026, 10, 18, 12, 0, 0),
            triggerType: ChargeTriggerTypeEnum.DailyCutOff,
            candidates: candidates,
            userId: "nurse_head");

        charge.ChargeCycleDate.Should().Be("2026-10-18");
        charge.PakaiBedId.Should().Be("PKB-02");
        charge.BillingClassId.Should().Be("KLS-1");
        charge.BaseTariff.Should().Be(500_000m);
        charge.ChargedUnits.Should().Be(1.0m);
        charge.TotalAmount.Should().Be(500_000m);
    }

    [Fact]
    public void GivenExistingRoomChargeModels_WhenConsolidateIntraDayTransfers_ThenReturnsHighestTariffInstance()
    {
        var time = new DateTime(2026, 10, 18, 12, 0, 0);

        var charge1 = RoomChargeModel.Create(
            "PKB-01", "REG-01", "PAS-01", "W01", "K01", "B01",
            "2026-10-18", time, ChargeTriggerTypeEnum.DailyCutOff,
            "KLS-3", "KLS-3", "KLS-3", false, false, 0m, false, "",
            1.0m, 150_000m, 0m, 150_000m, [], "user01");

        var charge2 = RoomChargeModel.Create(
            "PKB-02", "REG-01", "PAS-01", "W02", "K02", "B02",
            "2026-10-18", time, ChargeTriggerTypeEnum.DailyCutOff,
            "KLS-1", "KLS-3", "KLS-1", false, true, 350_000m, false, "",
            1.0m, 500_000m, 0m, 500_000m, [], "user01");

        var winning = RoomChargeCalculationPolicy.ConsolidateIntraDayTransfers([charge1, charge2]);

        winning.PakaiBedId.Should().Be("PKB-02");
        winning.BaseTariff.Should().Be(500_000m);
    }

    [Fact]
    public void GivenEmptyCandidates_WhenConsolidate_ThenThrowsArgumentException()
    {
        var act = () => RoomChargeCalculationPolicy.ConsolidateIntraDayTransfers(new List<RoomChargeCandidate>());
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Candidates collection cannot be empty*");
    }

    #endregion

    #region CLASS DISPARITY TESTS

    [Fact]
    public void GivenPatientRequestUpgrade_WhenEvaluateClassDisparity_ThenNaikKelasWithDifferential()
    {
        var result = RoomChargeCalculationPolicy.EvaluateClassDisparity(
            physicalClassId: "KLS-VIP",
            payerClassId: "KLS-1",
            physicalTariff: 750_000m,
            payerTariff: 450_000m,
            isPatientRequestUpgrade: true);

        result.BillingClassId.Should().Be("KLS-VIP");
        result.IsTitipKelas.Should().BeFalse();
        result.IsNaikKelas.Should().BeTrue();
        result.DifferentialAmount.Should().Be(300_000m);
    }

    [Fact]
    public void GivenRoomFullHospitalAssignment_WhenEvaluateClassDisparity_ThenTitipKelasWithZeroDifferential()
    {
        var result = RoomChargeCalculationPolicy.EvaluateClassDisparity(
            physicalClassId: "KLS-VIP",
            payerClassId: "KLS-1",
            physicalTariff: 750_000m,
            payerTariff: 450_000m,
            isPatientRequestUpgrade: false);

        result.BillingClassId.Should().Be("KLS-1", "Titip kelas must be billed at payer class rate");
        result.IsTitipKelas.Should().BeTrue();
        result.IsNaikKelas.Should().BeFalse();
        result.DifferentialAmount.Should().Be(0m, "Hospital internal subsidy covers the difference, no patient co-payment");
    }

    [Fact]
    public void GivenSameClass_WhenEvaluateClassDisparity_ThenNormalClassWithZeroDifferential()
    {
        var result = RoomChargeCalculationPolicy.EvaluateClassDisparity(
            physicalClassId: "KLS-1",
            payerClassId: "KLS-1",
            physicalTariff: 450_000m,
            payerTariff: 450_000m,
            isPatientRequestUpgrade: false);

        result.BillingClassId.Should().Be("KLS-1");
        result.IsTitipKelas.Should().BeFalse();
        result.IsNaikKelas.Should().BeFalse();
        result.DifferentialAmount.Should().Be(0m);
    }

    #endregion

    #region ADDITIONAL EDGE CASES & OVERLOAD TESTS

    [Fact]
    public void GivenTimeSpanOverload_WhenEvaluateDuration_ThenBehavesIdentically()
    {
        var checkIn = new DateTime(2026, 10, 17, 10, 0, 0);
        var checkOut = new DateTime(2026, 10, 18, 15, 0, 0);
        var cutOffTimeOfDay = new TimeSpan(12, 0, 0);

        var units = RoomChargeCalculationPolicy.EvaluateDuration(checkIn, checkOut, cutOffTimeOfDay);

        units.Should().Be(0.5m);
    }

    [Fact]
    public void GivenSingleCandidate_WhenConsolidateIntraDayTransfers_ThenReturnsSameCandidate()
    {
        var single = new RoomChargeCandidate("PKB-01", "W01", "K01", "B01", "K1", "K1", "K1", 200_000m);
        var result = RoomChargeCalculationPolicy.ConsolidateIntraDayTransfers([single]);

        result.Should().Be(single);
    }

    [Fact]
    public void GivenMixedRoomingInAndRegularCandidates_WhenConsolidate_ThenPicksHighestRegularRate()
    {
        var candidates = new List<RoomChargeCandidate>
        {
            new("PKB-BABY", "W01", "K01", "B01", "K1", "K1", "K1", 0m, IsRoomingIn: true),
            new("PKB-REG1", "W01", "K01", "B02", "K2", "K2", "K2", 200_000m),
            new("PKB-REG2", "W02", "K02", "B01", "K1", "K2", "K1", 350_000m)
        };

        var winning = RoomChargeCalculationPolicy.ConsolidateIntraDayTransfers(candidates);

        winning.PakaiBedId.Should().Be("PKB-REG2");
        winning.BaseTariff.Should().Be(350_000m);
    }

    [Fact]
    public void GivenAllRoomingInCandidates_WhenConsolidate_ThenReturnsFirstRoomingInCandidate()
    {
        var candidates = new List<RoomChargeCandidate>
        {
            new("PKB-BABY1", "W01", "K01", "B01", "K1", "K1", "K1", 0m, IsRoomingIn: true),
            new("PKB-BABY2", "W01", "K02", "B02", "K1", "K1", "K1", 0m, IsRoomingIn: true)
        };

        var winning = RoomChargeCalculationPolicy.ConsolidateIntraDayTransfers(candidates);

        winning.PakaiBedId.Should().Be("PKB-BABY1");
        winning.IsRoomingIn.Should().BeTrue();
    }

    [Fact]
    public void GivenSingleRoomChargeModel_WhenConsolidateIntraDayTransfers_ThenReturnsSameInstance()
    {
        var model = RoomChargeModel.Create(
            "PKB-01", "REG-01", "PAS-01", "W01", "K01", "B01",
            "2026-10-18", DateTime.Now, ChargeTriggerTypeEnum.DailyCutOff,
            "K1", "K1", "K1", false, false, 0m, false, "",
            1.0m, 200_000m, 0m, 200_000m, [], "user01");

        var result = RoomChargeCalculationPolicy.ConsolidateIntraDayTransfers([model]);

        result.Should().BeSameAs(model);
    }

    [Fact]
    public void GivenEmptyRoomChargeModelList_WhenConsolidate_ThenThrowsArgumentException()
    {
        var act = () => RoomChargeCalculationPolicy.ConsolidateIntraDayTransfers(new List<RoomChargeModel>());

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Charges collection cannot be empty*");
    }

    [Fact]
    public void GivenAllRoomingInRoomChargeModels_WhenConsolidate_ThenReturnsFirstRoomingInInstance()
    {
        var time = DateTime.Now;
        var baby1 = RoomChargeModel.Create(
            "PKB-B1", "REG-01", "PAS-01", "W01", "K01", "B01",
            "2026-10-18", time, ChargeTriggerTypeEnum.DailyCutOff,
            "K1", "K1", "K1", false, false, 0m, true, "PKB-M1",
            0m, 0m, 0m, 0m, [], "user01");

        var baby2 = RoomChargeModel.Create(
            "PKB-B2", "REG-01", "PAS-01", "W01", "K02", "B02",
            "2026-10-18", time, ChargeTriggerTypeEnum.DailyCutOff,
            "K1", "K1", "K1", false, false, 0m, true, "PKB-M1",
            0m, 0m, 0m, 0m, [], "user01");

        var result = RoomChargeCalculationPolicy.ConsolidateIntraDayTransfers([baby1, baby2]);

        result.Should().BeSameAs(baby1);
    }

    #endregion
}
