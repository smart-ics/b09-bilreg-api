using Farinv.Domain.BrgContext.KlasifikasiFeature;
using Farinv.Infrastructure.BrgContext.KlasifikasiFeature;
using FluentAssertions;
using Moq;

namespace Farinv.Test.BrgContext.KlasifikasiFeature;

public class PabrikRepoTests
{
    private readonly Mock<IPabrikDal> _pabrikDalMock;
    private readonly PabrikRepo _repository;

    public PabrikRepoTests()
    {
        _pabrikDalMock = new Mock<IPabrikDal>();
        _repository = new PabrikRepo(_pabrikDalMock.Object);
    }

    [Fact]
    public void UT1_GivenExistingEntity_WhenSaveChanges_ThenUpdateIsCalled()
    {
        // Arrange
        var existingModel = CreateTestModel();
        _pabrikDalMock
            .Setup(x => x.GetData(It.IsAny<IPabrikKey>()))
            .Returns(CreateTestDto());

        // Act
        _repository.SaveChanges(existingModel);

        // Assert
        _pabrikDalMock.Verify(x => x.Update(It.IsAny<PabrikDto>()), Times.Once);
        _pabrikDalMock.Verify(x => x.Insert(It.IsAny<PabrikDto>()), Times.Never);
    }

    [Fact]
    public void UT2_GivenNewEntity_WhenSaveChanges_ThenInsertIsCalled()
    {
        // Arrange
        var newModel = CreateTestModel();
        var key = CreateTestKey();
        _pabrikDalMock
            .Setup(x => x.GetData(key))
            .Returns((PabrikDto)null!);

        // Act
        _repository.SaveChanges(newModel);

        // Assert
        _pabrikDalMock.Verify(x => x.Insert(It.IsAny<PabrikDto>()), Times.Once);
        _pabrikDalMock.Verify(x => x.Update(It.IsAny<PabrikDto>()), Times.Never);
    }

    [Fact]
    public void UT3_GivenExistingEntity_WhenLoadEntity_ThenEntityIsReturned()
    {
        // Arrange
        var expectedDto = CreateTestDto();
        var key = CreateTestKey();
        _pabrikDalMock
            .Setup(x => x.GetData(key))
            .Returns(expectedDto);

        // Act
        var result = _repository.LoadEntity(key);

        // Assert
        result.HasValue.Should().BeTrue();
        result.Match(
            onSome: model => model.Should().NotBeNull(),
            onNone: () => Assert.Fail("Expected Some but got None"));
    }

    [Fact]
    public void UT4_GivenNonExistingEntity_WhenLoadEntity_ThenNoneIsReturned()
    {
        // Arrange
        var key = CreateTestKey();
        _pabrikDalMock
            .Setup(x => x.GetData(key))
            .Returns((PabrikDto)null!);

        // Act
        var result = _repository.LoadEntity(key);

        // Assert
        result.HasValue.Should().BeFalse();
        result.Match(
            onSome: model => Assert.Fail("Expected None but got Some"),
            onNone: () => { });
    }

    [Fact]
    public void UT5_GivenKey_WhenDeleteEntity_ThenDeleteIsCalled()
    {
        // Arrange
        var key = CreateTestKey();

        // Act
        _repository.DeleteEntity(key);

        // Assert
        _pabrikDalMock.Verify(x => x.Delete(key), Times.Once);
    }

    [Fact]
    public void UT6_GivenEmptyList_WhenListData_ThenEmptyListIsReturned()
    {
        // Arrange
        _pabrikDalMock
            .Setup(x => x.ListData())
            .Returns((IEnumerable<PabrikDto>)null!);

        // Act
        var result = _repository.ListData();

        // Assert
        var pabrikTypes = result.ToList();
        pabrikTypes.Should().NotBeNull();
        pabrikTypes.Should().BeEmpty();
    }

    [Fact]
    public void UT7_GivenListWithItems_WhenListData_ThenListWithModelsIsReturned()
    {
        // Arrange
        var dtos = new List<PabrikDto> { CreateTestDto(), CreateTestDto() };
        _pabrikDalMock
            .Setup(x => x.ListData())
            .Returns(dtos);

        // Act
        var result = _repository.ListData();

        // Assert
        var pabrikTypes = result.ToList();
        pabrikTypes.Should().NotBeNull();
        pabrikTypes.Count.Should().Be(2);
    }

    private static PabrikType CreateTestModel()
        => PabrikType.Default;

    private static PabrikDto CreateTestDto()
        => PabrikDto.FromModel(PabrikType.Default);

    private static IPabrikKey CreateTestKey()
        => PabrikType.Key("PB001");
}