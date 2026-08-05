using Farinv.Domain.BrgContext.KlasifikasiFeature;
using Farinv.Infrastructure.BrgContext.KlasifikasiFeature;
using FluentAssertions;
using Moq;

namespace Farinv.Test.BrgContext.KlasifikasiFeature;

public class SifatRepoTests
{
    private readonly Mock<ISifatDal> _sifatDalMock;
    private readonly SifatRepo _repository;

    public SifatRepoTests()
    {
        _sifatDalMock = new Mock<ISifatDal>();
        _repository = new SifatRepo(_sifatDalMock.Object);
    }

    [Fact]
    public void UT1_GivenExistingEntity_WhenSaveChanges_ThenUpdateIsCalled()
    {
        // Arrange
        var existingModel = CreateTestModel();
        _sifatDalMock
            .Setup(x => x.GetData(It.IsAny<ISifatKey>()))
            .Returns(CreateTestDto());

        // Act
        _repository.SaveChanges(existingModel);

        // Assert
        _sifatDalMock.Verify(x => x.Update(It.IsAny<SifatDto>()), Times.Once);
        _sifatDalMock.Verify(x => x.Insert(It.IsAny<SifatDto>()), Times.Never);
    }

    [Fact]
    public void UT2_GivenNewEntity_WhenSaveChanges_ThenInsertIsCalled()
    {
        // Arrange
        var newModel = CreateTestModel();
        var key = CreateTestKey();
        _sifatDalMock
            .Setup(x => x.GetData(key))
            .Returns((SifatDto)null!);

        // Act
        _repository.SaveChanges(newModel);

        // Assert
        _sifatDalMock.Verify(x => x.Insert(It.IsAny<SifatDto>()), Times.Once);
        _sifatDalMock.Verify(x => x.Update(It.IsAny<SifatDto>()), Times.Never);
    }

    [Fact]
    public void UT3_GivenExistingEntity_WhenLoadEntity_ThenEntityIsReturned()
    {
        // Arrange
        var expectedDto = CreateTestDto();
        var key = CreateTestKey();
        _sifatDalMock
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
        _sifatDalMock
            .Setup(x => x.GetData(key))
            .Returns((SifatDto)null!);

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
        _sifatDalMock.Verify(x => x.Delete(key), Times.Once);
    }

    [Fact]
    public void UT6_GivenEmptyList_WhenListData_ThenEmptyListIsReturned()
    {
        // Arrange
        _sifatDalMock
            .Setup(x => x.ListData())
            .Returns((IEnumerable<SifatDto>)null!);

        // Act
        var result = _repository.ListData();

        // Assert
        var sifatTypes = result.ToList();
        sifatTypes.Should().NotBeNull();
        sifatTypes.Should().BeEmpty();
    }

    [Fact]
    public void UT7_GivenListWithItems_WhenListData_ThenListWithModelsIsReturned()
    {
        // Arrange
        var dtos = new List<SifatDto> { CreateTestDto(), CreateTestDto() };
        _sifatDalMock
            .Setup(x => x.ListData())
            .Returns(dtos);

        // Act
        var result = _repository.ListData();

        // Assert
        var sifatTypes = result.ToList();
        sifatTypes.Should().NotBeNull();
        sifatTypes.Count.Should().Be(2);
    }

    private static SifatType CreateTestModel()
        => SifatType.Default;

    private static SifatDto CreateTestDto()
        => SifatDto.FromModel(SifatType.Default);

    private static ISifatKey CreateTestKey()
        => SifatType.Key("SFT");
}