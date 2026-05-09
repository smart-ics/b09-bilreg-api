using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Infrastructure.BedUsageContext.WardFeature;
using FluentAssertions;
using Moq;

namespace Bilreg.Test.BedUsageContext.BangsalFeature;

public class KamarRepoTests
{
    private readonly Mock<IKamarDal> _kamarDalMock;
    private readonly KamarRepo _repository;

    public KamarRepoTests()
    {
        _kamarDalMock = new Mock<IKamarDal>();
        _repository = new KamarRepo(_kamarDalMock.Object);
    }

    [Fact]
    public void UT1_GivenExistingEntity_WhenSaveChanges_ThenUpdateIsCalled()
    {
        // Arrange
        var existingModel = CreateTestModel();
        _kamarDalMock
            .Setup(x => x.GetData(It.IsAny<IKamarKey>()))
            .Returns(CreateTestDto());

        // Act
        _repository.SaveChanges(existingModel);

        // Assert
        _kamarDalMock.Verify(x => x.Update(It.IsAny<KamarDto>()), Times.Once);
        _kamarDalMock.Verify(x => x.Insert(It.IsAny<KamarDto>()), Times.Never);
    }

    [Fact]
    public void UT2_GivenNewEntity_WhenSaveChanges_ThenInsertIsCalled()
    {
        // Arrange
        var newModel = CreateTestModel();
        var key = CreateTestKey();
        _kamarDalMock
            .Setup(x => x.GetData(key))
            .Returns((KamarDto)null!);

        // Act
        _repository.SaveChanges(newModel);

        // Assert
        _kamarDalMock.Verify(x => x.Insert(It.IsAny<KamarDto>()), Times.Once);
        _kamarDalMock.Verify(x => x.Update(It.IsAny<KamarDto>()), Times.Never);
    }

    [Fact]
    public void UT3_GivenExistingEntity_WhenLoadEntity_ThenEntityIsReturned()
    {
        // Arrange
        var expectedDto = CreateTestDto();
        var key = CreateTestKey();
        _kamarDalMock
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
        _kamarDalMock
            .Setup(x => x.GetData(key))
            .Returns((KamarDto)null!);

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
        _kamarDalMock.Verify(x => x.Delete(key), Times.Once);
    }

    [Fact]
    public void UT6_GivenEmptyList_WhenListData_ThenEmptyListIsReturned()
    {
        // Arrange
        _kamarDalMock
            .Setup(x => x.ListData())
            .Returns((IEnumerable<KamarDto>)null!);

        // Act
        var result = _repository.ListData();

        // Assert
        var kamarTypes = result.ToList();
        kamarTypes.Should().NotBeNull();
        kamarTypes.Should().BeEmpty();
    }

    [Fact]
    public void UT7_GivenListWithItems_WhenListData_ThenListWithModelsIsReturned()
    {
        // Arrange
        var dtos = new List<KamarDto> { CreateTestDto(), CreateTestDto() };
        _kamarDalMock
            .Setup(x => x.ListData())
            .Returns(dtos);

        // Act
        var result = _repository.ListData();

        // Assert
        var kamarTypes = result.ToList();
        kamarTypes.Should().NotBeNull();
        kamarTypes.Count.Should().Be(2);
    }

    [Fact]
    public void UT8_GivenListWithItems_WhenListDataWithFilter_ThenListWithFilteredModelsIsReturned()
    {
        // Arrange
        var dtos = new List<KamarDto> 
        { 
            new KamarDto("A", "B", "X", "Y", "C", "D"),
            new KamarDto("E", "F", "Z", "W", "G", "H")
        };
        var filter = CreateTestBangsalKey();
        _kamarDalMock
            .Setup(x => x.ListData())
            .Returns(dtos);

        // Act
        var result = _repository.ListData(filter);

        // Assert
        var kamarTypes = result.ToList();
        kamarTypes.Should().NotBeNull();
        kamarTypes.Count.Should().Be(1);
        kamarTypes.Should().Contain(x => x.Bangsal.BangsalId == filter.BangsalId);
    }

    private static KamarType CreateTestModel()
        => KamarType.Default;
    private static KamarDto CreateTestDto()
        => KamarDto.FromModel(KamarType.Default); 
    private static IKamarKey CreateTestKey()
        => KamarType.Key("A"); 
    private static IBangsalKey CreateTestBangsalKey()
        => BangsalType.Key("X");
}