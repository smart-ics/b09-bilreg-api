using Bilreg.Domain.LabContext.LabComponentMasterFeature;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Domain.LabContext.LabResultFeature;
using Bilreg.Domain.LabContext.LabTestDefinitionFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Xunit;

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
                VacutainerTypeEnum.Edta, true, "admin", new DateTime(2025, 5, 3, 10, 15, 30), components, Catalog));

        Assert.Contains("LAB_DUPLICATE_SEQUENCE", ex.Message);
    }

    [Fact]
    public void CreateNew_RejectsInactiveComponent()
    {
        var components = new[] { new LabTestComponentModel(1, "MLC000C", "", false, true) };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            LabTestDefinitionModel.CreateNew(
                "LTD0001", "TR1", "", "", "CBC", "Complete Blood Count", "Blood",
                VacutainerTypeEnum.Edta, true, "admin", new DateTime(2025, 5, 3, 10, 15, 30), components, Catalog));

        Assert.Contains("LAB_COMPONENT_INACTIVE", ex.Message);
    }

    [Fact]
    public void CreateNew_RejectsActiveWithoutComponents()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            LabTestDefinitionModel.CreateNew(
                "LTD0001", "TR1", "", "", "CBC", "Complete Blood Count", "Blood",
                VacutainerTypeEnum.Edta, true, "admin", new DateTime(2025, 5, 3, 10, 15, 30), [], Catalog));

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

    [Fact]
    public void CreateNew_WithClassificationFields_SetsProperties()
    {
        var components = new[] { new LabTestComponentModel(1, "MLC0001", "", false, true) };

        var model = LabTestDefinitionModel.CreateNew(
            "LTD0001", "TR1", "T01", "Tarif PA", "PA-BIO", "Biopsi Jaringan", "Jaringan",
            VacutainerTypeEnum.None, true, "admin", new DateTime(2026, 11, 4), components, Catalog,
            serviceType: LaboratoryServiceType.AnatomicalPathology,
            disciplineId: "DISC-PAH",
            testGroupId: "GRP-BIO");

        model.ServiceType.Should().Be(LaboratoryServiceType.AnatomicalPathology);
        model.DisciplineId.Should().Be("DISC-PAH");
        model.TestGroupId.Should().Be("GRP-BIO");
        model.VacutainerType.Should().Be(VacutainerTypeEnum.None);
    }

    [Fact]
    public void CreateNew_RejectsInvalidServiceType()
    {
        var components = new[] { new LabTestComponentModel(1, "MLC0001", "", false, true) };

        var act = () => LabTestDefinitionModel.CreateNew(
            "LTD0001", "TR1", "T01", "Tarif PA", "PA-BIO", "Biopsi", "Jaringan",
            VacutainerTypeEnum.None, false, "admin", new DateTime(2026, 11, 4), components, Catalog,
            serviceType: (LaboratoryServiceType)999);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*ServiceType is not valid*");
    }

    [Fact]
    public void ApplyUpdate_UpdatesClassificationFields()
    {
        var components = new[] { new LabTestComponentModel(1, "MLC0001", "", false, true) };

        var model = LabTestDefinitionModel.CreateNew(
            "LTD0001", "TR1", "T01", "Tarif Awal", "TEST", "Nama Awal", "Blood",
            VacutainerTypeEnum.Edta, true, "admin", new DateTime(2026, 11, 4), components, Catalog);

        model.ServiceType.Should().Be(LaboratoryServiceType.ClinicalPathology);
        model.DisciplineId.Should().BeEmpty();
        model.TestGroupId.Should().BeEmpty();

        var updated = model.ApplyUpdate(
            "TR1", "T01", "Tarif Update", "TEST", "Nama Update", "Sputum",
            VacutainerTypeEnum.None, true, components, "user2", new DateTime(2026, 11, 4, 12, 0, 0), Catalog,
            serviceType: LaboratoryServiceType.Microbiology,
            disciplineId: "DISC-MIK",
            testGroupId: "GRP-KLT");

        updated.ServiceType.Should().Be(LaboratoryServiceType.Microbiology);
        updated.DisciplineId.Should().Be("DISC-MIK");
        updated.TestGroupId.Should().Be("GRP-KLT");
        updated.VacutainerType.Should().Be(VacutainerTypeEnum.None);
    }

    [Fact]
    public void Default_HasDefaultClassificationValues()
    {
        var def = LabTestDefinitionModel.Default;

        def.ServiceType.Should().Be(LaboratoryServiceType.ClinicalPathology);
        def.DisciplineId.Should().BeEmpty();
        def.TestGroupId.Should().BeEmpty();
    }

    [Fact]
    public void Load_PreservesClassificationFields()
    {
        var components = new[] { new LabTestComponentModel(1, "MLC0001", "", false, true) };
        var audit = AuditTrailType.Create("admin", new DateTime(2026, 11, 4));

        var loaded = LabTestDefinitionModel.Load(
            "LTD0001", "TR1", "T01", "Tarif", "CODE", "Name", "Specimen",
            VacutainerTypeEnum.None, true, audit, components,
            serviceType: LaboratoryServiceType.Microbiology,
            disciplineId: "DISC-MIK",
            testGroupId: "GRP-MIC");

        loaded.ServiceType.Should().Be(LaboratoryServiceType.Microbiology);
        loaded.DisciplineId.Should().Be("DISC-MIK");
        loaded.TestGroupId.Should().Be("GRP-MIC");
    }
}
