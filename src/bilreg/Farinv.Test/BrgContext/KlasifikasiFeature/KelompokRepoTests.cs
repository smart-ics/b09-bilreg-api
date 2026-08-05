using Farinv.Domain.BrgContext.KlasifikasiFeature;
using Farinv.Infrastructure.BrgContext.KlasifikasiFeature;
using FluentAssertions;
using Moq;

namespace Farinv.Test.BrgContext.KlasifikasiFeature;

public class KelompokRepoTests
{
    private readonly Mock<IKelompokDal> _kelompokDalMock;
    private readonly KelompokRepo _repository;

    public KelompokRepoTests()
    {
        _kelompokDalMock = new Mock<IKelompokDal>();
        _repository = new KelompokRepo(_kelompokDalMock.Object);
    }

    [Fact]
    public void UT1_GivenExistingEntity_WhenSaveChanges_ThenUpdateIsCalled()
    {
        // Arrange
        var existingModel = CreateTestModel();
        _kelompokDalMock
            .Setup(x => x.GetData(It.IsAny<IKelompokKey>()))
            .Returns(CreateTestDto());

        // Act
        _repository.SaveChanges(existingModel);

        // Assert
        _kelompokDalMock.Verify(x => x.Update(It.IsAny<KelompokDto>()), Times.Once);
        _kelompokDalMock.Verify(x => x.Insert(It.IsAny<KelompokDto>()), Times.Never);
    }

    [Fact]
    public void UT2_GivenNewEntity_WhenSaveChanges_ThenInsertIsCalled()
    {
        // Arrange
        var newModel = CreateTestModel();
        var key = CreateTestKey();
        _kelompokDalMock
            .Setup(x => x.GetData(key))
            .Returns((KelompokDto)null!);

        // Act
        _repository.SaveChanges(newModel);

        // Assert
        _kelompokDalMock.Verify(x => x.Insert(It.IsAny<KelompokDto>()), Times.Once);
        _kelompokDalMock.Verify(x => x.Update(It.IsAny<KelompokDto>()), Times.Never);
    }

    [Fact]
    public void UT3_GivenExistingEntity_WhenLoadEntity_ThenEntityIsReturned()
    {
        // Arrange
        var expectedDto = CreateTestDto();
        var key = CreateTestKey();
        _kelompokDalMock
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
        _kelompokDalMock
            .Setup(x => x.GetData(key))
            .Returns((KelompokDto)null!);

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
        _kelompokDalMock.Verify(x => x.Delete(key), Times.Once);
    }

    [Fact]
    public void UT6_GivenEmptyList_WhenListData_ThenEmptyListIsReturned()
    {
        // Arrange
        _kelompokDalMock
            .Setup(x => x.ListData())
            .Returns((IEnumerable<KelompokDto>)null!);

        // Act
        var result = _repository.ListData();

        // Assert
        var kelompokTypes = result.ToList();
        kelompokTypes.Should().NotBeNull();
        kelompokTypes.Should().BeEmpty();
    }

    [Fact]
    public void UT7_GivenListWithItems_WhenListData_ThenListWithModelsIsReturned()
    {
        // Arrange
        var dtos = new List<KelompokDto> { CreateTestDto(), CreateTestDto() };
        _kelompokDalMock
            .Setup(x => x.ListData())
            .Returns(dtos);

        // Act
        var result = _repository.ListData();

        // Assert
        var kelompokTypes = result.ToList();
        kelompokTypes.Should().NotBeNull();
        kelompokTypes.Count.Should().Be(2);
    }

    private static KelompokType CreateTestModel()
        => KelompokType.Default;

    private static KelompokDto CreateTestDto()
        => KelompokDto.FromModel(KelompokType.Default);

    private static IKelompokKey CreateTestKey()
        => KelompokType.Key("KLP");
}