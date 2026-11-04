using Bilreg.Application.BedUsageContext.WardCockpitFeature;
using Bilreg.Application.BedUsageContext.WardCockpitFeature.UseCases;
using FluentAssertions;
using Moq;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.BedUsageContext.WardCockpitFeature;

public class WardCockpitOverviewQueryHandlerTest
{
    private readonly Mock<IWardCockpitDal> _dalMock;
    private readonly Mock<ITglJamProvider> _clockMock;
    private readonly WardCockpitOverviewQueryHandler _sut;
    private readonly DateTime _testNow = new(2026, 10, 24, 10, 0, 0);

    public WardCockpitOverviewQueryHandlerTest()
    {
        _dalMock = new Mock<IWardCockpitDal>();
        _clockMock = new Mock<ITglJamProvider>();
        _clockMock.Setup(c => c.Now).Returns(_testNow);
        _sut = new WardCockpitOverviewQueryHandler(_dalMock.Object, _clockMock.Object);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Handle_GivenNullOrWhiteSpaceBangsalId_ThrowsArgumentException(string? invalidBangsalId)
    {
        // Arrange
        var query = new WardCockpitOverviewQuery(invalidBangsalId!);

        // Act
        var act = () => _sut.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
        _dalMock.Verify(x => x.GetOverviewAsync(It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_GivenValidBangsalId_CallsDalAndReturnsDto()
    {
        // Arrange
        var bangsalId = "BGS-01";
        var expectedDto = new WardCockpitOverviewDto(
            Bangsal: new WardCockpitBangsalDto("BGS-01", "Bangsal Dahlia", "KLS-03", "Kelas III"),
            Kpi: new WardCockpitKpiDto(24, 18, 75.0, 3, 2, 1, 4),
            Rooms: [],
            WaitingList: []
        );

        _dalMock
            .Setup(x => x.GetOverviewAsync(bangsalId, _testNow, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        var query = new WardCockpitOverviewQuery("  BGS-01  ");

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedDto);
        _dalMock.Verify(x => x.GetOverviewAsync("BGS-01", _testNow, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_GivenNonExistentBangsalId_ReturnsNull()
    {
        // Arrange
        _dalMock
            .Setup(x => x.GetOverviewAsync("BGS-999", _testNow, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WardCockpitOverviewDto?)null);

        var query = new WardCockpitOverviewQuery("BGS-999");

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }
}
