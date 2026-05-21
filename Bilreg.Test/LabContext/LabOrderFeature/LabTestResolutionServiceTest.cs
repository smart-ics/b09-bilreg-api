using Bilreg.Application.LabContext.LabComponentMasterFeature;
using Bilreg.Application.LabContext.LabOrderFeature;
using Bilreg.Application.LabContext.LabTestDefinitionFeature;
using Bilreg.Domain.LabContext.LabComponentMasterFeature;
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
    private readonly LabTestResolutionService _sut;

    public LabTestResolutionServiceTest()
    {
        _sut = new LabTestResolutionService(_definitionRepo.Object, _componentMasterRepo.Object);
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
            [new LabTestComponentModel(1, "MLC0001", "12-16", false, true)]);

        _definitionRepo.Setup(r => r.LoadActiveByTarifId("TR1")).Returns(MayBe.From(definition));
        _componentMasterRepo
            .Setup(r => r.LoadEntity(It.IsAny<ILabComponentMasterKey>()))
            .Returns(MayBe.From(new LabComponentMasterModel(
                "MLC0001", null, "HB", "Hemoglobin", LabResultTypeEnum.Numeric, "g/dL", true, true)));

        var lines = _sut.ResolveByTarifItems([new LabOrderTarifItemInput("TR1", null)]);

        lines.Should().HaveCount(1);
        lines[0].Item.TestDefinitionId.Should().Be("LTD0001");
        lines[0].Item.LabTestCode.Should().Be("HB");
        lines[0].Components.Should().HaveCount(1);
        lines[0].Components[0].ComponentId.Should().Be("MLC0001");
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
