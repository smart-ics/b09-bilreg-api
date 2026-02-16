using Bilreg.Domain.PaymentContext.RegOutFeature;
using FluentAssertions;

namespace Bilreg.Test.PaymentContext.RegOutFeature;

public class RegFinderTest
{
    [Fact]
    public void GivenValidParameters_WhenCreatingRegFinder_ThenPropertiesShouldBeInitialized()
    {
        // Arrange
        var regId = "REG001";
        var pasienId = "PAS001";
        var pasienName = "John Doe";
        var bookingId = "BK001";

        // Act
        var finder = RegFinder.Create(regId, pasienId, pasienName, bookingId);

        // Assert
        finder.RegId.Should().Be(regId);
        finder.PasienId.Should().Be(pasienId);
        finder.PasienName.Should().Be(pasienName);
        finder.BookingId.Should().Be(bookingId);
        finder.StringVariants.Should().ContainKey("PasienName");
        finder.StringVariants.Should().ContainKey("PasienId");
    }

    [Fact]
    public void GivenPatientNameWithSpaces_WhenCreatingRegFinder_ThenVariantsShouldIncludeCombinations()
    {
        // Arrange
        var pasienName = "John Michael Doe";

        // Act
        var finder = RegFinder.Create("REG001", "PAS001", pasienName, "BK001");

        // Assert
        var nameVariants = finder.StringVariants["PasienName"];
        nameVariants.Should().Contain("John Michael Doe");
        nameVariants.Should().Contain("John");
        nameVariants.Should().Contain("Doe");
        nameVariants.Should().Contain("John Doe");
        nameVariants.Should().Contain("Michael");
        nameVariants.Should().Contain("John Michael");
        nameVariants.Should().Contain("Michael Doe");
    }

    [Fact]
    public void GivenPatientIdWithLeadingZeros_WhenCreatingRegFinder_ThenVariantsShouldIncludeWithoutZeros()
    {
        // Arrange
        var pasienId = "00012345";

        // Act
        var finder = RegFinder.Create("REG001", pasienId, "John Doe", "BK001");

        // Assert
        var idVariants = finder.StringVariants["PasienId"];
        idVariants.Should().Contain("00012345");
        idVariants.Should().Contain("12345");
    }

    [Fact]
    public void GivenKeywordMatchingRegId_WhenMatchesKeyword_ThenShouldReturnTrue()
    {
        // Arrange
        var finder = RegFinder.Create("REG001", "PAS001", "John Doe", "BK001");
        var keyword = "REG001";

        // Act
        var result = finder.MatchesKeyword(keyword);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void GivenKeywordMatchingPartialRegId_WhenMatchesKeyword_ThenShouldReturnTrue()
    {
        // Arrange
        var finder = RegFinder.Create("REG001", "PAS001", "John Doe", "BK001");
        var keyword = "REG";

        // Act
        var result = finder.MatchesKeyword(keyword);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void GivenKeywordMatchingPatientNameVariant_WhenMatchesKeyword_ThenShouldReturnTrue()
    {
        // Arrange
        var finder = RegFinder.Create("REG001", "PAS001", "John Michael Doe", "BK001");
        var keyword = "John Doe";

        // Act
        var result = finder.MatchesKeyword(keyword);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void GivenKeywordNotMatchingAnyField_WhenMatchesKeyword_ThenShouldReturnFalse()
    {
        // Arrange
        var finder = RegFinder.Create("REG001", "PAS001", "John Doe", "BK001");
        var keyword = "Unknown";

        // Act
        var result = finder.MatchesKeyword(keyword);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GivenExactMatch_WhenCalculateSimilarityScore_ThenShouldReturnOne()
    {
        // Arrange
        var finder = RegFinder.Create("REG001", "PAS001", "John Doe", "BK001");
        var keyword = "REG001";

        // Act
        var score = finder.CalculateSimilarityScore(keyword);

        // Assert
        score.Should().Be(1.0);
    }

    [Fact]
    public void GivenPartialMatch_WhenCalculateSimilarityScore_ThenShouldReturnLessThanOne()
    {
        // Arrange
        var finder = RegFinder.Create("REG001", "PAS001", "John Doe", "BK001");
        var keyword = "REG";

        // Act
        var score = finder.CalculateSimilarityScore(keyword);

        // Assert
        score.Should().BeGreaterThan(0).And.BeLessThan(1.0);
    }

    [Fact]
    public void GivenEmptyKeyword_WhenCalculateSimilarityScore_ThenShouldReturnZero()
    {
        // Arrange
        var finder = RegFinder.Create("REG001", "PAS001", "John Doe", "BK001");
        var keyword = "";

        // Act
        var score = finder.CalculateSimilarityScore(keyword);

        // Assert
        score.Should().Be(0);
    }

    [Fact]
    public void GivenNameWithAccents_WhenCreatingRegFinder_ThenVariantsShouldIncludeWithoutAccents()
    {
        // Arrange
        var pasienName = "José María García";

        // Act
        var finder = RegFinder.Create("REG001", "PAS001", pasienName, "BK001");

        // Assert
        var nameVariants = finder.StringVariants["PasienName"];
        nameVariants.Should().Contain("Jose Maria Garcia");
        nameVariants.Should().Contain("JoséMaríaGarcía");
        nameVariants.Should().Contain("JoseMariaGarcia");
    }

    [Fact]
    public void GivenNameWithTitle_WhenCreatingRegFinder_ThenVariantsShouldIncludeWithoutTitle()
    {
        // Arrange
        var pasienName = "Dr. John Doe SH";

        // Act
        var finder = RegFinder.Create("REG001", "PAS001", pasienName, "BK001");

        // Assert
        var nameVariants = finder.StringVariants["PasienName"];
        nameVariants.Should().Contain("John Doe");
    }
}
