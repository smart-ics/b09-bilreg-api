using System.Security.Claims;
using Bilreg.Api.Controllers.AdmisiContext.AntrianFeature;
using Bilreg.Application.AdmisiContext.AntrianFeature.UseCases;
using Bilreg.Application.Shared;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Nuna.Lib.ActionResultHelper;
using Xunit;

namespace Bilreg.Test.AdmisiContext.AntrianFeature;

public class AdmissionQueueConfigurationControllerPolisTest
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<ICurrentUserContext> _currentUserMock = new();
    private readonly AdmissionQueueConfigurationController _sut;

    public AdmissionQueueConfigurationControllerPolisTest()
    {
        _currentUserMock.Setup(u => u.GetActorUserId()).Returns("CONFIG-ADMIN");
        _sut = new AdmissionQueueConfigurationController(_mediatorMock.Object, _currentUserMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    [Fact]
    public async Task ReplacePolis_WithObjectList_DispatchesReplaceDisplayPolisCmdAndReturnsJSendOk()
    {
        var expectedResponse = new AdmissionQueueDisplayDtoResponse(
            "DISP-CLI", "Clinic Display", "Lt 2", true, true, 5000, "clinic", "",
            Array.Empty<AdmissionDisplayLoketDtoResponse>(),
            "2026-10-28T00:00:00Z", "user1", "2026-10-28T00:00:00Z", "CONFIG-ADMIN", "2",
            new[] { new AdmissionDisplayPoliDtoResponse("POLI-A", 1), new AdmissionDisplayPoliDtoResponse("POLI-B", 2) });

        _mediatorMock.Setup(m => m.Send(It.IsAny<ReplaceDisplayPolisCmd>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var body = new AdmissionQueueConfigurationController.ReplacePolisBody(
            RowVersion: "1",
            Polis: new List<AdmissionQueueConfigurationController.PoliMapBody>
            {
                new("POLI-A", 1),
                new("POLI-B", 2)
            });

        var result = await _sut.ReplacePolis("DISP-CLI", body, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResponse);

        _mediatorMock.Verify(m => m.Send(
            It.Is<ReplaceDisplayPolisCmd>(c =>
                c.DisplayId == "DISP-CLI" &&
                c.RowVersion == "1" &&
                c.UserId == "CONFIG-ADMIN" &&
                c.Polis.Count == 2 &&
                c.Polis[0].PoliId == "POLI-A" &&
                c.Polis[1].PoliId == "POLI-B"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReplacePolis_WithStringList_DispatchesReplaceDisplayPolisCmdWithAutoSortOrder()
    {
        var expectedResponse = new AdmissionQueueDisplayDtoResponse(
            "DISP-CLI", "Clinic Display", "Lt 2", true, true, 5000, "clinic", "",
            Array.Empty<AdmissionDisplayLoketDtoResponse>(),
            "2026-10-28T00:00:00Z", "user1", "2026-10-28T00:00:00Z", "CONFIG-ADMIN", "2",
            new[] { new AdmissionDisplayPoliDtoResponse("POLI-X", 1), new AdmissionDisplayPoliDtoResponse("POLI-Y", 2) });

        _mediatorMock.Setup(m => m.Send(It.IsAny<ReplaceDisplayPolisCmd>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var body = new AdmissionQueueConfigurationController.ReplacePolisBody(
            RowVersion: "1",
            PoliIds: new List<string> { "POLI-X", "POLI-Y" });

        var result = await _sut.ReplacePolis("DISP-CLI", body, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();

        _mediatorMock.Verify(m => m.Send(
            It.Is<ReplaceDisplayPolisCmd>(c =>
                c.DisplayId == "DISP-CLI" &&
                c.Polis.Count == 2 &&
                c.Polis[0].PoliId == "POLI-X" && c.Polis[0].SortOrder == 1 &&
                c.Polis[1].PoliId == "POLI-Y" && c.Polis[1].SortOrder == 2),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
