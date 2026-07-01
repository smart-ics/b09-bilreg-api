using Farinv.Domain.InventoryContext.StokFeature;
using Farinv.Infrastructure.InventoryContext.StokFeature;
using FluentAssertions;
using Moq;

namespace Farinv.Test.InventoryContext.StokFeature;

public class JenisLokasiRepoTests
{
    private readonly Mock<IJenisLokasiDal> _jenisLokasiDalMock;
    private readonly JenisLokasiRepo _repository;
    public JenisLokasiRepoTests()
    {
        _jenisLokasiDalMock = new Mock<IJenisLokasiDal>();
        _repository = new JenisLokasiRepo(_jenisLokasiDalMock.Object);
    }
    
    [Fact]
    public void UT1_GivenExistingEntity_WhenSaveChanges_ThenUpdateIsCalled()
    {
        // Arrange
        var existingModel = CreateTestModel();
        _jenisLokasiDalMock
            .Setup(x => x.GetData(It.IsAny<IJenisLokasiKey>()))
            .Returns(CreateTestModel());
        // Act
        _repository.SaveChanges(existingModel);
        // Assert
        _jenisLokasiDalMock.Verify(x => x.Update(It.IsAny<JenisLokasiType>()), Times.Once);
        _jenisLokasiDalMock.Verify(x => x.Insert(It.IsAny<JenisLokasiType>()), Times.Never);
    }
    
    [Fact]
    public void UT2_GivenNewEntity_WhenSaveChanges_ThenInsertIsCalled()
    {
        // Arrange
        var newModel = CreateTestModel();
        var key = CreateTestKey();
        _jenisLokasiDalMock
            .Setup(x => x.GetData(key))
            .Returns((JenisLokasiType)null!);
        // Act
        _repository.SaveChanges(newModel);
        // Assert
        _jenisLokasiDalMock.Verify(x => x.Insert(It.IsAny<JenisLokasiType>()), Times.Once);
        _jenisLokasiDalMock.Verify(x => x.Update(It.IsAny<JenisLokasiType>()), Times.Never);
    }
    
    [Fact]
    public void UT3_GivenExistingEntity_WhenLoadEntity_ThenEntityIsReturned()
    {
        // Arrange
        var expectedModel = CreateTestModel();
        var key = CreateTestKey();
        _jenisLokasiDalMock
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
        _jenisLokasiDalMock
            .Setup(x => x.GetData(key))
            .Returns((JenisLokasiType)null!);
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
        _jenisLokasiDalMock.Verify(x => x.Delete(key), Times.Once);
    }
    
    [Fact]
    public void UT6_GivenEmptyList_WhenListData_ThenEmptyListIsReturned()
    {
        // Arrange
        _jenisLokasiDalMock
            .Setup(x => x.ListData())
            .Returns((IEnumerable<JenisLokasiType>)null!);
        // Act
        var result = _repository.ListData();
        // Assert
        var jenisLokasiTypes = result.ToList();
        jenisLokasiTypes.Should().NotBeNull();
        jenisLokasiTypes.Should().BeEmpty();
    }
    
    [Fact]
    public void UT7_GivenListWithItems_WhenListData_ThenListWithModelsIsReturned()
    {
        // Arrange
        var models = new List<JenisLokasiType> { CreateTestModel(), CreateTestModel() };
        _jenisLokasiDalMock
            .Setup(x => x.ListData())
            .Returns(models);
        // Act
        var result = _repository.ListData();
        // Assert
        var jenisLokasiTypes = result.ToList();
        jenisLokasiTypes.Should().NotBeNull();
        jenisLokasiTypes.Count.Should().Be(2);
    }
    
    private static JenisLokasiType CreateTestModel()
        => JenisLokasiType.Default;
    
    private static IJenisLokasiKey CreateTestKey()
        => JenisLokasiType.Key("A"); 
}