using Bilreg.Application.PasienContext.PasienFeature;
using FluentAssertions;
using Xunit;

namespace Bilreg.Infrastructure.PasienContext.PasienFeature;

public class PatientSearchParserTests
{
    [Theory]
    [InlineData("15-08-1990 Budi Santoso", "Budi Santoso", 1990, 8, 15)]
    [InlineData("Budi Santoso 15-08-1990", "Budi Santoso", 1990, 8, 15)]
    [InlineData("Budi 1990-08-15", "Budi", 1990, 8, 15)]
    public void UT1_GivenValidFullDate_WhenParsing_ThenReturnDateAndName(string keyword, string expectedName, 
        int year, int month, int day)
    {
        // Arrange done via InlineData
        var expectedDate = new DateTime(year, month, day);
        var searchKeyword = new SearchKeyword(keyword);
        
        // Act
        var result = searchKeyword.Parse();

        // Assert
        result.Name.Should().Be(expectedName);
        result.TglLahir.Should().Be(expectedDate);
    }

    [Fact]
    public void UT2_GivenEmptyKeyword_WhenParsing_ThenReturnEmptyResult()
    {
        // Arrange
        const string keyword = "";
        var searchKeyword = new SearchKeyword(keyword);

        // Act
        var result = searchKeyword.Parse();

        // Assert
        result.Name.Should().BeEmpty();
        result.TglLahir.Should().Be(new DateTime(3000, 1, 1));
    }

    [Fact]
    public void UT3_GivenNameOnly_WhenParsing_ThenReturnOnlyNameWithoutDate()
    {
        // Arrange
        const string keyword = "Agus Santoso";
        var searchKeyword = new SearchKeyword(keyword);

        // Act
        var result = searchKeyword.Parse();

        // Assert
        result.Name.Should().Be("Agus Santoso");
        result.TglLahir.Should().Be(new DateTime(3000, 1, 1));
    }
}