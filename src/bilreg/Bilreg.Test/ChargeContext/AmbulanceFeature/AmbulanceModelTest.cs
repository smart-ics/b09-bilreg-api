using Bilreg.Domain.ChargeContext.AmbulanceFeature;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.ChargeContext.AmbulanceFeature;

public class AmbulanceModelTest
{
    [Fact]
    public void AmbulanceModel_Constructor_SetsDefaultArmadaProperties()
    {
        // Act
        var model = new AmbulanceModel("A01", "Ambulans 01");

        // Assert
        model.AmbulanceId.Should().Be("A01");
        model.AmbulanceName.Should().Be("Ambulans 01");
        model.NoPolisi.Should().BeEmpty();
        model.TipeArmada.Should().Be("Transport");
        model.StatusArmada.Should().Be("Available");
        model.IsAktif.Should().BeTrue();
        model.Abonement.Should().Be(0m);
        model.ListKomponen.Should().BeEmpty();
        model.ListTujuan.Should().BeEmpty();
    }

    [Fact]
    public void AmbulanceModel_Constructor_WithCustomArmadaProperties_SetsProperly()
    {
        // Act
        var model = new AmbulanceModel(
            id: "A02",
            name: "Ambulans Emergency ALS",
            noPolisi: "B 9999 AMB",
            tipeArmada: "ALS",
            statusArmada: "Available"
        );

        // Assert
        model.AmbulanceId.Should().Be("A02");
        model.AmbulanceName.Should().Be("Ambulans Emergency ALS");
        model.NoPolisi.Should().Be("B 9999 AMB");
        model.TipeArmada.Should().Be("ALS");
        model.StatusArmada.Should().Be("Available");
    }

    [Fact]
    public void AmbulanceModel_StatusArmadaHelpers_MutateStateCorrectly()
    {
        // Arrange
        var model = new AmbulanceModel("A01", "Ambulans 01");

        // Act & Assert
        model.SetInUse();
        model.StatusArmada.Should().Be("InUse");

        model.SetMaintenance();
        model.StatusArmada.Should().Be("Maintenance");

        model.SetAvailable();
        model.StatusArmada.Should().Be("Available");
    }

    [Fact]
    public void AmbulanceModel_AddAndAttachTujuan_WorksAndSyncsId()
    {
        // Arrange
        var model = new AmbulanceModel("A01", "Ambulans 01");
        var tujuan1 = new TransportTujuanModel("", "TJ01", 1.5m);
        var tujuan2 = new TransportTujuanModel("", "TJ02", 2.0m);

        // Act
        model.AddTujuan(tujuan1);
        model.AttachTujuan([tujuan2]);
        model.SyncId();

        // Assert
        model.ListTujuan.Should().HaveCount(2);
        tujuan1.AmbulanceId.Should().Be("A01");
        tujuan2.AmbulanceId.Should().Be("A01");
        tujuan1.Konstanta.Should().Be(1.5m);
        tujuan2.Konstanta.Should().Be(2.0m);

        // Remove
        model.RemoveTujuan(x => x.TujuanTransportId == "TJ01");
        model.ListTujuan.Should().HaveCount(1);
        model.ListTujuan.First().TujuanTransportId.Should().Be("TJ02");
    }

    [Fact]
    public void TransportTujuanModel_PropertiesAndMutations_WorkProperly()
    {
        // Arrange
        var model = new TransportTujuanModel("A01", "TJ01", 1.25m);

        // Act
        model.SetKonstanta(2.5m);
        model.SetAmbulanceId("A02");
        model.SetTujuanTransportId("TJ02");

        // Assert
        model.AmbulanceId.Should().Be("A02");
        model.TujuanTransportId.Should().Be("TJ02");
        model.Konstanta.Should().Be(2.5m);
    }

    [Fact]
    public void TujuanTransportModel_DefaultAmbulanceOverload_WorksProperly()
    {
        // Arrange
        var tujuan = new TujuanTransportModel("TJ01", "Tujuan Surabaya", 1.5m, false);

        // Act
        tujuan.SetDefaultAmbulance("AMB01");
        tujuan.DefaultAmbulanceId.Should().Be("AMB01");

        tujuan.UnSetDefaultAmbulance();
        tujuan.DefaultAmbulanceId.Should().BeEmpty();
    }

    [Fact]
    public void TrsTransportItemModel_CalculatesSubTotalAndValidates()
    {
        // Arrange & Act
        var item = new TrsTransportItemModel(
            trsTransportId: "TP26111601",
            detilTarifId: "D01",
            tarif: 100000m,
            qty: 2m,
            petugasMedisId: "NUR01",
            diskon: 20000m,
            keterangan: "Jasa Perawat Pendamping"
        );

        // Assert
        // SubTotal = (100.000 * 2) - 20.000 = 180.000
        item.SubTotal.Should().Be(180000m);
        item.PetugasMedisId.Should().Be("NUR01");
        item.IsVoid.Should().BeFalse();

        // Mutate tarif and discount
        item.SetTarif(150000m);
        item.SetDiskon(50000m);
        // SubTotal = (150.000 * 2) - 50.000 = 250.000
        item.SubTotal.Should().Be(250000m);

        // Void
        item.SetVoid(true);
        item.IsVoid.Should().BeTrue();
    }

    [Fact]
    public void TrsTransportItemModel_InvalidParameters_ThrowsArgumentException()
    {
        // Empty DetilTarifId
        var actEmptyTarif = () => new TrsTransportItemModel("TP01", "", 100000m);
        actEmptyTarif.Should().Throw<ArgumentException>().WithMessage("*Detil tarif ID*");

        // Zero or negative Qty
        var actZeroQty = () => new TrsTransportItemModel("TP01", "D01", 100000m, qty: 0m);
        actZeroQty.Should().Throw<ArgumentException>().WithMessage("*Qty minimal 1*");
    }
}
