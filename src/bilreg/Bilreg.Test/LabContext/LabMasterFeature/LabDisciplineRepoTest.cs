using Bilreg.Application.LabContext.LabMasterFeature;
using Bilreg.Domain.LabContext.LabMasterFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Infrastructure.LabContext.LabMasterFeature;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.LabContext.LabMasterFeature;

public class LabDisciplineRepoTest
{
    private readonly Mock<ILabDisciplineDal> _dalMock = new();
    private readonly LabDisciplineRepo _sut;

    public LabDisciplineRepoTest()
    {
        _sut = new LabDisciplineRepo(_dalMock.Object);
    }

    [Fact]
    public void SaveChanges_WhenEntityDoesNotExist_CallsInsert()
    {
        var model = LabDisciplineModel.Create("DISC-HEM", "Hematologi", "user1");
        _dalMock.Setup(d => d.GetData(It.IsAny<ILabDisciplineKey>())).Returns((LabDisciplineDto?)null);

        _sut.SaveChanges(model);

        _dalMock.Verify(d => d.Insert(It.Is<LabDisciplineDto>(x => x.DisciplineId == "DISC-HEM" && x.DisciplineName == "Hematologi")), Times.Once);
        _dalMock.Verify(d => d.Update(It.IsAny<LabDisciplineDto>()), Times.Never);
    }

    [Fact]
    public void SaveChanges_WhenEntityExists_CallsUpdate()
    {
        var model = LabDisciplineModel.Create("DISC-HEM", "Hematologi", "user1");
        var existingDto = LabDisciplineDto.FromModel(model);
        _dalMock.Setup(d => d.GetData(It.IsAny<ILabDisciplineKey>())).Returns(existingDto);

        _sut.SaveChanges(model);

        _dalMock.Verify(d => d.Update(It.Is<LabDisciplineDto>(x => x.DisciplineId == "DISC-HEM" && x.DisciplineName == "Hematologi")), Times.Once);
        _dalMock.Verify(d => d.Insert(It.IsAny<LabDisciplineDto>()), Times.Never);
    }

    [Fact]
    public void LoadEntity_WhenFound_ReturnsSome()
    {
        var model = LabDisciplineModel.Create("DISC-HEM", "Hematologi", "user1");
        var dto = LabDisciplineDto.FromModel(model);
        _dalMock.Setup(d => d.GetData(It.IsAny<ILabDisciplineKey>())).Returns(dto);

        var result = _sut.LoadEntity(LabDisciplineModel.Key("DISC-HEM"));

        result.HasValue.Should().BeTrue();
        result.Value.DisciplineId.Should().Be("DISC-HEM");
        result.Value.DisciplineName.Should().Be("Hematologi");
    }

    [Fact]
    public void LoadEntity_WhenNotFound_ReturnsNone()
    {
        _dalMock.Setup(d => d.GetData(It.IsAny<ILabDisciplineKey>())).Returns((LabDisciplineDto?)null);

        var result = _sut.LoadEntity(LabDisciplineModel.Key("DISC-NOTFOUND"));

        result.HasValue.Should().BeFalse();
    }

    [Fact]
    public void ListData_ReturnsMappedModels()
    {
        var dtos = new List<LabDisciplineDto>
        {
            new("DISC-HEM", "Hematologi", "u1", DateTime.Now, "", new DateTime(3000, 1, 1), "", new DateTime(3000, 1, 1)),
            new("DISC-KIM", "Kimia Klinik", "u1", DateTime.Now, "", new DateTime(3000, 1, 1), "", new DateTime(3000, 1, 1))
        };
        _dalMock.Setup(d => d.ListData()).Returns(dtos);

        var result = _sut.ListData().ToList();

        result.Should().HaveCount(2);
        result[0].DisciplineId.Should().Be("DISC-HEM");
        result[1].DisciplineId.Should().Be("DISC-KIM");
    }

    [Fact]
    public void ListData_WithSearch_ReturnsFilteredModels()
    {
        var dtos = new List<LabDisciplineDto>
        {
            new("DISC-HEM", "Hematologi", "u1", DateTime.Now, "", new DateTime(3000, 1, 1), "", new DateTime(3000, 1, 1))
        };
        _dalMock.Setup(d => d.ListData("Hem")).Returns(dtos);

        var result = _sut.ListData("Hem").ToList();

        result.Should().HaveCount(1);
        result[0].DisciplineName.Should().Be("Hematologi");
    }
}
