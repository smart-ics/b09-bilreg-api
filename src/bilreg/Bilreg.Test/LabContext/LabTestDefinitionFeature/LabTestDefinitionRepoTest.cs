using Bilreg.Application.LabContext.LabTestDefinitionFeature;
using Bilreg.Domain.LabContext.LabComponentMasterFeature;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Domain.LabContext.LabResultFeature;
using Bilreg.Domain.LabContext.LabTestDefinitionFeature;
using Bilreg.Infrastructure.LabContext.LabTestDefinitionFeature;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.LabContext.LabTestDefinitionFeature;

public class LabTestDefinitionRepoTest
{
    private readonly Mock<ILabTestDefinitionDal> _definitionDalMock = new();
    private readonly Mock<ILabTestComponentDal> _componentDalMock = new();
    private readonly LabTestDefinitionRepo _sut;

    public LabTestDefinitionRepoTest()
    {
        _sut = new LabTestDefinitionRepo(_definitionDalMock.Object, _componentDalMock.Object);
    }

    private static LabTestDefinitionModel CreateSampleModel(
        string id = "LTD0001",
        LaboratoryServiceType serviceType = LaboratoryServiceType.AnatomicalPathology,
        string disciplineId = "DISC-PAH",
        string testGroupId = "GRP-BIO") =>
        LabTestDefinitionModel.CreateNew(
            id, "TR1", "T01", "Tarif PA", "PA-BIO", "Biopsi", "Jaringan",
            VacutainerTypeEnum.None, true, "admin", new DateTime(2026, 11, 4),
            [new LabTestComponentModel(1, "MLC0001", "", false, true)],
            new Dictionary<string, LabComponentMasterModel>
            {
                ["MLC0001"] = new("MLC0001", null, "BIO", "Biopsi", LabResultTypeEnum.Narrative, "", true, true)
            },
            serviceType: serviceType,
            disciplineId: disciplineId,
            testGroupId: testGroupId);

    [Fact]
    public void SaveChanges_WhenNew_MapsClassificationFieldsToDalInsert()
    {
        var model = CreateSampleModel(
            serviceType: LaboratoryServiceType.Microbiology,
            disciplineId: "DISC-MIK",
            testGroupId: "GRP-MIC");

        _definitionDalMock.Setup(d => d.GetData(It.IsAny<ILabTestDefinitionKey>())).Returns((LabTestDefinitionDto?)null);

        _sut.SaveChanges(model);

        _definitionDalMock.Verify(d => d.Insert(It.Is<LabTestDefinitionDto>(dto =>
            dto.ServiceType == 3 &&
            dto.DisciplineId == "DISC-MIK" &&
            dto.TestGroupId == "GRP-MIC")), Times.Once);
    }

    [Fact]
    public void SaveChanges_WhenExisting_MapsClassificationFieldsToDalUpdate()
    {
        var model = CreateSampleModel(
            serviceType: LaboratoryServiceType.AnatomicalPathology,
            disciplineId: "DISC-PAH",
            testGroupId: "GRP-BIO");

        var existingDto = LabTestDefinitionDto.FromModel(model);
        _definitionDalMock.Setup(d => d.GetData(It.IsAny<ILabTestDefinitionKey>())).Returns(existingDto);

        _sut.SaveChanges(model);

        _definitionDalMock.Verify(d => d.Update(It.Is<LabTestDefinitionDto>(dto =>
            dto.ServiceType == 2 &&
            dto.DisciplineId == "DISC-PAH" &&
            dto.TestGroupId == "GRP-BIO")), Times.Once);
    }

    [Fact]
    public void LoadEntity_WhenFound_RestoresClassificationFields()
    {
        var model = CreateSampleModel();
        var dto = LabTestDefinitionDto.FromModel(model);

        _definitionDalMock.Setup(d => d.GetData(It.IsAny<ILabTestDefinitionKey>())).Returns(dto);
        _componentDalMock.Setup(c => c.ListData(It.IsAny<ILabTestDefinitionKey>())).Returns([]);

        var result = _sut.LoadEntity(LabTestDefinitionModel.Key("LTD0001"));

        result.HasValue.Should().BeTrue();
        result.Value.ServiceType.Should().Be(LaboratoryServiceType.AnatomicalPathology);
        result.Value.DisciplineId.Should().Be("DISC-PAH");
        result.Value.TestGroupId.Should().Be("GRP-BIO");
    }

    [Fact]
    public void ListProjected_MapsClassificationFieldsToResponses()
    {
        var viewDtos = new List<LabTestDefinitionViewDto>
        {
            new("LTD0001", "TR1", "T01", "Tarif PA", "PA-BIO", "Biopsi", "Jaringan", 0, true, 1, 100000m, 2, "DISC-PAH", "GRP-BIO"),
            new("LTD0002", "TR2", "T02", "Tarif MIK", "MIC-01", "Kultur", "Urin", 0, true, 1, 150000m, 3, "DISC-MIK", "GRP-MIC")
        };

        _definitionDalMock.Setup(d => d.ListProjected(It.IsAny<LabTestDefinitionListFilter>())).Returns(viewDtos);

        var list = _sut.ListProjected(new LabTestDefinitionListFilter()).ToList();

        list.Should().HaveCount(2);
        list[0].ServiceType.Should().Be(2);
        list[0].DisciplineId.Should().Be("DISC-PAH");
        list[0].TestGroupId.Should().Be("GRP-BIO");

        list[1].ServiceType.Should().Be(3);
        list[1].DisciplineId.Should().Be("DISC-MIK");
        list[1].TestGroupId.Should().Be("GRP-MIC");
    }
}
