using Farinv.Application.BrgContext.BrgFeature;
using Farinv.Domain.BrgContext.BrgFeature;
using Farinv.Infrastructure.BrgContext.BrgFeature;
using FluentAssertions;
using Moq;

namespace Farinv.Test.BrgContext.BrgFeature;

public class BrgRepoTests
{
    private readonly Mock<IBrgDal> _brgDalMock;
    private readonly Mock<IBrgSatuanDal> _brgSatuanDalMock;
    private readonly BrgRepo _repository;
    public BrgRepoTests()
    {
        _brgDalMock = new Mock<IBrgDal>();
        _brgSatuanDalMock = new Mock<IBrgSatuanDal>();
        _repository = new BrgRepo(_brgDalMock.Object, _brgSatuanDalMock.Object);
    }
    
    [Fact]
    public void UT1_GivenExistingBrgObatEntity_WhenSaveChanges_ThenUpdateAndDeleteInsertAreCalled()
    {
        // Arrange
        var existingModel = CreateTestBrgObatModel();
        _brgDalMock
            .Setup(x => x.GetData(It.IsAny<IBrgKey>()))
            .Returns(CreateTestBrgDto());
        // Act
        _repository.SaveChanges(existingModel);
        // Assert
        _brgDalMock.Verify(x => x.Update(It.IsAny<BrgDto>()), Times.Once);
        _brgDalMock.Verify(x => x.Insert(It.IsAny<BrgDto>()), Times.Never);
        _brgSatuanDalMock.Verify(x => x.Delete(It.IsAny<IBrgKey>()), Times.Once);
        _brgSatuanDalMock.Verify(x => x.Insert(It.IsAny<IEnumerable<BrgSatuanDto>>()), Times.Once);
    }
    
    [Fact]
    public void UT2_GivenExistingBrgBhpEntity_WhenSaveChanges_ThenUpdateAndDeleteInsertAreCalled()
    {
        // Arrange
        var existingModel = CreateTestBrgBhpModel();
        _brgDalMock
            .Setup(x => x.GetData(It.IsAny<IBrgKey>()))
            .Returns(CreateTestBrgDto());
        // Act
        _repository.SaveChanges(existingModel);
        // Assert
        _brgDalMock.Verify(x => x.Update(It.IsAny<BrgDto>()), Times.Once);
        _brgDalMock.Verify(x => x.Insert(It.IsAny<BrgDto>()), Times.Never);
        _brgSatuanDalMock.Verify(x => x.Delete(It.IsAny<IBrgKey>()), Times.Once);
        _brgSatuanDalMock.Verify(x => x.Insert(It.IsAny<IEnumerable<BrgSatuanDto>>()), Times.Once);
    }
    
    [Fact]
    public void UT3_GivenNewBrgObatEntity_WhenSaveChanges_ThenInsertAndDeleteInsertAreCalled()
    {
        // Arrange
        var newModel = CreateTestBrgObatModel();
        var key = CreateTestKey();
        _brgDalMock
            .Setup(x => x.GetData(key))
            .Returns((BrgDto)null!);
        // Act
        _repository.SaveChanges(newModel);
        // Assert
        _brgDalMock.Verify(x => x.Insert(It.IsAny<BrgDto>()), Times.Once);
        _brgDalMock.Verify(x => x.Update(It.IsAny<BrgDto>()), Times.Never);
        _brgSatuanDalMock.Verify(x => x.Delete(It.IsAny<IBrgKey>()), Times.Once);
        _brgSatuanDalMock.Verify(x => x.Insert(It.IsAny<IEnumerable<BrgSatuanDto>>()), Times.Once);
    }
    
    [Fact]
    public void UT4_GivenNewBrgBhpEntity_WhenSaveChanges_ThenInsertAndDeleteInsertAreCalled()
    {
        // Arrange
        var newModel = CreateTestBrgBhpModel();
        var key = CreateTestKey();
        _brgDalMock
            .Setup(x => x.GetData(key))
            .Returns((BrgDto)null!);
        // Act
        _repository.SaveChanges(newModel);
        // Assert
        _brgDalMock.Verify(x => x.Insert(It.IsAny<BrgDto>()), Times.Once);
        _brgDalMock.Verify(x => x.Update(It.IsAny<BrgDto>()), Times.Never);
        _brgSatuanDalMock.Verify(x => x.Delete(It.IsAny<IBrgKey>()), Times.Once);
        _brgSatuanDalMock.Verify(x => x.Insert(It.IsAny<IEnumerable<BrgSatuanDto>>()), Times.Once);
    }
    
    [Fact]
    public void UT5_GivenExistingBrgObatEntity_WhenLoadEntity_ThenBrgObatEntityIsReturned()
    {
        // Arrange
        var expectedDto = CreateTestBrgDto();
        var key = CreateTestKey();
        _brgDalMock
            .Setup(x => x.GetData(key))
            .Returns(expectedDto);
        var satuanDtos = new List<BrgSatuanDto> { CreateTestBrgSatuanDto() };
        _brgSatuanDalMock
            .Setup(x => x.ListData(key))
            .Returns(satuanDtos);
        // Act
        var result = _repository.LoadEntity(key);
        // Assert
        result.HasValue.Should().BeTrue();
        result.Match(
            onSome: model => model.Should().NotBeNull(),
            onNone: () => Assert.Fail("Expected Some but got None"));
    }
    
    [Fact]
    public void UT6_GivenExistingBrgBhpEntity_WhenLoadEntity_ThenBrgBhpEntityIsReturned()
    {
        // Arrange
        var expectedDto = CreateTestBrgDto();
        var key = CreateTestKey();
        _brgDalMock
            .Setup(x => x.GetData(key))
            .Returns(expectedDto);
        var satuanDtos = new List<BrgSatuanDto> { CreateTestBrgSatuanDto() };
        _brgSatuanDalMock
            .Setup(x => x.ListData(key))
            .Returns(satuanDtos);
        // Act
        var result = _repository.LoadEntity(key);
        // Assert
        result.HasValue.Should().BeTrue();
        result.Match(
            onSome: model => model.Should().NotBeNull(),
            onNone: () => Assert.Fail("Expected Some but got None"));
    }
    
    [Fact]
    public void UT7_GivenNonExistingEntity_WhenLoadEntity_ThenNoneIsReturned()
    {
        // Arrange
        var key = CreateTestKey();
        _brgDalMock
            .Setup(x => x.GetData(key))
            .Returns((BrgDto)null!);
        // Act
        var result = _repository.LoadEntity(key);
        // Assert
        result.HasValue.Should().BeFalse();
        result.Match(
            onSome: model => Assert.Fail("Expected None but got Some"),
            onNone: () => { });
    }
    
    [Fact]
    public void UT8_GivenKey_WhenDeleteEntity_ThenDeleteIsCalled()
    {
        // Arrange
        var key = CreateTestKey();
        // Act
        _repository.DeleteEntity(key);
        // Assert
        _brgDalMock.Verify(x => x.Delete(key), Times.Once);
    }
    
    [Fact]
    public void UT9_GivenKeyword_WhenListData_ThenListWithViewsIsReturned()
    {
        // Arrange
        var keyword = "test";
        var listView = new List<BrgView> { CreateTestBrgView() };
        _brgDalMock
            .Setup(x => x.ListData(keyword))
            .Returns(listView);
        // Act
        var result = _repository.ListData(keyword);
        // Assert
        var brgViews = result.ToList();
        brgViews.Should().NotBeNull();
        brgViews.Count.Should().Be(1);
    }
    
    private static BrgObatType CreateTestBrgObatModel()
        => BrgObatType.Default;
    
    private static BrgBhpType CreateTestBrgBhpModel()
        => BrgBhpType.Default;
    
    private static BrgDto CreateTestBrgDto()
        => BrgDto.FromModel(BrgObatType.Default); 
    
    private static BrgSatuanDto CreateTestBrgSatuanDto()
        => BrgSatuanDto.FromModel("A", BrgSatuanType.Default);
    
    private static BrgView CreateTestBrgView()
        => BrgView.FromModel(BrgObatType.Default);
    
    private static IBrgKey CreateTestKey()
        => BrgObatType.Key("A"); 
}