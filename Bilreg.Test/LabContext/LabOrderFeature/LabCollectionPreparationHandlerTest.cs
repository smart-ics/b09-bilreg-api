using Bilreg.Application.LabContext.LabOrderFeature;
using Bilreg.Application.LabContext.LabOrderFeature.UseCases;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.LabContext.LabOrderFeature;

public class LabCollectionPreparationHandlerTest
{
    private readonly Mock<ILabCollectionPreparationDal> _dal = new();
    private readonly LabCollectionPreparationHandler _sut;

    public LabCollectionPreparationHandlerTest()
    {
        _sut = new LabCollectionPreparationHandler(_dal.Object);
    }

    [Fact]
    public async Task Handle_WhenFound_ReturnsView()
    {
        var expected = new LabCollectionPreparationView(
            "LBO1",
            "LAB1",
            "MR1",
            "N",
            3,
            [],
            []);
        _dal.Setup(d => d.Get("LBO1")).Returns(expected);

        var result = await _sut.Handle(new LabCollectionPreparationQuery("LBO1"), CancellationToken.None);

        result.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task Handle_WhenMissing_Throws()
    {
        _dal.Setup(d => d.Get("X")).Returns((LabCollectionPreparationView?)null);

        var act = async () => await _sut.Handle(new LabCollectionPreparationQuery("X"), CancellationToken.None);

        await act.Should().ThrowAsync<Exception>().WithMessage("*not found*");
    }
}
