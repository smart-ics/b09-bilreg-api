using Bilreg.Application.LabContext.LabComponentMasterFeature;
using Bilreg.Application.LabContext.LabMasterFeature;
using Bilreg.Application.LabContext.LabOrderFeature;
using Bilreg.Application.LabContext.LabTestDefinitionFeature;
using Bilreg.Domain.LabContext.LabComponentMasterFeature;
using Bilreg.Domain.LabContext.LabMasterFeature;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Domain.LabContext.LabResultFeature;
using Bilreg.Domain.LabContext.LabTestDefinitionFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Infrastructure.LabContext.LabOrderFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.LabContext.LabOrderFeature;

public class LabTestResolutionServiceTest
{
    private readonly Mock<ILabTestDefinitionRepo> _definitionRepo = new();
    private readonly Mock<ILabComponentMasterRepo> _componentMasterRepo = new();
    private readonly Mock<ILabDisciplineRepo> _disciplineRepo = new();
    private readonly Mock<ILabTestGroupRepo> _testGroupRepo = new();
    private readonly LabTestResolutionService _sut;

    public LabTestResolutionServiceTest()
    {
        _sut = new LabTestResolutionService(
            _definitionRepo.Object,
            _componentMasterRepo.Object,
            _disciplineRepo.Object,
            _testGroupRepo.Object);
    }

    [Fact]
    public void ResolveByTarifItems_ActiveDefinition_ReturnsItemAndComponents()
    {
        var definition = new LabTestDefinitionModel(
            "LTD0001",
            "TR1",
            "T-HB",
            "Tarif HB",
            "HB",
            "Hemoglobin",
            "Blood",
            VacutainerTypeEnum.Edta,
            true,
            AuditTrailType.Create("U1", DateTime.Now),
            [new LabTestComponentModel(1, "MLC0001", "12-16", false, true)],
            LaboratoryServiceType.ClinicalPathology,
            "DISC-HEM",
            "GRP-RUT");

        _definitionRepo.Setup(r => r.LoadActiveByTarifId("TR1")).Returns(MayBe.From(definition));
        _componentMasterRepo
            .Setup(r => r.LoadEntity(It.IsAny<ILabComponentMasterKey>()))
            .Returns(MayBe.From(new LabComponentMasterModel(
                "MLC0001", null, "HB", "Hemoglobin", LabResultTypeEnum.Numeric, "g/dL", true, true)));
        _disciplineRepo
            .Setup(r => r.LoadEntity(It.Is<ILabDisciplineKey>(k => k.DisciplineId == "DISC-HEM")))
            .Returns(MayBe.From(LabDisciplineModel.Create("DISC-HEM", "Hematologi", "U1")));
        _testGroupRepo
            .Setup(r => r.LoadEntity(It.Is<ILabTestGroupKey>(k => k.TestGroupId == "GRP-RUT")))
            .Returns(MayBe.From(LabTestGroupModel.Create("GRP-RUT", "Darah Rutin", "U1")));

        var lines = _sut.ResolveByTarifItems([new LabOrderTarifItemInput("TR1", null)]);

        lines.Should().HaveCount(1);
        lines[0].Item.TestDefinitionId.Should().Be("LTD0001");
        lines[0].Item.LabTestCode.Should().Be("HB");
        lines[0].Item.ServiceType.Should().Be(LaboratoryServiceType.ClinicalPathology);
        lines[0].Item.DisciplineId.Should().Be("DISC-HEM");
        lines[0].Item.DisciplineName.Should().Be("Hematologi");
        lines[0].Item.TestGroupId.Should().Be("GRP-RUT");
        lines[0].Item.TestGroupName.Should().Be("Darah Rutin");
        lines[0].Item.RequiredTubeCount.Should().Be(1);
        lines[0].Components.Should().HaveCount(1);
        lines[0].Components[0].ComponentId.Should().Be("MLC0001");
    }

    [Fact]
    public void ResolveByTarifItems_NonBloodVacutainerNone_SetsRequiredTubeCountZero()
    {
        var definition = new LabTestDefinitionModel(
            "LTD0002",
            "TR-PA",
            "T-PA",
            "Tarif Biopsi",
            "PA-BIO",
            "Biopsi Jaringan Kecil",
            "Jaringan",
            VacutainerTypeEnum.None,
            true,
            AuditTrailType.Create("U1", DateTime.Now),
            [new LabTestComponentModel(1, "MLC0001", "", false, true)],
            LaboratoryServiceType.AnatomicalPathology,
            "DISC-PAH",
            "GRP-BIO");

        _definitionRepo.Setup(r => r.LoadActiveByTarifId("TR-PA")).Returns(MayBe.From(definition));
        _componentMasterRepo
            .Setup(r => r.LoadEntity(It.IsAny<ILabComponentMasterKey>()))
            .Returns(MayBe.From(new LabComponentMasterModel(
                "MLC0001", null, "BIO", "Biopsi", LabResultTypeEnum.Narrative, "", true, true)));
        _disciplineRepo
            .Setup(r => r.LoadEntity(It.Is<ILabDisciplineKey>(k => k.DisciplineId == "DISC-PAH")))
            .Returns(MayBe.From(LabDisciplineModel.Create("DISC-PAH", "Histopatologi", "U1")));
        _testGroupRepo
            .Setup(r => r.LoadEntity(It.Is<ILabTestGroupKey>(k => k.TestGroupId == "GRP-BIO")))
            .Returns(MayBe.From(LabTestGroupModel.Create("GRP-BIO", "Biopsi Jaringan", "U1")));

        var lines = _sut.ResolveByTarifItems([new LabOrderTarifItemInput("TR-PA", null)]);

        lines.Should().HaveCount(1);
        lines[0].Item.RequiredTubeCount.Should().Be(0);
        lines[0].Item.ServiceType.Should().Be(LaboratoryServiceType.AnatomicalPathology);
        lines[0].Item.DisciplineId.Should().Be("DISC-PAH");
        lines[0].Item.DisciplineName.Should().Be("Histopatologi");
        lines[0].Item.TestGroupId.Should().Be("GRP-BIO");
        lines[0].Item.TestGroupName.Should().Be("Biopsi Jaringan");
    }

    [Fact]
    public void ResolveByTarifItems_WhenClassificationMasterNotFound_LeavesNamesEmpty()
    {
        var definition = new LabTestDefinitionModel(
            "LTD0003",
            "TR-MIC",
            "T-MIC",
            "Tarif Kultur",
            "MIC-KLT",
            "Kultur Urin",
            "Urin",
            VacutainerTypeEnum.None,
            true,
            AuditTrailType.Create("U1", DateTime.Now),
            [new LabTestComponentModel(1, "MLC0001", "", false, true)],
            LaboratoryServiceType.Microbiology,
            "DISC-UNKNOWN",
            "GRP-UNKNOWN");

        _definitionRepo.Setup(r => r.LoadActiveByTarifId("TR-MIC")).Returns(MayBe.From(definition));
        _componentMasterRepo
            .Setup(r => r.LoadEntity(It.IsAny<ILabComponentMasterKey>()))
            .Returns(MayBe.From(new LabComponentMasterModel(
                "MLC0001", null, "KLT", "Kultur", LabResultTypeEnum.Narrative, "", true, true)));
        _disciplineRepo
            .Setup(r => r.LoadEntity(It.IsAny<ILabDisciplineKey>()))
            .Returns(MayBe<LabDisciplineModel>.None);
        _testGroupRepo
            .Setup(r => r.LoadEntity(It.IsAny<ILabTestGroupKey>()))
            .Returns(MayBe<LabTestGroupModel>.None);

        var lines = _sut.ResolveByTarifItems([new LabOrderTarifItemInput("TR-MIC", null)]);

        lines.Should().HaveCount(1);
        lines[0].Item.RequiredTubeCount.Should().Be(0);
        lines[0].Item.ServiceType.Should().Be(LaboratoryServiceType.Microbiology);
        lines[0].Item.DisciplineId.Should().Be("DISC-UNKNOWN");
        lines[0].Item.DisciplineName.Should().BeEmpty();
        lines[0].Item.TestGroupId.Should().Be("GRP-UNKNOWN");
        lines[0].Item.TestGroupName.Should().BeEmpty();
    }

    [Fact]
    public void ResolveByTarifItems_NoDefinition_ThrowsNotFound()
    {
        _definitionRepo.Setup(r => r.LoadActiveByTarifId("TR-MISS")).Returns(MayBe<LabTestDefinitionModel>.None);
        _definitionRepo
            .Setup(r => r.ListData(It.IsAny<LabTestDefinitionListFilter>()))
            .Returns([]);

        var act = () => _sut.ResolveByTarifItems([new LabOrderTarifItemInput("TR-MISS", null)]);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*LAB_TEST_DEFINITION_NOT_FOUND*");
    }

    [Fact]
    public void ResolveByTarifItems_InactiveComponent_Throws()
    {
        var definition = new LabTestDefinitionModel(
            "LTD0001",
            "TR1",
            "T-HB",
            "Tarif HB",
            "HB",
            "Hemoglobin",
            "Blood",
            VacutainerTypeEnum.Edta,
            true,
            AuditTrailType.Create("U1", DateTime.Now),
            [new LabTestComponentModel(1, "MLC0001", "", false, true)]);

        _definitionRepo.Setup(r => r.LoadActiveByTarifId("TR1")).Returns(MayBe.From(definition));
        _componentMasterRepo
            .Setup(r => r.LoadEntity(It.IsAny<ILabComponentMasterKey>()))
            .Returns(MayBe.From(new LabComponentMasterModel(
                "MLC0001", null, "HB", "Hemoglobin", LabResultTypeEnum.Numeric, "g/dL", true, false)));

        var act = () => _sut.ResolveByTarifItems([new LabOrderTarifItemInput("TR1", null)]);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*LAB_COMPONENT_INACTIVE*");
    }
}
