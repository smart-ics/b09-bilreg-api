using Farinv.Domain.BrgContext.PricingPolicyFeature;
using Farinv.Infrastructure.BrgContext.PricingPolicyFeature;
using FluentAssertions;
using Moq;

namespace Farinv.Test.BrgContext.PricingPolicyFeature;

public class TipeBrgRepoTests
{
    private readonly Mock<ITipeBrgDal> _tipeBrgDalMock;
    private readonly TipeBrgRepo _repository;

    public TipeBrgRepoTests()
    {
        _tipeBrgDalMock = new Mock<ITipeBrgDal>();
        _repository = new TipeBrgRepo(_tipeBrgDalMock.Object);
    }

    [Fact]
    public void UT1_GivenExistingEntity_WhenSaveChanges_ThenUpdateIsCalled()
    {
        // Arrange
        var existingModel = CreateTestModel();
        _tipeBrgDalMock
            .Setup(x => x.GetData(It.IsAny<ITipeBrgKey>()))
            .Returns(CreateTestDto());

        // Act
        _repository.SaveChanges(existingModel);

        // Assert
        _tipeBrgDalMock.Verify(x => x.Update(It.IsAny<TipeBrgDto>()), Times.Once);
        _tipeBrgDalMock.Verify(x => x.Insert(It.IsAny<TipeBrgDto>()), Times.Never);
    }

    [Fact]
    public void UT2_GivenNewEntity_WhenSaveChanges_ThenInsertIsCalled()
    {
        // Arrange
        var newModel = CreateTestModel();
        var key = CreateTestKey();
        _tipeBrgDalMock
            .Setup(x => x.GetData(key))
            .Returns((TipeBrgDto)null!);

        // Act
        _repository.SaveChanges(newModel);

        // Assert
        _tipeBrgDalMock.Verify(x => x.Insert(It.IsAny<TipeBrgDto>()), Times.Once);
        _tipeBrgDalMock.Verify(x => x.Update(It.IsAny<TipeBrgDto>()), Times.Never);
    }

    [Fact]
    public void UT3_GivenExistingEntity_WhenLoadEntity_ThenEntityIsReturned()
    {
        // Arrange
        var expectedDto = CreateTestDto();
        var key = CreateTestKey();
        _tipeBrgDalMock
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
        _tipeBrgDalMock
            .Setup(x => x.GetData(key))
            .Returns((TipeBrgDto)null!);

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
        _tipeBrgDalMock.Verify(x => x.Delete(key), Times.Once);
    }

    [Fact]
    public void UT6_GivenEmptyList_WhenListData_ThenEmptyListIsReturned()
    {
        // Arrange
        _tipeBrgDalMock
            .Setup(x => x.ListData())
            .Returns((IEnumerable<TipeBrgDto>)null!);

        // Act
        var result = _repository.ListData();

        // Assert
        var tipeBrgTypes = result.ToList();
        tipeBrgTypes.Should().NotBeNull();
        tipeBrgTypes.Should().BeEmpty();
    }

    [Fact]
    public void UT7_GivenListWithItems_WhenListData_ThenListWithModelsIsReturned()
    {
        // Arrange
        var dtos = new List<TipeBrgDto> { CreateTestDto(), CreateTestDto() };
        _tipeBrgDalMock
            .Setup(x => x.ListData())
            .Returns(dtos);

        // Act
        var result = _repository.ListData();

        // Assert
        var tipeBrgTypes = result.ToList();
        tipeBrgTypes.Should().NotBeNull();
        tipeBrgTypes.Count.Should().Be(2);
    }

    private static TipeBrgType CreateTestModel()
        => TipeBrgType.Default;

    private static TipeBrgDto CreateTestDto()
        => TipeBrgDto.FromModel(TipeBrgType.Default);

    private static ITipeBrgKey CreateTestKey()
        => TipeBrgType.Key("T1");
}