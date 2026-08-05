using Farinv.Domain.InventoryContext.StokFeature;
using Farinv.Infrastructure.InventoryContext.StokFeature;
using FluentAssertions;
using Moq;

namespace Farinv.Test.InventoryContext.StokFeature;

public class LayananRepoTests
{
    private readonly Mock<ILayananDal> _layananDalMock;
    private readonly LayananRepo _repository;
    public LayananRepoTests()
    {
        _layananDalMock = new Mock<ILayananDal>();
        _repository = new LayananRepo(_layananDalMock.Object);
    }
    
    [Fact]
    public void UT1_GivenExistingEntity_WhenLoadEntity_ThenEntityIsReturned()
    {
        // Arrange
        var expectedDto = CreateTestDto();
        var key = CreateTestKey();
        _layananDalMock
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
    public void UT2_GivenNonExistingEntity_WhenLoadEntity_ThenNoneIsReturned()
    {
        // Arrange
        var key = CreateTestKey();
        _layananDalMock
            .Setup(x => x.GetData(key))
            .Returns((LayananDto)null!);
        // Act
        var result = _repository.LoadEntity(key);
        // Assert
        result.HasValue.Should().BeFalse();
        result.Match(
            onSome: model => Assert.Fail("Expected None but got Some"),
            onNone: () => { });
    }
    
    [Fact]
    public void UT3_GivenEmptyList_WhenListData_ThenEmptyListIsReturned()
    {
        // Arrange
        _layananDalMock
            .Setup(x => x.ListData())
            .Returns((IEnumerable<LayananDto>)null!);
        // Act
        var result = _repository.ListData();
        // Assert
        var layananTypes = result.ToList();
        layananTypes.Should().NotBeNull();
        layananTypes.Should().BeEmpty();
    }
    
    [Fact]
    public void UT4_GivenListWithItems_WhenListData_ThenListWithModelsIsReturned()
    {
        // Arrange
        var dtos = new List<LayananDto> { CreateTestDto(), CreateTestDto() };
        _layananDalMock
            .Setup(x => x.ListData())
            .Returns(dtos);
        // Act
        var result = _repository.ListData();
        // Assert
        var layananTypes = result.ToList();
        layananTypes.Should().NotBeNull();
        layananTypes.Count.Should().Be(2);
    }
    
    private static LayananType CreateTestModel()
        => LayananType.Default;
    
    private static LayananDto CreateTestDto()
        => LayananDto.FromModel(LayananType.Default); 
    
    private static JenisLokasiType CreateTestJenisLokasi()
        => JenisLokasiType.Default;
    
    private static ILayananKey CreateTestKey()
        => LayananType.Key("A"); 
}