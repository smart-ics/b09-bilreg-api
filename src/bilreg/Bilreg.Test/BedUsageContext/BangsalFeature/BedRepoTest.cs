using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Infrastructure.BedUsageContext.WardFeature;
using FluentAssertions;
using Moq;

namespace Bilreg.Test.BedUsageContext.BangsalFeature;

public class BedRepoTests
{
    private readonly Mock<IBedDal> _bedDalMock;
    private readonly BedRepo _repository;

    public BedRepoTests()
    {
        _bedDalMock = new Mock<IBedDal>();
        _repository = new BedRepo(_bedDalMock.Object);
    }

    [Fact]
    public void UT1_GivenExistingEntity_WhenSaveChanges_ThenUpdateIsCalled()
    {
        // Arrange
        var existingModel = CreateTestModel();
        _bedDalMock
            .Setup(x => x.GetData(It.IsAny<IBedKey>()))
            .Returns(CreateTestDto());

        // Act
        _repository.SaveChanges(existingModel);

        // Assert
        _bedDalMock.Verify(x => x.Update(It.IsAny<BedDto>()), Times.Once);
        _bedDalMock.Verify(x => x.Insert(It.IsAny<BedDto>()), Times.Never);
    }

    [Fact]
    public void UT2_GivenNewEntity_WhenSaveChanges_ThenInsertIsCalled()
    {
        // Arrange
        var newModel = CreateTestModel();
        var key = CreateTestKey();
        _bedDalMock
            .Setup(x => x.GetData(key))
            .Returns((BedDto)null!);

        // Act
        _repository.SaveChanges(newModel);

        // Assert
        _bedDalMock.Verify(x => x.Insert(It.IsAny<BedDto>()), Times.Once);
        _bedDalMock.Verify(x => x.Update(It.IsAny<BedDto>()), Times.Never);
    }

    [Fact]
    public void UT3_GivenExistingEntity_WhenLoadEntity_ThenEntityIsReturned()
    {
        // Arrange
        var expectedDto = CreateTestDto();
        var key = CreateTestKey();
        _bedDalMock
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
        _bedDalMock
            .Setup(x => x.GetData(key))
            .Returns((BedDto)null!);

        // Act
        var result = _repository.LoadEntity(key);

        // Assert
        result.HasValue.Should().BeFalse();
        result.Match(
            onSome: _ => Assert.Fail("Expected None but got Some"),
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
        _bedDalMock.Verify(x => x.Delete(key), Times.Once);
    }

    [Fact]
    public void UT6_GivenEmptyList_WhenListData_ThenEmptyListIsReturned()
    {
        // Arrange
        _bedDalMock
            .Setup(x => x.ListData())
            .Returns((IEnumerable<BedDto>)null!);

        // Act
        var result = _repository.ListData();

        // Assert
        var bedTypes = result.ToList();
        bedTypes.Should().NotBeNull();
        bedTypes.Should().BeEmpty();
    }

    [Fact]
    public void UT7_GivenListWithItems_WhenListData_ThenListWithModelsIsReturned()
    {
        // Arrange
        var dtos = new List<BedDto> { CreateTestDto(), CreateTestDto() };
        _bedDalMock
            .Setup(x => x.ListData())
            .Returns(dtos);

        // Act
        var result = _repository.ListData();

        // Assert
        var bedTypes = result.ToList();
        bedTypes.Should().NotBeNull();
        bedTypes.Count.Should().Be(2);
    }

    [Fact]
    public void UT8_GivenListWithItems_WhenListDataWithFilter_ThenListWithFilteredModelsIsReturned()
    {
        // Arrange
        var dtos = new List<BedDto> 
        { 
            new BedDto("A", "B", "C", true, "D", "X", "Y"),
            new BedDto("E", "F", "G", false, "H", "Z", "W")
        };
        var filter = CreateTestBangsalKey();
        _bedDalMock
            .Setup(x => x.ListData())
            .Returns(dtos);

        // Act
        var result = _repository.ListData(filter);

        // Assert
        var bedTypes = result.ToList();
        bedTypes.Should().NotBeNull();
        bedTypes.Count.Should().Be(1);
        bedTypes.Should().Contain(x => x.Bangsal.BangsalId == filter.BangsalId);
    }

    private static BedType CreateTestModel()
        => BedType.Default;
    private static BedDto CreateTestDto()
        => BedDto.FromModel(BedType.Default); 
    private static IBedKey CreateTestKey()
        => BedType.Key("A"); 
    private static IBangsalKey CreateTestBangsalKey()
        => BangsalType.Key("X");
}