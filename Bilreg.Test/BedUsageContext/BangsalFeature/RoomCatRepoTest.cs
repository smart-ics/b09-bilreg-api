using Bilreg.Domain.BedUsageContext.BangsalFeature;
using Bilreg.Infrastructure.BedUsageContext.BangsalFeature;
using FluentAssertions;
using Moq;

namespace Bilreg.Test.BedUsageContext.BangsalFeature;

public class RoomCatRepoTests
{
    private readonly Mock<IRoomCatDal> _roomCatDalMock;
    private readonly RoomCatRepo _repository;

    public RoomCatRepoTests()
    {
        _roomCatDalMock = new Mock<IRoomCatDal>();
        _repository = new RoomCatRepo(_roomCatDalMock.Object);
    }

    [Fact]
    public void UT1_GivenExistingEntity_WhenSaveChanges_ThenUpdateIsCalled()
    {
        // Arrange
        var existingModel = CreateTestModel();
        _roomCatDalMock
            .Setup(x => x.GetData(It.IsAny<IRoomCatKey>()))
            .Returns(CreateTestModel());

        // Act
        _repository.SaveChanges(existingModel);

        // Assert
        _roomCatDalMock.Verify(x => x.Update(It.IsAny<RoomCatType>()), Times.Once);
        _roomCatDalMock.Verify(x => x.Insert(It.IsAny<RoomCatType>()), Times.Never);
    }

    [Fact]
    public void UT2_GivenNewEntity_WhenSaveChanges_ThenInsertIsCalled()
    {
        // Arrange
        var newModel = CreateTestModel();
        var key = CreateTestKey();
        _roomCatDalMock
            .Setup(x => x.GetData(key))
            .Returns((RoomCatType)null!);

        // Act
        _repository.SaveChanges(newModel);

        // Assert
        _roomCatDalMock.Verify(x => x.Insert(It.IsAny<RoomCatType>()), Times.Once);
        _roomCatDalMock.Verify(x => x.Update(It.IsAny<RoomCatType>()), Times.Never);
    }

    [Fact]
    public void UT3_GivenExistingEntity_WhenLoadEntity_ThenEntityIsReturned()
    {
        // Arrange
        var expectedModel = CreateTestModel();
        var key = CreateTestKey();
        _roomCatDalMock
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
        _roomCatDalMock
            .Setup(x => x.GetData(key))
            .Returns((RoomCatType)null!);

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
        _roomCatDalMock.Verify(x => x.Delete(key), Times.Once);
    }

    [Fact]
    public void UT6_GivenEmptyList_WhenListData_ThenEmptyListIsReturned()
    {
        // Arrange
        _roomCatDalMock
            .Setup(x => x.ListData())
            .Returns((IEnumerable<RoomCatType>)null!);

        // Act
        var result = _repository.ListData();

        // Assert
        var roomCatTypes = result.ToList();
        roomCatTypes.Should().NotBeNull();
        roomCatTypes.Should().BeEmpty();
    }

    [Fact]
    public void UT7_GivenListWithItems_WhenListData_ThenListWithModelsIsReturned()
    {
        // Arrange
        var models = new List<RoomCatType> { CreateTestModel(), CreateTestModel() };
        _roomCatDalMock
            .Setup(x => x.ListData())
            .Returns(models);

        // Act
        var result = _repository.ListData();

        // Assert
        var roomCatTypes = result.ToList();
        roomCatTypes.Should().NotBeNull();
        roomCatTypes.Count.Should().Be(2);
    }

    private static RoomCatType CreateTestModel()
        => RoomCatType.Default;
    private static IRoomCatKey CreateTestKey()
        => RoomCatType.Key("A"); 
}