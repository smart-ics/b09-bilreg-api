using Bilreg.Application.AdmisiRanapContext.OperationalWorklistFeature;
using Bilreg.Application.AdmisiRanapContext.OperationalWorklistFeature.UseCases;
using FluentAssertions;
using Moq;

namespace Bilreg.Test.AdmisiRanapContext.OperationalWorklistFeature;

public class AdmListOperationalWorklistHandlerTest
{
    private readonly Mock<IOperationalWorklistDal> _worklistDalMock = new();

    [Fact]
    public async Task UT01_GivenDalReturnsItems_WhenList_ThenReturnsMappedResponse()
    {
        var crtDate = new DateTime(2026, 7, 1, 10, 0, 0);
        var sortDate = new DateTime(2026, 7, 2, 8, 0, 0);
        var items = new List<OperationalWorklistItemView>
        {
            new(
                "OP001",
                "opname",
                0,
                "MR001",
                "Andi",
                "L",
                "D001",
                "Dr. Budi",
                "",
                "",
                "",
                "",
                "",
                "",
                sortDate,
                crtDate,
                null),
        };

        _worklistDalMock
            .Setup(x => x.List(It.IsAny<OperationalWorklistFilter>()))
            .Returns(items);

        var handler = new AdmListOperationalWorklistHandler(_worklistDalMock.Object);

        var response = await handler.Handle(
            new AdmListOperationalWorklistQry(SearchTerm: "andi"),
            CancellationToken.None);

        response.Items.Should().HaveCount(1);
        response.Items[0].ItemId.Should().Be("OP001");
        response.Items[0].Jenis.Should().Be("opname");

        _worklistDalMock.Verify(
            x => x.List(It.Is<OperationalWorklistFilter>(f => f.SearchTerm == "andi")),
            Times.Once);
    }

    [Fact]
    public async Task UT02_GivenIncludeTerminal_WhenList_ThenPassesFlagToDal()
    {
        _worklistDalMock
            .Setup(x => x.List(It.IsAny<OperationalWorklistFilter>()))
            .Returns([]);

        var handler = new AdmListOperationalWorklistHandler(_worklistDalMock.Object);

        await handler.Handle(
            new AdmListOperationalWorklistQry(IncludeTerminal: true),
            CancellationToken.None);

        _worklistDalMock.Verify(
            x => x.List(It.Is<OperationalWorklistFilter>(f => f.IncludeTerminal)),
            Times.Once);
    }
}
