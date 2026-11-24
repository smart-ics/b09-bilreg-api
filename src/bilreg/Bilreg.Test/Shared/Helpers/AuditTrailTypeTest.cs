using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.Shared.Helpers;

public class AuditTrailTypeTest
{
    [Fact]
    public void Default_ShouldHaveIsVoidedFalse()
    {
        var sut = AuditTrailType.Default;

        sut.IsVoided.Should().BeFalse();
        sut.Voided.Timestamp.Should().Be(new DateTime(3000, 1, 1));
    }

    [Fact]
    public void Constructor_WhenVoidedIsDefaultOrSentinel_ShouldHaveIsVoidedFalse()
    {
        var created = new AuditInfoType("user1", new DateTime(2026, 1, 1));
        var modified = AuditInfoType.Default;
        var voided = new AuditInfoType("", new DateTime(3000, 1, 1));

        var sut = new AuditTrailType(created, modified, voided);

        sut.IsVoided.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WhenVoidedHasValidTimestamp_ShouldHaveIsVoidedTrue()
    {
        var created = new AuditInfoType("user1", new DateTime(2026, 1, 1));
        var modified = AuditInfoType.Default;
        var voided = new AuditInfoType("admin", new DateTime(2026, 11, 24, 10, 30, 0));

        var sut = new AuditTrailType(created, modified, voided);

        sut.IsVoided.Should().BeTrue();
    }

    [Fact]
    public void Batal_ShouldSetIsVoidedTrue()
    {
        var sut = AuditTrailType.Create("user1", new DateTime(2026, 1, 1));
        sut.IsVoided.Should().BeFalse();

        sut.Batal("admin", new DateTime(2026, 11, 24, 11, 0, 0));

        sut.IsVoided.Should().BeTrue();
        sut.Voided.UserId.Should().Be("admin");
        sut.Voided.Timestamp.Should().Be(new DateTime(2026, 11, 24, 11, 0, 0));
    }
}
