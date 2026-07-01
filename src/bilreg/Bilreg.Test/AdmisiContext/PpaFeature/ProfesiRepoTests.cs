using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Infrastructure.AdmisiContext.PpaFeature;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.AdmisiContext.PpaFeature;

public class ProfesiRepoTests
{
    private readonly Mock<IProfesiDal> _profesiDalMock;
    private readonly ProfesiRepo _repository;

    public ProfesiRepoTests()
    {
        _profesiDalMock = new Mock<IProfesiDal>();
        _repository = new ProfesiRepo(_profesiDalMock.Object);
    }

    [Fact]
    public void UT1_GivenExistingEntity_WhenSaveChanges_ThenUpdateIsCalled()
    {
        // Arrange
        var existingModel = CreateTestModel();
        _profesiDalMock
            .Setup(x => x.GetData(It.IsAny<IProfesiKey>()))
            .Returns(CreateTestModel());

        // Act
        _repository.SaveChanges(existingModel);

        // Assert
        _profesiDalMock.Verify(x => x.Update(It.IsAny<ProfesiType>()), Times.Once);
        _profesiDalMock.Verify(x => x.Insert(It.IsAny<ProfesiType>()), Times.Never);
    }

    [Fact]
    public void UT2_GivenNewEntity_WhenSaveChanges_ThenInsertIsCalled()
    {
        // Arrange
        var newModel = CreateTestModel();
        var key = CreateTestKey();
        _profesiDalMock
            .Setup(x => x.GetData(key))
            .Returns((ProfesiType)null!);

        // Act
        _repository.SaveChanges(newModel);

        // Assert
        _profesiDalMock.Verify(x => x.Insert(It.IsAny<ProfesiType>()), Times.Once);
        _profesiDalMock.Verify(x => x.Update(It.IsAny<ProfesiType>()), Times.Never);
    }

    [Fact]
    public void UT3_GivenExistingEntity_WhenLoadEntity_ThenEntityIsReturned()
    {
        // Arrange
        var expectedModel = CreateTestModel();
        var key = CreateTestKey();
        _profesiDalMock
            .Setup(x => x.GetData(key))
            .Returns(expectedModel);

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
        _profesiDalMock
            .Setup(x => x.GetData(key))
            .Returns((ProfesiType)null!);

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
        _profesiDalMock.Verify(x => x.Delete(key), Times.Once);
    }

    [Fact]
    public void UT6_GivenEmptyList_WhenListData_ThenEmptyListIsReturned()
    {
        // Arrange
        _profesiDalMock
            .Setup(x => x.ListData())
            .Returns((IEnumerable<ProfesiType>)null!);

        // Act
        var result = _repository.ListData();

        // Assert
        var profesiTypes = result.ToList();
        profesiTypes.Should().NotBeNull();
        profesiTypes.Should().BeEmpty();
    }

    [Fact]
    public void UT7_GivenListWithItems_WhenListData_ThenListWithModelsIsReturned()
    {
        // Arrange
        var models = new List<ProfesiType> { CreateTestModel(), CreateTestModel() };
        _profesiDalMock
            .Setup(x => x.ListData())
            .Returns(models);

        // Act
        var result = _repository.ListData();

        // Assert
        var profesiTypes = result.ToList();
        profesiTypes.Should().NotBeNull();
        profesiTypes.Count.Should().Be(2);
    }

    private static ProfesiType CreateTestModel()
        => ProfesiType.Default;
    private static IProfesiKey CreateTestKey()
        => ProfesiType.Key("A"); 
}