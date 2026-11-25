using Bilreg.Domain.ChargeContext.AmbulanceFeature;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.ChargeContext.AmbulanceFeature;

public class TransportTariffCalculatorTest
{
    private static AmbulanceModel CreateAmbulance(decimal abonement, params AmbulanceKomponenModel[] komponen)
    {
        var amb = new AmbulanceModel("AMB01", "Ambulans 1");
        amb.SetAbonement(abonement);
        amb.Attach(komponen);
        return amb;
    }

    [Fact]
    public void Calculate_ScenarioA_WithoutAbonement_AppliesTetapAndVariabel()
    {
        // Arrange
        var amb = CreateAmbulance(
            0m,
            new AmbulanceKomponenModel("AMB01", "DT01", 50000m, isTetap: true),
            new AmbulanceKomponenModel("AMB01", "DT02", 100000m, isTetap: false));
        var tujuan = new TujuanTransportModel("TJ01", "Dalam Kota", 2m, false);

        // Act
        var result = TransportTariffCalculator.Calculate(amb, tujuan);

        // Assert
        result.KonstantaEfektif.Should().Be(2m);
        result.Abonement.Should().Be(0m);
        result.Items.Should().HaveCount(2);
        result.Items[0].Should().Be(new CalculatedTariffItem("DT01", 50000m, true, 50000m));
        result.Items[1].Should().Be(new CalculatedTariffItem("DT02", 100000m, false, 200000m));
        result.Total.Should().Be(250000m);
    }

    [Fact]
    public void Calculate_ScenarioB_WithAbonement_DistributesProportionallyWithAwayFromZeroRounding()
    {
        // Arrange: totalTarifVariabel = 100000 + 200000 = 300000; abonement = 100000
        // DT02: Round((100000/300000)*100000) = 33333 + (100000 * 2) = 233333
        // DT03: Round((200000/300000)*100000) = 66667 + (200000 * 2) = 466667
        var amb = CreateAmbulance(
            100000m,
            new AmbulanceKomponenModel("AMB01", "DT01", 50000m, isTetap: true),
            new AmbulanceKomponenModel("AMB01", "DT02", 100000m, isTetap: false),
            new AmbulanceKomponenModel("AMB01", "DT03", 200000m, isTetap: false));
        var tujuan = new TujuanTransportModel("TJ01", "Luar Kota", 2m, false);

        // Act
        var result = TransportTariffCalculator.Calculate(amb, tujuan);

        // Assert
        result.Items[0].TarifFinal.Should().Be(50000m);
        result.Items[1].TarifFinal.Should().Be(233333m);
        result.Items[2].TarifFinal.Should().Be(466667m);
        result.Total.Should().Be(750000m);
    }

    [Fact]
    public void Calculate_WithTaTransport3Override_UsesOverrideKonstanta()
    {
        // Arrange
        var amb = CreateAmbulance(
            0m,
            new AmbulanceKomponenModel("AMB01", "DT02", 100000m, isTetap: false));
        amb.AddTujuan(new TransportTujuanModel("AMB01", "TJ01", 3.5m));
        var tujuan = new TujuanTransportModel("TJ01", "Luar Kota", 2m, false);

        // Act
        var result = TransportTariffCalculator.Calculate(amb, tujuan);

        // Assert
        result.KonstantaEfektif.Should().Be(3.5m);
        result.Items[0].TarifFinal.Should().Be(350000m);
        result.Total.Should().Be(350000m);
    }

    [Fact]
    public void Calculate_WhenTotalTarifVariabelIsZero_DoesNotDivideByZeroAndIgnoresAbonement()
    {
        // Arrange: only fixed component, abonement > 0
        var amb = CreateAmbulance(
            100000m,
            new AmbulanceKomponenModel("AMB01", "DT01", 50000m, isTetap: true));
        var tujuan = new TujuanTransportModel("TJ01", "Dalam Kota", 2m, false);

        // Act
        var result = TransportTariffCalculator.Calculate(amb, tujuan);

        // Assert
        result.Items[0].TarifFinal.Should().Be(50000m);
        result.Total.Should().Be(50000m);
    }
}
