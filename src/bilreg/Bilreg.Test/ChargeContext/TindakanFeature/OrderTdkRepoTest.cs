using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Infrastructure.ChargeContext.TindakanFeature;
using FluentAssertions;
using Moq;

namespace Bilreg.Test.ChargeContext.TindakanFeature;

public class OrderTdkRepoTests
{
    private readonly Mock<IOrderTdkDal> _orderTdkDalMock;
    private readonly OrderTdkRepo _repository;

    public OrderTdkRepoTests()
    {
        _orderTdkDalMock = new Mock<IOrderTdkDal>();
        _repository = new OrderTdkRepo(_orderTdkDalMock.Object);
    }

    [Fact]
    public void UT1_GivenExistingEntity_WhenSaveChanges_ThenUpdateIsCalled()
    {
        // Arrange
        var existingModel = CreateTestModel();
        _orderTdkDalMock
            .Setup(x => x.GetData(It.IsAny<IOrderTdkKey>()))
            .Returns(CreateTestDto());

        // Act
        _repository.SaveChanges(existingModel);

        // Assert
        _orderTdkDalMock.Verify(x => x.Update(It.IsAny<OrderTdkDto>()), Times.Once);
        _orderTdkDalMock.Verify(x => x.Insert(It.IsAny<OrderTdkDto>()), Times.Never);
    }

    [Fact]
    public void UT2_GivenNewEntity_WhenSaveChanges_ThenInsertIsCalled()
    {
        // Arrange
        var newModel = CreateTestModel();
        var key = CreateTestKey();
        _orderTdkDalMock
            .Setup(x => x.GetData(key))
            .Returns((OrderTdkDto)null!);

        // Act
        _repository.SaveChanges(newModel);

        // Assert
        _orderTdkDalMock.Verify(x => x.Insert(It.IsAny<OrderTdkDto>()), Times.Once);
        _orderTdkDalMock.Verify(x => x.Update(It.IsAny<OrderTdkDto>()), Times.Never);
    }

    [Fact]
    public void UT3_GivenExistingEntity_WhenLoadEntity_ThenEntityIsReturned()
    {
        // Arrange
        var expectedDto = CreateTestDto();
        var key = CreateTestKey();
        _orderTdkDalMock
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
        _orderTdkDalMock
            .Setup(x => x.GetData(key))
            .Returns((OrderTdkDto)null!);

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
        _repository.Delete(key);

        // Assert
        _orderTdkDalMock.Verify(x => x.Delete(key), Times.Once);
    }

    [Fact]
    public void UT6_GivenPasienKey_WhenListData_ThenListWithModelsIsReturned()
    {
        // Arrange
        var expectedDtos = new List<OrderTdkDto> { CreateTestDto() };
        var filter = CreateTestPasienKey();
        _orderTdkDalMock
            .Setup(x => x.ListData(filter))
            .Returns(expectedDtos);

        // Act
        var result = _repository.ListData(filter);

        // Assert
        var orderTdkModels = result.ToList();
        orderTdkModels.Should().NotBeNull();
        orderTdkModels.Count.Should().Be(1);
    }

    [Fact]
    public void UT7_GivenEmptyList_WhenListData_ThenEmptyListIsReturned()
    {
        // Arrange
        var filter = CreateTestPasienKey();
        _orderTdkDalMock
            .Setup(x => x.ListData(filter))
            .Returns((IEnumerable<OrderTdkDto>)null!);

        // Act
        var result = _repository.ListData(filter);

        // Assert
        var orderTdkModels = result.ToList();
        orderTdkModels.Should().NotBeNull();
        orderTdkModels.Should().BeEmpty();
    }

    private static OrderTdkModel CreateTestModel()
        => OrderTdkModel.Default;
    private static OrderTdkDto CreateTestDto()
        => OrderTdkDto.FromModel(OrderTdkModel.Default); 
    private static IOrderTdkKey CreateTestKey()
        => OrderTdkModel.Key("A"); 
    private static IPasienKey CreateTestPasienKey()
        => PasienModel.Key("B");
}