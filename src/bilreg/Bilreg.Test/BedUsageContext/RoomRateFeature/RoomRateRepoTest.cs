using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Domain.BedUsageContext.RoomRateFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Infrastructure.BedUsageContext.RoomRateFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.BedUsageContext.RoomRateFeature;

public class RoomRateRepoTests
{
    private readonly Mock<IRoomRateDal> _roomRateDalMock;
    private readonly Mock<IKamarRepo> _kamarRepoMock;
    private readonly RoomRateRepo _repository;

    public RoomRateRepoTests()
    {
        _roomRateDalMock = new Mock<IRoomRateDal>();
        _kamarRepoMock = new Mock<IKamarRepo>();
        _repository = new RoomRateRepo(_roomRateDalMock.Object, _kamarRepoMock.Object);
    }

    [Fact]
    public void UT1_GivenRoomRateRegulerEntity_WhenSaveChanges_ThenDeleteAndInsertAreCalled()
    {
        // Arrange
        var regulerModel = CreateTestRoomRateRegulerModel();
        var kamar = CreateTestKamar();
        _kamarRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IKamarKey>()))
            .Returns(MayBe<KamarType>.Some(kamar));

        // Act
        _repository.SaveChanges(regulerModel);

        // Assert
        _roomRateDalMock.Verify(x => x.Delete(It.IsAny<IRoomRate<IRoomRateDetail>>()), Times.Once);
        _roomRateDalMock.Verify(x => x.Insert(It.IsAny<IEnumerable<RoomRateDto>>()), Times.Once);
    }

    [Fact]
    public void UT2_GivenRoomRateDailyEntity_WhenSaveChanges_ThenDeleteAndInsertAreCalled()
    {
        // Arrange
        var dailyModel = CreateTestRoomRateDailyModel();
        var kamar = CreateTestKamar();
        _kamarRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IKamarKey>()))
            .Returns(MayBe<KamarType>.Some(kamar));

        // Act
        _repository.SaveChanges(dailyModel);

        // Assert
        _roomRateDalMock.Verify(x => x.Delete(It.IsAny<IRoomRate<IRoomRateDetail>>()), Times.Once);
        _roomRateDalMock.Verify(x => x.Insert(It.IsAny<IEnumerable<RoomRateDto>>()), Times.Once);
    }

    [Fact]
    public void UT3_GivenRoomRateFloatingEntity_WhenSaveChanges_ThenDeleteAndInsertAreCalled()
    {
        // Arrange
        var floatingModel = CreateTestRoomRateFloatingModel();

        // Act
        _repository.SaveChanges(floatingModel);

        // Assert
        _roomRateDalMock.Verify(x => x.Delete(It.IsAny<IRoomRate<IRoomRateDetail>>()), Times.Once);
        _roomRateDalMock.Verify(x => x.Insert(It.IsAny<IEnumerable<RoomRateDto>>()), Times.Once);
    }

    [Fact]
    public void UT4_GivenNonExistingKamar_WhenSaveChangesReguler_ThenExceptionIsThrown()
    {
        // Arrange
        var regulerModel = CreateTestRoomRateRegulerModel();
        _kamarRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IKamarKey>()))
            .Returns(MayBe<KamarType>.None);

        // Act & Assert
        Assert.Throws<KeyNotFoundException>(() => _repository.SaveChanges(regulerModel));
    }

    [Fact]
    public void UT5_GivenExistingEntity_WhenLoadEntity_ThenEntityIsReturned()
    {
        // Arrange
        var expectedDtos = new List<RoomRateDto> { CreateTestRoomRateDto() };
        var key = CreateTestKey();
        _roomRateDalMock
            .Setup(x => x.ListData(key))
            .Returns(expectedDtos);

        // Act
        var result = _repository.LoadEntity(key);

        // Assert
        result.HasValue.Should().BeTrue();
        result.Match(
            onSome: model => model.Should().NotBeNull(),
            onNone: () => Assert.Fail("Expected Some but got None"));
    }

    [Fact]
    public void UT6_GivenNonExistingEntity_WhenLoadEntity_ThenNoneIsReturned()
    {
        // Arrange
        var key = CreateTestKey();
        _roomRateDalMock
            .Setup(x => x.ListData(key))
            .Returns((IEnumerable<RoomRateDto>)null!);

        // Act
        var result = _repository.LoadEntity(key);

        // Assert
        result.HasValue.Should().BeFalse();
        result.Match(
            onSome: model => Assert.Fail("Expected None but got Some"),
            onNone: () => { });
    }

    [Fact]
    public void UT7_GivenKey_WhenDeleteEntity_ThenDeleteIsCalled()
    {
        // Arrange
        var key = CreateTestKey();

        // Act
        _repository.DeleteEntity(key);

        // Assert
        _roomRateDalMock.Verify(x => x.Delete(key), Times.Once);
    }

    private static IRoomRate<IRoomRateDetail> CreateTestRoomRateRegulerModel()
        => RoomRateRegulerType.Default;
    private static IRoomRate<IRoomRateDetail> CreateTestRoomRateDailyModel()
        => RoomRateDailyType.Default;
    private static IRoomRate<IRoomRateDetail> CreateTestRoomRateFloatingModel()
        => RoomRateFloatingType.Default;
    private static KamarType CreateTestKamar()
        => KamarType.Default;
    private static RoomRateDto CreateTestRoomRateDto()
        => new RoomRateDto("A", "B", "C", 100000, 1, "D", 1, "E", "F", "G", "H");
    private static IKamarKey CreateTestKey()
        => KamarType.Key("A");
}