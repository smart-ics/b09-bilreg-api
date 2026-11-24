using Bilreg.Application.LabContext.LabMasterFeature;
using Bilreg.Domain.LabContext.LabMasterFeature;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.LabContext.LabMasterFeature;

public class LabAntibioticUseCasesTest
{
    private static readonly DateTime FixedNow = new(2026, 11, 4, 10, 30, 0);
    private readonly Mock<ILabAntibioticRepo> _repoMock = new();

    [Fact]
    public async Task ListHandler_WhenSearchIsEmpty_CallsListDataAndReturnsAllAntibiotics()
    {
        var antibiotics = new List<LabAntibioticModel>
        {
            LabAntibioticModel.Create("101", "Penicillin G", "10 U", "<=11", "12 - 21", ">= 22", 10, "u1", FixedNow),
            LabAntibioticModel.Create("102", "Amikacin", "30 ug", "<=14", "15 - 16", ">= 17", 20, "u1", FixedNow)
        };
        _repoMock.Setup(r => r.ListData()).Returns(antibiotics);

        var handler = new LabAntibioticListHandler(_repoMock.Object);
        var result = (await handler.Handle(new LabAntibioticListQuery(), CancellationToken.None)).ToList();

        result.Should().HaveCount(2);

        result[0].AntibioticId.Should().Be("101");
        result[0].AntibioticName.Should().Be("Penicillin G");
        result[0].DiskConcentration.Should().Be("10 U");
        result[0].ResistenRange.Should().Be("<=11");
        result[0].IntermediateRange.Should().Be("12 - 21");
        result[0].SensitifRange.Should().Be(">= 22");
        result[0].DisplayOrder.Should().Be(10);

        result[1].AntibioticId.Should().Be("102");
        result[1].AntibioticName.Should().Be("Amikacin");
        result[1].DiskConcentration.Should().Be("30 ug");
        result[1].ResistenRange.Should().Be("<=14");
        result[1].IntermediateRange.Should().Be("15 - 16");
        result[1].SensitifRange.Should().Be(">= 17");
        result[1].DisplayOrder.Should().Be(20);

        _repoMock.Verify(r => r.ListData(), Times.Once);
        _repoMock.Verify(r => r.ListData(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ListHandler_WhenSearchIsWhitespace_CallsListDataAndReturnsAllAntibiotics()
    {
        var antibiotics = new List<LabAntibioticModel>
        {
            LabAntibioticModel.Create("101", "Penicillin G", "10 U", "<=11", "12 - 21", ">= 22", 10, "u1", FixedNow)
        };
        _repoMock.Setup(r => r.ListData()).Returns(antibiotics);

        var handler = new LabAntibioticListHandler(_repoMock.Object);
        var result = (await handler.Handle(new LabAntibioticListQuery("   "), CancellationToken.None)).ToList();

        result.Should().HaveCount(1);
        _repoMock.Verify(r => r.ListData(), Times.Once);
        _repoMock.Verify(r => r.ListData(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ListHandler_WithSearch_CallsListDataWithSearchFilterAndReturnsFiltered()
    {
        var antibiotics = new List<LabAntibioticModel>
        {
            LabAntibioticModel.Create("103", "Ciprofloxacin", "5 ug", "<=15", "16 - 20", ">= 21", 30, "u1", FixedNow)
        };
        _repoMock.Setup(r => r.ListData("Cipro")).Returns(antibiotics);

        var handler = new LabAntibioticListHandler(_repoMock.Object);
        var result = (await handler.Handle(new LabAntibioticListQuery("Cipro"), CancellationToken.None)).ToList();

        result.Should().HaveCount(1);
        result[0].AntibioticId.Should().Be("103");
        result[0].AntibioticName.Should().Be("Ciprofloxacin");
        result[0].DiskConcentration.Should().Be("5 ug");
        result[0].ResistenRange.Should().Be("<=15");
        result[0].IntermediateRange.Should().Be("16 - 20");
        result[0].SensitifRange.Should().Be(">= 21");
        result[0].DisplayOrder.Should().Be(30);

        _repoMock.Verify(r => r.ListData("Cipro"), Times.Once);
        _repoMock.Verify(r => r.ListData(), Times.Never);
    }

    [Fact]
    public async Task ListHandler_WhenRepoReturnsEmpty_ReturnsEmptyList()
    {
        _repoMock.Setup(r => r.ListData()).Returns(new List<LabAntibioticModel>());

        var handler = new LabAntibioticListHandler(_repoMock.Object);
        var result = (await handler.Handle(new LabAntibioticListQuery(), CancellationToken.None)).ToList();

        result.Should().BeEmpty();
    }
}
