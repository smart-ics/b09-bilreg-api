using Bilreg.Application.BillContext.TindakanFeature;
using Bilreg.Domain.BillContext.TindakanFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Infrastructure.BillContext.TindakanFeature;

public class TipeTarifRepo : ITipeTarifRepo
{
    private readonly ITipeTarifDal _tipeTarifDal;
    public TipeTarifRepo(ITipeTarifDal tipeTarifDal)
    {
        _tipeTarifDal = tipeTarifDal;
    }
    public void SaveChanges(TipeTarifType model)
    {
        LoadEntity(model)
            .Match(
                onSome: _ => _tipeTarifDal.Update(TipeTarifDto.FromModel(model)),
                onNone: () => _tipeTarifDal.Insert(TipeTarifDto.FromModel(model)));
    }

    public MayBe<TipeTarifType> LoadEntity(ITipeTarifKey key)
    {   
        var dto = _tipeTarifDal.GetData(key);
        if (dto is null)
            return MayBe<TipeTarifType>.None;
        var model = dto.ToModel();
        return MayBe.From(model);
    }

    public void DeleteEntity(ITipeTarifKey key)
    {
        _tipeTarifDal.Delete(key);
    }

    public IEnumerable<TipeTarifType> ListData()
    {
        var listDto = _tipeTarifDal.ListData()?.ToList() ?? [];
        var result = listDto.Select(x => x.ToModel()).ToList();
        return result;
    }
}

//  resharper disable inconsistentnaming
public record TipeTarifDto(string fs_kd_tarif_tipe, string fs_nm_tarif_tipe, bool fb_aktif, decimal fn_no_urut)
{
    public static TipeTarifDto FromModel(TipeTarifType model)
    {
        var result =  new TipeTarifDto(model.TipeTarifId, model.TipeTarifName, model.IsAktif, model.NoUrut);
        return result;
    }

    public TipeTarifType ToModel()
    {
        var result = new  TipeTarifType(fs_kd_tarif_tipe, fs_nm_tarif_tipe, fb_aktif, (int)fn_no_urut);
        return result;
    }
}

public class TipeTarifRepoTests
{
    private readonly Mock<ITipeTarifDal> _tipeTarifDalMock;
    private readonly TipeTarifRepo _repository;

    public TipeTarifRepoTests()
    {
        _tipeTarifDalMock = new Mock<ITipeTarifDal>();
        _repository = new TipeTarifRepo(_tipeTarifDalMock.Object);
    }

    [Fact]
    public void UT1_GivenExistingEntity_WhenSaveChanges_ThenUpdateIsCalled()
    {
        // Arrange
        var existingModel = CreateTestModel();
        _tipeTarifDalMock
            .Setup(x => x.GetData(It.IsAny<ITipeTarifKey>()))
            .Returns(CreateTestDto());

        // Act
        _repository.SaveChanges(existingModel);

        // Assert
        _tipeTarifDalMock.Verify(x => x.Update(It.IsAny<TipeTarifDto>()), Times.Once);
        _tipeTarifDalMock.Verify(x => x.Insert(It.IsAny<TipeTarifDto>()), Times.Never);
    }

    [Fact]
    public void UT2_GivenNewEntity_WhenSaveChanges_ThenInsertIsCalled()
    {
        // Arrange
        var newModel = CreateTestModel();
        var key = CreateTestKey();
        _tipeTarifDalMock
            .Setup(x => x.GetData(key))
            .Returns((TipeTarifDto)null!);

        // Act
        _repository.SaveChanges(newModel);

        // Assert
        _tipeTarifDalMock.Verify(x => x.Insert(It.IsAny<TipeTarifDto>()), Times.Once);
        _tipeTarifDalMock.Verify(x => x.Update(It.IsAny<TipeTarifDto>()), Times.Never);
    }

    [Fact]
    public void UT3_GivenExistingEntity_WhenLoadEntity_ThenEntityIsReturned()
    {
        // Arrange
        var expectedDto = CreateTestDto();
        var key = CreateTestKey();
        _tipeTarifDalMock
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
        _tipeTarifDalMock
            .Setup(x => x.GetData(key))
            .Returns((TipeTarifDto)null!);

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
        _tipeTarifDalMock.Verify(x => x.Delete(key), Times.Once);
    }

    [Fact]
    public void UT6_GivenEmptyList_WhenListData_ThenEmptyListIsReturned()
    {
        // Arrange
        _tipeTarifDalMock
            .Setup(x => x.ListData())
            .Returns((IEnumerable<TipeTarifDto>)null!);

        // Act
        var result = _repository.ListData();

        // Assert
        var tipeTarifTypes = result.ToList();
        tipeTarifTypes.Should().NotBeNull();
        tipeTarifTypes.Should().BeEmpty();
    }

    [Fact]
    public void UT7_GivenListWithItems_WhenListData_ThenListWithModelsIsReturned()
    {
        // Arrange
        var dtos = new List<TipeTarifDto> { CreateTestDto(), CreateTestDto() };
        _tipeTarifDalMock
            .Setup(x => x.ListData())
            .Returns(dtos);

        // Act
        var result = _repository.ListData();

        // Assert
        var tipeTarifTypes = result.ToList();
        tipeTarifTypes.Should().NotBeNull();
        tipeTarifTypes.Count.Should().Be(2);
    }

    private static TipeTarifType CreateTestModel()
        => TipeTarifType.Default;
    private static TipeTarifDto CreateTestDto()
        => TipeTarifDto.FromModel(TipeTarifType.Default); 
    private static ITipeTarifKey CreateTestKey()
        => TipeTarifType.Key("A"); 
}