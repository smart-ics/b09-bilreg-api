using Bilreg.Domain.ChargeContext.RoomChargeFeature;
using Bilreg.Infrastructure.ChargeContext.RoomChargeFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Bilreg.Test.ChargeContext.RoomChargeFeature;

public class RoomChargeLegacyDalTest
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ListDataByReg_GivenEmptyOrNullRegId_ReturnsEmpty(string? regId)
    {
        // Arrange
        var opt = Options.Create(new DatabaseOptions { ServerName = "localhost", DbName = "dummy" });
        var dal = new RoomChargeLegacyDal(opt);

        // Act
        var result = dal.ListDataByReg(regId!);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void RoomChargeLegacyDto_TimeCharge_GivenValidDateAndJam_ParsesCorrectly()
    {
        // Arrange
        var dto = new RoomChargeLegacyDto
        {
            ChargeCycleDate = "2026-10-30",
            JamTrs = "14:30:15"
        };

        // Act
        var timeCharge = dto.TimeCharge;

        // Assert
        timeCharge.Should().Be(new DateTime(2026, 10, 30, 14, 30, 15));
    }

    [Fact]
    public void RoomChargeLegacyDto_TimeCharge_GivenEmptyJam_FallsBackToDate()
    {
        // Arrange
        var dto = new RoomChargeLegacyDto
        {
            ChargeCycleDate = "2026-10-30",
            JamTrs = ""
        };

        // Act
        var timeCharge = dto.TimeCharge;

        // Assert
        timeCharge.Should().Be(new DateTime(2026, 10, 30, 0, 0, 0));
    }

    [Fact]
    public void RoomChargeLegacyDto_TimeCharge_GivenExplicitValue_ReturnsSetValue()
    {
        // Arrange
        var expected = new DateTime(2026, 10, 25, 8, 0, 0);
        var dto = new RoomChargeLegacyDto
        {
            ChargeCycleDate = "2026-10-30",
            JamTrs = "14:30:00",
            TimeCharge = expected
        };

        // Act & Assert
        dto.TimeCharge.Should().Be(expected);
    }

    [Fact]
    public void RoomChargeLegacyDto_ToModel_GivenPostedStatus_MapsSafelyPerArchitectureTD10()
    {
        // Arrange
        var dto = new RoomChargeLegacyDto
        {
            RoomChargeId = "RMC2610300001",
            PakaiBedId = "PB001",
            RegId = "RG001",
            BangsalId = "B01",
            BedId = "BED01",
            ChargeCycleDate = "2026-10-30",
            JamTrs = "12:00:00",
            PetugasId = "USR01",
            BaseTariff = 500000m,
            DiscountAmount = 50000m,
            TotalAmount = 450000m,
            ChargedUnits = 1.0m,
            PayerClassId = "KL1",
            Notes = "Regular stay",
            RoomChargeStatus = "Posted",
            ListKomponen =
            [
                new RoomChargeKomponenLegacyDto
                {
                    RoomChargeId = "RMC2610300001",
                    KomponenId = "K01",
                    KomponenName = "Sewa Kamar",
                    KomponenCategory = "Akomodasi",
                    Tarif = 300000m,
                    Diskon = 0m,
                    Total = 300000m,
                    RekeningPendapatan = "4101"
                },
                new RoomChargeKomponenLegacyDto
                {
                    RoomChargeId = "RMC2610300001",
                    KomponenId = "K02",
                    Tarif = 200000m,
                    Diskon = 50000m,
                    Total = 150000m,
                    RekeningPendapatan = "4102"
                }
            ]
        };

        // Act
        var model = dto.ToModel();

        // Assert
        model.Should().NotBeNull();
        model.RoomChargeId.Should().Be("RMC2610300001");
        model.PakaiBedId.Should().Be("PB001");
        model.RegId.Should().Be("RG001");
        model.BangsalId.Should().Be("B01");
        model.BedId.Should().Be("BED01");
        model.ChargeCycleDate.Should().Be("2026-10-30");
        model.TimeCharge.Should().Be(new DateTime(2026, 10, 30, 12, 0, 0));
        model.PhysicalClassId.Should().Be("B01", "PhysicalClassId falls back safely to BangsalId/LayananId per TD-10");
        model.PayerClassId.Should().Be("KL1");
        model.BillingClassId.Should().Be("KL1");
        model.IsTitipKelas.Should().BeFalse();
        model.IsNaikKelas.Should().BeFalse();
        model.DifferentialAmount.Should().Be(0m);
        model.IsRoomingIn.Should().BeFalse();
        model.ChargedUnits.Should().Be(1.0m);
        model.BaseTariff.Should().Be(500000m);
        model.DiscountAmount.Should().Be(50000m);
        model.TotalAmount.Should().Be(450000m);
        model.RoomChargeStatus.Should().Be(RoomChargeStatusEnum.Posted);
        model.ReversalReason.Should().Be("Regular stay");
        model.AuditTrail.Created.UserId.Should().Be("USR01");

        // Verify Komponen
        model.ListKomponen.Should().HaveCount(2);
        model.ListKomponen[0].KomponenId.Should().Be("K01");
        model.ListKomponen[0].KomponenName.Should().Be("Sewa Kamar");
        model.ListKomponen[0].KomponenCategory.Should().Be("Akomodasi");
        model.ListKomponen[0].Total.Should().Be(300000m);

        model.ListKomponen[1].KomponenId.Should().Be("K02");
        model.ListKomponen[1].KomponenName.Should().Be("K02", "Fallback when empty is KomponenId");
        model.ListKomponen[1].KomponenCategory.Should().Be("Akomodasi", "Fallback when empty is Akomodasi");
        model.ListKomponen[1].Total.Should().Be(150000m);
    }

    [Fact]
    public void RoomChargeLegacyDto_ToModel_GivenReversedStatus_MapsToReversedEnum()
    {
        // Arrange
        var dto = new RoomChargeLegacyDto
        {
            RoomChargeId = "RMC2610300002",
            RoomChargeStatus = "Reversed",
            Notes = "Void: double input"
        };

        // Act
        var model = dto.ToModel();

        // Assert
        model.RoomChargeStatus.Should().Be(RoomChargeStatusEnum.Reversed);
        model.ReversalReason.Should().Be("Void: double input");
    }

    [Fact]
    public void RoomChargeKomponenLegacyDto_ToModel_WithDefaults_HandlesGracefully()
    {
        // Arrange
        var dto = new RoomChargeKomponenLegacyDto
        {
            RoomChargeId = "RMC001",
            KomponenId = "K99",
            Tarif = 100000m,
            Diskon = 10000m,
            Total = 90000m,
            RekeningPendapatan = "4109"
        };

        // Act
        var model = dto.ToModel();

        // Assert
        model.KomponenId.Should().Be("K99");
        model.KomponenName.Should().Be("K99");
        model.KomponenCategory.Should().Be("Akomodasi");
        model.Tarif.Should().Be(100000m);
        model.Diskon.Should().Be(10000m);
        model.Total.Should().Be(90000m);
        model.RekeningPendapatan.Should().Be("4109");
    }
}
