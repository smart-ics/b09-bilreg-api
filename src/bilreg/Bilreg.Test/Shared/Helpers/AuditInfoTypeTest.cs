using System.Globalization;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.Shared.Helpers;

public class AuditInfoTypeTest
{
    [Fact]
    public void Default_ShouldHaveEmptyUserIdAndSentinelDate()
    {
        // Act
        var sut = AuditInfoType.Default;

        // Assert
        sut.UserId.Should().BeEmpty();
        sut.Timestamp.Should().Be(new DateTime(3000, 1, 1));
        sut.IsEmpty.Should().BeTrue();
    }

    [Theory]
    [InlineData("   ", "")]
    [InlineData("  usr123  ", "usr123")]
    [InlineData("", "")]
    [InlineData(null, "")]
    [InlineData("\t usr123 \n", "usr123")]
    public void TwoArgConstructor_ShouldNormalizeUserId(string? inputUserId, string expectedUserId)
    {
        // Arrange
        var timestamp = new DateTime(2026, 10, 19, 8, 30, 0);

        // Act
        var sut = new AuditInfoType(inputUserId!, timestamp);

        // Assert
        sut.UserId.Should().Be(expectedUserId);
        sut.Timestamp.Should().Be(timestamp);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("\t  \r\n")]
    public void ThreeArgConstructor_WhenUserIdIsWhitespaceOrNull_ShouldProduceDefaultEquivalentWithoutException(string? userId)
    {
        // Act - should be resilient even with invalid or missing date/time strings
        var sut1 = new AuditInfoType(userId!, "3000-01-01", "00:00:00");
        var sut2 = new AuditInfoType(userId!, "invalid-date", "invalid-time");
        var sut3 = new AuditInfoType(userId!, null!, null!);

        // Assert
        sut1.Should().Be(AuditInfoType.Default);
        sut1.IsEmpty.Should().BeTrue();
        sut1.UserId.Should().BeEmpty();
        sut1.Timestamp.Should().Be(new DateTime(3000, 1, 1));

        sut2.Should().Be(AuditInfoType.Default);
        sut2.IsEmpty.Should().BeTrue();

        sut3.Should().Be(AuditInfoType.Default);
        sut3.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void ThreeArgConstructor_WhenUserIdIsValid_ShouldParseDateTimeSuccessfully()
    {
        // Arrange
        var userId = "  dokter_ok  ";
        var tgl = "2026-10-19";
        var jam = "08:30:00";

        // Act
        var sut = new AuditInfoType(userId, tgl, jam);

        // Assert
        sut.UserId.Should().Be("dokter_ok");
        sut.Timestamp.Should().Be(new DateTime(2026, 10, 19, 8, 30, 0));
        sut.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public void ThreeArgConstructor_WhenUserIdIsValidAndDateTimeHasWhitespace_ShouldTrimAndParse()
    {
        // Arrange
        var userId = "dokter_ok";
        var tgl = " 2026-10-19 ";
        var jam = " 14:15:00 ";

        // Act
        var sut = new AuditInfoType(userId, tgl, jam);

        // Assert
        sut.UserId.Should().Be("dokter_ok");
        sut.Timestamp.Should().Be(new DateTime(2026, 10, 19, 14, 15, 0));
        sut.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public void ThreeArgConstructor_WhenUserIdIsValidAndDateInvalid_ShouldThrowFormatException()
    {
        // Act
        var act = () => new AuditInfoType("valid_user", "invalid_date", "invalid_time");

        // Assert
        act.Should().Throw<FormatException>();
    }

    [Theory]
    [InlineData("", 3000, 1, 1, true)]
    [InlineData("   ", 3000, 1, 1, true)]
    [InlineData(null, 3000, 1, 1, true)]
    [InlineData("", 2026, 10, 19, true)]       // Empty userId makes it empty regardless of date
    [InlineData("   ", 2026, 10, 19, true)]    // Whitespace userId normalized to empty
    [InlineData("usr1", 3000, 1, 1, true)]     // Sentinel date makes it empty even with userId
    [InlineData("usr1", 3000, 1, 2, true)]     // Sentinel date > 3000
    [InlineData("usr1", 3005, 12, 31, true)]   // Future sentinel date
    [InlineData("usr1", 2026, 10, 19, false)]  // Valid user and active date => NOT empty
    public void IsEmpty_ShouldReturnExpectedResult(string? userId, int year, int month, int day, bool expectedIsEmpty)
    {
        // Arrange
        var dt = new DateTime(year, month, day, 8, 0, 0);

        // Act
        var sut = new AuditInfoType(userId!, dt);

        // Assert
        sut.IsEmpty.Should().Be(expectedIsEmpty);
    }

    [Fact]
    public void StructuralEquality_WhenNormalizedToDefault_ShouldEqualDefault()
    {
        // Arrange
        var fromTwoArgWhitespace = new AuditInfoType("   ", new DateTime(3000, 1, 1));
        var fromTwoArgEmpty = new AuditInfoType("", new DateTime(3000, 1, 1));
        var fromTwoArgNull = new AuditInfoType(null!, new DateTime(3000, 1, 1));
        var fromThreeArgLegacyWhitespace = new AuditInfoType(" ", "3000-01-01", "00:00:00");
        var defaultInstance = AuditInfoType.Default;

        // Assert equality operator
        (fromTwoArgWhitespace == defaultInstance).Should().BeTrue();
        (fromTwoArgEmpty == defaultInstance).Should().BeTrue();
        (fromTwoArgNull == defaultInstance).Should().BeTrue();
        (fromThreeArgLegacyWhitespace == defaultInstance).Should().BeTrue();

        // Assert Equals method
        fromTwoArgWhitespace.Equals(defaultInstance).Should().BeTrue();
        fromThreeArgLegacyWhitespace.Equals(defaultInstance).Should().BeTrue();

        // Assert HashCode
        fromTwoArgWhitespace.GetHashCode().Should().Be(defaultInstance.GetHashCode());
        fromThreeArgLegacyWhitespace.GetHashCode().Should().Be(defaultInstance.GetHashCode());
    }

    [Fact]
    public void StructuralEquality_WhenSameValuesWithDifferentWhitespace_ShouldBeEqual()
    {
        // Arrange
        var a = new AuditInfoType("  petugas1  ", new DateTime(2026, 10, 19, 10, 0, 0));
        var b = new AuditInfoType("petugas1", new DateTime(2026, 10, 19, 10, 0, 0));

        // Assert
        (a == b).Should().BeTrue();
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void WithExpression_ShouldNormalizeUserId()
    {
        // Arrange
        var original = new AuditInfoType("usr1", new DateTime(2026, 10, 19));

        // Act
        var updated = original with { UserId = "   " };

        // Assert
        updated.UserId.Should().BeEmpty();
        updated.IsEmpty.Should().BeTrue();
    }
}
