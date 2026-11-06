using Bilreg.Domain.LabContext.LabMasterFeature;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.LabContext.LabMasterFeature;

public class LabDisciplineModelTest
{
    [Fact]
    public void Create_SetsPropertiesAndAuditTrail()
    {
        var createdAt = new DateTime(2026, 11, 4, 10, 0, 0);
        var model = LabDisciplineModel.Create("DISC-HEM", "Hematologi", "user1", createdAt);

        model.DisciplineId.Should().Be("DISC-HEM");
        model.DisciplineName.Should().Be("Hematologi");
        model.AuditTrail.Created.UserId.Should().Be("user1");
        model.AuditTrail.Created.Timestamp.Should().Be(createdAt);
    }

    [Theory]
    [InlineData(null, "Hematologi")]
    [InlineData("", "Hematologi")]
    [InlineData("   ", "Hematologi")]
    [InlineData("DISC-HEM", null)]
    [InlineData("DISC-HEM", "")]
    [InlineData("DISC-HEM", "   ")]
    public void Create_WithInvalidArguments_ThrowsArgumentException(string? id, string? name)
    {
        var act = () => LabDisciplineModel.Create(id!, name!, "user1");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Update_UpdatesDisciplineNameAndModifiedAudit()
    {
        var createdAt = new DateTime(2026, 11, 4, 10, 0, 0);
        var updatedAt = new DateTime(2026, 11, 4, 11, 0, 0);
        var model = LabDisciplineModel.Create("DISC-HEM", "Hematologi", "user1", createdAt);

        var updated = model.Update("Hematologi & Koagulasi", "user2", updatedAt);

        updated.DisciplineId.Should().Be("DISC-HEM");
        updated.DisciplineName.Should().Be("Hematologi & Koagulasi");
        updated.AuditTrail.Created.UserId.Should().Be("user1");
        updated.AuditTrail.Modified.UserId.Should().Be("user2");
        updated.AuditTrail.Modified.Timestamp.Should().Be(updatedAt);
    }

    [Fact]
    public void Default_ReturnsDefaultValues()
    {
        var def = LabDisciplineModel.Default;

        def.DisciplineId.Should().Be("-");
        def.DisciplineName.Should().Be("-");
    }

    [Fact]
    public void Key_ReturnsInstanceWithDisciplineId()
    {
        var key = LabDisciplineModel.Key("DISC-KIM");

        key.DisciplineId.Should().Be("DISC-KIM");
    }
}
