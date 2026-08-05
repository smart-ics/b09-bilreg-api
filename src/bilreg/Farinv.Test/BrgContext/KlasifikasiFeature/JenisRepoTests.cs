using Farinv.Domain.BrgContext.KlasifikasiFeature;
using Farinv.Infrastructure.BrgContext.KlasifikasiFeature;
using FluentAssertions;
using Moq;

namespace Farinv.Test.BrgContext.KlasifikasiFeature;

public class JenisRepoTests
{
    private readonly Mock<IJenisDal> _jenisDalMock;
    private readonly JenisRepo _repository;

    public JenisRepoTests()
    {
        _jenisDalMock = new Mock<IJenisDal>();
        _repository = new JenisRepo(_jenisDalMock.Object);
    }

    [Fact]
    public void UT1_GivenExistingEntity_WhenSaveChanges_ThenUpdateIsCalled()
    {
        // Arrange
        var existingModel = CreateTestModel();
        _jenisDalMock
            .Setup(x => x.GetData(It.IsAny<IJenisKey>()))
            .Returns(CreateTestDto());

        // Act
        _repository.SaveChanges(existingModel);

        // Assert
        _jenisDalMock.Verify(x => x.Update(It.IsAny<JenisDto>()), Times.Once);
        _jenisDalMock.Verify(x => x.Insert(It.IsAny<JenisDto>()), Times.Never);
    }

    [Fact]
    public void UT2_GivenNewEntity_WhenSaveChanges_ThenInsertIsCalled()
    {
        // Arrange
        var newModel = CreateTestModel();
        var key = CreateTestKey();
        _jenisDalMock
            .Setup(x => x.GetData(key))
            .Returns((JenisDto)null!);

        // Act
        _repository.SaveChanges(newModel);

        // Assert
        _jenisDalMock.Verify(x => x.Insert(It.IsAny<JenisDto>()), Times.Once);
        _jenisDalMock.Verify(x => x.Update(It.IsAny<JenisDto>()), Times.Never);
    }

    [Fact]
    public void UT3_GivenExistingEntity_WhenLoadEntity_ThenEntityIsReturned()
    {
        // Arrange
        var expectedDto = CreateTestDto();
        var key = CreateTestKey();
        _jenisDalMock
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
        _jenisDalMock
            .Setup(x => x.GetData(key))
            .Returns((JenisDto)null!);

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
        _jenisDalMock.Verify(x => x.Delete(key), Times.Once);
    }

    [Fact]
    public void UT6_GivenEmptyList_WhenListData_ThenEmptyListIsReturned()
    {
        // Arrange
        _jenisDalMock
            .Setup(x => x.ListData())
            .Returns((IEnumerable<JenisDto>)null!);

        // Act
        var result = _repository.ListData();

        // Assert
        var jenisTypes = result.ToList();
        jenisTypes.Should().NotBeNull();
        jenisTypes.Should().BeEmpty();
    }

    [Fact]
    public void UT7_GivenListWithItems_WhenListData_ThenListWithModelsIsReturned()
    {
        // Arrange
        var dtos = new List<JenisDto> { CreateTestDto(), CreateTestDto() };
        _jenisDalMock
            .Setup(x => x.ListData())
            .Returns(dtos);

        // Act
        var result = _repository.ListData();

        // Assert
        var jenisTypes = result.ToList();
        jenisTypes.Should().NotBeNull();
        jenisTypes.Count.Should().Be(2);
    }

    private static JenisType CreateTestModel()
        => JenisType.Default;

    private static JenisDto CreateTestDto()
        => JenisDto.FromModel(JenisType.Default);

    private static IJenisKey CreateTestKey()
        => JenisType.Key("J1");
}