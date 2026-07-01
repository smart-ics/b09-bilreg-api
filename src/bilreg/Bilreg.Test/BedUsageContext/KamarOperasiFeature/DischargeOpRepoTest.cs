using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;
using FluentAssertions;
using Moq;

namespace Bilreg.Test.BedUsageContext.KamarOperasiFeature;

public class DischargeOpRepoTests
{
    private readonly Mock<IDischargeOpDal> _dischargeOpDalMock;
    private readonly DischargeOpRepo _repository;

    public DischargeOpRepoTests()
    {
        _dischargeOpDalMock = new Mock<IDischargeOpDal>();
        _repository = new DischargeOpRepo(_dischargeOpDalMock.Object);
    }

    [Fact]
    public void UT1_GivenExistingEntity_WhenSaveChanges_ThenUpdateIsCalled()
    {
        // Arrange
        var existingModel = CreateTestModel();
        _dischargeOpDalMock
            .Setup(x => x.GetData(It.IsAny<IDischargeOpKey>()))
            .Returns(CreateTestDto());

        // Act
        _repository.SaveChanges(existingModel);

        // Assert
        _dischargeOpDalMock.Verify(x => x.Update(It.IsAny<DischargeOpDto>()), Times.Once);
        _dischargeOpDalMock.Verify(x => x.Insert(It.IsAny<DischargeOpDto>()), Times.Never);
    }

    [Fact]
    public void UT2_GivenNewEntity_WhenSaveChanges_ThenInsertIsCalled()
    {
        // Arrange
        var newModel = CreateTestModel();
        var key = CreateTestKey();

        _dischargeOpDalMock
            .Setup(x => x.GetData(key))
            .Returns((DischargeOpDto)null!);

        // Act
        _repository.SaveChanges(newModel);

        // Assert
        _dischargeOpDalMock.Verify(x => x.Insert(It.IsAny<DischargeOpDto>()), Times.Once);
        _dischargeOpDalMock.Verify(x => x.Update(It.IsAny<DischargeOpDto>()), Times.Never);
    }

    [Fact]
    public void UT3_GivenExistingEntity_WhenLoadEntity_ThenEntityIsReturned()
    {
        // Arrange
        var expectedDto = CreateTestDto();
        var key = CreateTestKey();

        _dischargeOpDalMock
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
        _dischargeOpDalMock
            .Setup(x => x.GetData(key))
            .Returns((DischargeOpDto)null!);

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
        _dischargeOpDalMock.Verify(x => x.Delete(key), Times.Once);
    }

    [Fact]
    public void UT6_GivenDateFilter_WhenListDataIsCalled_ThenCorrectListIsReturned()
    {
        // Arrange
        var filterDate = new DateTime(2025, 12, 1);
        var expectedDtos = new List<DischargeOpDto>
        {
            CreateTestDto(),
            CreateTestDto()
        };

        _dischargeOpDalMock
            .Setup(x => x.ListData(filterDate))
            .Returns(expectedDtos);

        // Act
        var result = _repository.ListData(filterDate).ToList();

        // Assert
        _dischargeOpDalMock.Verify(x => x.ListData(filterDate), Times.Once);
        result.Should().NotBeNullOrEmpty();
        result.Should().HaveCount(expectedDtos.Count);
    }

    private static DischargeOpModel CreateTestModel()
        => DischargeOpModel.Default;

    private static DischargeOpDto CreateTestDto()
        => DischargeOpDto.FromModel(DischargeOpModel.Default);

    private static IDischargeOpKey CreateTestKey()
        => DischargeOpModel.Key("DO-001");
}
