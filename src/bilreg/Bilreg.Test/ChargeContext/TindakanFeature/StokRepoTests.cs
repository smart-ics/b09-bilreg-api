using Bilreg.Application.ChargeContext.TarifFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Infrastructure.ChargeContext.TindakanFeature;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.ChargeContext.TindakanFeature;

public class StokRepoTests
{
    private readonly Mock<IStokDal> _stokDalMock;
    private readonly StokRepo _sut;

    public StokRepoTests()
    {
        _stokDalMock = new Mock<IStokDal>();
        _sut = new StokRepo(_stokDalMock.Object);
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    public void ListData_WhenKeywordLengthLessThan2_ReturnsEmptyListAndDoesNotCallDal(string keyword)
    {
        // Arrange
        var layanan = new LayananReff("LAY01", "Poli Umum");

        // Act
        var result = _sut.ListData(layanan, keyword);

        // Assert
        result.Should().BeEmpty();
        _stokDalMock.Verify(x => x.ListData(It.IsAny<ILayananKey>(), It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("para")]
    public void ListData_WhenKeywordLengthGreaterOrEqualTo2_CallsDalAndReturnsItems(string keyword)
    {
        // Arrange
        var layanan = new LayananReff("LAY01", "Poli Umum");
        var expectedItems = new List<StokView>
        {
            new("BRG01", "Paracetamol 500mg", 50, "TAB", "Tablet")
        };
        _stokDalMock
            .Setup(x => x.ListData(layanan, keyword))
            .Returns(expectedItems);

        // Act
        var result = _sut.ListData(layanan, keyword);

        // Assert
        result.Should().BeEquivalentTo(expectedItems);
        _stokDalMock.Verify(x => x.ListData(layanan, keyword), Times.Once);
    }
}
