using Bilreg.Application.BillContext.TindakanFeature;
using Bilreg.Domain.BillContext.TindakanFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Infrastructure.BillContext.TindakanFeature;

public class JenisTarifRepo : IJenisTarifRepo
{
    private readonly IJenisTarifDal _jenisTarifDal;
    public JenisTarifRepo(IJenisTarifDal jenisTarifDal)
    {
        _jenisTarifDal = jenisTarifDal;
    }
    public void SaveChanges(JenisTarifType model)
    {
        LoadEntity(model)
            .Match(
                onSome: _ => _jenisTarifDal.Update(JenisTarifDto.FromModel(model)),
                onNone: () => _jenisTarifDal.Insert(JenisTarifDto.FromModel(model)));
    }

    public MayBe<JenisTarifType> LoadEntity(IJenisTarifKey key)
    {   
        var dto = _jenisTarifDal.GetData(key);
        if (dto is null)
            return MayBe<JenisTarifType>.None;
        var model = dto.ToModel();
        return MayBe.From(model);
    }

    public void DeleteEntity(IJenisTarifKey key)
    {
        _jenisTarifDal.Delete(key);
    }

    public IEnumerable<JenisTarifType> ListData()
    {
        var listDto = _jenisTarifDal.ListData()?.ToList() ?? [];
        var result = listDto.Select(x => x.ToModel()).ToList();
        return result;
    }
}


public class JenisTarifRepoTests
{
    private readonly Mock<IJenisTarifDal> _jenisTarifDalMock;
    private readonly JenisTarifRepo _repository;

    public JenisTarifRepoTests()
    {
        _jenisTarifDalMock = new Mock<IJenisTarifDal>();
        _repository = new JenisTarifRepo(_jenisTarifDalMock.Object);
    }

    [Fact]
    public void UT1_GivenExistingEntity_WhenSaveChanges_ThenUpdateIsCalled()
    {
        // Arrange
        var existingModel = CreateTestModel();
        _jenisTarifDalMock
            .Setup(x => x.GetData(It.IsAny<IJenisTarifKey>()))
            .Returns(CreateTestDto());

        // Act
        _repository.SaveChanges(existingModel);

        // Assert
        _jenisTarifDalMock.Verify(x => x.Update(It.IsAny<JenisTarifDto>()), Times.Once);
        _jenisTarifDalMock.Verify(x => x.Insert(It.IsAny<JenisTarifDto>()), Times.Never);
    }

    [Fact]
    public void UT2_GivenNewEntity_WhenSaveChanges_ThenInsertIsCalled()
    {
        // Arrange
        var newModel = CreateTestModel();
        var key = CreateTestKey();
        _jenisTarifDalMock
            .Setup(x => x.GetData(key))
            .Returns((JenisTarifDto)null!);

        // Act
        _repository.SaveChanges(newModel);

        // Assert
        _jenisTarifDalMock.Verify(x => x.Insert(It.IsAny<JenisTarifDto>()), Times.Once);
        _jenisTarifDalMock.Verify(x => x.Update(It.IsAny<JenisTarifDto>()), Times.Never);
    }

    [Fact]
    public void UT3_GivenExistingEntity_WhenLoadEntity_ThenEntityIsReturned()
    {
        // Arrange
        var expectedDto = CreateTestDto();
        var key = CreateTestKey();
        _jenisTarifDalMock
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
        _jenisTarifDalMock
            .Setup(x => x.GetData(key))
            .Returns((JenisTarifDto)null!);

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
        _jenisTarifDalMock.Verify(x => x.Delete(key), Times.Once);
    }

    [Fact]
    public void UT6_GivenEmptyList_WhenListData_ThenEmptyListIsReturned()
    {
        // Arrange
        _jenisTarifDalMock
            .Setup(x => x.ListData())
            .Returns((IEnumerable<JenisTarifDto>)null!);

        // Act
        var result = _repository.ListData();

        // Assert
        var jenisTarifTypes = result.ToList();
        jenisTarifTypes.Should().NotBeNull();
        jenisTarifTypes.Should().BeEmpty();
    }

    [Fact]
    public void UT7_GivenListWithItems_WhenListData_ThenListWithModelsIsReturned()
    {
        // Arrange
        var dtos = new List<JenisTarifDto> { CreateTestDto(), CreateTestDto() };
        _jenisTarifDalMock
            .Setup(x => x.ListData())
            .Returns(dtos);

        // Act
        var result = _repository.ListData();

        // Assert
        var jenisTarifTypes = result.ToList();
        jenisTarifTypes.Should().NotBeNull();
        jenisTarifTypes.Count.Should().Be(2);
    }

    private static JenisTarifType CreateTestModel()
        => JenisTarifType.Default;
    private static JenisTarifDto CreateTestDto()
        => JenisTarifDto.FromModel(JenisTarifType.Default); 
    private static IJenisTarifKey CreateTestKey()
        => JenisTarifType.Key("A"); 
}
