using Farinv.Domain.BrgContext.KlasifikasiFeature;
using Farinv.Infrastructure.BrgContext.KlasifikasiFeature;
using FluentAssertions;
using Moq;

namespace Farinv.Test.BrgContext.KlasifikasiFeature;

public class GolonganRepoTests
{
    private readonly Mock<IGolonganDal> _golonganDalMock;
    private readonly GolonganRepo _repository;

    public GolonganRepoTests()
    {
        _golonganDalMock = new Mock<IGolonganDal>();
        _repository = new GolonganRepo(_golonganDalMock.Object);
    }

    [Fact]
    public void UT1_GivenExistingEntity_WhenSaveChanges_ThenUpdateIsCalled()
    {
        // Arrange
        var existingModel = CreateTestModel();
        _golonganDalMock
            .Setup(x => x.GetData(It.IsAny<IGolonganKey>()))
            .Returns(CreateTestDto());

        // Act
        _repository.SaveChanges(existingModel);

        // Assert
        _golonganDalMock.Verify(x => x.Update(It.IsAny<GolonganDto>()), Times.Once);
        _golonganDalMock.Verify(x => x.Insert(It.IsAny<GolonganDto>()), Times.Never);
    }

    [Fact]
    public void UT2_GivenNewEntity_WhenSaveChanges_ThenInsertIsCalled()
    {
        // Arrange
        var newModel = CreateTestModel();
        var key = CreateTestKey();
        _golonganDalMock
            .Setup(x => x.GetData(key))
            .Returns((GolonganDto)null!);

        // Act
        _repository.SaveChanges(newModel);

        // Assert
        _golonganDalMock.Verify(x => x.Insert(It.IsAny<GolonganDto>()), Times.Once);
        _golonganDalMock.Verify(x => x.Update(It.IsAny<GolonganDto>()), Times.Never);
    }

    [Fact]
    public void UT3_GivenExistingEntity_WhenLoadEntity_ThenEntityIsReturned()
    {
        // Arrange
        var expectedDto = CreateTestDto();
        var key = CreateTestKey();
        _golonganDalMock
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
        _golonganDalMock
            .Setup(x => x.GetData(key))
            .Returns((GolonganDto)null!);

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
        _golonganDalMock.Verify(x => x.Delete(key), Times.Once);
    }

    [Fact]
    public void UT6_GivenEmptyList_WhenListData_ThenEmptyListIsReturned()
    {
        // Arrange
        _golonganDalMock
            .Setup(x => x.ListData())
            .Returns((IEnumerable<GolonganDto>)null!);

        // Act
        var result = _repository.ListData();

        // Assert
        var golonganTypes = result.ToList();
        golonganTypes.Should().NotBeNull();
        golonganTypes.Should().BeEmpty();
    }

    [Fact]
    public void UT7_GivenListWithItems_WhenListData_ThenListWithModelsIsReturned()
    {
        // Arrange
        var dtos = new List<GolonganDto> { CreateTestDto(), CreateTestDto() };
        _golonganDalMock
            .Setup(x => x.ListData())
            .Returns(dtos);

        // Act
        var result = _repository.ListData();

        // Assert
        var golonganTypes = result.ToList();
        golonganTypes.Should().NotBeNull();
        golonganTypes.Count.Should().Be(2);
    }

    private static GolonganType CreateTestModel()
        => GolonganType.Default;

    private static GolonganDto CreateTestDto()
        => GolonganDto.FromModel(GolonganType.Default);

    private static IGolonganKey CreateTestKey()
        => GolonganType.Key("GLN");
}