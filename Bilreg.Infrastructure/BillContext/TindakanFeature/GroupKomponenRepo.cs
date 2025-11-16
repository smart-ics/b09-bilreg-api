using Bilreg.Application.BillContext.TindakanFeature;
using Bilreg.Domain.BillContext.TindakanFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Infrastructure.BillContext.TindakanFeature;

public class GroupKomponenRepo : IGroupKomponenRepo
{
    private readonly IGroupKomponenDal _groupKomponenDal;
    public GroupKomponenRepo(IGroupKomponenDal groupKomponenDal)
    {
        _groupKomponenDal = groupKomponenDal;
    }
    public void SaveChanges(GroupKomponenType model)
    {
        LoadEntity(model)
            .Match(
                onSome: _ => _groupKomponenDal.Update(GroupKomponenDto.FromModel(model)),
                onNone: () => _groupKomponenDal.Insert(GroupKomponenDto.FromModel(model)));
    }

    public MayBe<GroupKomponenType> LoadEntity(IGroupKomponenKey key)
    {   
        var dto = _groupKomponenDal.GetData(key);
        if (dto is null)
            return MayBe<GroupKomponenType>.None;
        var model = dto.ToModel();
        return MayBe.From(model);
    }

    public void DeleteEntity(IGroupKomponenKey key)
    {
        _groupKomponenDal.Delete(key);
    }

    public IEnumerable<GroupKomponenType> ListData()
    {
        var listDto = _groupKomponenDal.ListData()?.ToList() ?? [];
        var result = listDto.Select(x => x.ToModel()).ToList();
        return result;
    }
}

public class GroupKomponenRepoTests
{
    private readonly Mock<IGroupKomponenDal> _groupKomponenDalMock;
    private readonly GroupKomponenRepo _repository;

    public GroupKomponenRepoTests()
    {
        _groupKomponenDalMock = new Mock<IGroupKomponenDal>();
        _repository = new GroupKomponenRepo(_groupKomponenDalMock.Object);
    }

    [Fact]
    public void UT1_GivenExistingEntity_WhenSaveChanges_ThenUpdateIsCalled()
    {
        // Arrange
        var existingModel = CreateTestModel();
        _groupKomponenDalMock
            .Setup(x => x.GetData(It.IsAny<IGroupKomponenKey>()))
            .Returns(CreateTestDto());

        // Act
        _repository.SaveChanges(existingModel);

        // Assert
        _groupKomponenDalMock.Verify(x => x.Update(It.IsAny<GroupKomponenDto>()), Times.Once);
        _groupKomponenDalMock.Verify(x => x.Insert(It.IsAny<GroupKomponenDto>()), Times.Never);
    }

    [Fact]
    public void UT2_GivenNewEntity_WhenSaveChanges_ThenInsertIsCalled()
    {
        // Arrange
        var newModel = CreateTestModel();
        var key = CreateTestKey();
        _groupKomponenDalMock
            .Setup(x => x.GetData(key))
            .Returns((GroupKomponenDto)null!);

        // Act
        _repository.SaveChanges(newModel);

        // Assert
        _groupKomponenDalMock.Verify(x => x.Insert(It.IsAny<GroupKomponenDto>()), Times.Once);
        _groupKomponenDalMock.Verify(x => x.Update(It.IsAny<GroupKomponenDto>()), Times.Never);
    }

    [Fact]
    public void UT3_GivenExistingEntity_WhenLoadEntity_ThenEntityIsReturned()
    {
        // Arrange
        var expectedDto = CreateTestDto();
        var key = CreateTestKey();
        _groupKomponenDalMock
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
        _groupKomponenDalMock
            .Setup(x => x.GetData(key))
            .Returns((GroupKomponenDto)null!);

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
        _groupKomponenDalMock.Verify(x => x.Delete(key), Times.Once);
    }

    [Fact]
    public void UT6_GivenEmptyList_WhenListData_ThenEmptyListIsReturned()
    {
        // Arrange
        _groupKomponenDalMock
            .Setup(x => x.ListData())
            .Returns((IEnumerable<GroupKomponenDto>)null!);

        // Act
        var result = _repository.ListData();

        // Assert
        var groupKomponenTypes = result.ToList();
        groupKomponenTypes.Should().NotBeNull();
        groupKomponenTypes.Should().BeEmpty();
    }

    [Fact]
    public void UT7_GivenListWithItems_WhenListData_ThenListWithModelsIsReturned()
    {
        // Arrange
        var dtos = new List<GroupKomponenDto> { CreateTestDto(), CreateTestDto() };
        _groupKomponenDalMock
            .Setup(x => x.ListData())
            .Returns(dtos);

        // Act
        var result = _repository.ListData();

        // Assert
        var groupKomponenTypes = result.ToList();
        groupKomponenTypes.Should().NotBeNull();
        groupKomponenTypes.Count.Should().Be(2);
    }

    private static GroupKomponenType CreateTestModel()
        => GroupKomponenType.Default;
    private static GroupKomponenDto CreateTestDto()
        => GroupKomponenDto.FromModel(GroupKomponenType.Default); 
    private static IGroupKomponenKey CreateTestKey()
        => GroupKomponenType.Key("A"); 
}