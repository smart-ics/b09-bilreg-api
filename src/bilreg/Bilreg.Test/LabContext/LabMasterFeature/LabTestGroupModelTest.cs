using Bilreg.Domain.LabContext.LabMasterFeature;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.LabContext.LabMasterFeature;

public class LabTestGroupModelTest
{
    [Fact]
    public void Create_SetsPropertiesAndAuditTrail()
    {
        var createdAt = new DateTime(2026, 11, 4, 10, 0, 0);
        var model = LabTestGroupModel.Create("GRP-CBC", "Darah Lengkap", "user1", createdAt);

        model.TestGroupId.Should().Be("GRP-CBC");
        model.TestGroupName.Should().Be("Darah Lengkap");
        model.AuditTrail.Created.UserId.Should().Be("user1");
        model.AuditTrail.Created.Timestamp.Should().Be(createdAt);
    }

    [Theory]
    [InlineData(null, "Darah Lengkap")]
    [InlineData("", "Darah Lengkap")]
    [InlineData("   ", "Darah Lengkap")]
    [InlineData("GRP-CBC", null)]
    [InlineData("GRP-CBC", "")]
    [InlineData("GRP-CBC", "   ")]
    public void Create_WithInvalidArguments_ThrowsArgumentException(string? id, string? name)
    {
        var act = () => LabTestGroupModel.Create(id!, name!, "user1");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Update_UpdatesTestGroupNameAndModifiedAudit()
    {
        var createdAt = new DateTime(2026, 11, 4, 10, 0, 0);
        var updatedAt = new DateTime(2026, 11, 4, 11, 0, 0);
        var model = LabTestGroupModel.Create("GRP-CBC", "Darah Lengkap", "user1", createdAt);

        var updated = model.Update("Hematologi Lengkap (CBC)", "user2", updatedAt);

        updated.TestGroupId.Should().Be("GRP-CBC");
        updated.TestGroupName.Should().Be("Hematologi Lengkap (CBC)");
        updated.AuditTrail.Created.UserId.Should().Be("user1");
        updated.AuditTrail.Modified.UserId.Should().Be("user2");
        updated.AuditTrail.Modified.Timestamp.Should().Be(updatedAt);
    }

    [Fact]
    public void Default_ReturnsDefaultValues()
    {
        var def = LabTestGroupModel.Default;

        def.TestGroupId.Should().Be("-");
        def.TestGroupName.Should().Be("-");
    }

    [Fact]
    public void Key_ReturnsInstanceWithTestGroupId()
    {
        var key = LabTestGroupModel.Key("GRP-LIP");

        key.TestGroupId.Should().Be("GRP-LIP");
    }
}
