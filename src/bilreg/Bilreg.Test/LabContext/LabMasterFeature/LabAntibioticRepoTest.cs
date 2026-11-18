using Bilreg.Application.LabContext.LabMasterFeature;
using Bilreg.Domain.LabContext.LabMasterFeature;
using Bilreg.Infrastructure.LabContext.LabMasterFeature;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.LabContext.LabMasterFeature;

public class LabAntibioticRepoTest
{
    private readonly Mock<ILabAntibioticDal> _dalMock = new();
    private readonly LabAntibioticRepo _sut;

    public LabAntibioticRepoTest()
    {
        _sut = new LabAntibioticRepo(_dalMock.Object);
    }

    [Fact]
    public void SaveChanges_WhenEntityDoesNotExist_CallsInsert()
    {
        var model = LabAntibioticModel.Create(
            "ANTI-01",
            "Amikacin",
            "30 ug",
            "<=14",
            "15-16",
            ">=17",
            10,
            "user1");
        _dalMock.Setup(d => d.GetData(It.IsAny<ILabAntibioticKey>())).Returns((LabAntibioticDto?)null);

        _sut.SaveChanges(model);

        _dalMock.Verify(d => d.Insert(It.Is<LabAntibioticDto>(x =>
            x.AntibioticId == "ANTI-01" &&
            x.AntibioticName == "Amikacin" &&
            x.DiskConcentration == "30 ug" &&
            x.ResistenRange == "<=14" &&
            x.IntermediateRange == "15-16" &&
            x.SensitifRange == ">=17" &&
            x.DisplayOrder == 10)), Times.Once);
        _dalMock.Verify(d => d.Update(It.IsAny<LabAntibioticDto>()), Times.Never);
    }

    [Fact]
    public void SaveChanges_WhenEntityExists_CallsUpdate()
    {
        var model = LabAntibioticModel.Create(
            "ANTI-01",
            "Amikacin",
            "30 ug",
            "<=14",
            "15-16",
            ">=17",
            10,
            "user1");
        var existingDto = LabAntibioticDto.FromModel(model);
        _dalMock.Setup(d => d.GetData(It.IsAny<ILabAntibioticKey>())).Returns(existingDto);

        _sut.SaveChanges(model);

        _dalMock.Verify(d => d.Update(It.Is<LabAntibioticDto>(x =>
            x.AntibioticId == "ANTI-01" &&
            x.AntibioticName == "Amikacin" &&
            x.DiskConcentration == "30 ug" &&
            x.ResistenRange == "<=14" &&
            x.IntermediateRange == "15-16" &&
            x.SensitifRange == ">=17" &&
            x.DisplayOrder == 10)), Times.Once);
        _dalMock.Verify(d => d.Insert(It.IsAny<LabAntibioticDto>()), Times.Never);
    }

    [Fact]
    public void LoadEntity_WhenFound_ReturnsSome()
    {
        var model = LabAntibioticModel.Create(
            "ANTI-01",
            "Amikacin",
            "30 ug",
            "<=14",
            "15-16",
            ">=17",
            10,
            "user1");
        var dto = LabAntibioticDto.FromModel(model);
        _dalMock.Setup(d => d.GetData(It.IsAny<ILabAntibioticKey>())).Returns(dto);

        var result = _sut.LoadEntity(LabAntibioticModel.Key("ANTI-01"));

        result.HasValue.Should().BeTrue();
        result.Value.AntibioticId.Should().Be("ANTI-01");
        result.Value.AntibioticName.Should().Be("Amikacin");
        result.Value.DiskConcentration.Should().Be("30 ug");
        result.Value.ResistenRange.Should().Be("<=14");
        result.Value.IntermediateRange.Should().Be("15-16");
        result.Value.SensitifRange.Should().Be(">=17");
        result.Value.DisplayOrder.Should().Be(10);
    }

    [Fact]
    public void LoadEntity_WhenNotFound_ReturnsNone()
    {
        _dalMock.Setup(d => d.GetData(It.IsAny<ILabAntibioticKey>())).Returns((LabAntibioticDto?)null);

        var result = _sut.LoadEntity(LabAntibioticModel.Key("ANTI-NOTFOUND"));

        result.HasValue.Should().BeFalse();
    }

    [Fact]
    public void ListData_ReturnsMappedModels()
    {
        var dtos = new List<LabAntibioticDto>
        {
            new("ANTI-01", "Amikacin", "30 ug", "<=14", "15-16", ">=17", 10, "u1", DateTime.Now, "", new DateTime(3000, 1, 1), "", new DateTime(3000, 1, 1)),
            new("ANTI-02", "Cefepime", "30 ug", "<=18", "19-24", ">=25", 20, "u1", DateTime.Now, "", new DateTime(3000, 1, 1), "", new DateTime(3000, 1, 1))
        };
        _dalMock.Setup(d => d.ListData()).Returns(dtos);

        var result = _sut.ListData().ToList();

        result.Should().HaveCount(2);
        result[0].AntibioticId.Should().Be("ANTI-01");
        result[0].AntibioticName.Should().Be("Amikacin");
        result[1].AntibioticId.Should().Be("ANTI-02");
        result[1].AntibioticName.Should().Be("Cefepime");
    }

    [Fact]
    public void ListData_WithSearch_ReturnsFilteredModels()
    {
        var dtos = new List<LabAntibioticDto>
        {
            new("ANTI-01", "Amikacin", "30 ug", "<=14", "15-16", ">=17", 10, "u1", DateTime.Now, "", new DateTime(3000, 1, 1), "", new DateTime(3000, 1, 1))
        };
        _dalMock.Setup(d => d.ListData("Amik")).Returns(dtos);

        var result = _sut.ListData("Amik").ToList();

        result.Should().HaveCount(1);
        result[0].AntibioticName.Should().Be("Amikacin");
    }

    [Fact]
    public void ListData_WhenDalReturnsNull_ReturnsEmptyList()
    {
        _dalMock.Setup(d => d.ListData()).Returns((IEnumerable<LabAntibioticDto>?)null!);

        var result = _sut.ListData().ToList();

        result.Should().BeEmpty();
    }

    [Fact]
    public void ListData_WithSearch_WhenDalReturnsNull_ReturnsEmptyList()
    {
        _dalMock.Setup(d => d.ListData("Amik")).Returns((IEnumerable<LabAntibioticDto>?)null!);

        var result = _sut.ListData("Amik").ToList();

        result.Should().BeEmpty();
    }
}
