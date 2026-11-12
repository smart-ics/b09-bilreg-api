using Bilreg.Application.LabContext.LabResultFeature;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Domain.LabContext.LabResultFeature;
using Bilreg.Infrastructure.LabContext.LabResultFeature;
using Bilreg.Test.LabContext.LabOrderFeature;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.LabContext.LabResultFeature;

public class LabResultScaffoldServiceTest
{
    private readonly LabResultScaffoldService _sut = new();

    [Fact]
    public void BuildFromOrder_UsesOrderComponentSnapshots()
    {
        var component = LabOrderTestSupport.SampleComponent("MLC0002", "WBC", "Leukosit");
        var order = LabOrderTestSupport.CreateEmrOrder(lines:
            [LabOrderTestSupport.ResolvedLine(components: [component])]);

        var scaffold = _sut.BuildFromOrder(order);

        scaffold.Should().ContainSingle();
        scaffold[0].ComponentId.Should().Be("MLC0002");
        scaffold[0].ComponentCode.Should().Be("WBC");
        scaffold[0].TestDefinitionId.Should().Be("LTD0001");
    }

    [Fact]
    public void BuildCaptures_RejectsUnknownComponent()
    {
        var order = LabOrderTestSupport.CreateEmrOrder();
        var scaffold = _sut.BuildFromOrder(order);

        var act = () => _sut.BuildCaptures(scaffold, [new LabResultRecordValueDto("MLC9999", "1")]);

        act.Should().Throw<InvalidOperationException>().WithMessage("*LAB_RESULT_UNKNOWN_LINE*");
    }

    [Fact]
    public void BuildCaptures_RejectsExtraLine()
    {
        var order = LabOrderTestSupport.CreateEmrOrder();
        var scaffold = _sut.BuildFromOrder(order);

        var act = () => _sut.BuildCaptures(scaffold,
        [
            new LabResultRecordValueDto("MLC0001", "13"),
            new LabResultRecordValueDto("MLC0001", "14")
        ]);

        act.Should().Throw<InvalidOperationException>().WithMessage("*LAB_RESULT_EXTRA_LINE*");
    }

    [Fact]
    public void BuildCaptures_RejectsMissingMandatoryValue()
    {
        var order = LabOrderTestSupport.CreateEmrOrder();
        var scaffold = _sut.BuildFromOrder(order);

        var act = () => _sut.BuildCaptures(scaffold, [new LabResultRecordValueDto("MLC0001", "")]);

        act.Should().Throw<InvalidOperationException>().WithMessage("*LAB_MANDATORY_COMPONENT_MISSING*");
    }

    [Fact]
    public void BuildCaptures_AppliesValidValuesToFullScaffold()
    {
        var order = LabOrderTestSupport.CreateEmrOrder();
        var scaffold = _sut.BuildFromOrder(order);

        var captures = _sut.BuildCaptures(scaffold, [new LabResultRecordValueDto("MLC0001", "13.2")]);

        captures.Should().ContainSingle();
        captures[0].ComponentId.Should().Be("MLC0001");
        captures[0].NumericValue.Should().Be(13.2m);
        captures[0].ComponentName.Should().Be("Hemoglobin");
    }

    [Fact]
    public void BuildCaptures_ResolvesByComponentCodeWhenComponentIdMatchesCode()
    {
        var order = LabOrderTestSupport.CreateEmrOrder();
        var scaffold = _sut.BuildFromOrder(order);

        var captures = _sut.BuildCaptures(scaffold, [new LabResultRecordValueDto("HB", "14.5")]);

        captures.Should().ContainSingle();
        captures[0].ComponentId.Should().Be("MLC0001");
        captures[0].ComponentCode.Should().Be("HB");
        captures[0].NumericValue.Should().Be(14.5m);
    }

    [Fact]
    public void BuildFromOrder_ForAnatomicalPathology_SynthesizesPaScaffold()
    {
        var paItem = LabOrderItemModel.Create(
            "LTD004E", "LAB0216001", "PAP SMEAR", "TR4", "T-PAP", "PAP SMEAR",
            VacutainerTypeEnum.None, "Cytology", 1,
            serviceType: LaboratoryServiceType.AnatomicalPathology,
            disciplineId: "DISC-PAS");
        var paLine = LabOrderTestSupport.ResolvedLine(paItem, components: [LabOrderTestSupport.SampleComponent("MLC000C", "REMARK", "Remark", LabResultTypeEnum.Narrative)]);
        var order = LabOrderTestSupport.CreateEmrOrder(lines: [paLine]);

        var scaffold = _sut.BuildFromOrder(order);

        scaffold.Select(s => s.ComponentCode).Should().Contain([
            "PA_REG_NO",
            "PA_SLIDE_NO",
            "PA_SPECIMEN_SITE",
            "PA_MACROSCOPIC",
            "PA_SPECIMEN_ADEQUACY",
            "PA_MICROSCOPIC",
            "PA_CONCLUSION",
            "PA_RECOMMENDATION",
            "PA_SNOMED",
            "REMARK"
        ]);
    }

    [Fact]
    public void BuildFromOrder_ForMicrobiology_SynthesizesMicroScaffold()
    {
        var microItem = LabOrderItemModel.Create(
            "LTD003C", "LAB0116002", "SEKRET 1X", "TR3", "T-MIK", "SEKRET 1X",
            VacutainerTypeEnum.None, "Swab", 1,
            serviceType: LaboratoryServiceType.Microbiology,
            disciplineId: "DISC-MIK");
        var microLine = LabOrderTestSupport.ResolvedLine(microItem, components: [LabOrderTestSupport.SampleComponent("MLC00A2", "GRAM_STN", "Gram Stain Result", LabResultTypeEnum.Text)]);
        var order = LabOrderTestSupport.CreateEmrOrder(lines: [microLine]);

        var scaffold = _sut.BuildFromOrder(order);

        scaffold.Select(s => s.ComponentCode).Should().Contain([
            "MIC_DIRECT_SMEAR",
            "MIC_GROWTH_STATUS",
            "MIC_COLONY_COUNT",
            "MIC_ISOLATED_ORGANISM",
            "GRAM_STN"
        ]);
    }

    [Fact]
    public void BuildCaptures_WithExplicitItems_ForPa_CapturesSubmittedFieldsOnly()
    {
        var paItem = LabOrderItemModel.Create(
            "LTD004E", "LAB0216001", "PAP SMEAR", "TR4", "T-PAP", "PAP SMEAR",
            VacutainerTypeEnum.None, "Cytology", 1,
            serviceType: LaboratoryServiceType.AnatomicalPathology,
            disciplineId: "DISC-PAS");
        var paLine = LabOrderTestSupport.ResolvedLine(paItem, components: [LabOrderTestSupport.SampleComponent("MLC000C", "REMARK", "Remark", LabResultTypeEnum.Narrative)]);
        var order = LabOrderTestSupport.CreateEmrOrder(lines: [paLine]);
        var scaffold = _sut.BuildFromOrder(order);

        var explicitItems = new List<LabResultRecordItemInputDto>
        {
            new(TestId: "LTD004E", TestName: "PAP SMEAR", ComponentCode: "PA_REG_NO", ComponentName: "Nomor Registrasi PA", ResultType: 2, TextValue: "S.26.0052"),
            new(TestId: "LTD004E", TestName: "PAP SMEAR", ComponentCode: "PA_SLIDE_NO", ComponentName: "Nomor Sediaan / Slide", ResultType: 2, TextValue: "A1, A2, B1"),
            new(TestId: "LTD004E", TestName: "PAP SMEAR", ComponentCode: "PA_SPECIMEN_SITE", ComponentName: "Asal / Lokasi Jaringan", ResultType: 2, TextValue: "Darah"),
            new(TestId: "LTD004E", TestName: "PAP SMEAR", ComponentCode: "PA_SPECIMEN_ADEQUACY", ComponentName: "Kecukupan Sediaan (Adekuasi)", ResultType: 4, NarrativeValue: "Sediaan adekuat."),
            new(TestId: "LTD004E", TestName: "PAP SMEAR", ComponentCode: "PA_MICROSCOPIC", ComponentName: "Pemeriksaan Mikroskopis", ResultType: 4, NarrativeValue: "Sel epitel skuamosa berinti normal."),
            new(TestId: "LTD004E", TestName: "PAP SMEAR", ComponentCode: "PA_CONCLUSION", ComponentName: "Kesimpulan / Diagnosis Patologi", ResultType: 4, NarrativeValue: "FIBROADENOMA MAMMAE (FAM)."),
            new(TestId: "LTD004E", TestName: "PAP SMEAR", ComponentCode: "PA_SNOMED", ComponentName: "Kode Morfologi (ICD-O / SNOMED)", ResultType: 2, TextValue: "M-9010/0")
        };

        var captures = _sut.BuildCaptures(scaffold, [], explicitItems);

        captures.Should().HaveCount(7);
        captures.Select(c => c.ComponentCode).Should().Equal([
            "PA_REG_NO",
            "PA_SLIDE_NO",
            "PA_SPECIMEN_SITE",
            "PA_SPECIMEN_ADEQUACY",
            "PA_MICROSCOPIC",
            "PA_CONCLUSION",
            "PA_SNOMED"
        ]);
        captures.First(c => c.ComponentCode == "PA_REG_NO").TextValue.Should().Be("S.26.0052");
        captures.First(c => c.ComponentCode == "PA_SLIDE_NO").TextValue.Should().Be("A1, A2, B1");
        captures.First(c => c.ComponentCode == "PA_CONCLUSION").NarrativeValue.Should().Be("FIBROADENOMA MAMMAE (FAM).");
    }

    [Fact]
    public void BuildCaptures_WithExplicitItems_ForMicrobiology_SupportsDynamicAstRows()
    {
        var microItem = LabOrderItemModel.Create(
            "LTD003C", "LAB0116002", "SEKRET 1X", "TR3", "T-MIK", "SEKRET 1X",
            VacutainerTypeEnum.None, "Swab", 1,
            serviceType: LaboratoryServiceType.Microbiology,
            disciplineId: "DISC-MIK");
        var microLine = LabOrderTestSupport.ResolvedLine(microItem, components: [LabOrderTestSupport.SampleComponent("MLC00A2", "GRAM_STN", "Gram Stain Result", LabResultTypeEnum.Text)]);
        var order = LabOrderTestSupport.CreateEmrOrder(lines: [microLine]);
        var scaffold = _sut.BuildFromOrder(order);

        var explicitItems = new List<LabResultRecordItemInputDto>
        {
            new(TestId: "LTD003C", TestName: "SEKRET 1X", ComponentCode: "MIC_DIRECT_SMEAR", ComponentName: "Pemeriksaan Mikroskopis Langsung", ResultType: 2, TextValue: "Bacil gram negatif"),
            new(TestId: "LTD003C", TestName: "SEKRET 1X", ComponentCode: "MIC_GROWTH_STATUS", ComponentName: "Pertumbuhan Kuman", ResultType: 3, OptionValue: "Ada Pertumbuhan", NumericValue: 1),
            new(TestId: "LTD003C", TestName: "SEKRET 1X", ComponentCode: "AST_1", ComponentName: "AST: Amikacin", ResultType: 3, OptionValue: "S", TextValue: "MIC: <= 1"),
            new(TestId: "LTD003C", TestName: "SEKRET 1X", ComponentCode: "AST_2", ComponentName: "AST: Meropenem", ResultType: 3, OptionValue: "S", TextValue: "MIC: <= 0.25")
        };

        var captures = _sut.BuildCaptures(scaffold, [], explicitItems);

        captures.Should().HaveCount(4);
        captures.Select(c => c.ComponentCode).Should().Contain(["MIC_DIRECT_SMEAR", "MIC_GROWTH_STATUS", "AST_1", "AST_2"]);
        var ast1 = captures.First(c => c.ComponentCode == "AST_1");
        ast1.ComponentName.Should().Be("AST: Amikacin");
        ast1.OptionValue.Should().Be("S");
        ast1.TextValue.Should().Be("MIC: <= 1");
    }
}
