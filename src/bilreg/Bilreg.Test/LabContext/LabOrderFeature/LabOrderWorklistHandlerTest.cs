using Bilreg.Application.LabContext.LabOrderFeature;
using Bilreg.Application.LabContext.LabOrderFeature.UseCases;
using Bilreg.Domain.LabContext.LabOrderFeature;
using FluentAssertions;
using Moq;

namespace Bilreg.Test.LabContext.LabOrderFeature;

public class LabOrderWorklistHandlerTest
{
    private readonly Mock<ILabOrderWorklistDal> _dal = new();
    private readonly LabOrderWorklistHandler _sut;

    public LabOrderWorklistHandlerTest()
    {
        _sut = new LabOrderWorklistHandler(_dal.Object);
    }

    [Fact]
    public async Task Handle_PassesFilterToDal_ReturnsDalResult()
    {
        var expected = new[]
        {
            new LabOrderWorklistView(
                "LBO000000001",
                "LAB00000001",
                (int)LabOrderStatusEnum.Ordered,
                1,
                "MR0001",
                "Pasien Tes",
                "L",
                36,
                1,
                0,
                0,
                new DateTime(2026, 5, 18, 10, 0, 0),
                "",
                ["Hemoglobin"])
        };

        LabOrderWorklistFilter? captured = null;
        _dal.Setup(d => d.List(It.IsAny<LabOrderWorklistFilter>()))
            .Callback<LabOrderWorklistFilter>(f => captured = f)
            .Returns(expected);

        var result = await _sut.Handle(
            new LabOrderWorklistQuery(
                LabOrderStatus: (int)LabOrderStatusEnum.Ordered,
                SearchTerm: "HB",
                Date1: new DateTime(2026, 5, 1),
                Date2: new DateTime(2026, 5, 31)),
            CancellationToken.None);

        result.Should().BeEquivalentTo(expected);
        captured.Should().NotBeNull();
        captured!.LabOrderStatus.Should().Be((int)LabOrderStatusEnum.Ordered);
        captured.SearchTerm.Should().Be("HB");
        captured.Date1.Should().Be(new DateTime(2026, 5, 1));
        captured.Date2.Should().Be(new DateTime(2026, 5, 31));
        _dal.Verify(d => d.List(It.IsAny<LabOrderWorklistFilter>()), Times.Once);
    }
}
