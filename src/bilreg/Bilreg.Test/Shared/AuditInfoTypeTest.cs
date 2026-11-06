using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.Shared;

public class AuditInfoTypeTest
{
    [Fact]
    public void Constructor_GivenSentinelDate_ShouldSet3000Year()
    {
        // Act
        var result = new AuditInfoType("usr1", "3000-01-01", "  ");

        // Assert
        result.UserId.Should().Be("usr1");
        result.Timestamp.Should().Be(new DateTime(3000, 1, 1));
    }

    [Fact]
    public void Constructor_GivenEmptyDate_ShouldSet3000Year()
    {
        // Act
        var result = new AuditInfoType("usr1", "", "");

        // Assert
        result.Timestamp.Should().Be(new DateTime(3000, 1, 1));
    }

    [Fact]
    public void Constructor_GivenValidDateAndFullJam_ShouldParseCorrectly()
    {
        // Act
        var result = new AuditInfoType("usr1", "2026-10-26", "11:45:00");

        // Assert
        result.Timestamp.Should().Be(new DateTime(2026, 10, 26, 11, 45, 0));
    }

    [Fact]
    public void Constructor_GivenValidDateAndShortJam_ShouldParseCorrectly()
    {
        // Act
        var result = new AuditInfoType("usr1", "2026-10-26", "11:45");

        // Assert
        result.Timestamp.Should().Be(new DateTime(2026, 10, 26, 11, 45, 0));
    }

    [Fact]
    public void Constructor_GivenValidDateAndEmptyJam_ShouldParseWithMidnight()
    {
        // Act
        var result = new AuditInfoType("usr1", "2026-10-26", " ");

        // Assert
        result.Timestamp.Should().Be(new DateTime(2026, 10, 26, 0, 0, 0));
    }
}
