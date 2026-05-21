using Bilreg.Domain.LabContext.LabComponentMasterFeature;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Domain.LabContext.LabResultFeature;
using Bilreg.Domain.LabContext.LabTestDefinitionFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Test.LabContext.LabTestDefinitionFeature;

public class LabTestDefinitionModelTest
{
    private static readonly IReadOnlyDictionary<string, LabComponentMasterModel> Catalog =
        new Dictionary<string, LabComponentMasterModel>
        {
            ["MLC0001"] = new("MLC0001", null, "HB", "Hemoglobin", LabResultTypeEnum.Numeric, "g/dL", true, true),
            ["MLC000C"] = new("MLC000C", null, "REMARK", "Catatan", LabResultTypeEnum.Narrative, "", true, false)
        };

    [Fact]
    public void CreateNew_RejectsDuplicateSequence()
    {
        var components = new[]
        {
            new LabTestComponentModel(1, "MLC0001", "", false, true),
            new LabTestComponentModel(1, "MLC0001", "", false, false)
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            LabTestDefinitionModel.CreateNew(
                "LTD0001", "TR1", "", "", "CBC", "Complete Blood Count", "Blood",
                VacutainerTypeEnum.Edta, true, "admin", components, Catalog));

        Assert.Contains("LAB_DUPLICATE_SEQUENCE", ex.Message);
    }

    [Fact]
    public void CreateNew_RejectsInactiveComponent()
    {
        var components = new[] { new LabTestComponentModel(1, "MLC000C", "", false, true) };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            LabTestDefinitionModel.CreateNew(
                "LTD0001", "TR1", "", "", "CBC", "Complete Blood Count", "Blood",
                VacutainerTypeEnum.Edta, true, "admin", components, Catalog));

        Assert.Contains("LAB_COMPONENT_INACTIVE", ex.Message);
    }

    [Fact]
    public void CreateNew_RejectsActiveWithoutComponents()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            LabTestDefinitionModel.CreateNew(
                "LTD0001", "TR1", "", "", "CBC", "Complete Blood Count", "Blood",
                VacutainerTypeEnum.Edta, true, "admin", [], Catalog));

        Assert.Contains("LAB_TEST_DEFINITION_EMPTY_COMPONENTS", ex.Message);
    }

    [Fact]
    public void LabMasterIdFormat_ValidatesLtdAndMlc()
    {
        Assert.True(LabMasterIdFormat.IsValidLtd("LTD0001"));
        Assert.True(LabMasterIdFormat.IsValidLtd("LTD00AF"));
        Assert.False(LabMasterIdFormat.IsValidLtd("LTD001"));
        Assert.True(LabMasterIdFormat.IsValidMlc("MLC000A"));
        Assert.Equal("LTD0002", LabMasterIdFormat.FormatLtd(2));
    }
}
