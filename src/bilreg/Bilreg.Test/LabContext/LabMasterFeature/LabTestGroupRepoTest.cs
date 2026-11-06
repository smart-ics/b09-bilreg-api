using Bilreg.Application.LabContext.LabMasterFeature;
using Bilreg.Domain.LabContext.LabMasterFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Infrastructure.LabContext.LabMasterFeature;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.LabContext.LabMasterFeature;

public class LabTestGroupRepoTest
{
    private readonly Mock<ILabTestGroupDal> _dalMock = new();
    private readonly LabTestGroupRepo _sut;

    public LabTestGroupRepoTest()
    {
        _sut = new LabTestGroupRepo(_dalMock.Object);
    }

    [Fact]
    public void SaveChanges_WhenEntityDoesNotExist_CallsInsert()
    {
        var model = LabTestGroupModel.Create("GRP-CBC", "Darah Lengkap", "user1");
        _dalMock.Setup(d => d.GetData(It.IsAny<ILabTestGroupKey>())).Returns((LabTestGroupDto?)null);

        _sut.SaveChanges(model);

        _dalMock.Verify(d => d.Insert(It.Is<LabTestGroupDto>(x => x.TestGroupId == "GRP-CBC" && x.TestGroupName == "Darah Lengkap")), Times.Once);
        _dalMock.Verify(d => d.Update(It.IsAny<LabTestGroupDto>()), Times.Never);
    }

    [Fact]
    public void SaveChanges_WhenEntityExists_CallsUpdate()
    {
        var model = LabTestGroupModel.Create("GRP-CBC", "Darah Lengkap", "user1");
        var existingDto = LabTestGroupDto.FromModel(model);
        _dalMock.Setup(d => d.GetData(It.IsAny<ILabTestGroupKey>())).Returns(existingDto);

        _sut.SaveChanges(model);

        _dalMock.Verify(d => d.Update(It.Is<LabTestGroupDto>(x => x.TestGroupId == "GRP-CBC" && x.TestGroupName == "Darah Lengkap")), Times.Once);
        _dalMock.Verify(d => d.Insert(It.IsAny<LabTestGroupDto>()), Times.Never);
    }

    [Fact]
    public void LoadEntity_WhenFound_ReturnsSome()
    {
        var model = LabTestGroupModel.Create("GRP-CBC", "Darah Lengkap", "user1");
        var dto = LabTestGroupDto.FromModel(model);
        _dalMock.Setup(d => d.GetData(It.IsAny<ILabTestGroupKey>())).Returns(dto);

        var result = _sut.LoadEntity(LabTestGroupModel.Key("GRP-CBC"));

        result.HasValue.Should().BeTrue();
        result.Value.TestGroupId.Should().Be("GRP-CBC");
        result.Value.TestGroupName.Should().Be("Darah Lengkap");
    }

    [Fact]
    public void LoadEntity_WhenNotFound_ReturnsNone()
    {
        _dalMock.Setup(d => d.GetData(It.IsAny<ILabTestGroupKey>())).Returns((LabTestGroupDto?)null);

        var result = _sut.LoadEntity(LabTestGroupModel.Key("GRP-NOTFOUND"));

        result.HasValue.Should().BeFalse();
    }

    [Fact]
    public void ListData_ReturnsMappedModels()
    {
        var dtos = new List<LabTestGroupDto>
        {
            new("GRP-CBC", "Darah Lengkap", "u1", DateTime.Now, "", new DateTime(3000, 1, 1), "", new DateTime(3000, 1, 1)),
            new("GRP-LIP", "Profil Lipid", "u1", DateTime.Now, "", new DateTime(3000, 1, 1), "", new DateTime(3000, 1, 1))
        };
        _dalMock.Setup(d => d.ListData()).Returns(dtos);

        var result = _sut.ListData().ToList();

        result.Should().HaveCount(2);
        result[0].TestGroupId.Should().Be("GRP-CBC");
        result[1].TestGroupId.Should().Be("GRP-LIP");
    }

    [Fact]
    public void ListData_WithSearch_ReturnsFilteredModels()
    {
        var dtos = new List<LabTestGroupDto>
        {
            new("GRP-CBC", "Darah Lengkap", "u1", DateTime.Now, "", new DateTime(3000, 1, 1), "", new DateTime(3000, 1, 1))
        };
        _dalMock.Setup(d => d.ListData("CBC")).Returns(dtos);

        var result = _sut.ListData("CBC").ToList();

        result.Should().HaveCount(1);
        result[0].TestGroupName.Should().Be("Darah Lengkap");
    }
}
