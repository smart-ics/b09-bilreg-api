using Farinv.Domain.BrgContext.KlasifikasiFeature;
using Farinv.Infrastructure.BrgContext.KlasifikasiFeature;
using FluentAssertions;
using Moq;

namespace Farinv.Test.BrgContext.KlasifikasiFeature;

public class BentukRepoTests
{
    private readonly Mock<IBentukDal> _bentukDalMock;
    private readonly BentukRepo _repository;

    public BentukRepoTests()
    {
        _bentukDalMock = new Mock<IBentukDal>();
        _repository = new BentukRepo(_bentukDalMock.Object);
    }

    [Fact]
    public void UT1_GivenExistingEntity_WhenSaveChanges_ThenUpdateIsCalled()
    {
        // Arrange
        var existingModel = CreateTestModel();
        _bentukDalMock
            .Setup(x => x.GetData(It.IsAny<IBentukKey>()))
            .Returns(CreateTestDto());

        // Act
        _repository.SaveChanges(existingModel);

        // Assert
        _bentukDalMock.Verify(x => x.Update(It.IsAny<BentukDto>()), Times.Once);
        _bentukDalMock.Verify(x => x.Insert(It.IsAny<BentukDto>()), Times.Never);
    }

    [Fact]
    public void UT2_GivenNewEntity_WhenSaveChanges_ThenInsertIsCalled()
    {
        // Arrange
        var newModel = CreateTestModel();
        var key = CreateTestKey();
        _bentukDalMock
            .Setup(x => x.GetData(key))
            .Returns((BentukDto)null!);

        // Act
        _repository.SaveChanges(newModel);

        // Assert
        _bentukDalMock.Verify(x => x.Insert(It.IsAny<BentukDto>()), Times.Once);
        _bentukDalMock.Verify(x => x.Update(It.IsAny<BentukDto>()), Times.Never);
    }

    [Fact]
    public void UT3_GivenExistingEntity_WhenLoadEntity_ThenEntityIsReturned()
    {
        // Arrange
        var expectedDto = CreateTestDto();
        var key = CreateTestKey();
        _bentukDalMock
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
        _bentukDalMock
            .Setup(x => x.GetData(key))
            .Returns((BentukDto)null!);

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
        _bentukDalMock.Verify(x => x.Delete(key), Times.Once);
    }

    [Fact]
    public void UT6_GivenEmptyList_WhenListData_ThenEmptyListIsReturned()
    {
        // Arrange
        _bentukDalMock
            .Setup(x => x.ListData())
            .Returns((IEnumerable<BentukDto>)null!);

        // Act
        var result = _repository.ListData();

        // Assert
        var bentukTypes = result.ToList();
        bentukTypes.Should().NotBeNull();
        bentukTypes.Should().BeEmpty();
    }

    [Fact]
    public void UT7_GivenListWithItems_WhenListData_ThenListWithModelsIsReturned()
    {
        // Arrange
        var dtos = new List<BentukDto> { CreateTestDto(), CreateTestDto() };
        _bentukDalMock
            .Setup(x => x.ListData())
            .Returns(dtos);

        // Act
        var result = _repository.ListData();

        // Assert
        var bentukTypes = result.ToList();
        bentukTypes.Should().NotBeNull();
        bentukTypes.Count.Should().Be(2);
    }

    private static BentukType CreateTestModel()
        => BentukType.Default;

    private static BentukDto CreateTestDto()
        => BentukDto.FromModel(BentukType.Default);

    private static IBentukKey CreateTestKey()
        => BentukType.Key("BTN");
}
