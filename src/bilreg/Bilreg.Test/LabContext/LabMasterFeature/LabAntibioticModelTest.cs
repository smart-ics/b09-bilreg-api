using Bilreg.Domain.LabContext.LabMasterFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.LabContext.LabMasterFeature;

public class LabAntibioticModelTest
{
    [Fact]
    public void Constructor_SetsAllPropertiesAndAuditTrail()
    {
        var audit = AuditTrailType.Create("user1", new DateTime(2026, 11, 4, 10, 0, 0));
        var model = new LabAntibioticModel(
            "101",
            "Amikacin",
            "30 ug",
            "<=14",
            "15 - 16",
            ">= 17",
            10,
            audit);

        model.AntibioticId.Should().Be("101");
        model.AntibioticName.Should().Be("Amikacin");
        model.DiskConcentration.Should().Be("30 ug");
        model.ResistenRange.Should().Be("<=14");
        model.IntermediateRange.Should().Be("15 - 16");
        model.SensitifRange.Should().Be(">= 17");
        model.DisplayOrder.Should().Be(10);
        model.AuditTrail.Should().Be(audit);
    }

    [Fact]
    public void Constructor_WithNullOptionalStrings_DefaultsToEmptyString()
    {
        var audit = AuditTrailType.Create("user1", new DateTime(2026, 11, 4, 10, 0, 0));
        var model = new LabAntibioticModel(
            "101",
            "Amikacin",
            null!,
            null!,
            null!,
            null!,
            5,
            audit);

        model.DiskConcentration.Should().Be(string.Empty);
        model.ResistenRange.Should().Be(string.Empty);
        model.IntermediateRange.Should().Be(string.Empty);
        model.SensitifRange.Should().Be(string.Empty);
    }

    [Theory]
    [InlineData(null, "Amikacin")]
    [InlineData("", "Amikacin")]
    [InlineData("   ", "Amikacin")]
    [InlineData("101", null)]
    [InlineData("101", "")]
    [InlineData("101", "   ")]
    public void Constructor_WithInvalidArguments_ThrowsArgumentException(string? id, string? name)
    {
        var audit = AuditTrailType.Create("user1", new DateTime(2026, 11, 4, 10, 0, 0));
        var act = () => new LabAntibioticModel(id!, name!, "30 ug", "<=14", "15 - 16", ">= 17", 10, audit);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithNullAuditTrail_ThrowsArgumentNullException()
    {
        var act = () => new LabAntibioticModel("101", "Amikacin", "30 ug", "<=14", "15 - 16", ">= 17", 10, null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Create_SetsPropertiesAndAuditTrail()
    {
        var createdAt = new DateTime(2026, 11, 4, 10, 0, 0);
        var model = LabAntibioticModel.Create(
            "101",
            "Amikacin",
            "30 ug",
            "<=14",
            "15 - 16",
            ">= 17",
            10,
            "user1",
            createdAt);

        model.AntibioticId.Should().Be("101");
        model.AntibioticName.Should().Be("Amikacin");
        model.DiskConcentration.Should().Be("30 ug");
        model.ResistenRange.Should().Be("<=14");
        model.IntermediateRange.Should().Be("15 - 16");
        model.SensitifRange.Should().Be(">= 17");
        model.DisplayOrder.Should().Be(10);
        model.AuditTrail.Created.UserId.Should().Be("user1");
        model.AuditTrail.Created.Timestamp.Should().Be(createdAt);
    }

    [Fact]
    public void Create_SimplifiedOverload_SetsDefaults()
    {
        var createdAt = new DateTime(2026, 11, 4, 10, 0, 0);
        var model = LabAntibioticModel.Create("101", "Amikacin", "user1", createdAt);

        model.AntibioticId.Should().Be("101");
        model.AntibioticName.Should().Be("Amikacin");
        model.DiskConcentration.Should().Be(string.Empty);
        model.ResistenRange.Should().Be(string.Empty);
        model.IntermediateRange.Should().Be(string.Empty);
        model.SensitifRange.Should().Be(string.Empty);
        model.DisplayOrder.Should().Be(0);
        model.AuditTrail.Created.UserId.Should().Be("user1");
        model.AuditTrail.Created.Timestamp.Should().Be(createdAt);
    }

    [Theory]
    [InlineData(null, "Amikacin", "user1")]
    [InlineData("", "Amikacin", "user1")]
    [InlineData("   ", "Amikacin", "user1")]
    [InlineData("101", null, "user1")]
    [InlineData("101", "", "user1")]
    [InlineData("101", "   ", "user1")]
    [InlineData("101", "Amikacin", null)]
    [InlineData("101", "Amikacin", "")]
    [InlineData("101", "Amikacin", "   ")]
    public void Create_WithInvalidArguments_ThrowsArgumentException(string? id, string? name, string? userId)
    {
        var act = () => LabAntibioticModel.Create(id!, name!, "30 ug", "<=14", "15 - 16", ">= 17", 10, userId!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Update_UpdatesPropertiesAndModifiedAudit()
    {
        var createdAt = new DateTime(2026, 11, 4, 10, 0, 0);
        var updatedAt = new DateTime(2026, 11, 4, 11, 0, 0);
        var model = LabAntibioticModel.Create(
            "101",
            "Amikacin",
            "30 ug",
            "<=14",
            "15 - 16",
            ">= 17",
            10,
            "user1",
            createdAt);

        var updated = model.Update(
            "Amikacin Sulfate",
            "30 mcg",
            "<=12",
            "13 - 15",
            ">= 16",
            20,
            "user2",
            updatedAt);

        updated.AntibioticId.Should().Be("101");
        updated.AntibioticName.Should().Be("Amikacin Sulfate");
        updated.DiskConcentration.Should().Be("30 mcg");
        updated.ResistenRange.Should().Be("<=12");
        updated.IntermediateRange.Should().Be("13 - 15");
        updated.SensitifRange.Should().Be(">= 16");
        updated.DisplayOrder.Should().Be(20);
        updated.AuditTrail.Created.UserId.Should().Be("user1");
        updated.AuditTrail.Modified.UserId.Should().Be("user2");
        updated.AuditTrail.Modified.Timestamp.Should().Be(updatedAt);
    }

    [Fact]
    public void Update_SimplifiedOverload_PreservesExistingRanges()
    {
        var createdAt = new DateTime(2026, 11, 4, 10, 0, 0);
        var updatedAt = new DateTime(2026, 11, 4, 11, 0, 0);
        var model = LabAntibioticModel.Create(
            "101",
            "Amikacin",
            "30 ug",
            "<=14",
            "15 - 16",
            ">= 17",
            10,
            "user1",
            createdAt);

        var updated = model.Update("Amikacin Modified", "user2", updatedAt);

        updated.AntibioticId.Should().Be("101");
        updated.AntibioticName.Should().Be("Amikacin Modified");
        updated.DiskConcentration.Should().Be("30 ug");
        updated.ResistenRange.Should().Be("<=14");
        updated.IntermediateRange.Should().Be("15 - 16");
        updated.SensitifRange.Should().Be(">= 17");
        updated.DisplayOrder.Should().Be(10);
        updated.AuditTrail.Created.UserId.Should().Be("user1");
        updated.AuditTrail.Modified.UserId.Should().Be("user2");
        updated.AuditTrail.Modified.Timestamp.Should().Be(updatedAt);
    }

    [Theory]
    [InlineData(null, "user2")]
    [InlineData("", "user2")]
    [InlineData("   ", "user2")]
    [InlineData("Amikacin", null)]
    [InlineData("Amikacin", "")]
    [InlineData("Amikacin", "   ")]
    public void Update_WithInvalidArguments_ThrowsArgumentException(string? name, string? userId)
    {
        var model = LabAntibioticModel.Create("101", "Amikacin", "user1");
        var act = () => model.Update(name!, userId!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Default_ReturnsDefaultValues()
    {
        var def = LabAntibioticModel.Default;

        def.AntibioticId.Should().Be("-");
        def.AntibioticName.Should().Be("-");
        def.DiskConcentration.Should().BeEmpty();
        def.ResistenRange.Should().BeEmpty();
        def.IntermediateRange.Should().BeEmpty();
        def.SensitifRange.Should().BeEmpty();
        def.DisplayOrder.Should().Be(0);
        def.AuditTrail.Created.UserId.Should().BeEmpty();
    }

    [Fact]
    public void Key_ReturnsInstanceWithAntibioticId()
    {
        var key = LabAntibioticModel.Key("101");

        key.AntibioticId.Should().Be("101");
    }
}
