using Bilreg.Domain.ApotekContext.QueueFeature;
using FluentAssertions;

namespace Bilreg.Test.ApotekContext.QueueFeature;

public class PharmacyServicePointModelTest
{
    [Fact]
    public void Create_initializes_active_service_point_with_normalized_prefix()
    {
        var model = PharmacyServicePointModel.Create("SP01", "DEP01", "Resep Non-Racikan", "a");

        model.ServicePointId.Should().Be("SP01");
        model.DepoFarmasiId.Should().Be("DEP01");
        model.DisplayName.Should().Be("Resep Non-Racikan");
        model.QueuePrefix.Should().Be("A");
        model.Active.Should().BeTrue();
    }

    [Fact]
    public void Create_throws_when_required_fields_are_missing()
    {
        Action blankId = () => PharmacyServicePointModel.Create("", "DEP01", "Resep", "A");
        blankId.Should().Throw<ArgumentException>().WithParameterName("servicePointId");

        Action blankDepo = () => PharmacyServicePointModel.Create("SP01", "", "Resep", "A");
        blankDepo.Should().Throw<ArgumentException>().WithParameterName("depoFarmasiId");

        Action blankName = () => PharmacyServicePointModel.Create("SP01", "DEP01", "", "A");
        blankName.Should().Throw<ArgumentException>().WithParameterName("displayName");

        Action blankPrefix = () => PharmacyServicePointModel.Create("SP01", "DEP01", "Resep", "");
        blankPrefix.Should().Throw<ArgumentException>().WithParameterName("value");

        Action longPrefix = () => PharmacyServicePointModel.Create("SP01", "DEP01", "Resep", "ABCDEF");
        longPrefix.Should().Throw<ArgumentException>().WithParameterName("value");
    }

    [Fact]
    public void Deactivate_and_Activate_toggle_active_status()
    {
        var model = PharmacyServicePointModel.Create("SP01", "DEP01", "Resep Non-Racikan", "A");
        model.Active.Should().BeTrue();

        model.Deactivate();
        model.Active.Should().BeFalse();

        model.Activate();
        model.Active.Should().BeTrue();
    }

    [Fact]
    public void Update_changes_displayName_and_prefix()
    {
        var model = PharmacyServicePointModel.Create("SP01", "DEP01", "Resep Non-Racikan", "A");
        model.Update("Resep Racikan", "b");

        model.DisplayName.Should().Be("Resep Racikan");
        model.QueuePrefix.Should().Be("B");
    }

    [Fact]
    public void Rehydrate_restores_all_properties()
    {
        var model = PharmacyServicePointModel.Rehydrate("SP01", "DEP01", "Resep Non-Racikan", "A", false);

        model.ServicePointId.Should().Be("SP01");
        model.DepoFarmasiId.Should().Be("DEP01");
        model.DisplayName.Should().Be("Resep Non-Racikan");
        model.QueuePrefix.Should().Be("A");
        model.Active.Should().BeFalse();
    }

    [Fact]
    public void Key_creates_expected_interface_key()
    {
        var key = PharmacyServicePointModel.Key("SP01");
        key.ServicePointId.Should().Be("SP01");
    }
}
