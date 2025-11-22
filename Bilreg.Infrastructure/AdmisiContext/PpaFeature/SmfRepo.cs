using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Infrastructure.AdmisiContext.PpaFeature;

public class SmfRepo : ISmfRepo
{
    private readonly ISmfDal _smfDal;
    public SmfRepo(ISmfDal smfDal)
    {
        _smfDal = smfDal;
    }
    public void SaveChanges(SmfType model)
    {
        LoadEntity(model)
            .Match(
                onSome: _ => _smfDal.Update(SmfDto.FromModel(model)),
                onNone: () => _smfDal.Insert(SmfDto.FromModel(model)));
    }

    public MayBe<SmfType> LoadEntity(ISmfKey key)
    {   
        var dto = _smfDal.GetData(key);
        if (dto is null)
            return MayBe<SmfType>.None;
        var model = dto.ToModel();
        return MayBe.From(model);
    }

    public void DeleteEntity(ISmfKey key)
    {
        _smfDal.Delete(key);
    }

    public IEnumerable<SmfType> ListData()
    {
        var listDto = _smfDal.ListData()?.ToList() ?? [];
        var result = listDto.Select(x => x.ToModel()).ToList();
        return result;
    }
}

public class SmfRepoTests
{
    private readonly Mock<ISmfDal> _smfDalMock;
    private readonly SmfRepo _repository;

    public SmfRepoTests()
    {
        _smfDalMock = new Mock<ISmfDal>();
        _repository = new SmfRepo(_smfDalMock.Object);
    }

    [Fact]
    public void UT1_GivenExistingEntity_WhenSaveChanges_ThenUpdateIsCalled()
    {
        // Arrange
        var existingModel = CreateTestModel();
        _smfDalMock
            .Setup(x => x.GetData(It.IsAny<ISmfKey>()))
            .Returns(CreateTestDto());

        // Act
        _repository.SaveChanges(existingModel);

        // Assert
        _smfDalMock.Verify(x => x.Update(It.IsAny<SmfDto>()), Times.Once);
        _smfDalMock.Verify(x => x.Insert(It.IsAny<SmfDto>()), Times.Never);
    }

    [Fact]
    public void UT2_GivenNewEntity_WhenSaveChanges_ThenInsertIsCalled()
    {
        // Arrange
        var newModel = CreateTestModel();
        var key = CreateTestKey();
        _smfDalMock
            .Setup(x => x.GetData(key))
            .Returns((SmfDto)null!);

        // Act
        _repository.SaveChanges(newModel);

        // Assert
        _smfDalMock.Verify(x => x.Insert(It.IsAny<SmfDto>()), Times.Once);
        _smfDalMock.Verify(x => x.Update(It.IsAny<SmfDto>()), Times.Never);
    }

    [Fact]
    public void UT3_GivenExistingEntity_WhenLoadEntity_ThenEntityIsReturned()
    {
        // Arrange
        var expectedDto = CreateTestDto();
        var key = CreateTestKey();
        _smfDalMock
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
        _smfDalMock
            .Setup(x => x.GetData(key))
            .Returns((SmfDto)null!);

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
        _smfDalMock.Verify(x => x.Delete(key), Times.Once);
    }

    [Fact]
    public void UT6_GivenEmptyList_WhenListData_ThenEmptyListIsReturned()
    {
        // Arrange
        _smfDalMock
            .Setup(x => x.ListData())
            .Returns((IEnumerable<SmfDto>)null!);

        // Act
        var result = _repository.ListData();

        // Assert
        var smfTypes = result.ToList();
        smfTypes.Should().NotBeNull();
        smfTypes.Should().BeEmpty();
    }

    [Fact]
    public void UT7_GivenListWithItems_WhenListData_ThenListWithModelsIsReturned()
    {
        // Arrange
        var dtos = new List<SmfDto> { CreateTestDto(), CreateTestDto() };
        _smfDalMock
            .Setup(x => x.ListData())
            .Returns(dtos);

        // Act
        var result = _repository.ListData();

        // Assert
        var smfTypes = result.ToList();
        smfTypes.Should().NotBeNull();
        smfTypes.Count.Should().Be(2);
    }

    private static SmfType CreateTestModel()
        => SmfType.Default;
    private static SmfDto CreateTestDto()
        => SmfDto.FromModel(SmfType.Default); 
    private static ISmfKey CreateTestKey()
        => SmfType.Key("A"); 
}