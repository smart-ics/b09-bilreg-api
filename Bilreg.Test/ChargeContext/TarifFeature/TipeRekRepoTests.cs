using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Infrastructure.ChargeContext.TarifFeature;
using FluentAssertions;
using Moq;

namespace Bilreg.Test.ChargeContext.TarifFeature;

public class TipeRekRepoTests
{
    private readonly Mock<ITipeRekDal> _tipeRekDalMock;
    private readonly TipeRekRepo _repository;

    public TipeRekRepoTests()
    {
        _tipeRekDalMock = new Mock<ITipeRekDal>();
        _repository = new TipeRekRepo(_tipeRekDalMock.Object);
    }

    [Fact]
    public void UT1_GivenExistingEntity_WhenSaveChanges_ThenUpdateIsCalled()
    {
        // Arrange
        var existingModel = CreateTestModel();
        _tipeRekDalMock
            .Setup(x => x.GetData(It.IsAny<ITipeRekKey>()))
            .Returns(CreateTestDto());

        // Act
        _repository.SaveChanges(existingModel);

        // Assert
        _tipeRekDalMock.Verify(x => x.Update(It.IsAny<TipeRekDto>()), Times.Once);
        _tipeRekDalMock.Verify(x => x.Insert(It.IsAny<TipeRekDto>()), Times.Never);
    }

    [Fact]
    public void UT2_GivenNewEntity_WhenSaveChanges_ThenInsertIsCalled()
    {
        // Arrange
        var newModel = CreateTestModel();
        var key = CreateTestKey();
        _tipeRekDalMock
            .Setup(x => x.GetData(key))
            .Returns((TipeRekDto)null!);

        // Act
        _repository.SaveChanges(newModel);

        // Assert
        _tipeRekDalMock.Verify(x => x.Insert(It.IsAny<TipeRekDto>()), Times.Once);
        _tipeRekDalMock.Verify(x => x.Update(It.IsAny<TipeRekDto>()), Times.Never);
    }

    [Fact]
    public void UT3_GivenExistingEntity_WhenLoadEntity_ThenEntityIsReturned()
    {
        // Arrange
        var expectedDto = CreateTestDto();
        var key = CreateTestKey();
        _tipeRekDalMock
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
        _tipeRekDalMock
            .Setup(x => x.GetData(key))
            .Returns((TipeRekDto)null!);

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
        _tipeRekDalMock.Verify(x => x.Delete(key), Times.Once);
    }

    [Fact]
    public void UT6_GivenEmptyList_WhenListData_ThenEmptyListIsReturned()
    {
        // Arrange
        _tipeRekDalMock
            .Setup(x => x.ListData())
            .Returns((IEnumerable<TipeRekDto>)null!);

        // Act
        var result = _repository.ListData();

        // Assert
        var tipeRekTypes = result.ToList();
        tipeRekTypes.Should().NotBeNull();
        tipeRekTypes.Should().BeEmpty();
    }

    [Fact]
    public void UT7_GivenListWithItems_WhenListData_ThenListWithModelsIsReturned()
    {
        // Arrange
        var dtos = new List<TipeRekDto> { CreateTestDto(), CreateTestDto() };
        _tipeRekDalMock
            .Setup(x => x.ListData())
            .Returns(dtos);

        // Act
        var result = _repository.ListData();

        // Assert
        var tipeRekTypes = result.ToList();
        tipeRekTypes.Should().NotBeNull();
        tipeRekTypes.Count.Should().Be(2);
    }

    private static TipeRekType CreateTestModel()
        => TipeRekType.Default;
    private static TipeRekDto CreateTestDto()
        => TipeRekDto.FromModel(TipeRekType.Default); 
    private static ITipeRekKey CreateTestKey()
        => TipeRekType.Key("A"); 
}