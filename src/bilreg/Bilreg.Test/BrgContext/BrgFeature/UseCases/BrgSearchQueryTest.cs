using Bilreg.Application.BrgContext.BrgFeature;
using Bilreg.Application.BrgContext.BrgFeature.UseCases;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.BrgContext.BrgFeature.UseCases;

public class BrgSearchQueryTest
{
    private readonly Mock<IBrgRepo> _brgRepoMock = new();

    private BrgSearchHandler CreateSut() => new(_brgRepoMock.Object);

    private static List<BrgView> CreateSampleData()
    {
        return new List<BrgView>
        {
            new("BRG01", "Paracetamol 500mg", "Analgesik", "OBT", true),
            new("BRG02", "Amoxicillin 500mg", "Antibiotik nonaktif", "OBT", false),
            new("BRG03", "Kassa Steril 10x10", "Penutup luka", "BHP", true),
            new("BRG04", "Spuit 3cc", "Alat suntik nonaktif", "BHP", false),
            new("BRG05", "Kertas HVS A4", "Alat tulis", "ATK", true),
            new("BRG06", "Beras Pulen", "Bahan gizi dapur", "GIZ", true),
        };
    }

    [Fact]
    public async Task Handle_WhenKeywordMatches_ReturnsMappedResponseList()
    {
        // Arrange
        _brgRepoMock.Setup(x => x.ListData("paracetamol"))
            .Returns(new List<BrgView>
            {
                new("BRG01", "Paracetamol 500mg", "Analgesik", "OBT", true)
            });

        var query = new BrgSearchQuery("paracetamol");
        var sut = CreateSut();

        // Act
        var result = (await sut.Handle(query, CancellationToken.None)).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].BrgId.Should().Be("BRG01");
        result[0].BrgName.Should().Be("Paracetamol 500mg");
        result[0].KetBarang.Should().Be("Analgesik");
        result[0].GroupRekDkId.Should().Be("OBT");
        result[0].IsAktif.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenOnlyActiveIsTrueByDefault_FiltersOutInactiveItems()
    {
        // Arrange
        _brgRepoMock.Setup(x => x.ListData("med"))
            .Returns(CreateSampleData());

        var query = new BrgSearchQuery("med"); // OnlyActive defaults to true
        var sut = CreateSut();

        // Act
        var result = (await sut.Handle(query, CancellationToken.None)).ToList();

        // Assert
        result.Should().OnlyContain(x => x.IsAktif);
        result.Select(x => x.BrgId).Should().Contain(new[] { "BRG01", "BRG03", "BRG05", "BRG06" });
        result.Select(x => x.BrgId).Should().NotContain("BRG02");
        result.Select(x => x.BrgId).Should().NotContain("BRG04");
    }

    [Fact]
    public async Task Handle_WhenOnlyActiveIsFalse_ReturnsBothActiveAndInactiveItems()
    {
        // Arrange
        _brgRepoMock.Setup(x => x.ListData("all"))
            .Returns(CreateSampleData());

        var query = new BrgSearchQuery("all", OnlyActive: false);
        var sut = CreateSut();

        // Act
        var result = (await sut.Handle(query, CancellationToken.None)).ToList();

        // Assert
        result.Should().HaveCount(6);
        result.Select(x => x.BrgId).Should().Contain("BRG02");
        result.Select(x => x.BrgId).Should().Contain("BRG04");
    }

    [Fact]
    public async Task Handle_WhenGroupRekDkSpecified_FiltersByGroupCaseInsensitively()
    {
        // Arrange
        _brgRepoMock.Setup(x => x.ListData("test"))
            .Returns(CreateSampleData());

        var query = new BrgSearchQuery("test", GroupRekDk: "obt", OnlyActive: false);
        var sut = CreateSut();

        // Act
        var result = (await sut.Handle(query, CancellationToken.None)).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(x => x.GroupRekDkId.Should().Be("OBT"));
        result.Select(x => x.BrgId).Should().BeEquivalentTo(new[] { "BRG01", "BRG02" });
    }

    [Theory]
    [InlineData("BHP", "BRG03")]
    [InlineData("ATK", "BRG05")]
    [InlineData("GIZ", "BRG06")]
    public async Task Handle_WhenFilteringDifferentStandardCategories_ReturnsCorrespondingItems(
        string groupRekDk, string expectedBrgId)
    {
        // Arrange
        _brgRepoMock.Setup(x => x.ListData("item"))
            .Returns(CreateSampleData());

        var query = new BrgSearchQuery("item", GroupRekDk: groupRekDk, OnlyActive: true);
        var sut = CreateSut();

        // Act
        var result = (await sut.Handle(query, CancellationToken.None)).ToList();

        // Assert
        result.Should().ContainSingle();
        result[0].BrgId.Should().Be(expectedBrgId);
        result[0].GroupRekDkId.Should().Be(groupRekDk);
    }

    [Fact]
    public async Task Handle_WhenBothOnlyActiveAndGroupRekDkApplied_EnforcesBothConditions()
    {
        // Arrange
        _brgRepoMock.Setup(x => x.ListData("med"))
            .Returns(CreateSampleData());

        var query = new BrgSearchQuery("med", GroupRekDk: "OBT", OnlyActive: true);
        var sut = CreateSut();

        // Act
        var result = (await sut.Handle(query, CancellationToken.None)).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].BrgId.Should().Be("BRG01");
        result[0].IsAktif.Should().BeTrue();
        result[0].GroupRekDkId.Should().Be("OBT");
    }

    [Fact]
    public async Task Handle_WhenGroupRekDkIsNull_ReturnsItemsAcrossAllStandardCategories()
    {
        // Arrange
        _brgRepoMock.Setup(x => x.ListData("med"))
            .Returns(CreateSampleData());

        var query = new BrgSearchQuery("med", GroupRekDk: null, OnlyActive: true);
        var sut = CreateSut();

        // Act
        var result = (await sut.Handle(query, CancellationToken.None)).ToList();

        // Assert
        result.Select(x => x.GroupRekDkId).Distinct()
            .Should().BeEquivalentTo(new[] { "OBT", "BHP", "ATK", "GIZ" });
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_WhenKeywordIsNullOrWhitespace_ReturnsEmptyWithoutCallingRepo(string? keyword)
    {
        // Arrange
        var query = new BrgSearchQuery(keyword!);
        var sut = CreateSut();

        // Act
        var result = await sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
        _brgRepoMock.Verify(x => x.ListData(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenRepoReturnsEmpty_ReturnsEmptyList()
    {
        // Arrange
        _brgRepoMock.Setup(x => x.ListData("empty"))
            .Returns(new List<BrgView>());

        var query = new BrgSearchQuery("empty");
        var sut = CreateSut();

        // Act
        var result = await sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }
}
