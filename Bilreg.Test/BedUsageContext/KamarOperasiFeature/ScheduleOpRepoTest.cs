using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;
using FluentAssertions;
using Moq;

namespace Bilreg.Test.BedUsageContext.KamarOperasiFeature;

public class ScheduleOpRepoTests
{
    private readonly Mock<IScheduleOpDal> _scheduleOpDalMock;
    private readonly Mock<IScheduleOpPpaDal> _scheduleOpPpaDalMock;
    private readonly ScheduleOpRepo _repository;

    public ScheduleOpRepoTests()
    {
        _scheduleOpDalMock = new Mock<IScheduleOpDal>();
        _scheduleOpPpaDalMock = new Mock<IScheduleOpPpaDal>();
        _repository = new ScheduleOpRepo(_scheduleOpDalMock.Object, _scheduleOpPpaDalMock.Object);
    }

    [Fact]
    public void UT1_GivenExistingEntity_WhenSaveChanges_ThenUpdateIsCalled()
    {
        // Arrange
        var existingModel = CreateTestModel();
        _scheduleOpDalMock
            .Setup(x => x.GetData(It.IsAny<IScheduleOpKey>()))
            .Returns(CreateTestDto());

        // Act
        _repository.SaveChanges(existingModel);

        // Assert
        _scheduleOpDalMock.Verify(x => x.Update(It.IsAny<ScheduleOpDto>()), Times.Once);
        _scheduleOpDalMock.Verify(x => x.Insert(It.IsAny<ScheduleOpDto>()), Times.Never);
    }

    [Fact]
    public void UT2_GivenNewEntity_WhenSaveChanges_ThenInsertIsCalled()
    {
        // Arrange
        var newModel = CreateTestModel();
        var key = CreateTestKey();
        _scheduleOpDalMock
            .Setup(x => x.GetData(key))
            .Returns((ScheduleOpDto)null!);

        // Act
        _repository.SaveChanges(newModel);

        // Assert
        _scheduleOpDalMock.Verify(x => x.Insert(It.IsAny<ScheduleOpDto>()), Times.Once);
        _scheduleOpDalMock.Verify(x => x.Update(It.IsAny<ScheduleOpDto>()), Times.Never);
    }

    [Fact]
    public void UT3_GivenExistingEntity_WhenLoadEntity_ThenEntityIsReturned()
    {
        // Arrange
        var expectedDto = CreateTestDto();
        var expectedPpaDtos = new List<ScheduleOpPpaDto> { CreateTestPpaDto() };
        var key = CreateTestKey();
        _scheduleOpDalMock
            .Setup(x => x.GetData(key))
            .Returns(expectedDto);
        _scheduleOpPpaDalMock
            .Setup(x => x.ListData(key))
            .Returns(expectedPpaDtos);

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
        _scheduleOpDalMock
            .Setup(x => x.GetData(key))
            .Returns((ScheduleOpDto)null!);

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
        _scheduleOpDalMock.Verify(x => x.Delete(key), Times.Once);
    }

    [Fact]
    public void UT6_GivenKey_WhenLoadEntity_ThenPpaListIsLoaded()
    {
        // Arrange
        var expectedDto = CreateTestDto();
        var expectedPpaDtos = new List<ScheduleOpPpaDto> { CreateTestPpaDto() };
        var key = CreateTestKey();
        _scheduleOpDalMock
            .Setup(x => x.GetData(key))
            .Returns(expectedDto);
        _scheduleOpPpaDalMock
            .Setup(x => x.ListData(key))
            .Returns(expectedPpaDtos);

        // Act
        var result = _repository.LoadEntity(key);

        // Assert
        _scheduleOpPpaDalMock.Verify(x => x.ListData(key), Times.Once);
    }

    private static ScheduleOpModel CreateTestModel()
        => ScheduleOpModel.Default;
    private static ScheduleOpDto CreateTestDto()
        => ScheduleOpDto.FromModel(ScheduleOpModel.Default); 
    private static ScheduleOpPpaDto CreateTestPpaDto()
        => new ScheduleOpPpaDto("A", 1, "B", "C", "D", "E", "F", "G");
    private static IScheduleOpKey CreateTestKey()
        => ScheduleOpModel.Key("A"); 
}