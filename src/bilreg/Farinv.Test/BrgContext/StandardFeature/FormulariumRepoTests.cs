using Farinv.Domain.BrgContext.StandardFeature;
using Farinv.Infrastructure.BrgContext.StandardFeature;
using FluentAssertions;
using Moq;

namespace Farinv.Test.BrgContext.StandardFeature;

public class FormulariumRepoTests
{
    private readonly Mock<IFormulariumDal> _formulariumDalMock;
    private readonly FormulariumRepo _repository;

    public FormulariumRepoTests()
    {
        _formulariumDalMock = new Mock<IFormulariumDal>();
        _repository = new FormulariumRepo(_formulariumDalMock.Object);
    }

    [Fact]
    public void UT1_GivenExistingEntity_WhenSaveChanges_ThenUpdateIsCalled()
    {
        // Arrange
        var existingModel = CreateTestModel();
        _formulariumDalMock
            .Setup(x => x.GetData(It.IsAny<IFormulariumKey>()))
            .Returns(CreateTestDto());

        // Act
        _repository.SaveChanges(existingModel);

        // Assert
        _formulariumDalMock.Verify(x => x.Update(It.IsAny<FormulariumDto>()), Times.Once);
        _formulariumDalMock.Verify(x => x.Insert(It.IsAny<FormulariumDto>()), Times.Never);
    }

    [Fact]
    public void UT2_GivenNewEntity_WhenSaveChanges_ThenInsertIsCalled()
    {
        // Arrange
        var newModel = CreateTestModel();
        var key = CreateTestKey();
        _formulariumDalMock
            .Setup(x => x.GetData(key))
            .Returns((FormulariumDto)null!);

        // Act
        _repository.SaveChanges(newModel);

        // Assert
        _formulariumDalMock.Verify(x => x.Insert(It.IsAny<FormulariumDto>()), Times.Once);
        _formulariumDalMock.Verify(x => x.Update(It.IsAny<FormulariumDto>()), Times.Never);
    }

    [Fact]
    public void UT3_GivenExistingEntity_WhenLoadEntity_ThenEntityIsReturned()
    {
        // Arrange
        var expectedDto = CreateTestDto();
        var key = CreateTestKey();
        _formulariumDalMock
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
        _formulariumDalMock
            .Setup(x => x.GetData(key))
            .Returns((FormulariumDto)null!);

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
        _formulariumDalMock.Verify(x => x.Delete(key), Times.Once);
    }

    [Fact]
    public void UT6_GivenEmptyList_WhenListData_ThenEmptyListIsReturned()
    {
        // Arrange
        _formulariumDalMock
            .Setup(x => x.ListData())
            .Returns((IEnumerable<FormulariumDto>)null!);

        // Act
        var result = _repository.ListData();

        // Assert
        var formulariumTypes = result.ToList();
        formulariumTypes.Should().NotBeNull();
        formulariumTypes.Should().BeEmpty();
    }

    [Fact]
    public void UT7_GivenListWithItems_WhenListData_ThenListWithModelsIsReturned()
    {
        // Arrange
        var dtos = new List<FormulariumDto> { CreateTestDto(), CreateTestDto() };
        _formulariumDalMock
            .Setup(x => x.ListData())
            .Returns(dtos);

        // Act
        var result = _repository.ListData();

        // Assert
        var formulariumTypes = result.ToList();
        formulariumTypes.Should().NotBeNull();
        formulariumTypes.Count.Should().Be(2);
    }

    private static FormulariumType CreateTestModel()
        => FormulariumType.Default;

    private static FormulariumDto CreateTestDto()
        => FormulariumDto.FromModel(FormulariumType.Default);

    private static IFormulariumKey CreateTestKey()
        => FormulariumType.Key("A");
}
