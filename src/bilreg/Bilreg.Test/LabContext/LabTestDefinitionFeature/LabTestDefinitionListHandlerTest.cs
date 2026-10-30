using Bilreg.Application.LabContext.LabTestDefinitionFeature;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.LabContext.LabTestDefinitionFeature;

public class LabTestDefinitionListHandlerTest
{
    private readonly Mock<ILabTestDefinitionRepo> _definitionRepoMock = new();

    private static LabTestDefinitionListResponse CreateResponse(string id, string tarifId, string tarifName = "Tes", decimal price = 50000m) =>
        new(
            id,
            tarifId,
            tarifId,
            tarifName,
            "TEST-CODE",
            tarifName,
            "Darah",
            1,
            true,
            2,
            price);

    [Fact]
    public async Task Handle_CallsRepoListProjectedWithExpectedFilter()
    {
        // Arrange
        var expectedFilter = new LabTestDefinitionListFilter(true, "hemo", "TR-001");
        var expectedResponses = new List<LabTestDefinitionListResponse>
        {
            CreateResponse("LTD0001", "TR-001", "Hemoglobin", 75000m)
        };

        _definitionRepoMock
            .Setup(r => r.ListProjected(It.Is<LabTestDefinitionListFilter>(f =>
                f.ActiveOnly == expectedFilter.ActiveOnly &&
                f.Search == expectedFilter.Search &&
                f.TarifId == expectedFilter.TarifId)))
            .Returns(expectedResponses);

        var sut = new LabTestDefinitionListHandler(_definitionRepoMock.Object);

        // Act
        var result = (await sut.Handle(
            new LabTestDefinitionListQuery(true, "hemo", "TR-001"),
            CancellationToken.None)).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].TestDefinitionId.Should().Be("LTD0001");
        result[0].TarifPrice.Should().Be(75000m);
        result[0].ComponentCount.Should().Be(2);

        _definitionRepoMock.Verify(
            r => r.ListProjected(It.Is<LabTestDefinitionListFilter>(f =>
                f.ActiveOnly && f.Search == "hemo" && f.TarifId == "TR-001")),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenRepoReturnsEmpty_ReturnsEmptyList()
    {
        // Arrange
        _definitionRepoMock
            .Setup(r => r.ListProjected(It.IsAny<LabTestDefinitionListFilter>()))
            .Returns([]);

        var sut = new LabTestDefinitionListHandler(_definitionRepoMock.Object);

        // Act
        var result = (await sut.Handle(new LabTestDefinitionListQuery(false), CancellationToken.None)).ToList();

        // Assert
        result.Should().BeEmpty();
    }
}
