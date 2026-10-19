using Bilreg.Domain.ChargeContext.RoomChargeFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;

namespace Bilreg.Test.ChargeContext.RoomChargeFeature;

public class RoomChargeModelTest
{
    private static List<RoomChargeKomponenModel> CreateSampleKomponen(decimal totalTarif = 500_000m)
    {
        return
        [
            RoomChargeKomponenModel.Create("KOMP01", "Sewa Kamar", "Akomodasi", totalTarif * 0.6m, 0, "411.01"),
            RoomChargeKomponenModel.Create("KOMP02", "Asuhan Keperawatan Bangsal", "Pelayanan", totalTarif * 0.25m, 0, "411.02"),
            RoomChargeKomponenModel.Create("KOMP03", "Konsumsi & Gizi", "Akomodasi", totalTarif * 0.15m, 0, "411.03")
        ];
    }

    [Fact]
    public void GivenEnumDefinitions_WhenChecked_ThenMatchesRequiredValues()
    {
        ((int)RoomChargeStatusEnum.Posted).Should().Be(0);
        ((int)RoomChargeStatusEnum.Adjusted).Should().Be(1);
        ((int)RoomChargeStatusEnum.Reversed).Should().Be(2);

        ((int)ChargeTriggerTypeEnum.DailyCutOff).Should().Be(0);
        ((int)ChargeTriggerTypeEnum.CheckoutReconciliation).Should().Be(1);
        ((int)ChargeTriggerTypeEnum.ManualAdjustment).Should().Be(2);
    }

    [Fact]
    public void GivenValidParameters_WhenCreate_ThenCreatesRoomChargeWithAllAttributes()
    {
        var komponen = CreateSampleKomponen(500_000m);
        var timeCharge = new DateTime(2026, 10, 19, 12, 0, 0);

        var model = RoomChargeModel.Create(
            pakaiBedId: "PKB001",
            regId: "REG001",
            pasienId: "PAS001",
            bangsalId: "W01",
            kamarId: "K01",
            bedId: "B01",
            chargeCycleDate: "2026-10-19",
            timeCharge: timeCharge,
            chargeTriggerType: ChargeTriggerTypeEnum.DailyCutOff,
            physicalClassId: "KLS-VIP",
            payerClassId: "KLS-1",
            billingClassId: "KLS-VIP",
            isTitipKelas: false,
            isNaikKelas: true,
            differentialAmount: 200_000m,
            isRoomingIn: false,
            motherPakaiBedId: string.Empty,
            chargedUnits: 1.0m,
            baseTariff: 500_000m,
            discountAmount: 50_000m,
            totalAmount: 450_000m,
            listKomponen: komponen,
            userId: "nurse01",
            occurredAt: timeCharge,
            roomChargeId: "RC-TEST-001",
            billingItemRefId: "BIL-REF-001");

        model.RoomChargeId.Should().Be("RC-TEST-001");
        model.PakaiBedId.Should().Be("PKB001");
        model.RegId.Should().Be("REG001");
        model.PasienId.Should().Be("PAS001");
        model.BangsalId.Should().Be("W01");
        model.KamarId.Should().Be("K01");
        model.BedId.Should().Be("B01");
        model.ChargeCycleDate.Should().Be("2026-10-19");
        model.TimeCharge.Should().Be(timeCharge);
        model.ChargeTriggerType.Should().Be(ChargeTriggerTypeEnum.DailyCutOff);
        model.PhysicalClassId.Should().Be("KLS-VIP");
        model.PayerClassId.Should().Be("KLS-1");
        model.BillingClassId.Should().Be("KLS-VIP");
        model.IsTitipKelas.Should().BeFalse();
        model.IsNaikKelas.Should().BeTrue();
        model.DifferentialAmount.Should().Be(200_000m);
        model.IsRoomingIn.Should().BeFalse();
        model.MotherPakaiBedId.Should().BeEmpty();
        model.ChargedUnits.Should().Be(1.0m);
        model.BaseTariff.Should().Be(500_000m);
        model.DiscountAmount.Should().Be(50_000m);
        model.TotalAmount.Should().Be(450_000m);
        model.RoomChargeStatus.Should().Be(RoomChargeStatusEnum.Posted);
        model.BillingItemRefId.Should().Be("BIL-REF-001");
        model.ReversalOfRoomChargeId.Should().BeEmpty();
        model.ReversalReason.Should().BeEmpty();
        model.AuditTrail.Created.UserId.Should().Be("nurse01");
        model.AuditTrail.Created.Timestamp.Should().Be(timeCharge);
        model.ListKomponen.Should().HaveCount(3);
    }

    [Fact]
    public void GivenNewbornRoomingIn_WhenCreate_ThenChargedUnitsAndTotalAmountAreZero()
    {
        var model = RoomChargeModel.Create(
            pakaiBedId: "PKB-BABY",
            regId: "REG-BABY",
            pasienId: "PAS-BABY",
            bangsalId: "W-MATERNITY",
            kamarId: "K-MAT01",
            bedId: "B-MOTH01",
            chargeCycleDate: "2026-10-19",
            timeCharge: new DateTime(2026, 10, 19, 12, 0, 0),
            chargeTriggerType: ChargeTriggerTypeEnum.DailyCutOff,
            physicalClassId: "KLS-1",
            payerClassId: "KLS-1",
            billingClassId: "KLS-1",
            isTitipKelas: false,
            isNaikKelas: false,
            differentialAmount: 0m,
            isRoomingIn: true,
            motherPakaiBedId: "PKB-MOTH01",
            chargedUnits: 1.0m,
            baseTariff: 350_000m,
            discountAmount: 0m,
            totalAmount: 350_000m,
            listKomponen: [],
            userId: "nurse01");

        model.IsRoomingIn.Should().BeTrue();
        model.MotherPakaiBedId.Should().Be("PKB-MOTH01");
        model.ChargedUnits.Should().Be(0.0m);
        model.TotalAmount.Should().Be(0.0m);
        model.DiscountAmount.Should().Be(0.0m);
    }

    [Fact]
    public void GivenPostedRoomCharge_WhenReverse_ThenTransitionsToReversedAndRecordsAudit()
    {
        var timeCharge = new DateTime(2026, 10, 19, 12, 0, 0);
        var reverseTime = new DateTime(2026, 10, 19, 14, 30, 0);

        var model = RoomChargeModel.Create(
            pakaiBedId: "PKB001",
            regId: "REG001",
            pasienId: "PAS001",
            bangsalId: "W01",
            kamarId: "K01",
            bedId: "B01",
            chargeCycleDate: "2026-10-19",
            timeCharge: timeCharge,
            chargeTriggerType: ChargeTriggerTypeEnum.DailyCutOff,
            physicalClassId: "KLS-1",
            payerClassId: "KLS-1",
            billingClassId: "KLS-1",
            isTitipKelas: false,
            isNaikKelas: false,
            differentialAmount: 0m,
            isRoomingIn: false,
            motherPakaiBedId: string.Empty,
            chargedUnits: 1.0m,
            baseTariff: 300_000m,
            discountAmount: 0m,
            listKomponen: CreateSampleKomponen(300_000m),
            userId: "nurse01",
            occurredAt: timeCharge);

        model.Reverse("supervisor01", "Salah input pasien kamar", reverseTime);

        model.RoomChargeStatus.Should().Be(RoomChargeStatusEnum.Reversed);
        model.ReversalReason.Should().Be("Salah input pasien kamar");
        model.AuditTrail.Modified.UserId.Should().Be("supervisor01");
        model.AuditTrail.Modified.Timestamp.Should().Be(reverseTime);
    }

    [Fact]
    public void GivenAlreadyReversedCharge_WhenReverseAgain_ThenThrowsInvalidOperationException()
    {
        var model = RoomChargeModel.Create(
            pakaiBedId: "PKB001",
            regId: "REG001",
            pasienId: "PAS001",
            bangsalId: "W01",
            kamarId: "K01",
            bedId: "B01",
            chargeCycleDate: "2026-10-19",
            timeCharge: new DateTime(2026, 10, 19, 12, 0, 0),
            chargeTriggerType: ChargeTriggerTypeEnum.DailyCutOff,
            physicalClassId: "KLS-1",
            payerClassId: "KLS-1",
            billingClassId: "KLS-1",
            isTitipKelas: false,
            isNaikKelas: false,
            differentialAmount: 0m,
            isRoomingIn: false,
            motherPakaiBedId: string.Empty,
            chargedUnits: 1.0m,
            baseTariff: 300_000m,
            discountAmount: 0m,
            listKomponen: [],
            userId: "nurse01");

        model.Reverse("supervisor01", "Batal pertama", new DateTime(2026, 10, 19, 13, 0, 0));

        var act = () => model.Reverse("supervisor02", "Batal kedua", new DateTime(2026, 10, 19, 14, 0, 0));
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot reverse room charge*expected 'Posted'*");
    }

    [Fact]
    public void GivenPostedCharge_WhenCreateReversalRecord_ThenGeneratesStornoRecordWithNegativeValues()
    {
        var timeCharge = new DateTime(2026, 10, 19, 12, 0, 0);
        var stornoTime = new DateTime(2026, 10, 19, 15, 0, 0);
        var komponen = CreateSampleKomponen(400_000m);

        var source = RoomChargeModel.Create(
            pakaiBedId: "PKB001",
            regId: "REG001",
            pasienId: "PAS001",
            bangsalId: "W01",
            kamarId: "K01",
            bedId: "B01",
            chargeCycleDate: "2026-10-19",
            timeCharge: timeCharge,
            chargeTriggerType: ChargeTriggerTypeEnum.DailyCutOff,
            physicalClassId: "KLS-VIP",
            payerClassId: "KLS-1",
            billingClassId: "KLS-VIP",
            isTitipKelas: false,
            isNaikKelas: true,
            differentialAmount: 150_000m,
            isRoomingIn: false,
            motherPakaiBedId: string.Empty,
            chargedUnits: 1.0m,
            baseTariff: 400_000m,
            discountAmount: 40_000m,
            totalAmount: 360_000m,
            listKomponen: komponen,
            userId: "nurse01",
            occurredAt: timeCharge,
            roomChargeId: "RC-ORIG-001");

        var storno = source.CreateReversalRecord("RC-REV-001", "cashier_sup", "Dispute penjaminan", stornoTime);

        // Source is updated
        source.RoomChargeStatus.Should().Be(RoomChargeStatusEnum.Reversed);
        source.ReversalReason.Should().Be("Dispute penjaminan");

        // Storno record is created with negative amounts
        storno.RoomChargeId.Should().Be("RC-REV-001");
        storno.ReversalOfRoomChargeId.Should().Be("RC-ORIG-001");
        storno.ReversalReason.Should().Be("Dispute penjaminan");
        storno.ChargedUnits.Should().Be(-1.0m);
        storno.TotalAmount.Should().Be(-360_000m);
        storno.DifferentialAmount.Should().Be(-150_000m);
        storno.DiscountAmount.Should().Be(-40_000m);
        storno.RoomChargeStatus.Should().Be(RoomChargeStatusEnum.Reversed);
        storno.AuditTrail.Created.UserId.Should().Be("cashier_sup");
        storno.ListKomponen.Should().HaveCount(3);
        storno.ListKomponen.First().Total.Should().BeLessThan(0);
    }

    [Fact]
    public void GivenPostedCharge_WhenAdjusted_ThenUpdatesValuesAndSetsAdjustedStatus()
    {
        var timeCharge = new DateTime(2026, 10, 19, 12, 0, 0);
        var adjustTime = new DateTime(2026, 10, 19, 16, 0, 0);

        var model = RoomChargeModel.Create(
            pakaiBedId: "PKB001",
            regId: "REG001",
            pasienId: "PAS001",
            bangsalId: "W01",
            kamarId: "K01",
            bedId: "B01",
            chargeCycleDate: "2026-10-19",
            timeCharge: timeCharge,
            chargeTriggerType: ChargeTriggerTypeEnum.DailyCutOff,
            physicalClassId: "KLS-1",
            payerClassId: "KLS-1",
            billingClassId: "KLS-1",
            isTitipKelas: false,
            isNaikKelas: false,
            differentialAmount: 0m,
            isRoomingIn: false,
            motherPakaiBedId: string.Empty,
            chargedUnits: 1.0m,
            baseTariff: 300_000m,
            discountAmount: 0m,
            listKomponen: [],
            userId: "nurse01",
            occurredAt: timeCharge);

        var newKomponen = CreateSampleKomponen(250_000m);
        model.Adjust(
            newChargedUnits: 0.5m,
            newBaseTariff: 250_000m,
            newDiscountAmount: 0m,
            newTotalAmount: 125_000m,
            newListKomponen: newKomponen,
            userId: "supervisor01",
            occurredAt: adjustTime);

        model.RoomChargeStatus.Should().Be(RoomChargeStatusEnum.Adjusted);
        model.ChargedUnits.Should().Be(0.5m);
        model.TotalAmount.Should().Be(125_000m);
        model.AuditTrail.Modified.UserId.Should().Be("supervisor01");
        model.ListKomponen.Should().HaveCount(3);
    }

    [Fact]
    public void GivenKeyHelper_WhenCalled_ReturnsKeyWithId()
    {
        var key = RoomChargeModel.Key("RC-KEY-123");
        key.RoomChargeId.Should().Be("RC-KEY-123");
    }

    [Fact]
    public void GivenBillingItemRefId_WhenSet_ThenUpdatesProperty()
    {
        var model = RoomChargeModel.Create(
            "PKB01", "REG01", "PAS01", "W01", "K01", "B01",
            "2026-10-19", DateTime.Now, ChargeTriggerTypeEnum.DailyCutOff,
            "K1", "K1", "K1", false, false, 0m, false, "",
            1.0m, 100_000m, 0m, [], "user01");

        model.SetBillingItemRefId("BIL-NEW-999");
        model.BillingItemRefId.Should().Be("BIL-NEW-999");
    }

    [Fact]
    public void GivenKomponen_WhenAddedAndAttached_ThenAppendsToList()
    {
        var model = RoomChargeModel.Create(
            "PKB01", "REG01", "PAS01", "W01", "K01", "B01",
            "2026-10-19", DateTime.Now, ChargeTriggerTypeEnum.DailyCutOff,
            "K1", "K1", "K1", false, false, 0m, false, "",
            1.0m, 100_000m, 0m, [], "user01");

        model.AddKomponen(RoomChargeKomponenModel.Create("K01", "Kamar", "Akomodasi", 60_000m));
        model.AttachKomponen([RoomChargeKomponenModel.Create("K02", "Gizi", "Akomodasi", 40_000m)]);

        model.ListKomponen.Should().HaveCount(2);
    }

    [Theory]
    [InlineData("", "REG01", "PAS01", "W01", "K01", "B01", "2026-10-19", "user01")]
    [InlineData("PKB01", "", "PAS01", "W01", "K01", "B01", "2026-10-19", "user01")]
    [InlineData("PKB01", "REG01", "", "W01", "K01", "B01", "2026-10-19", "user01")]
    [InlineData("PKB01", "REG01", "PAS01", "", "K01", "B01", "2026-10-19", "user01")]
    [InlineData("PKB01", "REG01", "PAS01", "W01", "", "B01", "2026-10-19", "user01")]
    [InlineData("PKB01", "REG01", "PAS01", "W01", "K01", "", "2026-10-19", "user01")]
    [InlineData("PKB01", "REG01", "PAS01", "W01", "K01", "B01", "", "user01")]
    [InlineData("PKB01", "REG01", "PAS01", "W01", "K01", "B01", "2026-10-19", "")]
    public void GivenInvalidParameters_WhenCreate_ThenThrowsArgumentException(
        string pakaiBedId, string regId, string pasienId, string bangsalId, string kamarId, string bedId, string cycleDate, string userId)
    {
        var act = () => RoomChargeModel.Create(
            pakaiBedId, regId, pasienId, bangsalId, kamarId, bedId,
            cycleDate, DateTime.Now, ChargeTriggerTypeEnum.DailyCutOff,
            "K1", "K1", "K1", false, false, 0m, false, "",
            1.0m, 100_000m, 0m, [], userId);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GivenRoomChargeKomponen_WhenCreate_ThenCalculatesTotalCorrectly()
    {
        var komp = RoomChargeKomponenModel.Create("K01", "Sewa Kamar", "", 200_000m, 20_000m, "411.01");
        komp.KomponenId.Should().Be("K01");
        komp.KomponenName.Should().Be("Sewa Kamar");
        komp.KomponenCategory.Should().Be("Akomodasi");
        komp.Tarif.Should().Be(200_000m);
        komp.Diskon.Should().Be(20_000m);
        komp.Total.Should().Be(180_000m);
        komp.RekeningPendapatan.Should().Be("411.01");
    }
}
