using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Infrastructure.BedUsageContext.WardFeature;
using FluentAssertions;
using Moq;

namespace Bilreg.Test.BedUsageContext.BangsalFeature;

public class BangsalRepoTests
{
    private readonly Mock<IBangsalDal> _bangsalDalMock;
    private readonly BangsalRepo _repository;

    public BangsalRepoTests()
    {
        _bangsalDalMock = new Mock<IBangsalDal>();
        _repository = new BangsalRepo(_bangsalDalMock.Object);
    }

    [Fact]
    public void UT1_GivenExistingEntity_WhenSaveChanges_ThenUpdateIsCalled()
    {
        // Arrange
        var existingModel = CreateTestModel();
        _bangsalDalMock
            .Setup(x => x.GetData(It.IsAny<IBangsalKey>()))
            .Returns(CreateTestDto());

        // Act
        _repository.SaveChanges(existingModel);

        // Assert
        _bangsalDalMock.Verify(x => x.Update(It.IsAny<BangsalDto>()), Times.Once);
        _bangsalDalMock.Verify(x => x.Insert(It.IsAny<BangsalDto>()), Times.Never);
    }

    [Fact]
    public void UT2_GivenNewEntity_WhenSaveChanges_ThenInsertIsCalled()
    {
        // Arrange
        var newModel = CreateTestModel();
        var key = CreateTestKey();
        _bangsalDalMock
            .Setup(x => x.GetData(key))
            .Returns((BangsalDto)null!);

        // Act
        _repository.SaveChanges(newModel);

        // Assert
        _bangsalDalMock.Verify(x => x.Insert(It.IsAny<BangsalDto>()), Times.Once);
        _bangsalDalMock.Verify(x => x.Update(It.IsAny<BangsalDto>()), Times.Never);
    }

    [Fact]
    public void UT3_GivenExistingEntity_WhenLoadEntity_ThenEntityIsReturned()
    {
        // Arrange
        var expectedDto = CreateTestDto();
        var key = CreateTestKey();
        _bangsalDalMock
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
        _bangsalDalMock
            .Setup(x => x.GetData(key))
            .Returns((BangsalDto)null!);

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
        _bangsalDalMock.Verify(x => x.Delete(key), Times.Once);
    }

    [Fact]
    public void UT6_GivenEmptyList_WhenListData_ThenEmptyListIsReturned()
    {
        // Arrange
        _bangsalDalMock
            .Setup(x => x.ListData())
            .Returns((IEnumerable<BangsalDto>)null!);

        // Act
        var result = _repository.ListData();

        // Assert
        var bangsalTypes = result.ToList();
        bangsalTypes.Should().NotBeNull();
        bangsalTypes.Should().BeEmpty();
    }

    [Fact]
    public void UT7_GivenListWithItems_WhenListData_ThenListWithModelsIsReturned()
    {
        // Arrange
        var dtos = new List<BangsalDto> { CreateTestDto(), CreateTestDto() };
        _bangsalDalMock
            .Setup(x => x.ListData())
            .Returns(dtos);

        // Act
        var result = _repository.ListData();

        // Assert
        var bangsalTypes = result.ToList();
        bangsalTypes.Should().NotBeNull();
        bangsalTypes.Count.Should().Be(2);
    }

    [Fact]
    public void UT8_GivenLayananFilter_WhenListData_ThenOnlyMatchingBangsalIsReturned()
    {
        var target = new LayananReff("LYN-1", "Layanan 1");
        var other = new LayananReff("LYN-2", "Layanan 2");
        var dtos = new List<BangsalDto>
        {
            BangsalDto.FromModel(new BangsalType("B1", "Bangsal 1", RoomCatType.Default, target)),
            BangsalDto.FromModel(new BangsalType("B2", "Bangsal 2", RoomCatType.Default, other))
        };
        _bangsalDalMock
            .Setup(x => x.ListData())
            .Returns(dtos);

        var result = _repository.ListData(target).ToList();

        result.Should().ContainSingle();
        result[0].BangsalId.Should().Be("B1");
    }

    private static BangsalType CreateTestModel()
        => BangsalType.Default;
    private static BangsalDto CreateTestDto()
        => BangsalDto.FromModel(BangsalType.Default); 
    private static IBangsalKey CreateTestKey()
        => BangsalType.Key("A"); 
}
