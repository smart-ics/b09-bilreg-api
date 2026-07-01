using Bilreg.Domain.PasienContext.PasienFeature;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.PasienContext.PasienFeature;

public class PasienFinderTests
{
    private const string PREFIX = "1122334";

    [Fact]
    public void Given_RegisterIdKeyword_When_CreateNew_Then_ShouldParseAsRegisterId()
    {
        // Arrange
        const string keyword = "RG123";

        // Act
        var result = PasienFinder.CreateNew(keyword, PREFIX);

        // Assert
        result.RegId.Should().Be("RG00000123");
        result.PasienId.Should().BeEmpty();
        result.BookingId.Should().BeEmpty();
    }

    [Fact]
    public void Given_BookingIdKeyword_When_CreateNew_Then_ShouldParseAsBookingId()
    {
        // Arrange
        const string keyword = "BK123";

        // Act
        var result = PasienFinder.CreateNew(keyword, PREFIX);

        // Assert
        result.BookingId.Should().Be("BK00000123");
        result.PasienId.Should().BeEmpty();
        result.RegId.Should().BeEmpty();
    }

    [Fact]
    public void Given_PasienNumeric_When_CreateNew_Then_ShouldParseAsPasienId()
    {
        // Arrange
        const string keyword = "821";

        // Act
        var result = PasienFinder.CreateNew(keyword, PREFIX);

        // Assert
        result.PasienId.Should().Be("112233400000821");
        result.RegId.Should().Be("RG00000821");
        result.BookingId.Should().Be("BO00000821");
    }

    [Fact]
    public void Given_PasienDashedPattern_When_CreateNew_Then_ShouldParseAsPasienId()
    {
        // Arrange
        const string keyword = "81-02-11";

        // Act
        var result = PasienFinder.CreateNew(keyword, PREFIX);

        // Assert
        result.PasienId.Should().Be("112233400810211");
    }

    [Theory]
    [InlineData("2020-05-21", "2020-05-21")]
    [InlineData("21-05-2020", "2020-05-21")]
    public void Given_DateKeyword_When_CreateNew_Then_ShouldParseAsBirthDate(string keyword, string expectedDate)
    {
        // Arrange / Act
        var result = PasienFinder.CreateNew(keyword, PREFIX);

        // Assert
        result.TglLahir.Should().Be(expectedDate);
    }

    [Theory]
    [InlineData("AGUS", new string[] { "AGUS", "AGOES"})]
    [InlineData("TJANDRA", new string[] { "TJANDRA", "CANDRA"})]
    [InlineData("NOORDIN", new string[] { "NOORDIN", "NURDIN", "NORDIN"})]
    [InlineData("YUDHIS", new string[] { "YUDHIS", "YOEDHIS", "YUDIS"})]
    [InlineData("BENNY", new string[] { "BENNY", "BENY"})]
    public void Given_TextKeyword_When_CreateNew_Then_ShouldAssignStringVariants(string keyword, string[] expected)
    {
        // Act
        var result = PasienFinder.CreateNew(keyword, PREFIX);

        // Assert
        result.StringVariants.Should().ContainKey(keyword);
        result.StringVariants[keyword].Should().BeEquivalentTo(expected);
    }

     [Fact]
     public void Given_MultipleTokens_When_CreateNew_Then_ShouldParseEachProperly()
     {
         // Arrange
         const string keyword = "Budi 2020-05-21 RG12";
             
         // Act
         var result = PasienFinder.CreateNew(keyword, PREFIX);
    
         // Assert
         result.StringVariants["BUDI"].Should().BeEquivalentTo(["BUDI", "BOEDI"]);
         result.TglLahir.Should().Be("2020-05-21");
         result.RegId.Should().Be("RG00000012");
     }
    
     [Fact]
     public void Given_MultipleTokensIncludingBookingAndLocation_When_CreateNew_Then_ShouldParseBookingAndAddress()
     {
         // Arrange
         const string keyword = "BK3 Maria Jakarta";
    
         // Act
         var result = PasienFinder.CreateNew(keyword, PREFIX);
    
         // Assert
         result.BookingId.Should().Be("BK00000003");
         result.StringVariants["MARIA"].Should().BeEquivalentTo(["MARIA"]);
         result.StringVariants["JAKARTA"].Should().BeEquivalentTo(["JAKARTA", "DJAKARTA"]);
     }
    
     [Fact]
     public void Given_EmptyKeyword_When_CreateNew_Then_ShouldReturnEmptyFinder()
     {
         // Arrange
         const string keyword = " ";
    
         // Act
         var result = PasienFinder.CreateNew(keyword, PREFIX);
    
         // Assert
         result.PasienId.Should().BeEmpty();
         result.RegId.Should().BeEmpty();
         result.BookingId.Should().BeEmpty();
         result.TglLahir.Should().BeEmpty();
         result.StringVariants.Should().BeEmpty();
     }
    
     [Fact]
     public void Given_EjaanLama_When_CreateNew_Then_NameShouldBeEyd()
     {
         // Arrange
         const string keyword = "Tjandra Soemitro Djayadi";
    
         // Act
         var result = PasienFinder.CreateNew(keyword, PREFIX);
    
         // Assert
         result.PasienId.Should().BeEmpty();
         result.RegId.Should().BeEmpty();
         result.BookingId.Should().BeEmpty();
         result.TglLahir.Should().BeEmpty();
         result.StringVariants["TJANDRA"].Should().BeEquivalentTo(["TJANDRA", "CANDRA"]);
         result.StringVariants["SOEMITRO"].Should().BeEquivalentTo(["SOEMITRO", "SUMITRO"]);
         result.StringVariants["DJAYADI"].Should().BeEquivalentTo(["DJAYADI", "JAYADI"]);
    }
}