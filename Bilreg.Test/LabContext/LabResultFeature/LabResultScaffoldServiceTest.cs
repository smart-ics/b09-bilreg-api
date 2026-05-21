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
}
